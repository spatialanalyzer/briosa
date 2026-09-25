namespace Briosa.Worker.Control;

public sealed record WorkerPointNameValue(
    string CollectionName,
    string GroupName,
    string TargetName);
