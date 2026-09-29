using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class SetFeatureCheckReportingOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.set_feature_check_reporting_options", "Set Feature Check Reporting Options",
        "briosa.GdtOperations", "SetFeatureCheckReportingOptions", "/briosa.GdtOperations/SetFeatureCheckReportingOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetFeatureCheckReportingOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2"),
                new("Show Feature Control Frame Summary?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowFeatureControlFrameSummary ? request.ShowFeatureControlFrameSummary : true), "SetBoolArg"),
                new("Include Title?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasIncludeTitle && request.IncludeTitle), "SetBoolArg"),
                new("Show Datum and Tolerance Summary?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowDatumAndToleranceSummary && request.ShowDatumAndToleranceSummary), "SetBoolArg"),
                new("Show Feature Summary?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowFeatureSummary && request.ShowFeatureSummary), "SetBoolArg"),
                new("Show Point Details Summary?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowPointDetailsSummary && request.ShowPointDetailsSummary), "SetBoolArg"),
                new("Show Lower Tier Tables?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasShowLowerTierTables && request.ShowLowerTierTables), "SetBoolArg")
            ], []);
    }

    public static Api.SetFeatureCheckReportingOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
