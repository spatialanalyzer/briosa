using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.DimensionOperations;

internal static class DeleteDimensionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "dimension_operations.delete_dimension", "Delete Dimension",
        "briosa.DimensionOperations", "DeleteDimension", "/briosa.DimensionOperations/DeleteDimension",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteDimensionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Dimension Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.DimensionName, "dimension_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.DeleteDimensionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
