using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetScreenResolutionOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_screen_resolution", "Get Screen Resolution", "briosa.UtilityOperations",
        "GetScreenResolution", "/briosa.UtilityOperations/GetScreenResolution", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("integer_window_top_left_x_position", "Integer Window Top Left X Position", WorkerMpValueKind.WholeNumber),
        new("integer_window_top_left_y_position", "Integer Window Top Left Y Position", WorkerMpValueKind.WholeNumber),
        new("integer_width", "Integer Width", WorkerMpValueKind.WholeNumber),
        new("integer_height", "Integer Height", WorkerMpValueKind.WholeNumber),
        new("view_width", "View Width", WorkerMpValueKind.WholeNumber),
        new("view_height", "View Height", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetScreenResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Display (-1 = Primary)", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasDisplay ? request.Display : -1), "SetIntegerArg")],
            OutputContracts.Select(output => new WorkerMpOutputArgument(output.ArgumentName, output.Kind, "GetIntegerArg")).ToArray());
    }

    public static Api.GetScreenResolutionResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            IntegerWindowTopLeftXPosition = outputs[0].RequireValue<WorkerIntegerValue>().Value,
            IntegerWindowTopLeftYPosition = outputs[1].RequireValue<WorkerIntegerValue>().Value,
            IntegerWidth = outputs[2].RequireValue<WorkerIntegerValue>().Value,
            IntegerHeight = outputs[3].RequireValue<WorkerIntegerValue>().Value,
            ViewWidth = outputs[4].RequireValue<WorkerIntegerValue>().Value,
            ViewHeight = outputs[5].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details
        };
    }
}
