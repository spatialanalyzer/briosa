using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtIntersectionOfTwoLinesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_intersection_of_two_lines", "Construct Point at Intersection of Two Lines",
        "briosa.ConstructionOperations", "ConstructPointAtIntersectionOfTwoLines", "/briosa.ConstructionOperations/ConstructPointAtIntersectionOfTwoLines",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtIntersectionOfTwoLinesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("First Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FirstLineName, "first_line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Second Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SecondLineName, "second_line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Resulting Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ResultingPointName, "resulting_point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointAtIntersectionOfTwoLinesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
