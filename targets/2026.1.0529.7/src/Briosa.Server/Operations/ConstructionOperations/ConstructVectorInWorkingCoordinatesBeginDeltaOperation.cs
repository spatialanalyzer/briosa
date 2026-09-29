using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructVectorInWorkingCoordinatesBeginDeltaOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_vector_in_working_coordinates_begin_delta", "Construct a Vector in Working Coordinates(Begin/Delta)",
        "briosa.ConstructionOperations", "ConstructVectorInWorkingCoordinatesBeginDelta",
        "/briosa.ConstructionOperations/ConstructVectorInWorkingCoordinatesBeginDelta", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructVectorInWorkingCoordinatesBeginDeltaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
            new("New Vector Name", WorkerMpValueKind.Text, new WorkerTextValue(request.NewVectorName), "SetStringArg"),
            new("'Begin' in Working Coordinates", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.BeginInWorkingCoordinates, "begin_in_working_coordinates"), "SetVectorArg"),
            new("'Delta' in Working Coordinates", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.DeltaInWorkingCoordinates, "delta_in_working_coordinates"), "SetVectorArg"),
            new("Is Magnitude Negative", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.IsMagnitudeNegative), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructVectorInWorkingCoordinatesBeginDeltaResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
