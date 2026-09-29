using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointGroupsFromVectorGroupsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_groups_from_vector_groups", "Construct Point Groups from Vector Groups",
        "briosa.ConstructionOperations", "ConstructPointGroupsFromVectorGroups", "/briosa.ConstructionOperations/ConstructPointGroupsFromVectorGroups",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("point_groups", "Point Groups", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointGroupsFromVectorGroupsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<WorkerMpInputArgument>
        {
            new("Vector Groups", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.VectorGroups, "vector_groups"), "SetCollectionObjectNameRefListArg")
        };
        if (request.HasOptionalGroupNameSuffix)
        {
            arguments.Add(new("Optional Group Name Suffix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.OptionalGroupNameSuffix), "SetStringArg"));
        }
        arguments.Add(new("Make Vector Begin Points", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.MakeVectorBeginPoints), "SetBoolArg"));
        arguments.Add(new("Make Vector End Points", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.MakeVectorEndPoints), "SetBoolArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, arguments,
            [new("Point Groups", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.ConstructPointGroupsFromVectorGroupsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.ConstructPointGroupsFromVectorGroupsResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            result.PointGroups.Add(CollectionObjectNameMapper.ToProtocol(value));
        return result;
    }
}
