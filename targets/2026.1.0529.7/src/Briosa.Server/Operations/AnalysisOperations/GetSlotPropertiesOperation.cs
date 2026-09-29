using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetSlotPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_slot_properties", "Get Slot Properties",
        "briosa.AnalysisOperations", "GetSlotProperties", "/briosa.AnalysisOperations/GetSlotProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("slot_transform", "Slot Transform (in working coordinates", WorkerMpValueKind.Transform),
        new("center", "Center (in working coordinates)", WorkerMpValueKind.Vector),
        new("normal_direction", "Normal Direction (in working coordinates)", WorkerMpValueKind.Vector),
        new("slot_length", "Slot Length", WorkerMpValueKind.FloatingPoint),
        new("slot_width", "Slot Width", WorkerMpValueKind.FloatingPoint),
        new("round_slot_type", "Round Slot Type", WorkerMpValueKind.Logical),
        new("centerline_pt_1", "Centerline Pt. 1 (in working coordinates)", WorkerMpValueKind.Vector),
        new("centerline_pt_2", "Centerline Pt. 2 (in working coordinates)", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetSlotPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Slot Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SlotName, "slot_name"), "SetCollectionObjectNameArg2")],
            [
                new("Slot Transform (in working coordinates", WorkerMpValueKind.Transform, "GetTransformArg"),
                new("Center (in working coordinates)", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Normal Direction (in working coordinates)", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Slot Length", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Slot Width", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Round Slot Type", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Centerline Pt. 1 (in working coordinates)", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Centerline Pt. 2 (in working coordinates)", WorkerMpValueKind.Vector, "GetVectorArg")
            ]);
    }

    public static Api.GetSlotPropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            SlotTransform = TransformMapper.ToProtocol(values[0].RequireValue<WorkerTransformValue>()),
            Center = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            NormalDirection = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
            SlotLength = values[3].RequireValue<WorkerDoubleValue>().Value,
            SlotWidth = values[4].RequireValue<WorkerDoubleValue>().Value,
            RoundSlotType = values[5].RequireValue<WorkerBooleanValue>().Value,
            CenterlinePt1 = VectorMapper.ToProtocol(values[6].RequireValue<WorkerVectorValue>()),
            CenterlinePt2 = VectorMapper.ToProtocol(values[7].RequireValue<WorkerVectorValue>()),
            Execution = completed.Details
        };
    }
}
