using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsOnSurfacesByClickingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_on_surfaces_by_clicking", "Construct Points on Surface(s) by Clicking",
        "briosa.ConstructionOperations", "ConstructPointsOnSurfacesByClicking",
        "/briosa.ConstructionOperations/ConstructPointsOnSurfacesByClicking",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsOnSurfacesByClickingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Group Name for Points", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupNameForPoints, "group_name_for_points", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("First Point Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasFirstPointName ? request.FirstPointName : "p0"), "SetStringArg")
        ], []);
    }

    public static Api.ConstructPointsOnSurfacesByClickingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
