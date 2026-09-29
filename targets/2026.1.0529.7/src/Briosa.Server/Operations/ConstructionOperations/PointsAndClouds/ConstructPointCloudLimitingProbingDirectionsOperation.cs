using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointCloudLimitingProbingDirectionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_cloud_limiting_probing_directions", "Construct Point Cloud Limiting Probing Directions",
        "briosa.ConstructionOperations", "ConstructPointCloudLimitingProbingDirections", "/briosa.ConstructionOperations/ConstructPointCloudLimitingProbingDirections",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointCloudLimitingProbingDirectionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SourceCloudName, "source_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("'Normal to' Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NormalToObjectName, "normal_to_object_name"), "SetCollectionObjectNameArg2"),
            new("Acceptance Angle", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasAcceptanceAngle ? request.AcceptanceAngle : 30), "SetDoubleArg"),
            new("Destination Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.DestinationCloudName, "destination_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Hide Source Cloud", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasHideSourceCloud && request.HideSourceCloud), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructPointCloudLimitingProbingDirectionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
