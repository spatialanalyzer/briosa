using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class IncrementPointNameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.increment_point_name", "Increment Point Name", "briosa.UtilityOperations",
        "IncrementPointName", "/briosa.UtilityOperations/IncrementPointName", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_point_name", "Resultant Point Name", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.IncrementPointNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("'Base' Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.BasePointName, "base_point_name"), "SetPointNameArg"),
            new("Increment", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.Increment), "SetIntegerArg")
        ], [new("Resultant Point Name", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.IncrementPointNameResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ResultantPointName = PointNameMapper.ToProtocol(completed.Execution.OutputValues[0]
                .RequireValue<WorkerPointNameValue>()),
            Execution = completed.Details
        };
}
