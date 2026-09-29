using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class GetNumberOfCrossSectionsInCrossSectionCloudOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.get_number_of_cross_sections_in_cross_section_cloud", "Get Number of Cross Sections in Cross Section Cloud",
        "briosa.CloudAndMeshOperations", "GetNumberOfCrossSectionsInCrossSectionCloud", "/briosa.CloudAndMeshOperations/GetNumberOfCrossSectionsInCrossSectionCloud",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("cross_section_count", "Cross Section Count", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetNumberOfCrossSectionsInCrossSectionCloudRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName,
            CollectionObjectNameMapper.Required(request.CrossSectionCloudName, "cross_section_cloud_name", WorkerObjectTypeValue.CrossSectionCloud), "SetCollectionObjectNameArg2")],
        [new("Cross Section Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetNumberOfCrossSectionsInCrossSectionCloudResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            Execution = completed.Details,
            CrossSectionCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value
        };
}
