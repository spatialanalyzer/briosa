using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class SetOrConstructDefaultCollectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.set_or_construct_default_collection", "Set (or construct) default collection",
        "briosa.ConstructionOperations", "SetOrConstructDefaultCollection", "/briosa.ConstructionOperations/SetOrConstructDefaultCollection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetOrConstructDefaultCollectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.CollectionName, "collection_name"), "SetCollectionNameArg")], []);
    }

    public static Api.SetOrConstructDefaultCollectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
