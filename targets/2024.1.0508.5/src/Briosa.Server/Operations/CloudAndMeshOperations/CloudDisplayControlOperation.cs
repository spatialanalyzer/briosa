using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class CloudDisplayControlOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.cloud_display_control", "Cloud Display Control",
        "briosa.CloudAndMeshOperations", "CloudDisplayControl", "/briosa.CloudAndMeshOperations/CloudDisplayControl",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CloudDisplayControlRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Thin (Draw Increment)", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasThin ? request.Thin : 1), "SetIntegerArg"),
            new("Point Size", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasPointSize ? request.PointSize : 1), "SetIntegerArg")
        ], []);
    }

    public static Api.CloudDisplayControlResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
