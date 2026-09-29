using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MeasureExistingSinglePointManualGuideOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.measure_existing_single_point_manual_guide", "Measure Existing Single Point (Manual Guide)",
        "briosa.InstrumentOperations", "MeasureExistingSinglePointManualGuide",
        "/briosa.InstrumentOperations/MeasureExistingSinglePointManualGuide",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.MeasureExistingSinglePointManualGuideRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Existing Target ID", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ExistingTargetId, "existing_target_id"), "SetPointNameArg"),
            new("Group name for new point", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupNameForNewPoint, "group_name_for_new_point",
                    WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Measure Immediately", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.MeasureImmediately), "SetBoolArg")
        };
        if (request.HtmlPromptFile is not null)
        {
            inputs.Add(new("HTML Prompt File (optional)", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.HtmlPromptFile, "html_prompt_file"), "SetFilePathArg"));
        }

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs,
            [new("Resulting Point Name", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.MeasureExistingSinglePointManualGuideResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ResultingPointName = PointNameMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerPointNameValue>()),
        Execution = completed.Details
    };
}
