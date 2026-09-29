using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructVectorGroupAreaProfileCheckOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_vector_group_area_profile_check", "Construct a Vector Group - Area Profile Check",
        "briosa.ConstructionOperations", "ConstructVectorGroupAreaProfileCheck",
        "/briosa.ConstructionOperations/ConstructVectorGroupAreaProfileCheck", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructVectorGroupAreaProfileCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ResultantVectorGroupName is null || string.IsNullOrWhiteSpace(request.ResultantVectorGroupName.VectorGroupName))
            throw new ArgumentException("Resultant vector group name is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Reference Vectors", WorkerMpValueKind.VectorNameList,
                VectorNameMapper.RequiredList(request.ReferenceVectors, "reference_vectors"), "SetVectorNameRefListArg"),
            new("Vector Groups to Check", WorkerMpValueKind.CollectionVectorGroupNameList,
                CollectionVectorGroupNameListMapper.RequiredList(request.VectorGroupsToCheck, "vector_groups_to_check"), "SetCollectionVectorGroupNameRefListArg"),
            new("Area Radius", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.AreaRadius), "SetDoubleArg"),
            new("Area Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.AreaTolerance), "SetDoubleArg"),
            new("Resultant Vector Group Name", WorkerMpValueKind.CollectionVectorGroupName,
                new WorkerCollectionVectorGroupNameValue(request.ResultantVectorGroupName.CollectionName,
                    request.ResultantVectorGroupName.VectorGroupName), "SetColVectorGroupNameArg")
        ], []);
    }

    public static Api.ConstructVectorGroupAreaProfileCheckResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
