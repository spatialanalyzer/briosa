using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class StartTheodoliteInterfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.start_theodolite_interface", "Start Theodolite Interface",
        "briosa.InstrumentOperations", "StartTheodoliteInterface", "/briosa.InstrumentOperations/StartTheodoliteInterface",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.StartTheodoliteInterfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument's ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Theodolite Type (Must match Theodolite Manager Add Instrument type)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.TheodoliteType), "SetStringArg"),
            new("Comm Port", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.CommPort), "SetIntegerArg")
        };
        if (request.HasDeviceIpAddress)
            inputs.Add(new("Device IP Address (optional)", WorkerMpValueKind.Text,
                new WorkerTextValue(request.DeviceIpAddress), "SetStringArg"));
        inputs.Add(new("Simulation", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.HasSimulation && request.Simulation), "SetBoolArg"));

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.StartTheodoliteInterfaceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}