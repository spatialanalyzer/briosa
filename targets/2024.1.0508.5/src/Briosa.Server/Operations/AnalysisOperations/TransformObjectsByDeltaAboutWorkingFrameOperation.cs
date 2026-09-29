using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class TransformObjectsByDeltaAboutWorkingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.transform_objects_by_delta_about_working_frame", "Transform Objects by Delta (About Working Frame)",
        "briosa.AnalysisOperations", "TransformObjectsByDeltaAboutWorkingFrame",
        "/briosa.AnalysisOperations/TransformObjectsByDeltaAboutWorkingFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TransformObjectsByDeltaAboutWorkingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Objects to Transform", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectsToTransform, "objects_to_transform"),
                    "SetCollectionObjectNameRefListArg"),
                new("Delta Transform", WorkerMpValueKind.Transform,
                    TransformMapper.Required(request.DeltaTransform, "delta_transform"), "SetTransformArg")
            ], []);
    }

    public static Api.TransformObjectsByDeltaAboutWorkingFrameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
