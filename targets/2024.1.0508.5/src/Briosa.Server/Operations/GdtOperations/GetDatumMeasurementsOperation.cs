using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GetDatumMeasurementsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.get_datum_measurements", "Get Datum Measurements",
        "briosa.GdtOperations", "GetDatumMeasurements", "/briosa.GdtOperations/GetDatumMeasurements",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("measurements.point_names", "Point Names", WorkerMpValueKind.PointNameList),
        new("measurements.cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetDatumMeasurementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Datum", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.Datum, "datum"), "SetCollectionObjectNameArg2")],
            [
                new("Point Names", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg"),
                new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")
            ]);
    }

    public static Api.GetDatumMeasurementsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var measurements = new Api.GdtMeasurements();
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
            measurements.PointNames.Add(PointNameMapper.ToProtocol(value));
        foreach (var value in completed.Execution.OutputValues[1].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            measurements.CloudNames.Add(CollectionObjectNameMapper.ToProtocol(value));
        return new() { Measurements = measurements, Execution = completed.Details };
    }
}
