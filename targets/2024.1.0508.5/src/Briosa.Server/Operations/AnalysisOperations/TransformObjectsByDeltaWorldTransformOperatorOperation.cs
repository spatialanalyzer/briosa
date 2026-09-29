using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class TransformObjectsByDeltaWorldTransformOperatorOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.transform_objects_by_delta_world_transform_operator", "Transform Objects by Delta (World Transform Operator)",
        "briosa.AnalysisOperations", "TransformObjectsByDeltaWorldTransformOperator",
        "/briosa.AnalysisOperations/TransformObjectsByDeltaWorldTransformOperator",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TransformObjectsByDeltaWorldTransformOperatorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Objects to Transform", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectsToTransform, "objects_to_transform"),
                    "SetCollectionObjectNameRefListArg"),
                new("Delta Transform", WorkerMpValueKind.WorldTransform,
                    WorldTransformMapper.Required(request.DeltaTransform, "delta_transform"), "SetWorldTransformArg")
            ], []);
    }

    public static Api.TransformObjectsByDeltaWorldTransformOperatorResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
