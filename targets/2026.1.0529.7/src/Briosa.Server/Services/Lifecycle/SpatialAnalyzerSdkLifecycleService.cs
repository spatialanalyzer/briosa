using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Server.Services;

internal sealed class SpatialAnalyzerSdkLifecycleService(
    SpatialAnalyzerSdkLifecycleCoordinator coordinator,
    LifecycleAuditLogger? auditLogger = null,
    TimeProvider? timeProvider = null)
    : global::Briosa.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleBase
{
    private const string ErrorMetadataKey =
        "briosa-spatial-analyzer-sdk-lifecycle-error-bin";

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public override Task<global::Briosa.GetSpatialAnalyzerSdkStateResponse>
        GetSpatialAnalyzerSdkState(
            global::Briosa.GetSpatialAnalyzerSdkStateRequest request,
            ServerCallContext context) =>
        Task.FromResult(new global::Briosa.GetSpatialAnalyzerSdkStateResponse
        {
            State = coordinator.Current
        });

    public override Task<global::Briosa.StartSpatialAnalyzerSdkResponse>
        StartSpatialAnalyzerSdk(
            global::Briosa.StartSpatialAnalyzerSdkRequest request,
            ServerCallContext context) =>
        Execute(
            nameof(StartSpatialAnalyzerSdk),
            context,
            () => coordinator.StartAsync(context.CancellationToken),
            state => new global::Briosa.StartSpatialAnalyzerSdkResponse { State = state });

    public override Task<global::Briosa.ConnectToSpatialAnalyzerResponse>
        ConnectToSpatialAnalyzer(
            global::Briosa.ConnectToSpatialAnalyzerRequest request,
            ServerCallContext context) =>
        Execute(
            nameof(ConnectToSpatialAnalyzer),
            context,
            () => coordinator.ConnectAsync(
                request.ExpectedSdkGeneration,
                reconnect: false,
                context.CancellationToken),
            state => new global::Briosa.ConnectToSpatialAnalyzerResponse { State = state });

    public override Task<global::Briosa.ReconnectToSpatialAnalyzerResponse>
        ReconnectToSpatialAnalyzer(
            global::Briosa.ReconnectToSpatialAnalyzerRequest request,
            ServerCallContext context) =>
        Execute(
            nameof(ReconnectToSpatialAnalyzer),
            context,
            () => coordinator.ConnectAsync(
                request.ExpectedSdkGeneration,
                reconnect: true,
                context.CancellationToken),
            state => new global::Briosa.ReconnectToSpatialAnalyzerResponse { State = state });

    public override Task<global::Briosa.StopSpatialAnalyzerSdkResponse>
        StopSpatialAnalyzerSdk(
            global::Briosa.StopSpatialAnalyzerSdkRequest request,
            ServerCallContext context) =>
        Execute(
            nameof(StopSpatialAnalyzerSdk),
            context,
            () => coordinator.StopAsync(
                request.ExpectedSdkGeneration,
                context.CancellationToken),
            state => new global::Briosa.StopSpatialAnalyzerSdkResponse { State = state });

    public override Task<global::Briosa.RecoverSpatialAnalyzerSdkResponse>
        RecoverSpatialAnalyzerSdk(
            global::Briosa.RecoverSpatialAnalyzerSdkRequest request,
            ServerCallContext context) =>
        Execute(
            nameof(RecoverSpatialAnalyzerSdk),
            context,
            () => coordinator.RecoverAsync(
                request.ExpectedSdkGeneration,
                request.Mode,
                context.CancellationToken),
            state => new global::Briosa.RecoverSpatialAnalyzerSdkResponse { State = state });

    private async Task<TResponse> Execute<TResponse>(
        string rpc,
        ServerCallContext context,
        Func<Task<global::Briosa.SpatialAnalyzerSdkLifecycleState>> action,
        Func<global::Briosa.SpatialAnalyzerSdkLifecycleState, TResponse> responseFactory)
    {
        try
        {
            return responseFactory(await action().ConfigureAwait(false));
        }
        catch (SdkLifecycleException caught)
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
