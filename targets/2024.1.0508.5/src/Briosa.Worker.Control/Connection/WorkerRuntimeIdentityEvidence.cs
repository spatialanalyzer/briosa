namespace Briosa.Worker.Control;

public sealed record WorkerRuntimeIdentityEvidence(
    string? Version,
    WorkerRuntimeIdentityEvidenceSource Source);
