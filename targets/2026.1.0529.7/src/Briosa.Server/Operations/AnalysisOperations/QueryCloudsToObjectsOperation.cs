using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class QueryCloudsToObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.query_clouds_to_objects", "Query Clouds to Objects",
        "briosa.AnalysisOperations", "QueryCloudsToObjects", "/briosa.AnalysisOperations/QueryCloudsToObjects",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint),
        new("maximum_absolute_deviation", "Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.QueryCloudsToObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names"), "SetCollectionObjectNameRefListArg"),
                new("Object Names", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectNames, "object_names"), "SetCollectionObjectNameRefListArg"),
                new("Resulting Object Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ResultingObjectName, "resulting_object_name"), "SetCollectionObjectNameArg2"),
                new("Projection Options", WorkerMpValueKind.ProjectionOptions,
                    ProjectionOptionsMapper.FromRequest(request.ProjectionOptions), "SetProjectionOptionsArg"),
                new("Proximity", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasProximity ? request.Proximity : 0d), "SetDoubleArg"),
                new("Skip Factor", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasSkipFactor ? request.SkipFactor : 0), "SetIntegerArg"),
                new("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasRmsTolerance ? request.RmsTolerance : 0d), "SetDoubleArg"),
                new("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumAbsoluteTolerance ? request.MaximumAbsoluteTolerance : 0d), "SetDoubleArg")
            ],
            [
                new("RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.QueryCloudsToObjectsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        RmsDeviation = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        MaximumAbsoluteDeviation = completed.Execution.OutputValues[1].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
