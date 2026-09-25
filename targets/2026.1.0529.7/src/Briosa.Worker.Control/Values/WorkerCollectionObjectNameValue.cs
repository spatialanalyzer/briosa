namespace Briosa.Worker.Control;

public sealed record WorkerCollectionObjectNameValue(
    string CollectionName,
    string ObjectName,
    WorkerObjectTypeValue ObjectType) : WorkerMpValue;
