using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetRemeasureFailedChecksOnlyOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.set_remeasure_failed_checks_only", "Set Remeasure Failed Checks Only",
        "briosa.InstrumentOperations", "SetRemeasureFailedChecksOnly",
        "/briosa.InstrumentOperations/SetRemeasureFailedChecksOnly",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRemeasureFailedChecksOnlyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Collection is null || string.IsNullOrWhiteSpace(request.Collection.Name))
            throw new ArgumentException("Collection Name is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Name", WorkerMpValueKind.CollectionName,
                new WorkerTextValue(request.Collection.Name), "SetCollectionNameArg")], []);
    }

    public static Api.SetRemeasureFailedChecksOnlyResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
