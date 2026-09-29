using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructFrameOnObjectOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_frame_on_object", "Construct Frame on Object",
        "briosa.ConstructionOperations", "ConstructFrameOnObject", "/briosa.ConstructionOperations/ConstructFrameOnObject",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructFrameOnObjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var inputs = new List<WorkerMpInputArgument>
        {
            new("Reference Object", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ReferenceObject, "reference_object"),
                "SetCollectionObjectNameArg2")
        };
        if (request.FrameName is not null)
            inputs.Add(new("Frame Name (Optional)", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.FrameName, "frame_name", WorkerObjectTypeValue.Frame),
                "SetCollectionObjectNameArg2"));
        return new(Descriptor.OperationId, Descriptor.MpStep, inputs, []);
    }

    public static Api.ConstructFrameOnObjectResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
