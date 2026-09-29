using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class AverageSetOfGroupsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.average_set_of_groups", "Average a set of Groups",
        "briosa.ConstructionOperations", "AverageSetOfGroups", "/briosa.ConstructionOperations/AverageSetOfGroups",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("statistics.rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint),
        new("statistics.max_absolute_deviation", "Max Absolute Deviation", WorkerMpValueKind.FloatingPoint),
        new("statistics.average_deviation", "Average Deviation", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.AverageSetOfGroupsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Group Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.GroupNames, "group_names", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameRefListArg"),
            new("Resulting Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingGroupName, "resulting_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.RmsTolerance), "SetDoubleArg"),
            new("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.MaximumAbsoluteTolerance), "SetDoubleArg"),
            new("Maximum Average Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.MaximumAverageTolerance), "SetDoubleArg")
        ],
        [
            new("RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Max Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Average Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.AverageSetOfGroupsResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            Statistics = new Api.GroupAverageResult
            {
                RmsDeviation = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
                MaxAbsoluteDeviation = completed.Execution.OutputValues[1].RequireValue<WorkerDoubleValue>().Value,
                AverageDeviation = completed.Execution.OutputValues[2].RequireValue<WorkerDoubleValue>().Value
            },
            Execution = completed.Details
        };
}
