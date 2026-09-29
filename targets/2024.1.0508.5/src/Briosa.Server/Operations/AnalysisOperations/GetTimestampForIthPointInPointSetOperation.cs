using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetTimestampForIthPointInPointSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_timestamp_for_ith_point_in_point_set", "Get Timestamp for i-th Point in Point Set",
        "briosa.AnalysisOperations", "GetTimestampForIthPointInPointSet",
        "/briosa.AnalysisOperations/GetTimestampForIthPointInPointSet",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("timestamp", "Timestamp", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetTimestampForIthPointInPointSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Set", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.PointSet, "point_set"), "SetCollectionObjectNameArg2"),
                new("Point Set Index", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.PointSetIndex), "SetIntegerArg")
            ],
            [new("Timestamp", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetTimestampForIthPointInPointSetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Timestamp = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
