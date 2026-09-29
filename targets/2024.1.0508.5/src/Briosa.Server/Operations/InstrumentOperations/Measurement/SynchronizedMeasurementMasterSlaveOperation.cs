using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SynchronizedMeasurementMasterSlaveOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.synchronized_measurement_master_slave", "Synchronized Measurement (Master/Slave)",
        "briosa.InstrumentOperations", "SynchronizedMeasurementMasterSlave",
        "/briosa.InstrumentOperations/SynchronizedMeasurementMasterSlave",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SynchronizedMeasurementMasterSlaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var slaveGroupSuffix = request.HasSlaveGroupSuffix ? request.SlaveGroupSuffix : "_Slave";
        var locateOneOfTheInstruments = !request.HasLocateOneOfTheInstruments || request.LocateOneOfTheInstruments;
        var locateMaster = request.HasLocateMaster && request.LocateMaster;
        var waitForCompletion = !request.HasWaitForCompletion || request.WaitForCompletion;

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Master Instrument", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.MasterInstrument, "master_instrument"), "SetColInstIdArg"),
            new("Slave Instrument", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.SlaveInstrument, "slave_instrument"), "SetColInstIdArg"),
            new("Slave Group Suffix", WorkerMpValueKind.Text,
                new WorkerTextValue(slaveGroupSuffix), "SetStringArg"),
            new("Locate One of the Instruments?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(locateOneOfTheInstruments), "SetBoolArg"),
            new("Locate Master (FALSE = Slave)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(locateMaster), "SetBoolArg"),
            new("Wait for Completion?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(waitForCompletion), "SetBoolArg")
        ], []);
    }

    public static Api.SynchronizedMeasurementMasterSlaveResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
