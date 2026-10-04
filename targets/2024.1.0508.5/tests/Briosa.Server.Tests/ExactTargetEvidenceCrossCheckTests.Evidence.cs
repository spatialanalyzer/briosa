using System.Text.Json.Nodes;
using Briosa.Server.Operations;

namespace Briosa.Server.Tests;

/// <summary>
/// SA 2024 evidence for the shared cross-check: the curated public binding facts in
/// <c>evidence/sa/2024.1.0508.5/bindings.json</c> (#278, #285, decision #247). The test reads only that
/// committed file, never private <c>briosa-evidence</c> exports.
/// </summary>
/// <remarks>
/// Facts are keyed by operation id. Their <c>mp_step</c> is the SDK step text and is compared ordinally
/// with the descriptor step. Each argument's setter or getter carries its exact <c>sdk_name</c> unless
/// that side is <c>not_observed</c>, which matches the inventory shape the 2026 target reads.
/// <c>unavailable</c> sides (exporter <c>NOT_SUPPORTED</c>) keep the label with a null method.
/// </remarks>
public sealed partial class ExactTargetEvidenceCrossCheckTests
{
    /// <summary>
    /// Text fields whose operation rejects any value outside an exact SA 2024 choice set before it
    /// builds a command, keyed by protobuf field full name. The generated placeholder text would
    /// otherwise leave the operation unexercised.
    /// </summary>
    private static readonly Dictionary<string, string> RepresentativeText = new(StringComparer.Ordinal)
    {
        ["briosa.InstrumentTypeName.value"] = "API Radian"
    };

    private static Func<string, string, OperationEvidence> LoadEvidence()
    {
        var facts = JsonNode.Parse(File.ReadAllText(Path.Combine(
            TargetRoot(), "evidence", "sa", SpatialAnalyzerApi.TargetVersion, "bindings.json")))!;
        Assert.Equal(SpatialAnalyzerApi.TargetVersion, facts["spatial_analyzer_target"]!.GetValue<string>());
        var byOperation = facts["operations"]!.AsArray().ToDictionary(
            operation => operation!["operation_id"]!.GetValue<string>(), operation => operation!, StringComparer.Ordinal);

        return (operationId, step) =>
        {
            if (!byOperation.TryGetValue(operationId, out var operation))
            {
                return new("No bindings.json operation_id equals the operation id.", []);
            }

            var factStep = operation["mp_step"]!.GetValue<string>();
            var arguments = operation["arguments"]!.AsArray()
                .Select(argument => new EvidenceArgument(ReadFact(argument!, "setter"), ReadFact(argument!, "getter")))
                .ToArray();
            return new(
                string.Equals(factStep, step, StringComparison.Ordinal)
                    ? null
                    : $"bindings.json mp_step is '{factStep}'.",
                arguments);
        };
    }

    private static EvidenceBinding ReadFact(JsonNode argument, string side)
    {
        var binding = argument[side]!;
        var status = binding["status"]!.GetValue<string>();
        return new(
            status == "not_observed" ? null : argument["sdk_name"]!.GetValue<string>(),
            status,
            binding["method"]?.GetValue<string>());
    }
}
