using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class CreatePointUncertaintyCloudPointSetsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.create_point_uncertainty_cloud_point_sets", "Create Point Uncertainty Cloud Point Sets",
        "briosa.AnalysisOperations", "CreatePointUncertaintyCloudPointSets", "/briosa.AnalysisOperations/CreatePointUncertaintyCloudPointSets",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("point_groups", "Point Groups", WorkerMpValueKind.CollectionObjectNameList),
        new("point_sets", "Point Sets", WorkerMpValueKind.CollectionObjectNameList),
        new("point_clouds", "Point Clouds", WorkerMpValueKind.CollectionObjectNameList)
    ];

    public static WorkerMpCommand CreateCommand(Api.CreatePointUncertaintyCloudPointSetsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Name List", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointNameList, "point_name_list"), "SetPointNameRefListArg"),
                new("Number of Samples", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasNumberOfSamples ? request.NumberOfSamples : 1000), "SetIntegerArg"),
                new("Uncertainty Reference Frame Mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasUncertaintyReferenceFrameMode
                        ? request.UncertaintyReferenceFrameMode : "With respect to WORLD"), "SetStringArg"),
                new("Grouping mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasGroupingMode ? request.GroupingMode : "Group per point"), "SetStringArg"),
                new("Point set mode", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasPointSetMode ? request.PointSetMode : "Point clouds"), "SetStringArg")
            ],
            [
                new("Point Groups", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Point Sets", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg"),
                new("Point Clouds", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")
            ]);
    }

    public static Api.CreatePointUncertaintyCloudPointSetsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.CreatePointUncertaintyCloudPointSetsResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.PointGroups.Add(CollectionObjectNameMapper.ToProtocol(value));
        foreach (var value in completed.Execution.OutputValues[1].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.PointSets.Add(CollectionObjectNameMapper.ToProtocol(value));
        foreach (var value in completed.Execution.OutputValues[2].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.PointClouds.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
