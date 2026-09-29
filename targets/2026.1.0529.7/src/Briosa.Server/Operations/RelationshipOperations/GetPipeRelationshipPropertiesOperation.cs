using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetPipeRelationshipPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_pipe_relationship_properties", "Get Pipe Relationship Properties",
        "briosa.RelationshipOperations", "GetPipeRelationshipProperties",
        "/briosa.RelationshipOperations/GetPipeRelationshipProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("pipe_1_object_name", "Pipe 1 - Object Name", WorkerMpValueKind.CollectionObjectName),
        new("pipe_1_inner_diameter", "Pipe 1 - Inner Diameter", WorkerMpValueKind.FloatingPoint),
        new("pipe_1_outer_diameter", "Pipe 1 - Outer Diameter", WorkerMpValueKind.FloatingPoint),
        new("pipe_1_cut_begin", "Pipe 1 - Cut Begin", WorkerMpValueKind.FloatingPoint),
        new("pipe_1_cut_end", "Pipe 1 - Cut End", WorkerMpValueKind.FloatingPoint),
        new("pipe_2_object_name", "Pipe 2 - Object Name", WorkerMpValueKind.CollectionObjectName),
        new("pipe_2_inner_diameter", "Pipe 2 - Inner Diameter", WorkerMpValueKind.FloatingPoint),
        new("pipe_2_outer_diameter", "Pipe 2 - Outer Diameter", WorkerMpValueKind.FloatingPoint),
        new("pipe_2_cut_begin", "Pipe 2 - Cut Begin", WorkerMpValueKind.FloatingPoint),
        new("pipe_2_cut_end", "Pipe 2 - Cut End", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPipeRelationshipPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName, relationship, "SetCollectionObjectNameArg2")],
            [
                new("Pipe 1 - Object Name", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg"),
                new("Pipe 1 - Inner Diameter", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pipe 1 - Outer Diameter", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pipe 1 - Cut Begin", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pipe 1 - Cut End", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pipe 2 - Object Name", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg"),
                new("Pipe 2 - Inner Diameter", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pipe 2 - Outer Diameter", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pipe 2 - Cut Begin", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Pipe 2 - Cut End", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetPipeRelationshipPropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            Pipe1ObjectName = CollectionObjectNameMapper.ToProtocol(outputs[0].RequireValue<WorkerCollectionObjectNameValue>()),
            Pipe1InnerDiameter = outputs[1].RequireValue<WorkerDoubleValue>().Value,
            Pipe1OuterDiameter = outputs[2].RequireValue<WorkerDoubleValue>().Value,
            Pipe1CutBegin = outputs[3].RequireValue<WorkerDoubleValue>().Value,
            Pipe1CutEnd = outputs[4].RequireValue<WorkerDoubleValue>().Value,
            Pipe2ObjectName = CollectionObjectNameMapper.ToProtocol(outputs[5].RequireValue<WorkerCollectionObjectNameValue>()),
            Pipe2InnerDiameter = outputs[6].RequireValue<WorkerDoubleValue>().Value,
            Pipe2OuterDiameter = outputs[7].RequireValue<WorkerDoubleValue>().Value,
            Pipe2CutBegin = outputs[8].RequireValue<WorkerDoubleValue>().Value,
            Pipe2CutEnd = outputs[9].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
