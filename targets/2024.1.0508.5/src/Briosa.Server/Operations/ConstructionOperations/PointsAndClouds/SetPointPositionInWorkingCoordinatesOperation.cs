using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class SetPointPositionInWorkingCoordinatesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.set_point_position_in_working_coordinates", "Set Point Position in Working Coordinates",
        "briosa.ConstructionOperations", "SetPointPositionInWorkingCoordinates", "/briosa.ConstructionOperations/SetPointPositionInWorkingCoordinates",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointPositionInWorkingCoordinatesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
            new("Position In Working Coordinates", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.PositionInWorkingCoordinates, "position_in_working_coordinates"), "SetVectorArg")
        ], []);
    }

    public static Api.SetPointPositionInWorkingCoordinatesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
