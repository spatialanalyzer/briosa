using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetPipeRelationshipSegmentPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_pipe_relationship_segment_properties", "Set Pipe Relationship Segment Properties",
        "briosa.RelationshipOperations", "SetPipeRelationshipSegmentProperties",
        "/briosa.RelationshipOperations/SetPipeRelationshipSegmentProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPipeRelationshipSegmentPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName, relationship, "SetCollectionObjectNameArg2"),
                new("Pipe 1 - Inner Diameter", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasPipe1InnerDiameter ? request.Pipe1InnerDiameter : 0), "SetDoubleArg"),
                new("Pipe 1 - Outer Diameter", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasPipe1OuterDiameter ? request.Pipe1OuterDiameter : 0), "SetDoubleArg"),
                new("Pipe 1 - Cut Begin", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasPipe1CutBegin ? request.Pipe1CutBegin : 0), "SetDoubleArg"),
                new("Pipe 1 - Cut End", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasPipe1CutEnd ? request.Pipe1CutEnd : 0), "SetDoubleArg"),
                new("Pipe 2 - Inner Diameter", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasPipe2InnerDiameter ? request.Pipe2InnerDiameter : 0), "SetDoubleArg"),
                new("Pipe 2 - Outer Diameter", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasPipe2OuterDiameter ? request.Pipe2OuterDiameter : 0), "SetDoubleArg"),
                new("Pipe 2 - Cut Begin", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasPipe2CutBegin ? request.Pipe2CutBegin : 0), "SetDoubleArg"),
                new("Pipe 2 - Cut End", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasPipe2CutEnd ? request.Pipe2CutEnd : 0), "SetDoubleArg")
            ], []);
    }

    public static Api.SetPipeRelationshipSegmentPropertiesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
