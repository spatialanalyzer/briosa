using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class ResetCloudBoundingBoxOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.reset_cloud_bounding_box", "Reset Cloud Bounding Box",
        "briosa.CloudAndMeshOperations", "ResetCloudBoundingBox", "/briosa.CloudAndMeshOperations/ResetCloudBoundingBox",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("x_axis_dimension", "X-Axis Dimension", WorkerMpValueKind.FloatingPoint),
        new("y_axis_dimension", "Y-Axis Dimension", WorkerMpValueKind.FloatingPoint),
        new("z_axis_dimension", "Z-Axis Dimension", WorkerMpValueKind.FloatingPoint),
        new("x_axis_in_world", "X-Axis (in WORLD)", WorkerMpValueKind.Vector),
        new("y_axis_in_world", "Y-Axis (in WORLD)", WorkerMpValueKind.Vector),
        new("z_axis_in_world", "Z-Axis (in WORLD)", WorkerMpValueKind.Vector),
        new("centroid_in_world", "Centroid (in WORLD)", WorkerMpValueKind.Vector),
        new("reference_transform_in_world", "Reference Transform (in WORLD)", WorkerMpValueKind.Transform),
        new("reference_transform_in_working", "Reference Transform (in WORKING)", WorkerMpValueKind.Transform),
        new("points_used_for_bounding_box", "Points Used for Bounding Box", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.ResetCloudBoundingBoxRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cloudBoxType = request.HasCloudBoxType
            ? request.CloudBoxType
            : Api.CloudBoxType.WorldAxisAlignedBox;
        var cloudBoxLiteral = cloudBoxType switch
        {
            Api.CloudBoxType.WorldAxisAlignedBox => "World Axis Aligned Box",
            Api.CloudBoxType.WorkAxisAlignedBox => "Work Axis Aligned Box",
            Api.CloudBoxType.MinimumOrientedBoxUnconditional => "Minimum Oriented Box - Unconditional",
            Api.CloudBoxType.MinimumOrientedBoxVerifyVolume => "Minimum Oriented Box - Verify Volume",
            _ => throw new ArgumentException("Unsupported cloud box type.", nameof(request))
        };

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CloudName, "cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Cloud Box Type", WorkerMpValueKind.Text, new WorkerTextValue(cloudBoxLiteral), "SetStringArg"),
            new("Show Bounding Box?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasShowBoundingBox || request.ShowBoundingBox), "SetBoolArg"),
            new("Use All Points?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUseAllPoints && request.UseAllPoints), "SetBoolArg"),
            new("Desired Point Count", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasDesiredPointCount ? request.DesiredPointCount : 1000), "SetIntegerArg")
        ],
        [
            new("X-Axis Dimension", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Y-Axis Dimension", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Z-Axis Dimension", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("X-Axis (in WORLD)", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Y-Axis (in WORLD)", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Z-Axis (in WORLD)", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Centroid (in WORLD)", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Reference Transform (in WORLD)", WorkerMpValueKind.Transform, "GetTransformArg"),
            new("Reference Transform (in WORKING)", WorkerMpValueKind.Transform, "GetTransformArg"),
            new("Points Used for Bounding Box", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
        ]);
    }

    public static Api.ResetCloudBoundingBoxResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        XAxisDimension = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        YAxisDimension = completed.Execution.OutputValues[1].RequireValue<WorkerDoubleValue>().Value,
        ZAxisDimension = completed.Execution.OutputValues[2].RequireValue<WorkerDoubleValue>().Value,
        XAxisInWorld = VectorMapper.ToProtocol(completed.Execution.OutputValues[3].RequireValue<WorkerVectorValue>()),
        YAxisInWorld = VectorMapper.ToProtocol(completed.Execution.OutputValues[4].RequireValue<WorkerVectorValue>()),
        ZAxisInWorld = VectorMapper.ToProtocol(completed.Execution.OutputValues[5].RequireValue<WorkerVectorValue>()),
        CentroidInWorld = VectorMapper.ToProtocol(completed.Execution.OutputValues[6].RequireValue<WorkerVectorValue>()),
        ReferenceTransformInWorld = TransformMapper.ToProtocol(completed.Execution.OutputValues[7].RequireValue<WorkerTransformValue>()),
        ReferenceTransformInWorking = TransformMapper.ToProtocol(completed.Execution.OutputValues[8].RequireValue<WorkerTransformValue>()),
        PointsUsedForBoundingBox = completed.Execution.OutputValues[9].RequireValue<WorkerIntegerValue>().Value
    };
}
