using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AutoMeasureVectorsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.auto_measure_vectors", "Auto-Measure Vectors",
        "briosa.InstrumentOperations", "AutoMeasureVectors", "/briosa.InstrumentOperations/AutoMeasureVectors",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoMeasureVectorsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroup, "vector_group",
                    WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
            new("Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ActualsGroup, "actuals_group",
                    WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Project Point to Vector", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ProjectPointToVector), "SetBoolArg"),
            new("Angle Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.AngleTolerance), "SetDoubleArg"),
            new("High Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HighTolerance), "SetDoubleArg"),
            new("Low Tolerance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.LowTolerance), "SetDoubleArg")
        ], []);
    }

    public static Api.AutoMeasureVectorsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
