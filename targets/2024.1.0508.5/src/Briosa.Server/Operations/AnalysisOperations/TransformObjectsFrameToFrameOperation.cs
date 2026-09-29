using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class TransformObjectsFrameToFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.transform_objects_frame_to_frame", "Transform Objects - Frame To Frame",
        "briosa.AnalysisOperations", "TransformObjectsFrameToFrame",
        "/briosa.AnalysisOperations/TransformObjectsFrameToFrame",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TransformObjectsFrameToFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Object Name List", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectNameList, "object_name_list"),
                    "SetCollectionObjectNameRefListArg"),
                new("Initial Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.InitialFrameName, "initial_frame_name"),
                    "SetCollectionObjectNameArg2"),
                new("Destination Frame Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.DestinationFrameName, "destination_frame_name"),
                    "SetCollectionObjectNameArg2"),
                new("Number of Steps", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasNumberOfSteps ? request.NumberOfSteps : 0), "SetIntegerArg")
            ], []);
    }

    public static Api.TransformObjectsFrameToFrameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
