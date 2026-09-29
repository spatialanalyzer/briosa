using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GroupToSurfaceFitOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.group_to_surface_fit", "Group To Surface Fit",
        "briosa.AnalysisOperations", "GroupToSurfaceFit", "/briosa.AnalysisOperations/GroupToSurfaceFit",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("optimum_transform", "Optimum Transform", WorkerMpValueKind.WorldTransform),
        new("rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint),
        new("maximum_absolute_deviation", "Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GroupToSurfaceFitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Group to Fit", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.GroupToFit, "group_to_fit"), "SetCollectionObjectNameArg2"),
                new("Surface", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Surface, "surface"), "SetCollectionObjectNameArg2"),
                new("Do Conventional Fit", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasDoConventionalFit && request.DoConventionalFit), "SetBoolArg"),
                new("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasRmsTolerance ? request.RmsTolerance : 0d), "SetDoubleArg"),
                new("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasMaximumAbsoluteTolerance ? request.MaximumAbsoluteTolerance : 0d), "SetDoubleArg")
            ],
            [
                new("Optimum Transform", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg"),
                new("RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GroupToSurfaceFitResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            OptimumTransform = WorldTransformMapper.ToProtocol(values[0].RequireValue<WorkerWorldTransformValue>()),
            RmsDeviation = values[1].RequireValue<WorkerDoubleValue>().Value,
            MaximumAbsoluteDeviation = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
