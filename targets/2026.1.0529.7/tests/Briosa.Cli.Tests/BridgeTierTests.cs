using Briosa.Cli.Engine;
using Xunit.Abstractions;

namespace Briosa.Cli.Tests;

/// <summary>
/// Every registered operation maps to exactly one bridge tier from its discovery
/// metadata, and the per-tier counts are pinned so a classification change has to
/// be looked at.
/// </summary>
public sealed class BridgeTierTests(ITestOutputHelper output)
{
    private static readonly IReadOnlyDictionary<BridgeTier, int> PinnedCounts = new Dictionary<BridgeTier, int>
    {
        [BridgeTier.ReadOnly] = 184,
        [BridgeTier.ReadOnlyRestricted] = 17,
        [BridgeTier.Mutating] = 645,
        [BridgeTier.Excluded] = 172
    };

    [Fact]
    public void EveryRegisteredOperationHasExactlyOneTierAndTheCountsArePinned()
    {
        var capabilities = ServerProjection.AllCapabilities.Operations;
        Assert.Equal(ServerProjection.Registry.Count, capabilities.Count);
        Assert.True(ServerProjection.Registry.Select(operation => operation.OperationId)
            .ToHashSet(StringComparer.Ordinal)
            .SetEquals(capabilities.Select(operation => operation.OperationId)));

        var tiers = capabilities.ToDictionary(operation => operation.OperationId, BridgeTiers.Classify,
            StringComparer.Ordinal);
        var counts = BridgeTiers.All.ToDictionary(tier => tier, tier => tiers.Values.Count(value => value == tier));
        foreach (var group in capabilities.GroupBy(operation =>
                     $"{operation.Effect} {operation.ExecutionScope} [{string.Join(",", operation.RiskFlags)}] -> {BridgeTiers.Classify(operation)}")
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            output.WriteLine($"{group.Count(),4} {group.Key}");
        }

        foreach (var tier in BridgeTiers.All)
        {
            output.WriteLine($"{BridgeTiers.Name(tier)}: {counts[tier]}");
        }

        Assert.Equal(capabilities.Count, counts.Values.Sum());
        Assert.Equal(PinnedCounts, counts);
    }

    // The five flags the agent bridge never executes, as the architecture record names them.
    private static readonly OperationRiskFlag[] ExcludedFlags =
    [
        OperationRiskFlag.PhysicalMotion,
        OperationRiskFlag.DeviceSession,
        OperationRiskFlag.DeviceConfig,
        OperationRiskFlag.CodeExecution,
        OperationRiskFlag.ExternalIo
    ];

    [Fact]
    public void TheExcludedFlagsAreExactlyTheFiveTheRecordNames() =>
        Assert.Equal(ExcludedFlags, BridgeTiers.ExcludedFlags);

    [Fact]
    public void EveryOperationWithAnExcludedFlagOrAnExclusiveScopeIsExcluded()
    {
        foreach (var operation in ServerProjection.AllCapabilities.Operations)
        {
            var mustExclude = operation.RiskFlags.Any(flag => ExcludedFlags.Contains(flag)) ||
                operation.ExecutionScope == OperationExecutionScope.ExclusiveWorkflow;
            Assert.Equal(mustExclude, BridgeTiers.Classify(operation) == BridgeTier.Excluded);
        }
    }

    [Fact]
    public void ReadOnlyOperationsThatReadFileContentOrOpenOperatorUiAreRestricted()
    {
        var operations = ServerProjection.AllCapabilities.Operations
            .Where(operation => operation.Effect == OperationEffect.ReadOnly &&
                !operation.RiskFlags.Any(flag => ExcludedFlags.Contains(flag)) &&
                operation.ExecutionScope != OperationExecutionScope.ExclusiveWorkflow)
            .ToArray();
        var fileContent = operations.Where(operation => operation.RiskFlags.Contains(OperationRiskFlag.FilesystemRead)).ToArray();
        var operatorUi = operations.Where(operation => operation.RiskFlags.Contains(OperationRiskFlag.InteractiveUi)).ToArray();
        output.WriteLine($"read-only filesystem_read: {fileContent.Length}; read-only interactive_ui: {operatorUi.Length}");
        Assert.Equal(6, fileContent.Length);
        Assert.Equal(11, operatorUi.Length);
        Assert.All(fileContent.Concat(operatorUi), operation =>
            Assert.Equal(BridgeTier.ReadOnlyRestricted, BridgeTiers.Classify(operation)));
    }

    [Fact]
    public void ReadOnlyTierOperationsCarryNoFlagButFilesystemMetadata()
    {
        foreach (var operation in ServerProjection.AllCapabilities.Operations
                     .Where(operation => BridgeTiers.Classify(operation) == BridgeTier.ReadOnly))
        {
            Assert.Equal(OperationEffect.ReadOnly, operation.Effect);
            Assert.All(operation.RiskFlags, flag => Assert.Equal(OperationRiskFlag.FilesystemMetadata, flag));
            Assert.Contains(operation.ExecutionScope,
                new[] { OperationExecutionScope.SelfContained, OperationExecutionScope.GlobalStateRead });
        }
    }

    [Fact]
    public void UnreviewedOrUnrecognizedMetadataFailsClosed()
    {
        var reviewed = ServerProjection.Capability("file_operations.get_working_directory");
        Assert.Equal(BridgeTier.ReadOnly, BridgeTiers.Classify(reviewed));

        var unreviewed = reviewed.Clone();
        unreviewed.RiskFlags.Clear();
        unreviewed.DurationClass = OperationDurationClass.Unspecified;
        unreviewed.ValidationStatus = OperationValidationStatus.Unspecified;
        Assert.Equal(BridgeTier.Excluded, BridgeTiers.Classify(unreviewed));

        foreach (var mutate in new Action<OperationCapability>[]
                 {
                     capability => capability.RiskFlags.Add((OperationRiskFlag)42),
                     capability => capability.RiskFlags.Add(OperationRiskFlag.Unspecified),
                     capability => capability.Effect = OperationEffect.Unknown,
                     capability => capability.Effect = (OperationEffect)9,
                     capability => capability.ExecutionScope = OperationExecutionScope.Unknown,
                     capability => capability.ExecutionScope = OperationExecutionScope.Unspecified,
                     capability => capability.ExecutionScope = (OperationExecutionScope)9,
                     capability => capability.DurationClass = (OperationDurationClass)9,
                     capability => capability.ValidationStatus = (OperationValidationStatus)9
                 })
        {
            var changed = reviewed.Clone();
            mutate(changed);
            Assert.Equal(BridgeTier.Excluded, BridgeTiers.Classify(changed));
        }
    }
}
