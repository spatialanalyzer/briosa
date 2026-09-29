using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class SetDatumMeasurementsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.set_datum_measurements", "Set Datum Measurements",
        "briosa.GdtOperations", "SetDatumMeasurements", "/briosa.GdtOperations/SetDatumMeasurements",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetDatumMeasurementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Datum", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.Datum, "datum"), "SetCollectionObjectNameArg2"),
                new("Point Names", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
                new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names"), "SetCollectionObjectNameRefListArg"),
                new("Replace Existing Measurements?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReplaceExistingMeasurements && request.ReplaceExistingMeasurements), "SetBoolArg")
            ], []);
    }

    public static Api.SetDatumMeasurementsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
