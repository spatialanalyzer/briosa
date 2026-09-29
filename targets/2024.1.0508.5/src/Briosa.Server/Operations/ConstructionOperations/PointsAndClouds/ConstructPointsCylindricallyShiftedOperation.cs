using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsCylindricallyShiftedOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_cylindrically_shifted", "Construct Points Cylindrically Shifted",
        "briosa.ConstructionOperations", "ConstructPointsCylindricallyShifted", "/briosa.ConstructionOperations/ConstructPointsCylindricallyShifted",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsCylindricallyShiftedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceObjectName, "reference_object_name", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2"),
            new("Original Points", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.OriginalPoints, "original_points"), "SetPointNameRefListArg"),
            new("Group for New Points", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupForNewPoints, "group_for_new_points", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Radial Shift", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasRadialShift ? request.RadialShift : 0), "SetDoubleArg"),
            new("Theta Shift (degrees)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasThetaShift ? request.ThetaShift : 0), "SetDoubleArg"),
            new("Planar Shift", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPlanarShift ? request.PlanarShift : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructPointsCylindricallyShiftedResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
