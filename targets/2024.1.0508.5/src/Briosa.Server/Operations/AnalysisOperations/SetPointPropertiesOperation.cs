using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetPointPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_point_properties", "Set Point Properties",
        "briosa.AnalysisOperations", "SetPointProperties", "/briosa.AnalysisOperations/SetPointProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Name List", WorkerMpValueKind.PointNameList,
                    PointNameMapper.RequiredList(request.PointNameList, "point_name_list"), "SetPointNameRefListArg"),
                new("Planar Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasPlanarOffset ? request.PlanarOffset : 0d), "SetDoubleArg"),
                new("Radial Offset", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasRadialOffset ? request.RadialOffset : 0d), "SetDoubleArg"),
                new("Position Tolerance", WorkerMpValueKind.ToleranceVectorOptions,
                    ToleranceVectorOptionsMapper.Required(request.PositionTolerance, "position_tolerance"),
                    "SetToleranceVectorOptionsArg"),
                new("Component Weights", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.ComponentWeights, "component_weights"), "SetVectorArg")
            ], []);
    }

    public static Api.SetPointPropertiesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
