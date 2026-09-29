using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipProjectionOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_projection_options", "Get Relationship Projection Options",
        "briosa.RelationshipOperations", "GetRelationshipProjectionOptions",
        "/briosa.RelationshipOperations/GetRelationshipProjectionOptions",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("ignore_edge_projections", "Ignore Edge Projections?", WorkerMpValueKind.Logical),
        new("probe_offsets_override_target_values", "Probe Offsets - Override Target Values?", WorkerMpValueKind.Logical),
        new("probe_offsets_override_value", "Probe Offsets - Override Value", WorkerMpValueKind.FloatingPoint),
        new("add_extra_material", "Add Extra Material?", WorkerMpValueKind.Logical),
        new("extra_material_thickness", "Extra Material Thickness", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipProjectionOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [
                new("Ignore Edge Projections?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Probe Offsets - Override Target Values?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Probe Offsets - Override Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Add Extra Material?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Extra Material Thickness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetRelationshipProjectionOptionsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            IgnoreEdgeProjections = outputs[0].RequireValue<WorkerBooleanValue>().Value,
            ProbeOffsetsOverrideTargetValues = outputs[1].RequireValue<WorkerBooleanValue>().Value,
            ProbeOffsetsOverrideValue = outputs[2].RequireValue<WorkerDoubleValue>().Value,
            AddExtraMaterial = outputs[3].RequireValue<WorkerBooleanValue>().Value,
            ExtraMaterialThickness = outputs[4].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
