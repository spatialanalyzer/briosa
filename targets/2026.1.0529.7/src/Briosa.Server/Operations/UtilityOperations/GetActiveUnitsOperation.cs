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

    public static Api.GetActiveUnitsResult CreateResult(
        SuccessfulOperationExecution completed)
    {
        ArgumentNullException.ThrowIfNull(completed);
        var outputs = completed.Execution.OutputValues;

        return new Api.GetActiveUnitsResult
        {
            Length = (outputs.Single(value =>
                value.Name == LengthArgumentName &&
                value.Kind == WorkerMpValueKind.Text).ReadValue() as WorkerTextValue)?.Value!,
            Angular = (outputs.Single(value =>
                value.Name == AngularArgumentName &&
                value.Kind == WorkerMpValueKind.Text).ReadValue() as WorkerTextValue)?.Value!,
            Temperature = (outputs.Single(value =>
                value.Name == TemperatureArgumentName &&
                value.Kind == WorkerMpValueKind.Text).ReadValue() as WorkerTextValue)?.Value!,
            Execution = completed.Details
        };
    }
}
