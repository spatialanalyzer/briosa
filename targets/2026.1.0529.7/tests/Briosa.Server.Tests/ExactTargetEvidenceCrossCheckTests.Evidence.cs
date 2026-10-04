using System.Text.Json.Nodes;
using Briosa.Server.Operations;

namespace Briosa.Server.Tests;

/// <summary>
/// SA 2026 evidence for the shared cross-check: the committed exact-target inventory (#272, F23).
/// </summary>
/// <remarks>
/// Evidence is the inventory's <c>sdk_evidence[].mp_step</c> and
/// <c>sdk_binding.setter|getter.argument_name/method</c>, never documentation argument names. An
/// operation's evidence is every command whose SDK evidence step equals the descriptor step. When none
/// does, the step is reported and the command is located by its documented step only to keep checking
/// the arguments.
/// </remarks>
public sealed partial class ExactTargetEvidenceCrossCheckTests
{
    /// <summary>
    /// Text fields whose operation rejects any value outside an exact choice set before it builds a
    /// command, keyed by protobuf field full name. No SA 2026 operation needs one.
    /// </summary>
    private static readonly Dictionary<string, string> RepresentativeText = new(StringComparer.Ordinal);

    private static Func<string, string, OperationEvidence> LoadEvidence()
    {
        var inventory = JsonNode.Parse(File.ReadAllText(
            Path.Combine(TargetRoot(), "inventory", "sa", SpatialAnalyzerApi.TargetVersion, "inventory.json")))!;
        Assert.Equal(SpatialAnalyzerApi.TargetVersion, inventory["spatial_analyzer_target"]!.GetValue<string>());
        var commands = inventory["commands"]!.AsArray().Select(command => command!).ToArray();
        var bySdkStep = commands
            .SelectMany(command => command["sdk_evidence"]!.AsArray()
                .Select(evidence => (Step: evidence!["mp_step"]!.GetValue<string>(), Command: command)))
            .ToLookup(item => item.Step, item => item.Command, StringComparer.Ordinal);
        var byDocumentedStep = commands
            .ToLookup(command => command["mp_step"]!.GetValue<string>(), StringComparer.Ordinal);

        return (_, step) =>
        {
            string? stepFinding = null;
            var evidence = bySdkStep[step].ToArray();
            if (evidence.Length == 0)
            {
                stepFinding = "No inventory sdk_evidence[].mp_step equals the descriptor step.";
                evidence = byDocumentedStep[step].ToArray();
            }

            return new(stepFinding, evidence
                .SelectMany(command => command["arguments"]!.AsArray().Select(argument => argument!["sdk_binding"]!))
                .Select(binding => new EvidenceArgument(ReadBinding(binding["setter"]!), ReadBinding(binding["getter"]!)))
                .ToArray());
        };
    }

    private static EvidenceBinding ReadBinding(JsonNode binding) => new(
        binding["argument_name"]?.GetValue<string>(),
        binding["status"]!.GetValue<string>(),
        binding["method"]?.GetValue<string>());
}
