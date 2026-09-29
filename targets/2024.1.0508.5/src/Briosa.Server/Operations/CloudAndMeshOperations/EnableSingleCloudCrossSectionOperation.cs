using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class EnableSingleCloudCrossSectionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.enable_single_cloud_cross_section", "Enable Single Cloud Cross Section",
        "briosa.CloudAndMeshOperations", "EnableSingleCloudCrossSection", "/briosa.CloudAndMeshOperations/EnableSingleCloudCrossSection",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EnableSingleCloudCrossSectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CrossSectionCloudName, "cross_section_cloud_name", WorkerObjectTypeValue.CrossSectionCloud), "SetCollectionObjectNameArg2"),
            new("Cross Section ID", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasCrossSectionId ? request.CrossSectionId : 0), "SetIntegerArg")
        ], []);
    }

    public static Api.EnableSingleCloudCrossSectionResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
