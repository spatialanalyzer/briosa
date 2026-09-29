using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_at_intersection_of_principal_object_axes_and_surfaces",
        "Construct Points at Intersection of Principle Object Axes and Surfaces", "briosa.ConstructionOperations",
        "ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfaces",
        "/briosa.ConstructionOperations/ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfaces",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<WorkerMpInputArgument>
        {
            new("Axis Object List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.AxisObjectList, "axis_object_list"), "SetCollectionObjectNameRefListArg"),
            new("Surface List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SurfaceList, "surface_list"), "SetCollectionObjectNameRefListArg")
        };
        if (request.HasPointSuffix)
            arguments.Add(new("Point Suffix (optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.PointSuffix), "SetStringArg"));
        arguments.Add(new("Resultant Group Name", WorkerMpValueKind.CollectionObjectName,
            CollectionObjectNameMapper.Required(request.ResultantGroupName, "resultant_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"));
        return new(Descriptor.OperationId, Descriptor.MpStep, arguments, []);
    }

    public static Api.ConstructPointsAtIntersectionOfPrincipalObjectAxesAndSurfacesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
