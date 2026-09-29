using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class DoRelationshipFitOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.do_relationship_fit", "Do Relationship Fit",
        "briosa.RelationshipOperations", "DoRelationshipFit",
        "/briosa.RelationshipOperations/DoRelationshipFit",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("transform_in_reference", "Transform In Reference", WorkerMpValueKind.Transform),
        new("transform_in_working", "Transform In Working", WorkerMpValueKind.WorldTransform),
        new("transform_in_world", "Transform In World", WorkerMpValueKind.WorldTransform),
        new("fit_objective_value", "Fit Objective Value", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.DoRelationshipFitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.CollectionContainingRelationships))
            throw new ArgumentException("Request field 'collection_containing_relationships' is required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Collection Containing Relationships", WorkerMpValueKind.CollectionName,
                    new WorkerTextValue(request.CollectionContainingRelationships), "SetCollectionNameArg"),
                new("Objects to Move", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.ObjectsToMove, "objects_to_move"), "SetCollectionObjectNameRefListArg"),
                new("Instruments to Move", WorkerMpValueKind.CollectionInstrumentIdList,
                    CollectionInstrumentIdMapper.RequiredList(request.InstrumentsToMove, "instruments_to_move"), "SetColInstIdRefListArg"),
                new("Solver Mode", WorkerMpValueKind.Text,
                    RelationshipOperationValueMapper.Solver(request.HasSolverMode ? request.SolverMode : null), "SetStringArg"),
                new("Motion to allow", WorkerMpValueKind.FitDegreeOfFreedomOptions,
                    RelationshipOperationValueMapper.RequiredMotion(request.MotionToAllow, "motion_to_allow"), "SetFitDofOptionsArg"),
                new("Use Fit Dialog", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasUseFitDialog && request.UseFitDialog), "SetBoolArg")
            ],
            [
                new("Transform In Reference", WorkerMpValueKind.Transform, "GetTransformArg"),
                new("Transform In Working", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg"),
                new("Transform In World", WorkerMpValueKind.WorldTransform, "GetWorldTransformArg"),
                new("Fit Objective Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.DoRelationshipFitResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            TransformInReference = TransformMapper.ToProtocol(outputs[0].RequireValue<WorkerTransformValue>()),
            TransformInWorking = WorldTransformMapper.ToProtocol(outputs[1].RequireValue<WorkerWorldTransformValue>()),
            TransformInWorld = WorldTransformMapper.ToProtocol(outputs[2].RequireValue<WorkerWorldTransformValue>()),
            FitObjectiveValue = outputs[3].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}
