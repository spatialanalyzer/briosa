using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class CombinePointGroupsOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.combine_point_groups", "Combine Point Groups", "CombinePointGroups");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CombinePointGroupsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Groups to Combine", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.GroupsToCombine, "groups_to_combine"),
                "SetCollectionObjectNameRefListArg"),
            new("Combined Point Group", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CombinedPointGroup, "combined_point_group",
                    WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.CombinePointGroupsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
