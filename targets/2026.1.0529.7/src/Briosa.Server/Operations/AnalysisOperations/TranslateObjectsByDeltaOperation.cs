using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class TranslateObjectsByDeltaOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.translate_objects_by_delta", "Translate Objects by Delta",
        "briosa.AnalysisOperations", "TranslateObjectsByDelta",
        "/briosa.AnalysisOperations/TranslateObjectsByDelta",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TranslateObjectsByDeltaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Objects to Translate", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectsToTranslate, "objects_to_translate"),
                    "SetCollectionObjectNameRefListArg"),
                new("Delta Translation", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.DeltaTranslation, "delta_translation"), "SetVectorArg")
            ], []);
    }

    public static Api.TranslateObjectsByDeltaResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
