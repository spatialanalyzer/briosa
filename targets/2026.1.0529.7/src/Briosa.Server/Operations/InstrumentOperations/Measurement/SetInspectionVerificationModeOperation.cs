using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class SetInspectionVerificationModeOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.set_inspection_verification_mode", "Set Inspection Verification Mode", "SetInspectionVerificationMode");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInspectionVerificationModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Enable Verification?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasVerificationEnabled && request.VerificationEnabled), "SetBoolArg")], []);
    }

    public static Api.SetInspectionVerificationModeResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
