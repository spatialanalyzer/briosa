namespace Briosa.Worker.Control;

public sealed record WorkerMpInputArgument
{
    public WorkerMpInputArgument(string name, WorkerMpValueKind kind, WorkerMpValue value, string? sdkBinding = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.MatchesInputKind(kind))
        {
            throw new ArgumentException("The input value does not match its MP kind.", nameof(value));
        }
        Name = name;
        Kind = kind;
        Value = value;
        SdkBinding = sdkBinding;
    }

    public string Name { get; }
    public WorkerMpValueKind Kind { get; }
    public WorkerMpValue Value { get; }
    public string? SdkBinding { get; }

    public TValue RequireValue<TValue>() where TValue : WorkerMpValue =>
        Value is TValue value
            ? value
            : throw new InvalidOperationException("The input value has a different type.");
}
