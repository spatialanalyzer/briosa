using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class PointAtTargetOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.point_at_target", "Point At Target", "PointAtTarget");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.PointAtTargetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Target ID", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.TargetId, "target_id"), "SetPointNameArg")
        };
        if (request.HtmlPromptFile is not null)
            inputs.Add(new("HTML Prompt File (optional)", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.HtmlPromptFile, "html_prompt_file"), "SetFilePathArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.PointAtTargetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
