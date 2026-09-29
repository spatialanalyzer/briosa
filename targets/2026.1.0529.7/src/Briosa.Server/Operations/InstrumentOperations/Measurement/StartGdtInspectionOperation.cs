using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class StartGdtInspectionOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.start_gdt_inspection", "Start GD&T Inspection", "StartGdtInspection");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StartGdtInspectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var filter = request.HasFilter ? request.Filter : Api.InspectionFilter.Unspecified;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Collection Name", WorkerMpValueKind.CollectionName,
                    CollectionNameMapper.Required(request.Collection, "collection"), "SetCollectionNameArg"),
                new("Filter (ALL/CHECKS/DATUMS)", WorkerMpValueKind.Text,
                    InspectionFilterMapper.ToWorker(filter), "SetStringArg")
            ], []);
    }

    public static Api.StartGdtInspectionResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
