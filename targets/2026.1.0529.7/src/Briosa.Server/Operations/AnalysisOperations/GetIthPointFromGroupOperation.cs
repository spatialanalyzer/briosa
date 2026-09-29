using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetIthPointFromGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_ith_point_from_group", "Get i-th Point From Group",
        "briosa.AnalysisOperations", "GetIthPointFromGroup",
        "/briosa.AnalysisOperations/GetIthPointFromGroup",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("complete_point_name", "Complete Point Name", WorkerMpValueKind.PointName),
        new("point_name_only", "Point Name Only", WorkerMpValueKind.Text),
        new("vector_in_working", "Vector in Working", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetIthPointFromGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupName, "group_name"), "SetCollectionObjectNameArg2"),
            new("Point Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.PointIndex), "SetIntegerArg")
        ],
        [
            new("Complete Point Name", WorkerMpValueKind.PointName, "GetPointNameArg"),
            new("Point Name Only", WorkerMpValueKind.Text, "GetStringArg"),
            new("Vector in Working", WorkerMpValueKind.Vector, "GetVectorArg")
        ]);
    }

    public static Api.GetIthPointFromGroupResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            CompletePointName = PointNameMapper.ToProtocol(values[0].RequireValue<WorkerPointNameValue>()),
            PointNameOnly = values[1].RequireValue<WorkerTextValue>().Value,
            VectorInWorking = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
            Execution = completed.Details
        };
    }
}
