using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class ClearCloudPointDeviationsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.clear_cloud_point_deviations", "Clear Cloud Point Deviations",
        "briosa.CloudAndMeshOperations", "ClearCloudPointDeviations", "/briosa.CloudAndMeshOperations/ClearCloudPointDeviations",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ClearCloudPointDeviationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CloudName, "cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ClearCloudPointDeviationsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
