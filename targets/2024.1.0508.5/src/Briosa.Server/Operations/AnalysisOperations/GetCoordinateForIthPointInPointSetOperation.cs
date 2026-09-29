using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetCoordinateForIthPointInPointSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_coordinate_for_ith_point_in_point_set", "Get Coordinate for i-th Point in Point Set",
        "briosa.AnalysisOperations", "GetCoordinateForIthPointInPointSet",
        "/briosa.AnalysisOperations/GetCoordinateForIthPointInPointSet",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("point_name", "Point Name", WorkerMpValueKind.Text),
        new("point_coordinates", "Point Coordinates", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetCoordinateForIthPointInPointSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Set", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PointSet, "point_set"), "SetCollectionObjectNameArg2"),
            new("Point Set Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.PointSetIndex), "SetIntegerArg")
        ],
        [
            new("Point Name", WorkerMpValueKind.Text, "GetStringArg"),
            new("Point Coordinates", WorkerMpValueKind.Vector, "GetVectorArg")
        ]);
    }

    public static Api.GetCoordinateForIthPointInPointSetResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            PointName = values[0].RequireValue<WorkerTextValue>().Value,
            PointCoordinates = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            Execution = completed.Details
        };
    }
}
