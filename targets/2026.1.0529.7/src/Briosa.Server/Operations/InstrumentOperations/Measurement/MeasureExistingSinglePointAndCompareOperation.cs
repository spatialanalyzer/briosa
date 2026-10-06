using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class MeasureExistingSinglePointAndCompareOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.measure_existing_single_point_and_compare", "Measure Existing Single Point and Compare",
        "briosa.InstrumentOperations", "MeasureExistingSinglePointAndCompare",
        "/briosa.InstrumentOperations/MeasureExistingSinglePointAndCompare",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("vector_representation", "Vector Representation", WorkerMpValueKind.Vector),
        new("x_value", "X Value", WorkerMpValueKind.FloatingPoint),
        new("y_value", "Y Value", WorkerMpValueKind.FloatingPoint),
        new("z_value", "Z Value", WorkerMpValueKind.FloatingPoint),
        new("magnitude", "Magnitude", WorkerMpValueKind.FloatingPoint),
        new("resulting_point_name", "Resulting Point Name", WorkerMpValueKind.PointName)
    ];

    public static WorkerMpCommand CreateCommand(Api.MeasureExistingSinglePointAndCompareRequest request)
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
                new WorkerBooleanValue(!request.HasMeasureImmediately || request.MeasureImmediately), "SetBoolArg")
        };
        if (request.HtmlPromptFile is not null)
        {
            inputs.Add(new("HTML Prompt File (optional)", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.HtmlPromptFile, "html_prompt_file"), "SetFilePathArg"));
        }
        inputs.Add(new("Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
            new WorkerDoubleValue(request.Tolerance), "SetDoubleArg"));

        return new(Descriptor.OperationId, Descriptor.MpStep, inputs,
        [
            new("Vector Representation", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("X Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Y Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Z Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Magnitude", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Resulting Point Name", WorkerMpValueKind.PointName, "GetPointNameArg")
        ]);
    }

    public static Api.MeasureExistingSinglePointAndCompareResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            VectorRepresentation = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            XValue = values[1].RequireValue<WorkerDoubleValue>().Value,
            YValue = values[2].RequireValue<WorkerDoubleValue>().Value,
            ZValue = values[3].RequireValue<WorkerDoubleValue>().Value,
            Magnitude = values[4].RequireValue<WorkerDoubleValue>().Value,
            ResultingPointName = PointNameMapper.ToProtocol(values[5].RequireValue<WorkerPointNameValue>()),
            Execution = completed.Details
        };
    }
}
