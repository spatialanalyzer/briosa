using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInstrumentTargetsAndModeProfilesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_instrument_targets_and_mode_profiles", "Get Instrument Targets and Mode/Profiles",
        "briosa.InstrumentOperations", "GetInstrumentTargetsAndModeProfiles",
        "/briosa.InstrumentOperations/GetInstrumentTargetsAndModeProfiles",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("mode_profiles", "Mode/Profile", WorkerMpValueKind.StringList),
        new("target_names", "Target Names", WorkerMpValueKind.StringList)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetInstrumentTargetsAndModeProfilesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Instrument to get", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg")],
        [
            new("Mode/Profile", WorkerMpValueKind.StringList, "GetStringRefListArg"),
            new("Target Names", WorkerMpValueKind.StringList, "GetStringRefListArg")
        ]);
    }

    public static Api.GetInstrumentTargetsAndModeProfilesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        var result = new Api.GetInstrumentTargetsAndModeProfilesResult { Execution = completed.Details };
        result.ModeProfiles.Add(values[0].RequireValue<WorkerStringListValue>().Values);
        result.TargetNames.Add(values[1].RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
