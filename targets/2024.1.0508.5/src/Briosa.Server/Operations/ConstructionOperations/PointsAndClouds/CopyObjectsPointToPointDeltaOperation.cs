using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CopyObjectsPointToPointDeltaOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.copy_objects_point_to_point_delta", "Copy Objects - Point to Point Delta",
        "briosa.ConstructionOperations", "CopyObjectsPointToPointDelta", "/briosa.ConstructionOperations/CopyObjectsPointToPointDelta",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CopyObjectsPointToPointDeltaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<WorkerMpInputArgument>
        {
            new("Objects to Copy", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.ObjectsToCopy, "objects_to_copy"), "SetCollectionObjectNameRefListArg"),
            new("First Delta Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.FirstDeltaPoint, "first_delta_point"), "SetPointNameArg"),
            new("Second Delta Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SecondDeltaPoint, "second_delta_point"), "SetPointNameArg")
        };
        if (request.DestinationCollectionName is not null)
        {
            arguments.Add(new("Destination Collection Name (Optional)", WorkerMpValueKind.CollectionName,
                CollectionNameMapper.Required(request.DestinationCollectionName, "destination_collection_name"), "SetCollectionNameArg"));
        }
        return new(Descriptor.OperationId, Descriptor.MpStep, arguments, []);
    }

    public static Api.CopyObjectsPointToPointDeltaResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
