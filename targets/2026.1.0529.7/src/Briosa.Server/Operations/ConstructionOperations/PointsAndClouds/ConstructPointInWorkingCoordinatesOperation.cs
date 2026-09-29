using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointInWorkingCoordinatesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_in_working_coordinates", "Construct a Point in Working Coordinates",
        "briosa.ConstructionOperations", "ConstructPointInWorkingCoordinates", "/briosa.ConstructionOperations/ConstructPointInWorkingCoordinates",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointInWorkingCoordinatesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
            new("Working Coordinates", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.WorkingCoordinates, "working_coordinates"), "SetVectorArg")
        ], []);
    }

    public static Api.ConstructPointInWorkingCoordinatesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
