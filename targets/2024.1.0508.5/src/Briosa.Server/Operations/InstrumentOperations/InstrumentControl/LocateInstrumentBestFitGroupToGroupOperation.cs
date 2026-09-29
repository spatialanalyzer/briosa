using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LocateInstrumentBestFitGroupToGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.locate_instrument_best_fit_group_to_group", "Locate Instrument (Best Fit - Group to Group)",
        "briosa.InstrumentOperations", "LocateInstrumentBestFitGroupToGroup", "/briosa.InstrumentOperations/LocateInstrumentBestFitGroupToGroup",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("transform_in_working", "Transform in Working", WorkerMpValueKind.Transform),
        new("optimum_transform", "Optimum Transform", WorkerMpValueKind.WorldTransform),
        new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint),
        new("maximum_absolute_deviation", "Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint),
        new("number_of_unknowns", "Number of Unknowns", WorkerMpValueKind.WholeNumber),
        new("number_of_equations", "Number of Equations", WorkerMpValueKind.WholeNumber),
        new("robustness", "Robustness", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.LocateInstrumentBestFitGroupToGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Reference Group", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Corresponding Group", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CorrespondingGroup, "corresponding_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Show Interface", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ShowInterface), "SetBoolArg"),
            new("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.RmsTolerance), "SetDoubleArg"),
            new("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.MaximumAbsoluteTolerance), "SetDoubleArg"),
            new("Allow Scale", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.AllowScale), "SetBoolArg"),
            new("Allow X", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAllowX ? request.AllowX : true), "SetBoolArg"),
            new("Allow Y", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAllowY ? request.AllowY : true), "SetBoolArg"),
            new("Allow Z", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAllowZ ? request.AllowZ : true), "SetBoolArg"),
            new("Allow Rx", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAllowRx ? request.AllowRx : true), "SetBoolArg"),
            new("Allow Ry", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAllowRy ? request.AllowRy : true), "SetBoolArg"),
            new("Allow Rz", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasAllowRz ? request.AllowRz : true), "SetBoolArg"),
            new("Lock Degrees of Freedom", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.LockDegreesOfFreedom), "SetBoolArg"),
            new("Generate Event", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.GenerateEvent), "SetBoolArg")
        };
        if (request.CsvReport is not null)
        {
            inputs.Add(new("File Path for CSV Text Report (requires Show Interface = TRUE)", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.CsvReport, "csv_report"), "SetFilePathArg"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs,
        [
            new("Transform in Working", WorkerMpValueKind.Transform, "GetTransformArg"),
            new("Optimum Transform", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg"),
            new("RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Number of Unknowns", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Number of Equations", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Robustness", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.LocateInstrumentBestFitGroupToGroupResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        var optimumTransform = values[1].RequireValue<WorkerWorldTransformValue>();
        return new()
        {
            TransformInWorking = TransformMapper.ToProtocol(values[0].RequireValue<WorkerTransformValue>()),
            OptimumTransform = new Api.WorldTransform
            {
                Transform = TransformMapper.ToProtocol(optimumTransform.Transform),
                ScaleFactor = optimumTransform.ScaleFactor
            },
            RmsDeviation = values[2].RequireValue<WorkerDoubleValue>().Value,
            MaximumAbsoluteDeviation = values[3].RequireValue<WorkerDoubleValue>().Value,
            NumberOfUnknowns = values[4].RequireValue<WorkerIntegerValue>().Value,
            NumberOfEquations = values[5].RequireValue<WorkerIntegerValue>().Value,
            Robustness = values[6].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
