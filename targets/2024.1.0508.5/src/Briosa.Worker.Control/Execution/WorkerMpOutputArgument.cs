namespace Briosa.Worker.Control;

public sealed record WorkerMpOutputArgument(
    string Name,
    WorkerMpValueKind Kind,
    string? SdkBinding = null,
    WorkerObjectTypeValue? ObjectTypeWhenOmitted = null,
    int? ArraySize = null);
