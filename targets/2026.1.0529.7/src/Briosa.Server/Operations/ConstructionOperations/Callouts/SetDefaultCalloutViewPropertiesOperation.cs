using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class SetDefaultCalloutViewPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.set_default_callout_view_properties", "Set Default Callout View Properties",
        "briosa.ConstructionOperations", "SetDefaultCalloutViewProperties", "/briosa.ConstructionOperations/SetDefaultCalloutViewProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetDefaultCalloutViewPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<WorkerMpInputArgument>
        {
            new("Default Callout View Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasDefaultCalloutViewName ? request.DefaultCalloutViewName : "Callout 1"), "SetStringArg")
        };
        arguments.AddRange(CalloutViewPropertyArguments.Create(request.Properties));
        return new(Descriptor.OperationId, Descriptor.MpStep, arguments, []);
    }
    public static Api.SetDefaultCalloutViewPropertiesResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}
