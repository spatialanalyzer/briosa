using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructBoundaryPointsFromCloudOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_boundary_points_from_cloud", "Construct Boundary Points from Cloud",
        "briosa.ConstructionOperations", "ConstructBoundaryPointsFromCloud", "/briosa.ConstructionOperations/ConstructBoundaryPointsFromCloud",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructBoundaryPointsFromCloudRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SourceCloudName, "source_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Destination Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.DestinationCloudName, "destination_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructBoundaryPointsFromCloudResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
