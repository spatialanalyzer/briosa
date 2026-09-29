using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class RenamePointsBasedOnProximityToReferencePointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.rename_points_based_on_proximity_to_reference_points",
        "Rename points based on proximity to reference points",
        "briosa.AnalysisOperations", "RenamePointsBasedOnProximityToReferencePoints",
        "/briosa.AnalysisOperations/RenamePointsBasedOnProximityToReferencePoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenamePointsBasedOnProximityToReferencePointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Reference Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceGroupName, "reference_group_name"), "SetCollectionObjectNameArg2"),
                new("Group To Rename Points", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.GroupToRenamePoints, "group_to_rename_points"), "SetCollectionObjectNameArg2"),
                new("Proximity Threshold", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasProximityThreshold ? request.ProximityThreshold : 0d), "SetDoubleArg"),
                new("Verify Results?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasVerifyResults && request.VerifyResults), "SetBoolArg"),
                new("Rename All Proximate Points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasRenameAllProximatePoints && request.RenameAllProximatePoints), "SetBoolArg")
            ], []);
    }

    public static Api.RenamePointsBasedOnProximityToReferencePointsResult CreateResult(
        SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
