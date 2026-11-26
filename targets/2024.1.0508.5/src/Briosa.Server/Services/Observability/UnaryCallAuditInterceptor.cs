using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Briosa.Server.Operations;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Briosa.Server.Services;

/// <summary>
/// Records the arrival and end of every unary call, so a log shows whether a call
/// reached the server, whether its handler returned, and whether its response was
/// finished (#450). Discovery and state reads have no other record, and a client
/// can call them often, so every record is Debug and off by default. With Debug off
/// the interceptor makes one level check and passes the call through unchanged.
/// </summary>
/// <remarks>
/// A call is named by its operation ID when it is an MP operation and otherwise by
/// its RPC name, never by its request, response, status message or metadata. The
/// records are best effort, like the lifecycle timing records: a failing clock or
/// logger loses the record and nothing else.
/// </remarks>
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Diagnostics must not change the outcome of the call they describe.")]
internal sealed partial class UnaryCallAuditInterceptor(
    ILogger<UnaryCallAuditInterceptor> logger,
    TimeProvider? timeProvider = null) : Interceptor
{
    /// <summary>The name recorded for a method whose name is not a plain identifier.</summary>
    internal const string UnrecognizedRpc = "unrecognized";

    private static readonly FrozenDictionary<string, string> OperationIds =
        SpatialAnalyzerApi.Operations.ToFrozenDictionary(
            operation => operation.FullyQualifiedMethod,
            operation => operation.OperationId,
            StringComparer.Ordinal);

    private readonly ILogger<UnaryCallAuditInterceptor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation) =>
        IsEnabled()
            ? ObserveAsync(request, context, continuation)
            : continuation(request, context);

    /// <summary>
    /// The operation ID of an MP operation, or the RPC name of any other method;
    /// exactly one of the two is set.
    /// </summary>
    internal static (string? Rpc, string? OperationId) Identify(string? method)
    {
        if (method is not null && OperationIds.TryGetValue(method, out var operationId))
        {
            return (null, operationId);
        }

        var name = method?[(method.LastIndexOf('/') + 1)..] ?? string.Empty;
        return (name.Length is > 0 and <= 128 && name.All(char.IsAsciiLetterOrDigit)
            ? name
            : UnrecognizedRpc, null);
    }

    private bool IsEnabled()
    {
        try
        {
            return _logger.IsEnabled(LogLevel.Debug);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return false;
        }
    }

    private async Task<TResponse> ObserveAsync<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        var (rpc, operationId) = Identify(context.Method);
        var started = BestEffortDiagnostics.Timestamp(_timeProvider);
        BestEffortDiagnostics.Record(() => LogStarted(rpc, operationId));
        // After the framework has written the status and trailers, or abandoned the
        // response. A call whose handler returned but whose response never ended
        // stalled in the server's transport, not in its handler.
        BestEffortDiagnostics.Record(() => context.GetHttpContext().Response.OnCompleted(() =>
        {
            var elapsed = BestEffortDiagnostics.ElapsedMilliseconds(_timeProvider, started);
            BestEffortDiagnostics.Record(() =>
            {
                var grpcStatus = context.Status.StatusCode;
                LogResponseEnded(rpc, operationId, grpcStatus, elapsed);
            });
            return Task.CompletedTask;
        }));
        var recorded = false;
        try
        {
            var response = await continuation(request, context).ConfigureAwait(false);
            recorded = true;
            Completed(rpc, operationId, StatusCode.OK, started);
            return response;
        }
        catch (RpcException exception)
        {
            recorded = true;
            Completed(rpc, operationId, exception.StatusCode, started);
            throw;
        }
        finally
        {
            if (!recorded)
            {
                var elapsed = BestEffortDiagnostics.ElapsedMilliseconds(_timeProvider, started);
                BestEffortDiagnostics.Record(() => LogFaulted(rpc, operationId, elapsed));
            }
        }
    }

    private void Completed(string? rpc, string? operationId, StatusCode grpcStatus, long? started)
    {
        var elapsed = BestEffortDiagnostics.ElapsedMilliseconds(_timeProvider, started);
        BestEffortDiagnostics.Record(() => LogCompleted(rpc, operationId, grpcStatus, elapsed));
    }

    [LoggerMessage(EventId = 1600, Level = LogLevel.Debug,
        Message = "Unary call {Rpc}{OperationId} started.")]
    private partial void LogStarted(string? rpc, string? operationId);

    // The handler returned a response, or ended the call with a gRPC status.
    [LoggerMessage(EventId = 1601, Level = LogLevel.Debug,
        Message = "Unary call {Rpc}{OperationId} returned gRPC status {GrpcStatus} after {ElapsedMilliseconds} ms.")]
    private partial void LogCompleted(string? rpc, string? operationId, StatusCode grpcStatus,
        long elapsedMilliseconds);

    // Neither a response nor a gRPC status. The exception is not logged.
    [LoggerMessage(EventId = 1602, Level = LogLevel.Debug,
        Message = "Unary call {Rpc}{OperationId} ended without a gRPC status after {ElapsedMilliseconds} ms.")]
    private partial void LogFaulted(string? rpc, string? operationId, long elapsedMilliseconds);

    [LoggerMessage(EventId = 1603, Level = LogLevel.Debug,
        Message = "Unary call {Rpc}{OperationId} response ended with gRPC status {GrpcStatus} after {ElapsedMilliseconds} ms.")]
    private partial void LogResponseEnded(string? rpc, string? operationId, StatusCode grpcStatus,
        long elapsedMilliseconds);
}
