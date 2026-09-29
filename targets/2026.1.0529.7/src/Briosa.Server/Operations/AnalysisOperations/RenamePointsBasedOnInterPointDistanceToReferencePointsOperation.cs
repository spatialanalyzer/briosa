using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class RenamePointsBasedOnInterPointDistanceToReferencePointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.rename_points_based_on_inter_point_distance_to_reference_points",
        "Rename points based on inter-point distance to reference points",
        "briosa.AnalysisOperations", "RenamePointsBasedOnInterPointDistanceToReferencePoints",
        "/briosa.AnalysisOperations/RenamePointsBasedOnInterPointDistanceToReferencePoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenamePointsBasedOnInterPointDistanceToReferencePointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Reference Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceGroupName, "reference_group_name"), "SetCollectionObjectNameArg2"),
                new("Group To Rename Points", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.GroupToRenamePoints, "group_to_rename_points"), "SetCollectionObjectNameArg2"),
                new("Distance Threshold", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasDistanceThreshold ? request.DistanceThreshold : 0d), "SetDoubleArg"),
                new("Verify Results?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasVerifyResults && request.VerifyResults), "SetBoolArg")
            ], []);
    }

    public static Api.RenamePointsBasedOnInterPointDistanceToReferencePointsResult CreateResult(
        SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
