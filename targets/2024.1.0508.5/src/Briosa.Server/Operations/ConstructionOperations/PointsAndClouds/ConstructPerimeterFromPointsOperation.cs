using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPerimeterFromPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_perimeter_from_points", "Construct Perimeter From Points",
        "briosa.ConstructionOperations", "ConstructPerimeterFromPoints", "/briosa.ConstructionOperations/ConstructPerimeterFromPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPerimeterFromPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Resulting Perimeter Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ResultingPerimeterName, "resulting_perimeter_name", WorkerObjectTypeValue.Perimeter), "SetCollectionObjectNameArg2"),
            new("Point List", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointList, "point_list"), "SetPointNameRefListArg"),
            new("Open Perimeter?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasOpenPerimeter && request.OpenPerimeter), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructPerimeterFromPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
