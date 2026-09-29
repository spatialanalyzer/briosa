using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameWithWizardOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_with_wizard", "Construct Frame with Wizard",
        "briosa.ConstructionOperations", "ConstructFrameWithWizard", "/briosa.ConstructionOperations/ConstructFrameWithWizard",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameWithWizardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("New Frame Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewFrameName, "new_frame_name", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2"),
            new("Wait for Completion", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasWaitForCompletion || request.WaitForCompletion), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructFrameWithWizardResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
