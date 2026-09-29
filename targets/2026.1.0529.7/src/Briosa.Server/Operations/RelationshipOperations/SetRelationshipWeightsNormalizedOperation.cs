using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipWeightsNormalizedOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_weights_normalized", "Set Relationship Weights Normalized",
        "briosa.RelationshipOperations", "SetRelationshipWeightsNormalized",
        "/briosa.RelationshipOperations/SetRelationshipWeightsNormalized",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipWeightsNormalizedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var collectionName = RequiredCollectionName(request.CollectionName);
        var weightingMode = request.HasPickWeightingMode
            ? request.PickWeightingMode
            : Api.RelWeightingMode.NormalizeEquationCount;
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Collection Name", WorkerMpValueKind.CollectionName,
                    new WorkerTextValue(collectionName), "SetCollectionNameArg"),
                new("Pick Weighting Mode", WorkerMpValueKind.RelationshipWeightingMode,
                    new WorkerChoiceValue<WorkerRelationshipWeightingModeValue>(ToWorker(weightingMode)),
                    "SetRelWeightingModeArg")
            ], []);
    }

    public static Api.SetRelationshipWeightsNormalizedResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };

    private static string RequiredCollectionName(Api.CollectionName? value)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.Name))
        {
            throw new ArgumentException("Request field 'collection_name' is required.", nameof(value));
        }

        return value.Name;
    }

    private static WorkerRelationshipWeightingModeValue ToWorker(Api.RelWeightingMode value) => value switch
    {
        Api.RelWeightingMode.NormalizeEquationCount => WorkerRelationshipWeightingModeValue.NormalizeEquationCount,
        Api.RelWeightingMode.NormalizeEquationCountAndToleranceWidth => WorkerRelationshipWeightingModeValue.NormalizeEquationCountAndToleranceWidth,
        Api.RelWeightingMode.ResetAllWeights => WorkerRelationshipWeightingModeValue.ResetAllWeights,
        Api.RelWeightingMode.NormalizeSquareRootEquationCount => WorkerRelationshipWeightingModeValue.NormalizeSquareRootEquationCount,
        Api.RelWeightingMode.NormalizeSquareRootAndToleranceWidth => WorkerRelationshipWeightingModeValue.NormalizeSquareRootAndToleranceWidth,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Relationship weighting mode is not supported.")
    };
}
