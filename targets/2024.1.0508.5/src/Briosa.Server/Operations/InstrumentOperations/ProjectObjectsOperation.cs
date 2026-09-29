using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ProjectObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.project_objects", "Project Objects", "briosa.InstrumentOperations",
        "ProjectObjects", "/briosa.InstrumentOperations/ProjectObjects", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["instrument-control", "at-risk-no-runtime-validation"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ProjectObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Objects To Project", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ObjectsToProject, "objects_to_project"),
                "SetCollectionObjectNameRefListArg")
        ], []);
    }

    public static Api.ProjectObjectsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
