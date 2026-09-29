using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MirrorObjectsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.mirror_objects", "Mirror Object(s)", "briosa.ConstructionOperations", "MirrorObjects",
        "/briosa.ConstructionOperations/MirrorObjects", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MirrorObjectsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plane = request.FramePlaneToMirrorAround switch
        {
            Api.MirrorFramePlane.Xy => "XY",
            Api.MirrorFramePlane.Xz => "XZ",
            Api.MirrorFramePlane.Yz => "YZ",
            _ => throw new ArgumentException("A frame plane is required.", nameof(request))
        };
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object(s)", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg"),
            new("Frame Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FrameName, "frame_name", WorkerObjectTypeValue.Frame), "SetCollectionObjectNameArg2"),
            new("Frame Plane to Mirror Around", WorkerMpValueKind.Text, new WorkerTextValue(plane), "SetStringArg"),
            new("Copy? [FALSE = Move]", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasCopy || request.Copy), "SetBoolArg")
        ], []);
    }

    public static Api.MirrorObjectsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
