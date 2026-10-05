using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AutoMeasurePointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.auto_measure_points", "Auto Measure Points",
        "briosa.InstrumentOperations", "AutoMeasurePoints", "/briosa.InstrumentOperations/AutoMeasurePoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoMeasurePointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                InstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Reference Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group",
                    WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ActualsGroup, "actuals_group",
                    WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Force use of existing group?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ForceExistingGroup), "SetBoolArg"),
            new("Show complete dialog?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowCompleteDialog), "SetBoolArg"),
            new("Wait for Completion?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasWaitForCompletion ? request.WaitForCompletion : true), "SetBoolArg"),
            new("Auto Start?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAutoStart || request.AutoStart), "SetBoolArg")
        ], []);
    }

    public static Api.AutoMeasurePointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
