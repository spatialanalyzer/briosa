using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class CenterGraphicsAboutObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.center_graphics_about_objects", "Center Graphics About Object(s)", "briosa.ViewControl",
        "CenterGraphicsAboutObjects", "/briosa.ViewControl/CenterGraphicsAboutObjects", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CenterGraphicsAboutObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var objectType = request.HasObjectType ? request.ObjectType : Api.ObjectType.Any;
        if (!Enum.IsDefined(objectType) || objectType == Api.ObjectType.Unspecified)
        {
            throw new ArgumentException("A supported object type is required.", nameof(request));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object Type", WorkerMpValueKind.ObjectType,
                new WorkerChoiceValue<WorkerObjectTypeValue>((WorkerObjectTypeValue)objectType), "SetObjectTypeArg"),
            new("Collection Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasCollectionWildcardCriteria ? request.CollectionWildcardCriteria : "*"), "SetStringArg"),
            new("Object Wildcard Criteria", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasObjectWildcardCriteria ? request.ObjectWildcardCriteria : "*"), "SetStringArg")
        ], []);
    }

    public static Api.CenterGraphicsAboutObjectsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
