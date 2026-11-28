namespace Briosa.Cli.Engine;

/// <summary>
/// How the agent bridge treats an operation. Every operation belongs to exactly
/// one tier, decided from its discovery metadata alone by <see cref="BridgeTiers.Classify"/>.
/// A tier is a description and a local pre-check. The server's policy, narrowed by
/// the caller restriction header, stays the authority for every call.
/// </summary>
internal enum BridgeTier
{
    /// <summary>
    /// Read-only, reads or depends on application state only, and carries no risk
    /// flag other than <c>filesystem_metadata</c>. <c>call</c> executes these under
    /// <c>profile=read-only; deny=interactive_ui</c>.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Read-only, but reads caller-named file content (<c>filesystem_read</c>) or
    /// opens operator UI (<c>interactive_ui</c>). The read-only restriction denies
    /// both on the server, so <c>call</c> does not execute them.
    /// </summary>
    ReadOnlyRestricted,

    /// <summary>
    /// Changes the job, application state, or files. Reserved for a later controlled
    /// mutation tier; <c>call</c> does not execute them.
    /// </summary>
    Mutating,

    /// <summary>
    /// Moves hardware, needs a device session or its configuration, runs code, talks
    /// to systems other than SpatialAnalyzer, spans several calls, or has metadata the
    /// bridge does not recognize. The agent bridge never executes them.
    /// </summary>
    Excluded
}

internal static class BridgeTiers
{
    /// <summary>The risk flags that put an operation in <see cref="BridgeTier.Excluded"/>.</summary>
    public static IReadOnlyList<OperationRiskFlag> ExcludedFlags { get; } =
        [
            OperationRiskFlag.PhysicalMotion,
            OperationRiskFlag.DeviceSession,
            OperationRiskFlag.DeviceConfig,
            OperationRiskFlag.CodeExecution,
            OperationRiskFlag.ExternalIo
        ];

    public static IReadOnlyList<BridgeTier> All { get; } =
        [BridgeTier.ReadOnly, BridgeTier.ReadOnlyRestricted, BridgeTier.Mutating, BridgeTier.Excluded];

    public static string Name(BridgeTier tier) => tier switch
    {
        BridgeTier.ReadOnly => "read_only",
        BridgeTier.ReadOnlyRestricted => "read_only_restricted",
        BridgeTier.Mutating => "mutating",
        BridgeTier.Excluded => "excluded",
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    public static bool TryParse(string text, out BridgeTier tier)
    {
        foreach (var candidate in All)
        {
            if (string.Equals(Name(candidate), text, StringComparison.Ordinal))
            {
                tier = candidate;
                return true;
            }
        }

        tier = default;
        return false;
    }

    /// <summary>Whether <c>call</c> in this release executes operations of the tier.</summary>
    public static bool ServedByCall(BridgeTier tier) => tier == BridgeTier.ReadOnly;

    public static string Description(BridgeTier tier) => tier switch
    {
        BridgeTier.ReadOnly =>
            "Read-only inspection. call executes it, and every call asks the server to enforce " +
            "the read-only profile with operator UI denied.",
        BridgeTier.ReadOnlyRestricted =>
            "Read-only, but it reads caller-named file content or opens operator UI. The read-only " +
            "restriction denies both on the server, so call does not execute it.",
        BridgeTier.Mutating =>
            "Changes the job, application state, or files. Reserved for a later controlled mutation " +
            "tier; call does not execute it.",
        BridgeTier.Excluded =>
            "Moves hardware, needs a device session or its configuration, runs code, talks to systems " +
            "other than SpatialAnalyzer, spans several calls, or has metadata this CLI does not " +
            "recognize. The agent bridge never executes it.",
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    /// <summary>
    /// Assigns exactly one tier from effect, risk flags, execution scope, and the
    /// presence of reviewed classes. The first matching rule decides:
    /// <list type="number">
    /// <item>Excluded: an excluded risk flag; an exclusive-workflow, unknown, or
    /// unrecognized execution scope; an unknown or unrecognized effect; an
    /// unspecified or unrecognized risk flag; or an unreviewed row (no duration class
    /// or validation status).</item>
    /// <item>Read-only: effect read-only, scope self-contained or global-state read,
    /// and no flag other than <c>filesystem_metadata</c>.</item>
    /// <item>Read-only restricted: effect read-only, the same scopes, and every flag
    /// one of <c>filesystem_metadata</c>, <c>filesystem_read</c>, <c>interactive_ui</c>.</item>
    /// <item>Mutating: everything else.</item>
    /// </list>
    /// </summary>
    public static BridgeTier Classify(OperationCapability capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        if (IsExcluded(capability))
        {
            return BridgeTier.Excluded;
        }

        var readsOnly = capability.Effect == OperationEffect.ReadOnly &&
            capability.ExecutionScope is OperationExecutionScope.SelfContained or
                OperationExecutionScope.GlobalStateRead;
        if (readsOnly && capability.RiskFlags.All(flag => flag == OperationRiskFlag.FilesystemMetadata))
        {
            return BridgeTier.ReadOnly;
        }

        if (readsOnly && capability.RiskFlags.All(flag => flag is OperationRiskFlag.FilesystemMetadata or
                OperationRiskFlag.FilesystemRead or OperationRiskFlag.InteractiveUi))
        {
            return BridgeTier.ReadOnlyRestricted;
        }

        return BridgeTier.Mutating;
    }

    private static bool IsExcluded(OperationCapability capability)
    {
        if (capability.RiskFlags.Any(flag => ExcludedFlags.Contains(flag) || !IsKnownFlag(flag)))
        {
            return true;
        }

        if (capability.ExecutionScope is not (OperationExecutionScope.SelfContained or
            OperationExecutionScope.GlobalStateRead or OperationExecutionScope.GlobalStateMutation))
        {
            return true;
        }

        if (capability.Effect is not (OperationEffect.ReadOnly or OperationEffect.Mutating))
        {
            return true;
        }

        // An unreviewed row reports empty risk flags and unspecified classes; empty
        // flags must not read as "no risk" then.
        return capability.DurationClass is not (OperationDurationClass.Quick or
                OperationDurationClass.LongRunning or OperationDurationClass.Interactive) ||
            capability.ValidationStatus is not (OperationValidationStatus.NoRecordedGap or
                OperationValidationStatus.FixturePending or OperationValidationStatus.AtRiskUnvalidated);
    }

    private static bool IsKnownFlag(OperationRiskFlag flag) =>
        flag is >= OperationRiskFlag.FilesystemMetadata and <= OperationRiskFlag.ExternalIo &&
        Enum.IsDefined(flag);
}
