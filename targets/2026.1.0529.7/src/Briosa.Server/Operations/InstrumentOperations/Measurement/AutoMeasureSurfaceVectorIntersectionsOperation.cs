using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AutoMeasureSurfaceVectorIntersectionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.auto_measure_surface_vector_intersections",
        "Auto-Measure Surface Vector Intersections", "briosa.InstrumentOperations",
        "AutoMeasureSurfaceVectorIntersections", "/briosa.InstrumentOperations/AutoMeasureSurfaceVectorIntersections",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoMeasureSurfaceVectorIntersectionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Vector Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroup, "vector_group",
                    WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
            new("Resultant Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultantGroup, "resultant_group",
                    WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Wait for Complete", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasWaitForComplete ? request.WaitForComplete : true), "SetBoolArg")
        ], []);
    }

    public static Api.AutoMeasureSurfaceVectorIntersectionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
