using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class DeletePointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.delete_points", "Delete Points",
        "briosa.ConstructionOperations", "DeletePoints", "/briosa.ConstructionOperations/DeletePoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeletePointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Names", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg")
        ], []);
    }

    public static Api.DeletePointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
