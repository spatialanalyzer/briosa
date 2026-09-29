using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsOnObjectVerticesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_on_object_vertices", "Construct Points on Objects Vertices",
        "briosa.ConstructionOperations", "ConstructPointsOnObjectVertices", "/briosa.ConstructionOperations/ConstructPointsOnObjectVertices",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsOnObjectVerticesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object Name List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ObjectNameList, "object_name_list"), "SetCollectionObjectNameRefListArg"),
            new("Resultant Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultantGroupName, "resultant_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPointsOnObjectVerticesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
