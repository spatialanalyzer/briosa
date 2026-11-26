using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Briosa.Server.Operations;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http.Features;

namespace Briosa.Server.Services;

/// <summary>
/// Records the arrival and end of every unary call, so a log shows whether a call
/// reached the server, whether its handler returned, and whether its response ended
/// with a gRPC status (#450). Discovery and state reads have no other record, and a
/// client can call them often, so every record is Debug. The interceptor joins the
/// gRPC pipeline only when that level is enabled for its category as gRPC builds its
/// endpoints, at startup; otherwise gRPC builds the same call path as without it.
/// </summary>
/// <remarks>
/// A call is named from the endpoint that gRPC routed it to: by its operation ID when it
/// is an MP operation, otherwise by the RPC name in the server's own protocol
/// descriptors, and never from the request path, request, response, status message or
/// metadata. The records are best effort, like the lifecycle timing records: a failing
/// clock or logger loses the record and nothing else.
/// </remarks>
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Diagnostics must not change the outcome of the call they describe.")]
internal sealed partial class UnaryCallAuditInterceptor(
    ILogger<UnaryCallAuditInterceptor> logger,
    TimeProvider? timeProvider = null) : Interceptor
{
    /// <summary>The name recorded for a call whose routed method is not a known RPC.</summary>
    internal const string UnrecognizedRpc = "unrecognized";

    private const string GrpcStatusHeader = "grpc-status";

    private static readonly FrozenDictionary<string, string> OperationIds =
        SpatialAnalyzerApi.Operations.ToFrozenDictionary(
            operation => operation.FullyQualifiedMethod,
            operation => operation.OperationId,
            StringComparer.Ordinal);

    // The server's unary RPCs that are not MP operations, by full method, with the
    // name from their descriptors: discovery, the SDK and application lifecycle, and
    // the health check. A test fails when a unary RPC elsewhere is not an operation.
    private static readonly FrozenDictionary<string, string> RpcNames = new[]
        {
            global::Briosa.DiscoveryReflection.Descriptor,
            global::Briosa.LifecycleReflection.Descriptor,
            Grpc.Health.V1.HealthReflection.Descriptor
        }
        .SelectMany(file => file.Services)
        .SelectMany(service => service.Methods)
        .Where(method => !method.IsClientStreaming && !method.IsServerStreaming)
        .Select(method => (FullName: $"/{method.Service.FullName}/{method.Name}", method.Name))
        .Where(method => !OperationIds.ContainsKey(method.FullName))
        .DistinctBy(method => method.FullName, StringComparer.Ordinal)
        .ToFrozenDictionary(method => method.FullName, method => method.Name, StringComparer.Ordinal);

    private readonly ILogger<UnaryCallAuditInterceptor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Adds the interceptor to the gRPC options only when its Debug records are enabled.
    /// gRPC reads the options once, as it builds its endpoints, so a level set later
    /// takes effect at the next start.
    /// </summary>
    internal static void AddWhenEnabled(GrpcServiceOptions options, ILoggerFactory? loggers)
    {
        ArgumentNullException.ThrowIfNull(options);
        bool enabled;
        try
        {
            enabled = loggers?.CreateLogger<UnaryCallAuditInterceptor>().IsEnabled(LogLevel.Debug) == true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            enabled = false;
        }

        if (enabled)
        {
            options.Interceptors.Add<UnaryCallAuditInterceptor>();
        }
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation) =>
        ObserveAsync(request, context, continuation);

    /// <summary>
    /// The operation ID of an MP operation, or the descriptor name of any other known
    /// unary RPC, from its canonical full method name; exactly one of the two is set.
    /// </summary>
    internal static (string? Rpc, string? OperationId) Identify(string? canonicalMethod)
    {
        if (canonicalMethod is null)
        {
            return (UnrecognizedRpc, null);
        }

        if (OperationIds.TryGetValue(canonicalMethod, out var operationId))
        {
            return (null, operationId);
        }

        return (RpcNames.TryGetValue(canonicalMethod, out var name) ? name : UnrecognizedRpc, null);
    }

    // The status the server committed to the response: in the trailers, or in the
    // headers of a trailers-only response. Null when the response ended without one,
    // for example when the client reset the stream.
    internal static StatusCode? CommittedStatus(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            return null;
        }

        var value = httpContext.Features.Get<IHttpResponseTrailersFeature>()?.Trailers[GrpcStatusHeader].ToString();
        if (string.IsNullOrEmpty(value))
        {
            value = httpContext.Response.Headers[GrpcStatusHeader].ToString();
        }

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var code) &&
            Enum.IsDefined((StatusCode)code)
                ? (StatusCode)code
                : null;
    }

    private async Task<TResponse> ObserveAsync<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        HttpContext? httpContext = null;
        string? method = null;
        BestEffortDiagnostics.Record(() =>
        {
            httpContext = context.GetHttpContext();
            method = httpContext.GetEndpoint()?.Metadata.GetMetadata<GrpcMethodMetadata>()?.Method.FullName;
        });
        var (rpc, operationId) = Identify(method);
        var started = BestEffortDiagnostics.Timestamp(_timeProvider);
        BestEffortDiagnostics.Record(() => LogStarted(rpc, operationId));
        // After the framework has written the status and trailers, or abandoned the
        // response. It shows what the server committed, not that the client received
        // it. A call whose handler returned but whose response never ended stalled in
        // the server's transport, not in its handler.
        if (httpContext is { } routed)
        {
            BestEffortDiagnostics.Record(() => routed.Response.OnCompleted(() =>
            {
                var elapsed = BestEffortDiagnostics.ElapsedMilliseconds(_timeProvider, started);
                BestEffortDiagnostics.Record(() =>
                {
                    if (CommittedStatus(routed) is { } committed)
                    {
                        LogResponseEnded(rpc, operationId, committed, elapsed);
                    }
                    else
                    {
                        LogResponseAbandoned(rpc, operationId, elapsed);
                    }
                });
                return Task.CompletedTask;
            }));
        }

        var recorded = false;
        try
        {
            var response = await continuation(request, context).ConfigureAwait(false);
            recorded = true;
            // A handler can end the call with a status of its own and still return.
            var assigned = StatusCode.OK;
            BestEffortDiagnostics.Record(() => assigned = context.Status.StatusCode);
            Completed(rpc, operationId, assigned, started);
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

    // The handler returned, with the status it assigned, or ended the call with a status.
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

    // The response ended without a committed status: the stream was reset, the
    // connection was lost, or the status was not written.
    [LoggerMessage(EventId = 1604, Level = LogLevel.Debug,
        Message = "Unary call {Rpc}{OperationId} response ended without a gRPC status after {ElapsedMilliseconds} ms.")]
    private partial void LogResponseAbandoned(string? rpc, string? operationId, long elapsedMilliseconds);
}
