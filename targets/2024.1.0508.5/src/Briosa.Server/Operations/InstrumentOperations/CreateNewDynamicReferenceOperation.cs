using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class CreateNewDynamicReferenceOperation
{
    public static OperationDescriptor Descriptor { get; } = InstrumentOperationDescriptor.Mutating(
        "instrument_operations.create_new_dynamic_reference", "Create New Dynamic Reference",
        "CreateNewDynamicReference");
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateNewDynamicReferenceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Instrument ID", WorkerMpValueKind.CollectionInstrumentId,
                CollectionInstrumentIdMapper.Required(request.Instrument, "instrument"), "SetColInstIdArg"),
            new("Points defining Dynamic Reference", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointsDefiningDynamicReference, "points_defining_dynamic_reference"),
                "SetPointNameRefListArg"),
            new("Dynamic Reference Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasDynamicReferenceName ? request.DynamicReferenceName : string.Empty),
                "SetStringArg")
        ], []);
    }

    public static Api.CreateNewDynamicReferenceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
