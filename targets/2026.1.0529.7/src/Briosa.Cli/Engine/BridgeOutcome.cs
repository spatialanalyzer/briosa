namespace Briosa.Cli.Engine;

/// <summary>
/// The one overall outcome of a command. It summarizes, and never replaces, the
/// separate dimensions in the envelope: transport status, failure kind, diagnostic
/// code, execution disposition, recovery and replay guidance, and MP result.
/// </summary>
internal enum BridgeOutcome
{
    /// <summary>The command did what it was asked; for <c>call</c>, MP code 2 with every output retrieved.</summary>
    Succeeded,

    /// <summary>The command line could not be read. Nothing was sent.</summary>
    UsageError,

    /// <summary>The CLI refused before sending the operation: input, operation, tier, or output file. Not started.</summary>
    RejectedLocally,

    /// <summary>The server could not be reached or did not answer discovery. No operation was sent.</summary>
    ServerUnreachable,

    /// <summary>The server is not the exact target, compatibility major, or revision this command needs. No operation was sent.</summary>
    ServerIncompatible,

    /// <summary>The server reported <c>NOT_STARTED</c> for a reason other than request validation: policy, caller restriction, readiness, capacity, or similar.</summary>
    NotStarted,

    /// <summary>The server reported <c>NOT_STARTED</c> because request validation failed.</summary>
    InvalidRequest,

    /// <summary>The server reported <c>COMPLETED</c> without success. Do not replay.</summary>
    MpFailed,

    /// <summary>MP code 4: the command did its work without its full intended effect. Completed; do not replay.</summary>
    PartialSuccess,

    /// <summary>The MP completed successfully but at least one output was not delivered. Completed; do not replay.</summary>
    OutputsIncomplete,

    /// <summary>The request was sent and no trustworthy completion evidence came back. Reconcile before trying again.</summary>
    CompletionEvidenceUnavailable,

    /// <summary>The server reported <c>STARTED_OUTCOME_UNKNOWN</c>. Reconcile before trying again.</summary>
    StartedOutcomeUnknown,

    /// <summary>The operation completed, but its result could not be written to the requested or spill file.</summary>
    OutputWriteFailed,

    /// <summary>The CLI failed unexpectedly before sending any operation.</summary>
    InternalError
}

internal static class BridgeOutcomes
{
    public static IReadOnlyList<BridgeOutcome> All { get; } = Enum.GetValues<BridgeOutcome>();

    public static string Name(BridgeOutcome outcome) => outcome switch
    {
        BridgeOutcome.Succeeded => "succeeded",
        BridgeOutcome.UsageError => "usage_error",
        BridgeOutcome.RejectedLocally => "rejected_locally",
        BridgeOutcome.ServerUnreachable => "server_unreachable",
        BridgeOutcome.ServerIncompatible => "server_incompatible",
        BridgeOutcome.NotStarted => "not_started",
        BridgeOutcome.InvalidRequest => "invalid_request",
        BridgeOutcome.MpFailed => "mp_failed",
        BridgeOutcome.PartialSuccess => "partial_success",
        BridgeOutcome.OutputsIncomplete => "outputs_incomplete",
        BridgeOutcome.CompletionEvidenceUnavailable => "completion_evidence_unavailable",
        BridgeOutcome.StartedOutcomeUnknown => "started_outcome_unknown",
        BridgeOutcome.OutputWriteFailed => "output_write_failed",
        BridgeOutcome.InternalError => "internal_error",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };
}
