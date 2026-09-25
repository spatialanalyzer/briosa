namespace Briosa.Worker.Control;

public sealed record WorkerVectorNameValue(
    string CollectionName,
    string GroupName,
    string VectorName);
