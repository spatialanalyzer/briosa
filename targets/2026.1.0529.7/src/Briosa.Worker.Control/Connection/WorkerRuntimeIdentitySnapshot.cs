namespace Briosa.Worker.Control;

public sealed record WorkerRuntimeIdentitySnapshot(
    WorkerRuntimeIdentityEvidence ActivatedSdk,
    WorkerRuntimeIdentityEvidence ConnectedSpatialAnalyzer);
