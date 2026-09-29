using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GetGdtOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.get_gdt_options", "Get GD&T Options",
        "briosa.GdtOperations", "GetGdtOptions", "/briosa.GdtOperations/GetGdtOptions",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("options.use_high_points", "Use High Points", WorkerMpValueKind.Logical),
        new("options.extrapolate_axial_extent", "Extrapolate Axial Extent", WorkerMpValueKind.Logical),
        new("options.exclude_from_auto_evaluation", "Exclude From Auto Evaluation", WorkerMpValueKind.Logical),
        new("options.create_actual_features", "Create Actual Features", WorkerMpValueKind.Logical),
        new("options.create_solved_points", "Create Solved Points", WorkerMpValueKind.Logical),
        new("options.cross_section_criteria", "Cross Section Criteria", WorkerMpValueKind.FloatingPoint),
        new("options.enable_auto_feature_detection", "Enable Auto Feature Detection?", WorkerMpValueKind.Logical)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetGdtOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
        [
            new("Use High Points", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Extrapolate Axial Extent", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Exclude From Auto Evaluation", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Create Actual Features", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Create Solved Points", WorkerMpValueKind.Logical, "GetBoolArg"),
            new("Cross Section Criteria", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Enable Auto Feature Detection?", WorkerMpValueKind.Logical, "GetBoolArg")
        ]);
    }

    public static Api.GetGdtOptionsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Options = new Api.GdtOptions
            {
                UseHighPoints = values[0].RequireValue<WorkerBooleanValue>().Value,
                ExtrapolateAxialExtent = values[1].RequireValue<WorkerBooleanValue>().Value,
                ExcludeFromAutoEvaluation = values[2].RequireValue<WorkerBooleanValue>().Value,
                CreateActualFeatures = values[3].RequireValue<WorkerBooleanValue>().Value,
                CreateSolvedPoints = values[4].RequireValue<WorkerBooleanValue>().Value,
                CrossSectionCriteria = values[5].RequireValue<WorkerDoubleValue>().Value,
                EnableAutoFeatureDetection = values[6].RequireValue<WorkerBooleanValue>().Value
            },
            Execution = completed.Details
        };
    }
}
