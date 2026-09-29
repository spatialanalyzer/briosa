using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructLineTwoPointsVectorNotationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_line_two_points_vector_notation", "Construct Line 2 Points (Vector Notation)",
        "briosa.ConstructionOperations", "ConstructLineTwoPointsVectorNotation", "/briosa.ConstructionOperations/ConstructLineTwoPointsVectorNotation",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructLineTwoPointsVectorNotationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("First Vector", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.FirstVector, "first_vector"), "SetVectorArg"),
            new("Second Vector", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.SecondVector, "second_vector"), "SetVectorArg")
        ], []);
    }

    public static Api.ConstructLineTwoPointsVectorNotationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
