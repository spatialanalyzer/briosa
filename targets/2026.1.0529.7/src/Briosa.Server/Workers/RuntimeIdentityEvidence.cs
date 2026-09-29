using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed record RuntimeIdentityEvidence(
    string? Version,
    RuntimeIdentityEvidenceSource Source,
    RuntimeIdentityMatchState MatchState);
