using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class StartGdtInspectionRehearseOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.start_gdt_inspection_rehearse", "Start GD&T Inspection Rehearse", "StartGdtInspectionRehearse");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StartGdtInspectionRehearseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var filter = request.HasFilter ? request.Filter : Api.InspectionFilter.Unspecified;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Collection Name", WorkerMpValueKind.CollectionName,
                    CollectionNameMapper.Required(request.Collection, "collection"), "SetCollectionNameArg"),
                new("Filter (ALL/CHECKS/DATUMS)", WorkerMpValueKind.Text,
                    InspectionFilterMapper.ToWorker(filter), "SetStringArg")
            ], []);
    }

    public static Api.StartGdtInspectionRehearseResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
