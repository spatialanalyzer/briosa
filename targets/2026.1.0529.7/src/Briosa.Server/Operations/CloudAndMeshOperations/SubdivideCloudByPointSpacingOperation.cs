using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class SubdivideCloudByPointSpacingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.subdivide_cloud_by_point_spacing", "Subdivide Cloud by Point Spacing",
        "briosa.CloudAndMeshOperations", "SubdivideCloudByPointSpacing", "/briosa.CloudAndMeshOperations/SubdivideCloudByPointSpacing",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SubdivideCloudByPointSpacingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SourceCloudName, "source_cloud_name", WorkerObjectTypeValue.EnhancedCloud), "SetCollectionObjectNameArg2"),
            new("Point Spacing", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasPointSpacing ? request.PointSpacing : 0), "SetDoubleArg"),
            new("Minimum Points Per Group", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasMinimumPointsPerGroup ? request.MinimumPointsPerGroup : 0), "SetIntegerArg"),
            new("New Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewCloudName, "new_cloud_name", WorkerObjectTypeValue.EnhancedCloud), "SetCollectionObjectNameArg2"),
            new("Keep All Groups?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasKeepAllGroups || request.KeepAllGroups), "SetBoolArg")
        ], []);
    }

    public static Api.SubdivideCloudByPointSpacingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
