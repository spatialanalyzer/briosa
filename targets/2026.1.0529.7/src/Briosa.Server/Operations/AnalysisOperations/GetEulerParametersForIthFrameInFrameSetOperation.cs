using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetEulerParametersForIthFrameInFrameSetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_euler_parameters_for_ith_frame_in_frame_set", "Get Euler Parameters for i-th Frame in Frame Set",
        "briosa.AnalysisOperations", "GetEulerParametersForIthFrameInFrameSet",
        "/briosa.AnalysisOperations/GetEulerParametersForIthFrameInFrameSet",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("x", "X", WorkerMpValueKind.FloatingPoint),
        new("y", "Y", WorkerMpValueKind.FloatingPoint),
        new("z", "Z", WorkerMpValueKind.FloatingPoint),
        new("e1", "e1", WorkerMpValueKind.FloatingPoint),
        new("e2", "e2", WorkerMpValueKind.FloatingPoint),
        new("e3", "e3", WorkerMpValueKind.FloatingPoint),
        new("e4", "e4", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetEulerParametersForIthFrameInFrameSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Frame Set", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.FrameSet, "frame_set"), "SetCollectionObjectNameArg2"),
                new("Frame Set Index", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasFrameSetIndex ? request.FrameSetIndex : 0), "SetIntegerArg")
            ],
            [
                new("X", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Y", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Z", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("e1", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("e2", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("e3", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("e4", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetEulerParametersForIthFrameInFrameSetResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            X = values[0].RequireValue<WorkerDoubleValue>().Value,
            Y = values[1].RequireValue<WorkerDoubleValue>().Value,
            Z = values[2].RequireValue<WorkerDoubleValue>().Value,
            E1 = values[3].RequireValue<WorkerDoubleValue>().Value,
            E2 = values[4].RequireValue<WorkerDoubleValue>().Value,
            E3 = values[5].RequireValue<WorkerDoubleValue>().Value,
            E4 = values[6].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
