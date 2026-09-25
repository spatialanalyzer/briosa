namespace Briosa.Worker.Control;

public sealed record WorkerCollectionMachineIdValue(
    string CollectionName,
    int MachineId) : WorkerMpValue;
