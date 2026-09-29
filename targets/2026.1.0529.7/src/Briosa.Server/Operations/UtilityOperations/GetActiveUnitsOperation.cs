using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

/// <summary>
/// Implements the exact "Get Active Units" MP command contract.
/// </summary>
internal static class GetActiveUnitsOperation
{
    public const string OperationId = "utility_operations.get_active_units";
    public const string StepName = "Get Active Units";
    public const string LengthArgumentName = "Length";
    public const string AngularArgumentName = "Angular";
    public const string TemperatureArgumentName = "Temperature";
    public const string StringGetter = "GetStringArg";

    public static OperationDescriptor Descriptor { get; } = new(
        OperationId,
        StepName,
        "briosa.UtilityOperations",
        "GetActiveUnits",
        "/briosa.UtilityOperations/GetActiveUnits",
        "read_only",
        Api.OperationExecutionScope.GlobalStateRead,
        Api.ReplaySafety.Safe,
        []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [
            new("length", LengthArgumentName, WorkerMpValueKind.Text),
            new("angular", AngularArgumentName, WorkerMpValueKind.Text),
            new("temperature", TemperatureArgumentName, WorkerMpValueKind.Text)
        ];

    public static WorkerMpCommand CreateCommand(Api.GetActiveUnitsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new WorkerMpCommand(
            OperationId,
            StepName,
            inputArguments: [],
            outputArguments:
            [
                new(LengthArgumentName, WorkerMpValueKind.Text, StringGetter),
                new(AngularArgumentName, WorkerMpValueKind.Text, StringGetter),
                new(TemperatureArgumentName, WorkerMpValueKind.Text, StringGetter)
            ]);
    }

    // OperationExecutor validates ordered output shape and retrieval before mapping.
    public static Api.GetActiveUnitsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Length = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Angular = completed.Execution.OutputValues[1].RequireValue<WorkerTextValue>().Value,
        Temperature = completed.Execution.OutputValues[2].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
