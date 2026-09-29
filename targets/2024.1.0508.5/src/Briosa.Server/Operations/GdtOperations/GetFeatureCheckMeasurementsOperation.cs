using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GetFeatureCheckMeasurementsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.get_feature_check_measurements", "Get Feature Check Measurements",
        "briosa.GdtOperations", "GetFeatureCheckMeasurements", "/briosa.GdtOperations/GetFeatureCheckMeasurements",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("measurements.point_names", "Point Names", WorkerMpValueKind.PointNameList),
        new("measurements.cloud_names", "Cloud Names", WorkerMpValueKind.CollectionObjectNameList)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetFeatureCheckMeasurementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Feature Check", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check"), "SetCollectionObjectNameArg2")],
            [
                new("Point Names", WorkerMpValueKind.PointNameList, "GetPointNameRefListArg"),
                new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")
            ]);
    }

    public static Api.GetFeatureCheckMeasurementsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var measurements = new Api.GdtMeasurements();
        foreach (var value in completed.Execution.OutputValues[0].RequireValue<WorkerPointNameListValue>().Values)
            measurements.PointNames.Add(PointNameMapper.ToProtocol(value));
        foreach (var value in completed.Execution.OutputValues[1].RequireValue<WorkerCollectionObjectNameListValue>().Values)
            measurements.CloudNames.Add(CollectionObjectNameMapper.ToProtocol(value));
        return new() { Measurements = measurements, Execution = completed.Details };
    }
}
