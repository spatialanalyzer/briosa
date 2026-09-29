using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetDefaultColorizationOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_default_colorization_options", "Set Default Colorization Options",
        "briosa.AnalysisOperations", "SetDefaultColorizationOptions", "/briosa.AnalysisOperations/SetDefaultColorizationOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetDefaultColorizationOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Colorization Options", WorkerMpValueKind.ColorizationOptions,
                ColorizationOptionsMapper.WithDefaults(request.ColorizationOptions), "SetColorizationOptionsArg")], []);
    }

    public static Api.SetDefaultColorizationOptionsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
