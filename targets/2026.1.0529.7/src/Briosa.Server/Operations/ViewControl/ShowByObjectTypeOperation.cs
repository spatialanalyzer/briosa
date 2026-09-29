using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowByObjectTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_by_object_type", "Show by Object Type", "briosa.ViewControl",
        "ShowByObjectType", "/briosa.ViewControl/ShowByObjectType", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowByObjectTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object Type To Show", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectTypeToShow, "object_type_to_show"), "SetCollectionObjectNameArg2"),
            new("All Collections?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AllCollections), "SetBoolArg")
        ], []);
    }

    public static Api.ShowByObjectTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
