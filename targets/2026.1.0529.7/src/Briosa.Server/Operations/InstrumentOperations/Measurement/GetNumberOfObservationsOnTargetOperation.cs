using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class GetNumberOfObservationsOnTargetOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.get_number_of_observations_on_target", "Get Number of Observations on Target",
        "briosa.InstrumentOperations", "GetNumberOfObservationsOnTarget",
        "/briosa.InstrumentOperations/GetNumberOfObservationsOnTarget",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("observation_count", "Number of Shots", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetNumberOfObservationsOnTargetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg")],
            [new("Number of Shots", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetNumberOfObservationsOnTargetResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ObservationCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
