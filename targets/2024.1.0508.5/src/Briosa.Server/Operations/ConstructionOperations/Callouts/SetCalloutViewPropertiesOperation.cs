using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class SetCalloutViewPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.set_callout_view_properties", "Set Callout View Properties",
        "briosa.ConstructionOperations", "SetCalloutViewProperties", "/briosa.ConstructionOperations/SetCalloutViewProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetCalloutViewPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<WorkerMpInputArgument>
        {
            new("Callout View List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.CalloutViews, "callout_views", WorkerItemTypeValue.CalloutView), "SetCollectionObjectNameRefListArg")
        };
        arguments.AddRange(CalloutViewPropertyArguments.Create(request.Properties));
        return new(Descriptor.OperationId, Descriptor.MpStep, arguments, []);
    }
    public static Api.SetCalloutViewPropertiesResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
