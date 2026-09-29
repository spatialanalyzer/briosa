using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class ScaleObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.scale_objects", "Scale Objects", "briosa.UtilityOperations",
        "ScaleObjects", "/briosa.UtilityOperations/ScaleObjects", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ScaleObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg"),
            new("Scale Factor", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.ScaleFactor), "SetDoubleArg")
        ], []);
    }

    public static Api.ScaleObjectsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
