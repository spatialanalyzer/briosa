using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameThreePlanesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_three_planes", "Construct Frame, 3 Planes",
        "briosa.ConstructionOperations", "ConstructFrameThreePlanes",
        "/briosa.ConstructionOperations/ConstructFrameThreePlanes", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameThreePlanesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("X Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.XPlane, "x_plane", WorkerObjectTypeValue.Plane),
                "SetCollectionObjectNameArg2"),
            new("X Value on PLane", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.XValueOnPlane), "SetDoubleArg"),
            new("Y Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.YPlane, "y_plane", WorkerObjectTypeValue.Plane),
                "SetCollectionObjectNameArg2"),
            new("Y Value on PLane", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.YValueOnPlane), "SetDoubleArg"),
            new("Z Plane", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ZPlane, "z_plane", WorkerObjectTypeValue.Plane),
                "SetCollectionObjectNameArg2"),
            new("Z Value on Plane", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.ZValueOnPlane), "SetDoubleArg")
        };
        if (request.FrameName is not null)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FrameName, "frame_name", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFrameThreePlanesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
