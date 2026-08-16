using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Server.Operations.WaveA;

internal sealed record MpArgumentContract(
    string FieldName,
    string MpName,
    WorkerMpValueKind Kind,
    string SdkBinding,
    string DefaultValue,
    bool Required,
    WorkerObjectTypeValue? ObjectTypeWhenOmitted = null);

internal sealed class MpOperationContract
{
    public MpOperationContract(
        string operationId,
        string stepName,
        string grpcService,
        string rpc,
        string effect,
        global::Briosa.OperationExecutionScope executionScope,
        global::Briosa.ReplaySafety replaySafety,
        IReadOnlyList<string> riskFlags,
        IReadOnlyList<MpArgumentContract> inputs,
        IReadOnlyList<MpArgumentContract> outputs)
    {
        Inputs = [.. inputs];
        Outputs = [.. outputs];
        Descriptor = new OperationDescriptor(
            operationId,
            stepName,
            grpcService,
            rpc,
            $"/{grpcService}/{rpc}",
            effect,
            executionScope,
            replaySafety,
            [.. riskFlags]);
        OutputContracts =
        [
            .. Outputs.Select(output =>
                new OperationOutputContract(output.FieldName, output.MpName, output.Kind))
        ];
    }

    public OperationDescriptor Descriptor { get; }

    public IReadOnlyList<MpArgumentContract> Inputs { get; }

    public IReadOnlyList<MpArgumentContract> Outputs { get; }

    public IReadOnlyList<OperationOutputContract> OutputContracts { get; }

    public WorkerMpCommand CreateCommand(IMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new WorkerMpCommand(
            Descriptor.OperationId,
            Descriptor.MpStep,
            [.. Inputs.Select(input => MpOperationValueMapper.ToInput(request, input))],
            [
                .. Outputs.Select(output => new WorkerMpOutputArgument(
                    output.MpName,
                    output.Kind,
                    output.SdkBinding,
                    output.ObjectTypeWhenOmitted))
            ]);
    }

    public TResponse CreateResult<TResponse>(
        SuccessfulOperationExecution completed)
        where TResponse : class, IMessage<TResponse>, new() =>
        MpOperationValueMapper.ToResult<TResponse>(completed, Outputs);
}

internal static class MpOperationServiceExecutor
{
    public static Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        OperationExecutor executor,
        TRequest request,
        ServerCallContext context,
        string operationId)
        where TRequest : class, IMessage<TRequest>
        where TResponse : class, IMessage<TResponse>, new()
    {
        ArgumentNullException.ThrowIfNull(executor);
        var operation = WaveAOperationCatalog.Get(operationId);
        return executor.ExecuteAsync(
            request,
            context,
            operation.Descriptor,
            operation.CreateCommand,
            operation.OutputContracts,
            operation.CreateResult<TResponse>);
    }
}
