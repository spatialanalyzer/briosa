using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetPipeRelationshipWeightsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_pipe_relationship_weights", "Get Pipe Relationship Weights",
        "briosa.RelationshipOperations", "GetPipeRelationshipWeights",
        "/briosa.RelationshipOperations/GetPipeRelationshipWeights",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("overall_weight", "Overall Weight", WorkerMpValueKind.FloatingPoint),
        new("axis_offset", "Axis Offset", WorkerMpValueKind.FloatingPoint),
        new("axis_alignment", "Axis Alignment", WorkerMpValueKind.FloatingPoint),
        new("center_pull", "Center Pull", WorkerMpValueKind.FloatingPoint),
        new("out_of_material_weight", "Out of material - Weight", WorkerMpValueKind.FloatingPoint),
        new("out_of_material_static_offset", "Out of material - Static Offset", WorkerMpValueKind.FloatingPoint),
        new("constrain_region_at_od", "Constrain Region at OD", WorkerMpValueKind.Logical),
        new("constrain_id_od_overlap", "Constrain ID/OD overlap", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetPipeRelationshipWeightsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var relationship = CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name");
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName, relationship, "SetCollectionObjectNameArg2")],
            [
                new("Overall Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Axis Offset", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Axis Alignment", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Center Pull", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Out of material - Weight", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Out of material - Static Offset", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Constrain Region at OD", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Constrain ID/OD overlap", WorkerMpValueKind.Logical, "GetBoolArg")
            ]);
    }

    public static Api.GetPipeRelationshipWeightsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            OverallWeight = outputs[0].RequireValue<WorkerDoubleValue>().Value,
            AxisOffset = outputs[1].RequireValue<WorkerDoubleValue>().Value,
            AxisAlignment = outputs[2].RequireValue<WorkerDoubleValue>().Value,
            CenterPull = outputs[3].RequireValue<WorkerDoubleValue>().Value,
            OutOfMaterialWeight = outputs[4].RequireValue<WorkerDoubleValue>().Value,
            OutOfMaterialStaticOffset = outputs[5].RequireValue<WorkerDoubleValue>().Value,
            ConstrainRegionAtOd = outputs[6].RequireValue<WorkerBooleanValue>().Value,
            ConstrainIdOdOverlap = outputs[7].RequireValue<WorkerBooleanValue>().Value,
            Execution = completed.Details
        };
    }
}
