using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class SetGdtOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.set_gdt_options", "Set GD&T Options",
        "briosa.GdtOperations", "SetGdtOptions", "/briosa.GdtOperations/SetGdtOptions",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetGdtOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.HasDistanceBetweenMode || request.DistanceBetweenMode == Api.GdtDistanceBetweenMode.Unspecified ||
            !Enum.IsDefined(request.DistanceBetweenMode))
            throw new ArgumentException("Distance between mode must specify a supported value.", nameof(request));
        if (!request.HasEvaluationMethod || request.EvaluationMethod == Api.GdtEvaluationMethod.Unspecified ||
            !Enum.IsDefined(request.EvaluationMethod))
            throw new ArgumentException("Evaluation method must specify a supported value.", nameof(request));

        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Use High Points", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasUseHighPoints && request.UseHighPoints), "SetBoolArg"),
            new("Extrapolate Axial Extent", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasExtrapolateAxialExtent || request.ExtrapolateAxialExtent), "SetBoolArg"),
            new("Exclude From Auto Evaluation", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasExcludeFromAutoEvaluation || request.ExcludeFromAutoEvaluation), "SetBoolArg"),
            // Both choice literals use SetStringArg in the exact-target live MP check.
            new("Distance Between Mode", WorkerMpValueKind.GdtDistanceBetweenMode,
                new WorkerChoiceValue<WorkerGdtDistanceBetweenModeValue>((WorkerGdtDistanceBetweenModeValue)((int)request.DistanceBetweenMode - 1)), "SetStringArg"),
            new("Evaluation Method", WorkerMpValueKind.GdtEvaluationMethod,
                new WorkerChoiceValue<WorkerGdtEvaluationMethodValue>((WorkerGdtEvaluationMethodValue)((int)request.EvaluationMethod - 1)), "SetStringArg"),
            new("Create Actual Features", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasCreateActualFeatures && request.CreateActualFeatures), "SetBoolArg"),
            new("Create Solved Points", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasCreateSolvedPoints && request.CreateSolvedPoints), "SetBoolArg"),
            new("Cross Section Criteria", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasCrossSectionCriteria ? request.CrossSectionCriteria : 0.039370), "SetDoubleArg"),
            new("Enable Auto Feature Detection?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasEnableAutoFeatureDetection || request.EnableAutoFeatureDetection), "SetBoolArg")
        ], []);
    }

    public static Api.SetGdtOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
