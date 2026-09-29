using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructLineTwoPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_line_two_points", "Construct Line 2 Points",
        "briosa.ConstructionOperations", "ConstructLineTwoPoints", "/briosa.ConstructionOperations/ConstructLineTwoPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructLineTwoPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("First Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.FirstPoint, "first_point"), "SetPointNameArg"),
            new("Second Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SecondPoint, "second_point"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructLineTwoPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
