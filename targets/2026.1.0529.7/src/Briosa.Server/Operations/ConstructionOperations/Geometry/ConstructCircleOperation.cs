using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructCircleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_circle", "Construct Circle",
        "briosa.ConstructionOperations", "ConstructCircle", "/briosa.ConstructionOperations/ConstructCircle",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructCircleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Circle Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CircleName, "circle_name", WorkerObjectTypeValue.Circle), "SetCollectionObjectNameArg2"),
            new("Circle Center (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.CircleCenter, "circle_center"), "SetVectorArg"),
            new("Circle Normal (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.CircleNormal, "circle_normal"), "SetVectorArg"),
            new("Circle Radius", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasCircleRadius ? request.CircleRadius : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructCircleResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
