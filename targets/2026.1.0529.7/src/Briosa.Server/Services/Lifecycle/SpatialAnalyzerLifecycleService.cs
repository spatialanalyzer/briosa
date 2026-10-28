using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Server.Services;

internal sealed class SpatialAnalyzerLifecycleService(
    SpatialAnalyzerLifecycleCoordinator coordinator,
    LifecycleAuditLogger? auditLogger = null,
    TimeProvider? timeProvider = null)
    : global::Briosa.SpatialAnalyzerLifecycle.SpatialAnalyzerLifecycleBase
{
    private const string ErrorMetadataKey =
        "briosa-spatial-analyzer-lifecycle-error-bin";

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public override async Task<global::Briosa.GetSpatialAnalyzerStateResponse>
        GetSpatialAnalyzerState(
            global::Briosa.GetSpatialAnalyzerStateRequest request,
            ServerCallContext context) =>
        new()
        {
            State = await coordinator.GetCurrentAsync(context.CancellationToken)
                .ConfigureAwait(false)
        };

    public override Task<global::Briosa.LaunchSpatialAnalyzerResponse>
        LaunchSpatialAnalyzer(
            global::Briosa.LaunchSpatialAnalyzerRequest request,
            ServerCallContext context) =>
        Execute(
            nameof(LaunchSpatialAnalyzer),
            context,
            () => coordinator.LaunchAsync(request, context.CancellationToken),
            state => new global::Briosa.LaunchSpatialAnalyzerResponse { State = state });

    public override Task<global::Briosa.CloseOwnedSpatialAnalyzerResponse>
        CloseOwnedSpatialAnalyzer(
            global::Briosa.CloseOwnedSpatialAnalyzerRequest request,
            ServerCallContext context) =>
        Execute(
            nameof(CloseOwnedSpatialAnalyzer),
            context,
            () => coordinator.CloseOwnedAsync(
                request.ExpectedApplicationGeneration,
                context.CancellationToken),
            state => new global::Briosa.CloseOwnedSpatialAnalyzerResponse { State = state });

    private async Task<TResponse> Execute<TResponse>(
        string rpc,
        ServerCallContext context,
        Func<Task<global::Briosa.SpatialAnalyzerLifecycleState>> action,
        Func<global::Briosa.SpatialAnalyzerLifecycleState, TResponse> responseFactory)
    {
        try
        {
            return responseFactory(await action().ConfigureAwait(false));
        }
        catch (SpatialAnalyzerLifecycleException caught)
        {
            // The coordinator only sees a cancelled token; the call's deadline tells a
            // caller that cancelled from one whose deadline passed.
            var exception = CallerDeadline.HasPassed(context.Deadline, _timeProvider)
                ? caught.AsCallerDeadlineExceeded()
                : caught;
            auditLogger?.Rejected(rpc, exception.StatusCode, exception.Detail.DiagnosticCode);
            exception.Detail.Rpc = rpc;
            var metadata = new Metadata
            {
                { ErrorMetadataKey, exception.Detail.ToByteArray() }
            };
            throw new RpcException(
                new Status(exception.StatusCode, exception.Detail.DiagnosticCode),
                metadata);
        }
    }
}
