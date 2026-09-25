using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetVectorVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_vector_variable", "Get Vector Variable",
        "briosa.Variables", "GetVectorVariable", "/briosa.Variables/GetVectorVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.Vector)];

    public static WorkerMpCommand CreateCommand(Api.GetVectorVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.Vector, "GetVectorArg")]);
    }

    public static Api.GetVectorVariableResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = VectorMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerVectorValue>())
    };
}
