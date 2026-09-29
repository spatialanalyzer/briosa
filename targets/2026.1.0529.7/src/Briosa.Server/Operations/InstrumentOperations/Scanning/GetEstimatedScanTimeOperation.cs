using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetEstimatedScanTimeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_estimated_scan_time", "Get Estimated Scan Time",
        "briosa.InstrumentOperations", "GetEstimatedScanTime",
        "/briosa.InstrumentOperations/GetEstimatedScanTime",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("estimated_scan_time", "Estimated Scan Time", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetEstimatedScanTimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profileName = request.HasProfileName ? request.ProfileName : string.Empty;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Profile name", WorkerMpValueKind.Text, new WorkerTextValue(profileName), "SetStringArg")
        ], [new("Estimated Scan Time", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetEstimatedScanTimeResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        EstimatedScanTime = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
