using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MeasureSinglePointHereOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.measure_single_point_here", "Measure Single Point Here",
        "briosa.InstrumentOperations", "MeasureSinglePointHere",
        "/briosa.InstrumentOperations/MeasureSinglePointHere",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MeasureSinglePointHereRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Target ID", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.TargetId, "target_id"), "SetPointNameArg"),
            new("Measure Immediately", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasMeasureImmediately || request.MeasureImmediately), "SetBoolArg")
        };
        if (request.HtmlPromptFile is not null)
        {
            inputs.Add(new("HTML Prompt File (optional)", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.HtmlPromptFile, "html_prompt_file"), "SetFilePathArg"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.MeasureSinglePointHereResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
