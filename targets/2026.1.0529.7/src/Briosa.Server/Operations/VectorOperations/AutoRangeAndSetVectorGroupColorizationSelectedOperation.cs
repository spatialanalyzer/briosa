using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.VectorOperations;

internal static class AutoRangeAndSetVectorGroupColorizationSelectedOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "vector_operations.auto_range_and_set_vector_group_colorization_selected",
        "Auto-Range and Set Vector Group Colorization (Selected)", "briosa.VectorOperations",
        "AutoRangeAndSetVectorGroupColorizationSelected",
        "/briosa.VectorOperations/AutoRangeAndSetVectorGroupColorizationSelected",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.AutoRangeAndSetVectorGroupColorizationSelectedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Groups to be Set", WorkerMpValueKind.CollectionVectorGroupNameList,
                CollectionVectorGroupNameListMapper.RequiredList(request.VectorGroupsToBeSet, "vector_groups_to_be_set"),
                "SetCollectionVectorGroupNameRefListArg"),
            new("Treat Individually?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.TreatIndividually), "SetBoolArg"),
            new("Colorization Options (Uses Mode Only)", WorkerMpValueKind.ColorizationOptions,
                ColorizationOptionsMapper.WithDefaults(request.ColorizationOptions), "SetColorizationOptionsArg")
        ], []);
    }
    public static Api.AutoRangeAndSetVectorGroupColorizationSelectedResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
