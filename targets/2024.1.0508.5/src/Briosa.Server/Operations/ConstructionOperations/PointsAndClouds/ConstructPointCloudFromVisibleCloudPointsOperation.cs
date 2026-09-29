using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointCloudFromVisibleCloudPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_cloud_from_visible_cloud_points", "Construct Point Cloud from Visible Cloud Points",
        "briosa.ConstructionOperations", "ConstructPointCloudFromVisibleCloudPoints", "/briosa.ConstructionOperations/ConstructPointCloudFromVisibleCloudPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointCloudFromVisibleCloudPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Clouds", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.SourceClouds, "source_clouds"), "SetCollectionObjectNameRefListArg"),
            new("Destination Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.DestinationCloudName, "destination_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPointCloudFromVisibleCloudPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
