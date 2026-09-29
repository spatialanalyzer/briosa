using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class SetVectorGroupColorizationOptionsAllOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.set_vector_group_colorization_options_all",
        "Set Vector Group Colorization Options (All)", "briosa.VectorOperations",
        "SetVectorGroupColorizationOptionsAll",
        "/briosa.VectorOperations/SetVectorGroupColorizationOptionsAll",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetVectorGroupColorizationOptionsAllRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Colorization Options", WorkerMpValueKind.ColorizationOptions,
                ColorizationOptionsMapper.WithDefaults(request.ColorizationOptions), "SetColorizationOptionsArg")], []);
    }
    public static Api.SetVectorGroupColorizationOptionsAllResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
