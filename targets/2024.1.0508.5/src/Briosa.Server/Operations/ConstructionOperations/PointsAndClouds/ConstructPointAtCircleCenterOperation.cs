using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtCircleCenterOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_circle_center", "Construct a Point at Circle Center",
        "briosa.ConstructionOperations", "ConstructPointAtCircleCenter", "/briosa.ConstructionOperations/ConstructPointAtCircleCenter",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtCircleCenterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Circle Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CircleName, "circle_name", WorkerObjectTypeValue.Circle), "SetCollectionObjectNameArg2"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointAtCircleCenterResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
