namespace Briosa.Worker.Control;

public sealed record WorkerChoiceValue<T>(T Value) : WorkerMpValue, IWorkerChoiceValue
    where T : struct, Enum
{
    bool IWorkerChoiceValue.IsDefined => Enum.IsDefined(Value);
}
