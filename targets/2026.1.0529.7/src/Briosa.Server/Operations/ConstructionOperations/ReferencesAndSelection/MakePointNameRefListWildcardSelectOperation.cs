using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakePointNameRefListWildcardSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_point_name_ref_list_wildcard_select", "Make a Point Name Ref List - Wildcard Select",
        "briosa.ConstructionOperations", "MakePointNameRefListWildcardSelect", "/briosa.ConstructionOperations/MakePointNameRefListWildcardSelect",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_point_name_list", "Resultant Point Name List", WorkerMpValueKind.PointNameList)];

    public static WorkerMpCommand CreateCommand(Api.MakePointNameRefListWildcardSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
            new("Group Name Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasGroupNameWildcardCriteria ? request.GroupNameWildcardCriteria : "*"), "SetStringArg"),
            new("Point Name Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasPointNameWildcardCriteria ? request.PointNameWildcardCriteria : "*"), "SetStringArg")
        ], [new("Resultant Point Name List", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg")]);
    }

    public static Api.MakePointNameRefListWildcardSelectResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.MakePointNameRefListWildcardSelectResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
            result.ResultantPointNameList.Add(PointNameMapper.ToProtocol(value));
        return result;
    }
}
