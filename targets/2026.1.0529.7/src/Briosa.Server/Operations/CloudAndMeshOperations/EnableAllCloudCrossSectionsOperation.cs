using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class EnableAllCloudCrossSectionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.enable_all_cloud_cross_sections", "Enable All Cloud Cross Sections",
        "briosa.CloudAndMeshOperations", "EnableAllCloudCrossSections", "/briosa.CloudAndMeshOperations/EnableAllCloudCrossSections",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EnableAllCloudCrossSectionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName,
            CollectionObjectNameMapper.Required(request.CrossSectionCloudName, "cross_section_cloud_name", WorkerObjectTypeValue.CrossSectionCloud), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.EnableAllCloudCrossSectionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
