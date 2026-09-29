using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class DriftCheckOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.drift_check", "Drift Check",
        "briosa.InstrumentOperations", "DriftCheck",
        "/briosa.InstrumentOperations/DriftCheck",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("maximum_error", "Max error", WorkerMpValueKind.FloatingPoint),
        new("rms_error", "RMS error", WorkerMpValueKind.FloatingPoint),
        new("instrument_added", "Instrument added?", WorkerMpValueKind.Logical),
        new("new_instrument", "New Instrument", WorkerMpValueKind.CollectionInstrumentId)
    ];

    public static WorkerMpCommand CreateCommand(Api.DriftCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument to check", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Reference Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group",
                        WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ActualsGroup, "actuals_group",
                        WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Tolerance", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasTolerance ? request.Tolerance : 0), "SetDoubleArg"),
                new("Minimum point count", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasMinimumPointCount ? request.MinimumPointCount : 0),
                    "SetIntegerArg"),
                new("Use Closest Reference Point", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasUseClosestReferencePoint ||
                        request.UseClosestReferencePoint), "SetBoolArg")
            ],
            [
                new("Max error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("RMS error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Instrument added?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("New Instrument", WorkerMpValueKind.CollectionInstrumentId, "GetColInstIdArg")
            ]);
    }

    public static Api.DriftCheckResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        var instrument = outputs[3].RequireValue<WorkerCollectionInstrumentIdValue>();
        return new()
        {
            MaximumError = outputs[0].RequireValue<WorkerDoubleValue>().Value,
            RmsError = outputs[1].RequireValue<WorkerDoubleValue>().Value,
            InstrumentAdded = outputs[2].RequireValue<WorkerBooleanValue>().Value,
            NewInstrument = new Api.CollectionInstrumentId
            {
                CollectionName = instrument.CollectionName,
                InstrumentId = instrument.InstrumentId
            },
            Execution = completed.Details
        };
    }
}
