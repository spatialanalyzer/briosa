using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class FilterGeometryRelationshipOutlierCloudPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.filter_geometry_relationship_outlier_cloud_points",
        "Filter Geometry Relationship Outlier Cloud Points",
        "briosa.RelationshipOperations", "FilterGeometryRelationshipOutlierCloudPoints",
        "/briosa.RelationshipOperations/FilterGeometryRelationshipOutlierCloudPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("metrics", "First Pass RMS Error", WorkerMpValueKind.FloatingPoint),
        new("metrics", "First Pass Maximum Error", WorkerMpValueKind.FloatingPoint),
        new("metrics", "First Pass Minimum Error", WorkerMpValueKind.FloatingPoint),
        new("metrics", "First Pass Average Error", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Final Pass RMS Error", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Final Pass Maximum Error", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Final Pass Minimum Error", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Final Pass Average Error", WorkerMpValueKind.FloatingPoint),
        new("metrics", "Total Input Point Count", WorkerMpValueKind.WholeNumber),
        new("metrics", "Exclude Point Count", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.FilterGeometryRelationshipOutlierCloudPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.RelationshipName, "relationship_name", WorkerItemTypeValue.Relationship), "SetCollectionObjectNameArg2"),
                new("Sigma Threshold", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasSigmaThreshold ? request.SigmaThreshold : 3d), "SetDoubleArg"),
                new("Modify Existing Input Clouds", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasModifyExistingInputClouds && request.ModifyExistingInputClouds), "SetBoolArg")
            ],
            [
                new("First Pass RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("First Pass Maximum Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("First Pass Minimum Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("First Pass Average Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Final Pass RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Final Pass Maximum Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Final Pass Minimum Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Final Pass Average Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Total Input Point Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
                new("Exclude Point Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
            ]);
    }

    public static Api.FilterGeometryRelationshipOutlierCloudPointsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            Metrics = new()
            {
                FirstPassRmsError = outputs[0].RequireValue<WorkerDoubleValue>().Value,
                FirstPassMaximumError = outputs[1].RequireValue<WorkerDoubleValue>().Value,
                FirstPassMinimumError = outputs[2].RequireValue<WorkerDoubleValue>().Value,
                FirstPassAverageError = outputs[3].RequireValue<WorkerDoubleValue>().Value,
                FinalPassRmsError = outputs[4].RequireValue<WorkerDoubleValue>().Value,
                FinalPassMaximumError = outputs[5].RequireValue<WorkerDoubleValue>().Value,
                FinalPassMinimumError = outputs[6].RequireValue<WorkerDoubleValue>().Value,
                FinalPassAverageError = outputs[7].RequireValue<WorkerDoubleValue>().Value,
                TotalInputPointCount = outputs[8].RequireValue<WorkerIntegerValue>().Value,
                ExcludePointCount = outputs[9].RequireValue<WorkerIntegerValue>().Value
            },
            Execution = completed.Details
        };
    }
}
