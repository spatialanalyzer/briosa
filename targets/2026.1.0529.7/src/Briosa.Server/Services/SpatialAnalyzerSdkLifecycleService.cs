using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Server.Services;

internal sealed class SpatialAnalyzerSdkLifecycleService(
    SpatialAnalyzerSdkLifecycleCoordinator coordinator)
    : global::Briosa.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleBase
{
    private const string ErrorMetadataKey =
        "briosa-spatial-analyzer-sdk-lifecycle-error-bin";

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
            () => coordinator.StartAsync(context.CancellationToken),
            state => new global::Briosa.StartSpatialAnalyzerSdkResponse { State = state });

    public override Task<global::Briosa.ConnectToSpatialAnalyzerResponse>
        ConnectToSpatialAnalyzer(
            global::Briosa.ConnectToSpatialAnalyzerRequest request,
            ServerCallContext context) =>
        Execute(
            nameof(ConnectToSpatialAnalyzer),
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
            () => coordinator.RecoverAsync(
                request.ExpectedSdkGeneration,
                request.Mode,
                context.CancellationToken),
            state => new global::Briosa.RecoverSpatialAnalyzerSdkResponse { State = state });

    private static async Task<TResponse> Execute<TResponse>(
        string rpc,
        Func<Task<global::Briosa.SpatialAnalyzerSdkLifecycleState>> action,
        Func<global::Briosa.SpatialAnalyzerSdkLifecycleState, TResponse> responseFactory)
    {
        try
        {
            return responseFactory(await action().ConfigureAwait(false));
        }
        catch (SdkLifecycleException exception)
        {
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
