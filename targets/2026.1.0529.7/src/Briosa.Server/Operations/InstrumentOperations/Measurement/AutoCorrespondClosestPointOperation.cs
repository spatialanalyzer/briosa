using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AutoCorrespondClosestPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.auto_correspond_closest_point", "Auto-Correspond Closest Point",
        "briosa.InstrumentOperations", "AutoCorrespondClosestPoint",
        "/briosa.InstrumentOperations/AutoCorrespondClosestPoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AutoCorrespondClosestPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                    CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
                new("Reference Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group",
                        WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Actuals Group Name (to be measured)", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ActualsGroup, "actuals_group",
                        WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
                new("Wait for Completion?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasWaitForCompletion || request.WaitForCompletion), "SetBoolArg")
            ], []);
    }

    public static Api.AutoCorrespondClosestPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
