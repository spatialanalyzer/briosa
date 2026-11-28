using Briosa.Cli.Engine;

namespace Briosa.Cli.CommandLine;

/// <summary>
/// The process exit code for each outcome. The table is fixed: a change is a
/// breaking change for every script that reads it.
/// </summary>
internal static class ExitCodes
{
    public static int For(BridgeOutcome outcome) => outcome switch
    {
        BridgeOutcome.Succeeded => 0,
        BridgeOutcome.InternalError => 1,
        BridgeOutcome.UsageError => 2,
        BridgeOutcome.RejectedLocally => 3,
        BridgeOutcome.ServerUnreachable => 4,
        BridgeOutcome.ServerIncompatible => 5,
        BridgeOutcome.NotStarted => 6,
        BridgeOutcome.InvalidRequest => 7,
        BridgeOutcome.MpFailed => 8,
        BridgeOutcome.PartialSuccess => 9,
        BridgeOutcome.OutputsIncomplete => 9,
        BridgeOutcome.CompletionEvidenceUnavailable => 10,
        BridgeOutcome.StartedOutcomeUnknown => 11,
        BridgeOutcome.OutputWriteFailed => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };
}
