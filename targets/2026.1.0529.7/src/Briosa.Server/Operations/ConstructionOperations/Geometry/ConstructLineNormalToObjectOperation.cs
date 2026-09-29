using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructLineNormalToObjectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_line_normal_to_object", "Construct Line Normal to Object",
        "briosa.ConstructionOperations", "ConstructLineNormalToObject", "/briosa.ConstructionOperations/ConstructLineNormalToObject",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructLineNormalToObjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Line Length", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasLineLength ? request.LineLength : 1.0), "SetDoubleArg"),
            new("Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Object, "object", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructLineNormalToObjectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
