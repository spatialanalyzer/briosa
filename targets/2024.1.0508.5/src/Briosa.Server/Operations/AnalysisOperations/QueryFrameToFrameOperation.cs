using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class QueryFrameToFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.query_frame_to_frame", "Query Frame to Frame",
        "briosa.AnalysisOperations", "QueryFrameToFrame", "/briosa.AnalysisOperations/QueryFrameToFrame",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("x", "X", WorkerMpValueKind.FloatingPoint), new("y", "Y", WorkerMpValueKind.FloatingPoint),
        new("z", "Z", WorkerMpValueKind.FloatingPoint), new("rx", "Rx (Roll)", WorkerMpValueKind.FloatingPoint),
        new("ry", "Ry (Pitch)", WorkerMpValueKind.FloatingPoint), new("rz", "Rz (Yaw)", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.QueryFrameToFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Reference Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceFrameName, "reference_frame_name"), "SetCollectionObjectNameArg2"),
                new("Corresponding Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CorrespondingFrameName, "corresponding_frame_name"), "SetCollectionObjectNameArg2")
            ],
            [
                new("X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"), new("Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"), new("Rx (Roll)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Ry (Pitch)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"), new("Rz (Yaw)", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.QueryFrameToFrameResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            X = values[0].RequireValue<WorkerDoubleValue>().Value, Y = values[1].RequireValue<WorkerDoubleValue>().Value,
            Z = values[2].RequireValue<WorkerDoubleValue>().Value, Rx = values[3].RequireValue<WorkerDoubleValue>().Value,
            Ry = values[4].RequireValue<WorkerDoubleValue>().Value, Rz = values[5].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
