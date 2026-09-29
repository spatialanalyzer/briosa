using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SaveInstrumentConfigurationOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.save_instrument_configuration", "Save Instrument Configuration", "SaveInstrumentConfiguration");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SaveInstrumentConfigurationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")
        };
        if (request.ConfigurationFile is not null)
            inputs.Add(new("Configuration File", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.ConfigurationFile, "configuration_file"), "SetFilePathArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.SaveInstrumentConfigurationResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
