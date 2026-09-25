using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed record ExactTargetIdentitySnapshot(
    RuntimeIdentityEvidence ActivatedSdk,
    RuntimeIdentityEvidence ConnectedSpatialAnalyzer)
{
    public bool AllowsExecution =>
        ActivatedSdk.MatchState == RuntimeIdentityMatchState.ExactMatch &&
        ConnectedSpatialAnalyzer.MatchState == RuntimeIdentityMatchState.ExactMatch;
}
