using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetInspectionVerificationModeOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.ReadOnly(
        "instrument_operations.get_inspection_verification_mode", "Get Inspection Verification Mode", "GetInspectionVerificationMode");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("verification_enabled", "Verification Enabled?", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.GetInspectionVerificationModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
            [new("Verification Enabled?", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.GetInspectionVerificationModeResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        VerificationEnabled = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        Execution = completed.Details
    };
}
