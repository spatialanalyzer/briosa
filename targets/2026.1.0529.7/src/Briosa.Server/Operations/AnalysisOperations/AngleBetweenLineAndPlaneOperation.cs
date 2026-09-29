using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class AngleBetweenLineAndPlaneOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.angle_between_line_and_plane", "Angle Between Line and Plane",
        "briosa.AnalysisOperations", "AngleBetweenLineAndPlane", "/briosa.AnalysisOperations/AngleBetweenLineAndPlane",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("angle", "Angle", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.AngleBetweenLineAndPlaneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Selected Line", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SelectedLine, "selected_line"), "SetCollectionObjectNameArg2"),
                new("Selected Plane", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.SelectedPlane, "selected_plane"), "SetCollectionObjectNameArg2"),
                new("Nominal Angle", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.NominalAngle), "SetDoubleArg"),
                new("Angle Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.AngleTolerance), "SetDoubleArg")
            ],
            [new("Angle", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.AngleBetweenLineAndPlaneResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Angle = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
