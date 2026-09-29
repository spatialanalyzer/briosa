using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class SetVectorGroupColorizationOptionsSelectedOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.set_vector_group_colorization_options_selected",
        "Set Vector Group Colorization Options (Selected)", "briosa.VectorOperations",
        "SetVectorGroupColorizationOptionsSelected",
        "/briosa.VectorOperations/SetVectorGroupColorizationOptionsSelected",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.SetVectorGroupColorizationOptionsSelectedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Groups to be Set", WorkerMpValueKind.CollectionVectorGroupNameList,
                CollectionVectorGroupNameListMapper.RequiredList(request.VectorGroupsToBeSet, "vector_groups_to_be_set"),
                "SetCollectionVectorGroupNameRefListArg"),
            new("Colorization Options", WorkerMpValueKind.ColorizationOptions,
                ColorizationOptionsMapper.WithDefaults(request.ColorizationOptions), "SetColorizationOptionsArg")
        ], []);
    }
    public static Api.SetVectorGroupColorizationOptionsSelectedResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
