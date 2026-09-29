using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LocateInstrumentGroupToSurfaceQuickFitOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.locate_instrument_group_to_surface_quick_fit", "Locate Instrument (Group to Surface Quick Fit)",
        "briosa.InstrumentOperations", "LocateInstrumentGroupToSurfaceQuickFit", "/briosa.InstrumentOperations/LocateInstrumentGroupToSurfaceQuickFit",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("rms_error", "RMS Error", WorkerMpValueKind.FloatingPoint),
        new("maximum_absolute_error", "Maximum Absolute Error", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.LocateInstrumentGroupToSurfaceQuickFitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument to Locate", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Name of Measured Group", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.MeasuredGroup, "measured_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Name of Group containing Surface Pts", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SurfacePointsGroup, "surface_points_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Surface to fit", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SurfaceToFit, "surface_to_fit", WorkerObjectTypeValue.Surface),
                "SetCollectionObjectNameArg2"),
            new("Other Objects to Transform", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.OtherObjectsToTransform, "other_objects_to_transform"),
                "SetCollectionObjectNameRefListArg"),
            new("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.RmsTolerance), "SetDoubleArg"),
            new("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.MaximumAbsoluteTolerance), "SetDoubleArg")
        ],
        [
            new("RMS Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Maximum Absolute Error", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.LocateInstrumentGroupToSurfaceQuickFitResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            RmsError = values[0].RequireValue<WorkerDoubleValue>().Value,
            MaximumAbsoluteError = values[1].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
