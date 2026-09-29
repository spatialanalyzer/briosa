using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class QueryPointsToObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.query_points_to_objects", "Query Points to Objects",
        "briosa.AnalysisOperations", "QueryPointsToObjects", "/briosa.AnalysisOperations/QueryPointsToObjects",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint),
        new("max_absolute_deviation", "Max Absolute Deviation", WorkerMpValueKind.FloatingPoint),
        new("average_deviation", "Average Deviation", WorkerMpValueKind.FloatingPoint),
        new("standard_deviation", "Standard Deviation", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.QueryPointsToObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Names", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
                new("Object Name List (Objects to Project to)", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectNameList, "object_name_list"), "SetCollectionObjectNameRefListArg"),
                new("Resulting Object Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ResultingObjectName, "resulting_object_name"), "SetCollectionObjectNameArg2"),
                new("Projection Options", WorkerMpValueKind.ProjectionOptions,
                    ProjectionOptionsMapper.FromRequest(request.ProjectionOptions), "SetProjectionOptionsArg"),
                new("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasRmsTolerance ? request.RmsTolerance : 0d), "SetDoubleArg"),
                new("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumAbsoluteTolerance ? request.MaximumAbsoluteTolerance : 0d), "SetDoubleArg"),
                new("Show Results Dialog?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowResultsDialog && request.ShowResultsDialog), "SetBoolArg")
            ],
            [
                new("RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Max Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Average Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Standard Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.QueryPointsToObjectsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            RmsDeviation = values[0].RequireValue<WorkerDoubleValue>().Value,
            MaxAbsoluteDeviation = values[1].RequireValue<WorkerDoubleValue>().Value,
            AverageDeviation = values[2].RequireValue<WorkerDoubleValue>().Value,
            StandardDeviation = values[3].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
