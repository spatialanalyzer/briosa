using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AutoMeasureSpecifiedGeometryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.auto_measure_specified_geometry", "Auto-Measure Specified Geometry",
        "briosa.InstrumentOperations", "AutoMeasureSpecifiedGeometry",
        "/briosa.InstrumentOperations/AutoMeasureSpecifiedGeometry",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoMeasureSpecifiedGeometryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Geometry, "geometry", WorkerObjectTypeValue.Any),
                "SetCollectionObjectNameArg2"),
            new("Mode/Profile", WorkerMpValueKind.Text,
                new WorkerTextValue(request.ModeProfile), "SetStringArg"),
            new("Wait for Complete", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.WaitForComplete), "SetBoolArg")
        ], []);
    }

    public static Api.AutoMeasureSpecifiedGeometryResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
