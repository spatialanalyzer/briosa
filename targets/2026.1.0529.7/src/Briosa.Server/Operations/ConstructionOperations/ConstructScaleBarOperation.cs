using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructScaleBarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_scale_bar", "Construct Scale Bar", "briosa.ConstructionOperations", "ConstructScaleBar",
        "/briosa.ConstructionOperations/ConstructScaleBar", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructScaleBarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Scale Bar Name", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.ScaleBarName, "scale_bar_name", WorkerItemTypeValue.ScaleBar), "SetCollectionObjectNameArg2"),
            new("Begin Target", WorkerMpValueKind.PointName, PointNameMapper.Required(request.BeginTarget, "begin_target"), "SetPointNameArg"),
            new("End Target", WorkerMpValueKind.PointName, PointNameMapper.Required(request.EndTarget, "end_target"), "SetPointNameArg"),
            new("Length", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Length), "SetDoubleArg"),
            new("Uncertainty", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Uncertainty), "SetDoubleArg"),
            new("Use Relative Tolerances?", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasUseRelativeTolerances || request.UseRelativeTolerances), "SetBoolArg"),
            new("Use High Tolerances?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.UseHighTolerances), "SetBoolArg"),
            new("Use Low Tolerances?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.UseLowTolerances), "SetBoolArg"),
            new("High Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HighTolerance), "SetDoubleArg"),
            new("Low Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.LowTolerance), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructScaleBarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
