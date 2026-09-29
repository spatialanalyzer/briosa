using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GetFeatureCheckReportingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.get_feature_check_reporting_frame", "Get Feature Check Reporting Frame",
        "briosa.GdtOperations", "GetFeatureCheckReportingFrame", "/briosa.GdtOperations/GetFeatureCheckReportingFrame",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("reporting_frame", "Reporting Frame", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.GetFeatureCheckReportingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck,
                "SetCollectionObjectNameArg2")],
            [new("Reporting Frame", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")]);
    }

    public static Api.GetFeatureCheckReportingFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ReportingFrame = CollectionObjectNameMapper.ToProtocol(
                completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>()),
            Execution = completed.Details
        };
}
