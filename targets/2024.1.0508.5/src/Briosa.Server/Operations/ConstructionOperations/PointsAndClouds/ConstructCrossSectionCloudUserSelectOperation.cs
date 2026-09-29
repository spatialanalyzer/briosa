using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructCrossSectionCloudUserSelectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_cross_section_cloud_user_select", "Construct Cross Section Cloud - User Select",
        "briosa.ConstructionOperations", "ConstructCrossSectionCloudUserSelect", "/briosa.ConstructionOperations/ConstructCrossSectionCloudUserSelect",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructCrossSectionCloudUserSelectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cross Section Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CrossSectionCloudName, "cross_section_cloud_name", WorkerObjectTypeValue.CrossSectionCloud), "SetCollectionObjectNameArg2"),
            new("Proximity Threshold", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasProximityThreshold ? request.ProximityThreshold : 0), "SetDoubleArg"),
            new("Limit Cross Section Extent", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasLimitCrossSectionExtent && request.LimitCrossSectionExtent), "SetBoolArg"),
            new("Radius Limit", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasRadiusLimit ? request.RadiusLimit : 0), "SetDoubleArg"),
            new("Project to Reference Surface", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasProjectToReferenceSurface && request.ProjectToReferenceSurface), "SetBoolArg"),
            new("Reference Planes", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ReferencePlanes, "reference_planes"), "SetCollectionObjectNameRefListArg"),
            new("Input Clouds", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.InputClouds, "input_clouds"), "SetCollectionObjectNameRefListArg"),
            new("Cloud Thinning Settings", WorkerMpValueKind.CloudThinningOptions,
                CloudThinningOptionsMapper.ToWorker(request.CloudThinningSettings), "SetCloudThinningOptionsArg"),
            new("Update Existing Cloud", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUpdateExistingCloud && request.UpdateExistingCloud), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructCrossSectionCloudUserSelectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
