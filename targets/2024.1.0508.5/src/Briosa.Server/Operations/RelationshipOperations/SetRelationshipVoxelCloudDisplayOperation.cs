using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipVoxelCloudDisplayOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_voxel_cloud_display", "Set Relationship Voxel Cloud Display",
        "briosa.RelationshipOperations", "SetRelationshipVoxelCloudDisplay",
        "/briosa.RelationshipOperations/SetRelationshipVoxelCloudDisplay",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipVoxelCloudDisplayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mode = request.HasSurfaceAnalysisMode ? request.SurfaceAnalysisMode : Api.SurfaceAnalysisMode.Relationship;
        if (mode == Api.SurfaceAnalysisMode.Unspecified || !Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(request), "Surface analysis mode is not supported.");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Enable Voxel Cloud Display?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasEnableVoxelCloudDisplay ? request.EnableVoxelCloudDisplay : true), "SetBoolArg"),
                new("Voxel Size (-1.0 autodetect)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasVoxelSize ? request.VoxelSize : -1d), "SetDoubleArg"),
                new("Min Pts Count Per Voxel", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasMinPtsCountPerVoxel ? request.MinPtsCountPerVoxel : 3), "SetIntegerArg"),
                new("Voxel Rendering Diameter % (-1.0 fast)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasVoxelRenderingDiameter ? request.VoxelRenderingDiameter : 125d), "SetDoubleArg"),
                new("Surface Analysis Mode", WorkerMpValueKind.SurfaceAnalysisMode,
                    WorkerChoiceFactory.FromOrdinal(WorkerMpValueKind.SurfaceAnalysisMode, (int)mode - 1), "SetSurfaceAnalysisModeArg"),
                new("Colorization Options", WorkerMpValueKind.ColorizationOptions,
                    ColorizationOptionsMapper.WithDefaults(request.ColorizationOptions), "SetColorizationOptionsArg"),
                new("Show Color Bar in View?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowColorBarInView && request.ShowColorBarInView), "SetBoolArg")
            ], []);
    }

    public static Api.SetRelationshipVoxelCloudDisplayResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
