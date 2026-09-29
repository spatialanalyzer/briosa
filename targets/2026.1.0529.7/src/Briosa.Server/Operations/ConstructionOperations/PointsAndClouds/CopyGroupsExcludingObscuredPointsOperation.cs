using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CopyGroupsExcludingObscuredPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.copy_groups_excluding_obscured_points", "Copy Groups Excluding Obscured Points",
        "briosa.ConstructionOperations", "CopyGroupsExcludingObscuredPoints", "/briosa.ConstructionOperations/CopyGroupsExcludingObscuredPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CopyGroupsExcludingObscuredPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var instrument = request.InstrumentId;
        if (instrument is null || string.IsNullOrWhiteSpace(instrument.CollectionName))
            throw new ArgumentException("Request field 'instrument_id' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                new WorkerCollectionInstrumentIdValue(instrument.CollectionName, instrument.InstrumentId), "SetColInstIdArg"),
            new("Group Names", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.GroupNames, "group_names"), "SetCollectionObjectNameRefListArg"),
            new("New Collection Name", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.NewCollectionName, "new_collection_name"), "SetCollectionNameArg")
        ], []);
    }

    public static Api.CopyGroupsExcludingObscuredPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
