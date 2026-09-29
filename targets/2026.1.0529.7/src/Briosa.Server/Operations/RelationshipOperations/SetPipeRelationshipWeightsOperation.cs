using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetPipeRelationshipWeightsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_pipe_relationship_weights", "Set Pipe Relationship Weights",
        "briosa.RelationshipOperations", "SetPipeRelationshipWeights",
        "/briosa.RelationshipOperations/SetPipeRelationshipWeights",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPipeRelationshipWeightsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName, relationship, "SetCollectionObjectNameArg2"),
                new("Overall Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasOverallWeight ? request.OverallWeight : 1), "SetDoubleArg"),
                new("Axis Offset", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasAxisOffset ? request.AxisOffset : 2), "SetDoubleArg"),
                new("Axis Alignment", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasAxisAlignment ? request.AxisAlignment : 1), "SetDoubleArg"),
                new("Center Pull", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasCenterPull ? request.CenterPull : 0.1), "SetDoubleArg"),
                new("Out of material - Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasOutOfMaterialWeight ? request.OutOfMaterialWeight : 10), "SetDoubleArg"),
                new("Out of material - Offset", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HasOutOfMaterialOffset ? request.OutOfMaterialOffset : 1), "SetDoubleArg"),
                new("Constrain Region at OD", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasConstrainRegionAtOd && request.ConstrainRegionAtOd), "SetBoolArg"),
                new("Constrain ID/OD overlap", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasConstrainIdOdOverlap && request.ConstrainIdOdOverlap), "SetBoolArg")
            ], []);
    }

    public static Api.SetPipeRelationshipWeightsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
