using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class BuildTargetOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.build_target", "'Build' Target", "BuildTarget");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.BuildTargetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Output Target Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.OutputTargetName, "output_target_name"), "SetPointNameArg"),
            new("Nominal Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.NominalPoint, "nominal_point"), "SetPointNameArg"),
            new("Tolerance", WorkerMpValueKind.ToleranceVectorOptions,
                ToleranceVectorOptionsMapper.Required(request.Tolerance, "tolerance"), "SetToleranceVectorOptionsArg")
        };
        if (request.HtmlPromptFile is not null)
            inputs.Add(new("HTML Prompt File (optional)", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.HtmlPromptFile, "html_prompt_file"), "SetFilePathArg"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.BuildTargetResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
