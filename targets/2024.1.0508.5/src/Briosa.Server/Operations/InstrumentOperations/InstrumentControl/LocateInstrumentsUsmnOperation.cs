using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class LocateInstrumentsUsmnOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.locate_instruments_usmn", "Locate Instruments (USMN)",
        "briosa.InstrumentOperations", "LocateInstrumentsUsmn", "/briosa.InstrumentOperations/LocateInstrumentsUsmn",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("rms_error", "RMS Error Value", WorkerMpValueKind.FloatingPoint),
        new("maximum_error", "Max Error Value", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.LocateInstrumentsUsmnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var showDialog = request.HasShowUsmnDialog
            ? MapShowUsmnDialog(request.ShowUsmnDialog)
            : throw new ArgumentException("Request field 'show_usmn_dialog' is required.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instruments to Locate", WorkerMpValueKind.CollectionInstrumentIdList,
                InstrumentIdMapper.RequiredList(request.Instruments, "instruments"), "SetColInstIdRefListArg"),
            new("Nominals Group Name (blank for none)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.NominalsGroup, "nominals_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Output Group Name (to be established)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.OutputGroup, "output_group", WorkerObjectTypeValue.PointGroup),
                "SetCollectionObjectNameArg2"),
            new("Move In Working Frame (TRUE) or Instrument Frame (FALSE)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.MoveInWorkingFrame), "SetBoolArg"),
            new("AutoReject Outliers and Resolve", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.AutoRejectOutliersAndResolve), "SetBoolArg"),
            new("Show USMN Dialog", WorkerMpValueKind.ShowUsmnDialogType,
                new WorkerChoiceValue<WorkerShowUsmnDialogTypeValue>(showDialog), "SetShowUsmnDialogTypeArg"),
            new("Max Acceptable RMS Error Value (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.MaximumAcceptableRmsError), "SetDoubleArg"),
            new("Max Acceptable Error Value (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.MaximumAcceptableError), "SetDoubleArg"),
            new("Groups to be Excluded", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ExcludedGroups, "excluded_groups"),
                "SetCollectionObjectNameRefListArg"),
            new("Exclude Points Measured By Only One Instrument", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ExcludeSingleInstrumentPoints), "SetBoolArg"),
            new("Run Uncertainty Field Analysis?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.RunUncertaintyFieldAnalysis), "SetBoolArg"),
            new("Analysis Samples", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasAnalysisSamples ? request.AnalysisSamples : 300), "SetIntegerArg"),
            new("Analysis Time Limit (Minutes - 0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasAnalysisTimeLimit ? request.AnalysisTimeLimit : 4.0), "SetDoubleArg")
        ],
        [
            new("RMS Error Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Max Error Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    private static WorkerShowUsmnDialogTypeValue MapShowUsmnDialog(Api.ShowUsmnDialog value) => value switch
    {
        Api.ShowUsmnDialog.No => WorkerShowUsmnDialogTypeValue.No,
        Api.ShowUsmnDialog.Yes => WorkerShowUsmnDialogTypeValue.Yes,
        Api.ShowUsmnDialog.OnToleranceViolation => WorkerShowUsmnDialogTypeValue.OnToleranceViolation,
        _ => throw new ArgumentException("Request field 'show_usmn_dialog' has an unsupported value.", nameof(value))
    };

    public static Api.LocateInstrumentsUsmnResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            RmsError = values[0].RequireValue<WorkerDoubleValue>().Value,
            MaximumError = values[1].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
