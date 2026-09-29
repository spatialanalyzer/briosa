using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class StartInstrumentInterfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.start_instrument_interface", "Start Instrument Interface",
        "briosa.InstrumentOperations", "StartInstrumentInterface", "/briosa.InstrumentOperations/StartInstrumentInterface",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StartInstrumentInterfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Initialize at Startup", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasInitializeAtStartup && request.InitializeAtStartup), "SetBoolArg")
        };
        if (request.HasDeviceIpAddress)
            inputs.Add(new("Device IP Address (optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.DeviceIpAddress), "SetStringArg"));
        inputs.Add(new("Interface Type (0=default)", WorkerMpValueKind.WholeNumber,
            new WorkerIntegerValue(request.InterfaceType), "SetIntegerArg"));
        inputs.Add(new("Run in Simulation", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.HasRunInSimulation && request.RunInSimulation), "SetBoolArg"));
        inputs.Add(new("Allow Start w/o Init Requirements", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.HasAllowStartWithoutInitializationRequirements && request.AllowStartWithoutInitializationRequirements), "SetBoolArg"));

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.StartInstrumentInterfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}