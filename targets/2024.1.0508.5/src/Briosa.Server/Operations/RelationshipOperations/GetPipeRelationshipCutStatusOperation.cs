using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetPipeRelationshipCutStatusOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_pipe_relationship_cut_status",
        "Get Pipe Relationship Cut Status",
        "briosa.RelationshipOperations", "GetPipeRelationshipCutStatus",
        "/briosa.RelationshipOperations/GetPipeRelationshipCutStatus",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("pipe_1_cut_available", "Pipe 1 - Cut Available?", WorkerMpValueKind.Logical),
        new("pipe_1_cut_active", "Pipe 1 - Cut Active?", WorkerMpValueKind.Logical),
        new("pipe_2_cut_available", "Pipe 2 - Cut Available?", WorkerMpValueKind.Logical),
        new("pipe_2_cut_active", "Pipe 2 - Cut Active?", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPipeRelationshipCutStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName, relationship, "SetCollectionObjectNameArg2")],
            [
                new("Pipe 1 - Cut Available?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Pipe 1 - Cut Active?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Pipe 2 - Cut Available?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Pipe 2 - Cut Active?", WorkerMpValueKind.Logical, "GetBoolArg")
            ]);
    }

    public static Api.GetPipeRelationshipCutStatusResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            Pipe1CutAvailable = outputs[0].RequireValue<WorkerBooleanValue>().Value,
            Pipe1CutActive = outputs[1].RequireValue<WorkerBooleanValue>().Value,
            Pipe2CutAvailable = outputs[2].RequireValue<WorkerBooleanValue>().Value,
            Pipe2CutActive = outputs[3].RequireValue<WorkerBooleanValue>().Value,
            Execution = completed.Details
        };
    }
}
