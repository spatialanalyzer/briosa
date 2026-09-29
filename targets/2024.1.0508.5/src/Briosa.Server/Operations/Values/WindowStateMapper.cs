using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class WindowStateMapper
{
    public static WorkerChoiceValue<WorkerWindowStateValue> Required(Api.WindowState? value, string fieldName) => value switch
    {
        Api.WindowState.Maximize => new(WorkerWindowStateValue.Maximize),
        Api.WindowState.Minimize => new(WorkerWindowStateValue.Minimize),
        Api.WindowState.Restore => new(WorkerWindowStateValue.Restore),
        Api.WindowState.Show => new(WorkerWindowStateValue.Show),
        Api.WindowState.Hide => new(WorkerWindowStateValue.Hide),
        _ => throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value))
    };
}
