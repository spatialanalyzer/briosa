using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideByObjectTypeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_by_object_type", "Show/Hide by Object Type", "briosa.ViewControl",
        "ShowHideByObjectType", "/briosa.ViewControl/ShowHideByObjectType", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideByObjectTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var objectType = request.HasObjectTypeToShowHide
            ? request.ObjectTypeToShowHide : Api.ObjectType.Any;
        if (!Enum.IsDefined(objectType) || objectType == Api.ObjectType.Unspecified)
        {
            throw new ArgumentException("A supported object type is required.", nameof(request));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("All Collections?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AllCollections), "SetBoolArg"),
            new("Specific Collection", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.SpecificCollection, "specific_collection"), "SetCollectionNameArg"),
            new("Object Type To Show / Hide", WorkerMpValueKind.ObjectType,
                new WorkerChoiceValue<WorkerObjectTypeValue>((WorkerObjectTypeValue)objectType), "SetObjectTypeArg"),
            new("Hide? (Show = FALSE)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasHide || request.Hide), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHideByObjectTypeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
