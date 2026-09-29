using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsShiftedInWorkingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_shifted_in_working_frame", "Construct Points Shifted in Working Frame",
        "briosa.ConstructionOperations", "ConstructPointsShiftedInWorkingFrame", "/briosa.ConstructionOperations/ConstructPointsShiftedInWorkingFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsShiftedInWorkingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Original Points", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.OriginalPoints, "original_points"), "SetPointNameRefListArg"),
            new("Group for New Points", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupForNewPoints, "group_for_new_points", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Shift Vector", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.ShiftVector, "shift_vector"), "SetVectorArg")
        ], []);
    }

    public static Api.ConstructPointsShiftedInWorkingFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
