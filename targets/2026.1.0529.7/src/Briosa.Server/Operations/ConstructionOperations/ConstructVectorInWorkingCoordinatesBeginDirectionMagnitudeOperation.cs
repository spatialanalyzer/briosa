using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_vector_in_working_coordinates_begin_direction_magnitude", "Construct a Vector in Working Coordinates(Begin/Direction/Mag.)",
        "briosa.ConstructionOperations", "ConstructVectorInWorkingCoordinatesBeginDirectionMagnitude",
        "/briosa.ConstructionOperations/ConstructVectorInWorkingCoordinatesBeginDirectionMagnitude", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Vector Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.VectorGroupName, "vector_group_name", WorkerObjectTypeValue.VectorGroup), "SetCollectionObjectNameArg2"),
            new("New Vector Name", WorkerMpValueKind.Text, new WorkerTextValue(request.NewVectorName), "SetStringArg"),
            new("'Begin' in Working Coordinates", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.BeginInWorkingCoordinates, "begin_in_working_coordinates"), "SetVectorArg"),
            new("'Direction' in Working Coordinates", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.DirectionInWorkingCoordinates, "direction_in_working_coordinates"), "SetVectorArg"),
            new("Signed Magnitude", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.SignedMagnitude), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructVectorInWorkingCoordinatesBeginDirectionMagnitudeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
