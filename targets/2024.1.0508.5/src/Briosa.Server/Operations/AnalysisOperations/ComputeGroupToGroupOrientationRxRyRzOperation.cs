using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class ComputeGroupToGroupOrientationRxRyRzOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.compute_group_to_group_orientation_rx_ry_rz", "Compute Group to Group Orientation (Rx,Ry,Rz)",
        "briosa.AnalysisOperations", "ComputeGroupToGroupOrientationRxRyRz",
        "/briosa.AnalysisOperations/ComputeGroupToGroupOrientationRxRyRz",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("rx", "Rx", WorkerMpValueKind.FloatingPoint),
        new("ry", "Ry", WorkerMpValueKind.FloatingPoint),
        new("rz", "Rz", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.ComputeGroupToGroupOrientationRxRyRzRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Reference Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ReferenceGroup, "reference_group"), "SetCollectionObjectNameArg2"),
                new("Corresponding Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CorrespondingGroup, "corresponding_group"), "SetCollectionObjectNameArg2")
            ],
            [
                new("Rx", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Ry", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Rz", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.ComputeGroupToGroupOrientationRxRyRzResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Rx = values[0].RequireValue<WorkerDoubleValue>().Value,
            Ry = values[1].RequireValue<WorkerDoubleValue>().Value,
            Rz = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
