using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GetFeatureCheckReportingOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.get_feature_check_reporting_options", "Get Feature Check Reporting Options",
        "briosa.GdtOperations", "GetFeatureCheckReportingOptions", "/briosa.GdtOperations/GetFeatureCheckReportingOptions",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("options.show_feature_control_frame_summary", "Show Feature Control Frame Summary?", WorkerMpValueKind.Logical),
        new("options.include_title", "Include Title?", WorkerMpValueKind.Logical),
        new("options.show_datum_and_tolerance_summary", "Show Datum and Tolerance Summary?", WorkerMpValueKind.Logical),
        new("options.show_feature_summary", "Show Feature Summary?", WorkerMpValueKind.Logical),
        new("options.show_point_details", "Show Point Details Table?", WorkerMpValueKind.Logical),
        new("options.show_lower_tier_tables", "Show Lower Tier Tables?", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetFeatureCheckReportingOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var featureCheck = CollectionItemNameMapper.Required(request.FeatureCheck, "feature_check");
        if (featureCheck.ItemType == WorkerItemTypeValue.Any)
            featureCheck = featureCheck with { ItemType = WorkerItemTypeValue.FeatureCheck };
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Feature Check", WorkerMpValueKind.CollectionItemName, featureCheck, "SetCollectionObjectNameArg2")],
            [
                new("Show Feature Control Frame Summary?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Include Title?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Show Datum and Tolerance Summary?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Show Feature Summary?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Show Point Details Table?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Show Lower Tier Tables?", WorkerMpValueKind.Logical, "GetBoolArg")
            ]);
    }

    public static Api.GetFeatureCheckReportingOptionsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Options = new Api.FeatureCheckReportingOptions
            {
                ShowFeatureControlFrameSummary = values[0].RequireValue<WorkerBooleanValue>().Value,
                IncludeTitle = values[1].RequireValue<WorkerBooleanValue>().Value,
                ShowDatumAndToleranceSummary = values[2].RequireValue<WorkerBooleanValue>().Value,
                ShowFeatureSummary = values[3].RequireValue<WorkerBooleanValue>().Value,
                ShowPointDetails = values[4].RequireValue<WorkerBooleanValue>().Value,
                ShowLowerTierTables = values[5].RequireValue<WorkerBooleanValue>().Value
            },
            Execution = completed.Details
        };
    }
}
