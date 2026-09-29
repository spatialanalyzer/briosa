using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetWorkingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_working_frame", "Set Working Frame", "briosa.UtilityOperations",
        "SetWorkingFrame", "/briosa.UtilityOperations/SetWorkingFrame", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetWorkingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("New Working Frame Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewWorkingFrameName, "new_working_frame_name"),
                "SetCollectionObjectNameArg2")], []);
    }

    public static Api.SetWorkingFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
