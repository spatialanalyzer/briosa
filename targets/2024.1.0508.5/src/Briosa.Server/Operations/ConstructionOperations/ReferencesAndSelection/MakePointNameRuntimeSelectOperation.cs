using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakePointNameRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_point_name_runtime_select", "Make a Point Name - Runtime Select",
        "briosa.ConstructionOperations", "MakePointNameRuntimeSelect", "/briosa.ConstructionOperations/MakePointNameRuntimeSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_point_name", "Resultant Point Name", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.MakePointNameRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("User Prompt", WorkerMpValueKind.Text, new WorkerTextValue(request.UserPrompt), "SetStringArg")],
            [new("Resultant Point Name", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.MakePointNameRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ResultantPointName = PointNameMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerPointNameValue>()),
            Execution = completed.Details
        };
}
