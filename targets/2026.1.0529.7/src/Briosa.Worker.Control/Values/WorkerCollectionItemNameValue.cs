namespace Briosa.Worker.Control;

public sealed record WorkerCollectionItemNameValue(
    string CollectionName,
    string ItemName,
    WorkerItemTypeValue ItemType) : WorkerMpValue;
