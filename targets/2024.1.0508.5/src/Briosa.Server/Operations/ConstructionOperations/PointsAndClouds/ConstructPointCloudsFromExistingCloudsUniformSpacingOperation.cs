using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointCloudsFromExistingCloudsUniformSpacingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_clouds_from_existing_clouds_uniform_spacing", "Construct Point Clouds from Existing Clouds - Uniform Spacing",
        "briosa.ConstructionOperations", "ConstructPointCloudsFromExistingCloudsUniformSpacing", "/briosa.ConstructionOperations/ConstructPointCloudsFromExistingCloudsUniformSpacing",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointCloudsFromExistingCloudsUniformSpacingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Existing Point Cloud List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ExistingPointCloudList, "existing_point_cloud_list"), "SetCollectionObjectNameRefListArg"),
            new("Desired Point Spacing", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasDesiredPointSpacing ? request.DesiredPointSpacing : 0.02), "SetDoubleArg"),
            new("Minimum Points Per Output Point", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasMinimumPointsPerOutputPoint ? request.MinimumPointsPerOutputPoint : 3), "SetIntegerArg"),
            new("New Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NewCloudName, "new_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Hide Original Point Clouds", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasHideOriginalPointClouds || request.HideOriginalPointClouds), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructPointCloudsFromExistingCloudsUniformSpacingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
