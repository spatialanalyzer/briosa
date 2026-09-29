using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsSubsetWithGreatestSpacingOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_subset_with_greatest_spacing", "Construct Points Subset with greatest spacing",
        "briosa.ConstructionOperations", "ConstructPointsSubsetWithGreatestSpacing", "/briosa.ConstructionOperations/ConstructPointsSubsetWithGreatestSpacing",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsSubsetWithGreatestSpacingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Points to Subsample", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointsToSubsample, "points_to_subsample"), "SetPointNameRefListArg"),
            new("Subset Size", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasSubsetSize ? request.SubsetSize : 10), "SetIntegerArg"),
            new("Group for Subset", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupForSubset, "group_for_subset", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2")
        ], []);
    }

    public static Api.ConstructPointsSubsetWithGreatestSpacingResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
