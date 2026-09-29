using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class QueryPointToObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.query_point_to_objects", "Query Point to Objects",
        "briosa.AnalysisOperations", "QueryPointToObjects", "/briosa.AnalysisOperations/QueryPointToObjects",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("d_x", "dX", WorkerMpValueKind.FloatingPoint), new("d_y", "dY", WorkerMpValueKind.FloatingPoint),
        new("d_z", "dZ", WorkerMpValueKind.FloatingPoint), new("d_mag", "dMag", WorkerMpValueKind.FloatingPoint),
        new("resultant_object", "Resultant Object", WorkerMpValueKind.CollectionObjectName)
    ];

    public static WorkerMpCommand CreateCommand(Api.QueryPointToObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Name", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
                new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg"),
                new("Ignore Target Offset", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasIgnoreTargetOffset && request.IgnoreTargetOffset), "SetBoolArg")
            ],
            [
                new("dX", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"), new("dY", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("dZ", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"), new("dMag", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Resultant Object", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")
            ]);
    }

    public static Api.QueryPointToObjectsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            DX = values[0].RequireValue<WorkerDoubleValue>().Value,
            DY = values[1].RequireValue<WorkerDoubleValue>().Value,
            DZ = values[2].RequireValue<WorkerDoubleValue>().Value,
            DMag = values[3].RequireValue<WorkerDoubleValue>().Value,
            ResultantObject = CollectionObjectNameMapper.ToProtocol(values[4].RequireValue<WorkerCollectionObjectNameValue>()),
            Execution = completed.Details
        };
    }
}
