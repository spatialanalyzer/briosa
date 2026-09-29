using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class SetFeatureCheckMeasurementsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.set_feature_check_measurements", "Set Feature Check Measurements",
        "briosa.GdtOperations", "SetFeatureCheckMeasurements", "/briosa.GdtOperations/SetFeatureCheckMeasurements",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetFeatureCheckMeasurementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Feature Check", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check"), "SetCollectionObjectNameArg2"),
                new("Point Names", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
                new("Cloud Names", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.CloudNames, "cloud_names"), "SetCollectionObjectNameRefListArg"),
                new("Replace Existing Measurements?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReplaceExistingMeasurements && request.ReplaceExistingMeasurements), "SetBoolArg")
            ], []);
    }

    public static Api.SetFeatureCheckMeasurementsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
