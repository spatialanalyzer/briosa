using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointCloudFromExistingCloudsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_cloud_from_existing_clouds", "Construct Point Cloud from Existing Clouds",
        "briosa.ConstructionOperations", "ConstructPointCloudFromExistingClouds", "/briosa.ConstructionOperations/ConstructPointCloudFromExistingClouds",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointCloudFromExistingCloudsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Existing Point Cloud List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ExistingPointCloudList, "existing_point_cloud_list"), "SetCollectionObjectNameRefListArg"),
            new("New Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewCloudName, "new_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions,
                CloudThinningOptionsMapper.ToWorker(request.CloudThinningSettings), "SetCloudThinningOptionsArg"),
            new("Hide Original Point Clouds", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasHideOriginalPointClouds || request.HideOriginalPointClouds), "SetBoolArg"),
            new("Set Cloud Point RGB from Voxels?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasSetCloudPointRgbFromVoxels && request.SetCloudPointRgbFromVoxels), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructPointCloudFromExistingCloudsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
