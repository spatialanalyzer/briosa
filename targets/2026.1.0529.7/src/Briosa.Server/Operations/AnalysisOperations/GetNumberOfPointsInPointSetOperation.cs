using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetNumberOfPointsInPointSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_number_of_points_in_point_set", "Get Number of Points In Point Set",
        "briosa.AnalysisOperations", "GetNumberOfPointsInPointSet",
        "/briosa.AnalysisOperations/GetNumberOfPointsInPointSet",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("total_count", "Total Count", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetNumberOfPointsInPointSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point Set Container", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PointSetContainer, "point_set_container"), "SetCollectionObjectNameArg2")],
            [new("Total Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetNumberOfPointsInPointSetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        TotalCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
