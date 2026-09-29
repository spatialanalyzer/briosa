using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class GetTransformVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.get_transform_variable", "Get Transform Variable",
        "briosa.Variables", "GetTransformVariable", "/briosa.Variables/GetTransformVariable",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.GetTransformVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg")],
            [new("Value", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.GetTransformVariableResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        Value = TransformMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerTransformValue>())
    };
}
