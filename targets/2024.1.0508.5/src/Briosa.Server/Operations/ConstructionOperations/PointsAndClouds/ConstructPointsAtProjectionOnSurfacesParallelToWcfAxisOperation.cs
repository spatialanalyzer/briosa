using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_at_projection_on_surfaces_parallel_to_wcf_axis",
        "Construct Points at Projection on Surfaces - Parallel to WCF Axis", "briosa.ConstructionOperations",
        "ConstructPointsAtProjectionOnSurfacesParallelToWcfAxis",
        "/briosa.ConstructionOperations/ConstructPointsAtProjectionOnSurfacesParallelToWcfAxis",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var axis = request.Axis switch
        {
            Api.WcfAxis.X => WorkerAxisIdentifierValue.PositiveX,
            Api.WcfAxis.Y => WorkerAxisIdentifierValue.PositiveY,
            Api.WcfAxis.Z => WorkerAxisIdentifierValue.PositiveZ,
            _ => throw new ArgumentException("A supported axis is required.", nameof(request))
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Surface List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SurfaceList, "surface_list"), "SetCollectionObjectNameRefListArg"),
            new("Point Names", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
            new("Group Name to Contain New Points", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasGroupNameToContainNewPoints ? request.GroupNameToContainNewPoints : ""), "SetStringArg"),
            new("Point Name Prefix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasPointNamePrefix ? request.PointNamePrefix : ""), "SetStringArg"),
            new("Point Name Suffix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasPointNameSuffix ? request.PointNameSuffix : ""), "SetStringArg"),
            new("Axis", WorkerMpValueKind.AxisIdentifier,
                new WorkerChoiceValue<WorkerAxisIdentifierValue>(axis), "SetAxisNameArg")
        ], []);
    }

    public static Api.ConstructPointsAtProjectionOnSurfacesParallelToWcfAxisResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
