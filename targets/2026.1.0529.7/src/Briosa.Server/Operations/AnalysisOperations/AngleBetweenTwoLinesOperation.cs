using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class AngleBetweenTwoLinesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.angle_between_two_lines", "Angle Between Two Lines",
        "briosa.AnalysisOperations", "AngleBetweenTwoLines", "/briosa.AnalysisOperations/AngleBetweenTwoLines",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("angle", "Angle", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.AngleBetweenTwoLinesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Line 1", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Line1, "line_1"), "SetCollectionObjectNameArg2"),
                new("Line 2", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.Line2, "line_2"), "SetCollectionObjectNameArg2"),
                new("Nominal Angle", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.NominalAngle), "SetDoubleArg"),
                new("Angle Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.AngleTolerance), "SetDoubleArg")
            ],
            [new("Angle", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.AngleBetweenTwoLinesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Angle = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
