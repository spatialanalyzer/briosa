using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetCirclePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_circle_properties", "Set Circle Properties",
        "briosa.AnalysisOperations", "SetCircleProperties", "/briosa.AnalysisOperations/SetCircleProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCirclePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Circle Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CircleName, "circle_name"), "SetCollectionObjectNameArg2"),
                new("Center Coordinate", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.CenterCoordinate, "center_coordinate"), "SetVectorArg"),
                new("Normal Direction", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.NormalDirection, "normal_direction"), "SetVectorArg"),
                new("Radius", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasRadius ? request.Radius : 0d), "SetDoubleArg")
            ], []);
    }

    public static Api.SetCirclePropertiesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
