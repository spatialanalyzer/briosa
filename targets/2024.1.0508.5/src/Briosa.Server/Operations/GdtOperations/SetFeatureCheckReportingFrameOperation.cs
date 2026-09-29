using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class SetFeatureCheckReportingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.set_feature_check_reporting_frame", "Set Feature Check Reporting Frame",
        "briosa.GdtOperations", "SetFeatureCheckReportingFrame", "/briosa.GdtOperations/SetFeatureCheckReportingFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetFeatureCheckReportingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        var frame = CollectionObjectNameMapper.Required(request.ReportingFrame, "reporting_frame");
        if (frame.ObjectType == WorkerObjectTypeValue.Any)
            frame = frame with { ObjectType = WorkerObjectTypeValue.Frame };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck,
                    "SetCollectionObjectNameArg2"),
                new("Reporting Frame", WorkerMpValueKind.CollectionObjectName, frame,
                    "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.SetFeatureCheckReportingFrameResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
