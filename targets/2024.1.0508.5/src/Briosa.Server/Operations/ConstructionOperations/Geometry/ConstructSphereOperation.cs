using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructSphereOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_sphere", "Construct Sphere",
        "briosa.ConstructionOperations", "ConstructSphere", "/briosa.ConstructionOperations/ConstructSphere",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructSphereRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Sphere Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SphereName, "sphere_name", WorkerObjectTypeValue.Sphere), "SetCollectionObjectNameArg2"),
            new("Sphere Center (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.SphereCenter, "sphere_center"), "SetVectorArg"),
            new("Sphere Radius", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSphereRadius ? request.SphereRadius : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructSphereResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
