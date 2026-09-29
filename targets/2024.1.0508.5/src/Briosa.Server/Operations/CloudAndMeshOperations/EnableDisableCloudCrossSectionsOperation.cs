using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class EnableDisableCloudCrossSectionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.enable_disable_cloud_cross_sections", "Enable/Disable Cloud Cross Sections",
        "briosa.CloudAndMeshOperations", "EnableDisableCloudCrossSections", "/briosa.CloudAndMeshOperations/EnableDisableCloudCrossSections",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.EnableDisableCloudCrossSectionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CrossSectionCloudName, "cross_section_cloud_name", WorkerObjectTypeValue.CrossSectionCloud), "SetCollectionObjectNameArg2"),
            new("Cross Section ID", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasCrossSectionId ? request.CrossSectionId : 0), "SetIntegerArg"),
            new("Enable (TRUE) / Disable (FALSE)?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnable || request.Enable), "SetBoolArg")
        ], []);
    }

    public static Api.EnableDisableCloudCrossSectionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
