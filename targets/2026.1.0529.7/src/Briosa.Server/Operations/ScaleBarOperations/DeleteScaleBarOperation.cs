using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ScaleBarOperations;

internal static class DeleteScaleBarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "scale_bar_operations.delete_scale_bar", "Delete Scale Bar",
        "briosa.ScaleBarOperations", "DeleteScaleBar", "/briosa.ScaleBarOperations/DeleteScaleBar",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteScaleBarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Scale Bar Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ScaleBarName, "scale_bar_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.DeleteScaleBarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
