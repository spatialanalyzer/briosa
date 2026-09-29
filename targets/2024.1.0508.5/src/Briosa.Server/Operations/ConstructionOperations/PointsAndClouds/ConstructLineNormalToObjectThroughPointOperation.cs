using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructLineNormalToObjectThroughPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_line_normal_to_object_through_point", "Construct Line - Normal to Object through Point",
        "briosa.ConstructionOperations", "ConstructLineNormalToObjectThroughPoint", "/briosa.ConstructionOperations/ConstructLineNormalToObjectThroughPoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructLineNormalToObjectThroughPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Line To Create", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineToCreate, "line_to_create", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectName, "object_name", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructLineNormalToObjectThroughPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
