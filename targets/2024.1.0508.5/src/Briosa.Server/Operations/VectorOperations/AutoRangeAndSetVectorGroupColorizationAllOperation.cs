using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class AutoRangeAndSetVectorGroupColorizationAllOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.auto_range_and_set_vector_group_colorization_all",
        "Auto-Range and Set Vector Group Colorization (All)", "briosa.VectorOperations",
        "AutoRangeAndSetVectorGroupColorizationAll",
        "/briosa.VectorOperations/AutoRangeAndSetVectorGroupColorizationAll",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.AutoRangeAndSetVectorGroupColorizationAllRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Treat Individually?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.TreatIndividually), "SetBoolArg"),
            new("Colorization Options (Uses Mode Only)", WorkerMpValueKind.ColorizationOptions,
                ColorizationOptionsMapper.WithDefaults(request.ColorizationOptions), "SetColorizationOptionsArg")
        ], []);
    }
    public static Api.AutoRangeAndSetVectorGroupColorizationAllResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
