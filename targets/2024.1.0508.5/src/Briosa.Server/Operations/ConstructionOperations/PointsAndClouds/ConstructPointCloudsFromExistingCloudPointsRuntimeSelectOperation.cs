using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointCloudsFromExistingCloudPointsRuntimeSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_clouds_from_existing_cloud_points_runtime_select", "Construct Point Clouds from Existing Cloud Points - Runtime Select",
        "briosa.ConstructionOperations", "ConstructPointCloudsFromExistingCloudPointsRuntimeSelect", "/briosa.ConstructionOperations/ConstructPointCloudsFromExistingCloudPointsRuntimeSelect",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CloudName, "cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
