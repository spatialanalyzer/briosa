namespace Briosa.Worker.Control;

public sealed record WorkerCollectionInstrumentIdValue(
    string CollectionName,
    int InstrumentId) : WorkerMpValue;
