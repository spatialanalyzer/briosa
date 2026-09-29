using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetDecimalDigitsForDisplayOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_decimal_digits_for_display", "Set Decimal Digits for Display", "briosa.UtilityOperations",
        "SetDecimalDigitsForDisplay", "/briosa.UtilityOperations/SetDecimalDigitsForDisplay", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetDecimalDigitsForDisplayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Length", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasLength ? request.Length : 4), "SetIntegerArg"),
            new("Angle", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasAngle ? request.Angle : 4), "SetIntegerArg"),
            new("Scale", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasScale ? request.Scale : 6), "SetIntegerArg"),
            new("Unit Vector", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasUnitVector ? request.UnitVector : 6), "SetIntegerArg"),
            new("Weight", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasWeight ? request.Weight : 3), "SetIntegerArg")
        ], []);
    }

    public static Api.SetDecimalDigitsForDisplayResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
