using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class AngleBetweenTwoPlanesNormalsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.angle_between_two_planes_normals", "Angle Between Two Planes’ normals",
        "briosa.AnalysisOperations", "AngleBetweenTwoPlanesNormals", "/briosa.AnalysisOperations/AngleBetweenTwoPlanesNormals",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("angle", "Angle", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.AngleBetweenTwoPlanesNormalsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Plane A", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.PlaneA, "plane_a"), "SetCollectionObjectNameArg2"),
                new("Plane B", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.PlaneB, "plane_b"), "SetCollectionObjectNameArg2"),
                new("Nominal Angle", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.NominalAngle), "SetDoubleArg"),
                new("Angle Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.AngleTolerance), "SetDoubleArg")
            ],
            [new("Angle", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.AngleBetweenTwoPlanesNormalsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Angle = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
