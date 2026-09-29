using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetNumberOfPointsInGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_number_of_points_in_group", "Get Number of Points in Group",
        "briosa.AnalysisOperations", "GetNumberOfPointsInGroup",
        "/briosa.AnalysisOperations/GetNumberOfPointsInGroup",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("total_count", "Total Count", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetNumberOfPointsInGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupName, "group_name"), "SetCollectionObjectNameArg2")],
            [new("Total Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetNumberOfPointsInGroupResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TotalCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
