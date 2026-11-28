using Briosa.Cli.Engine;
using Briosa.Worker.Control;
using Xunit.Abstractions;

namespace Briosa.Cli.Tests;

/// <summary>
/// For every operation this target registers, genuine responses built by the server's
/// own mapper and result mapping are classified as their real outcome, and never as
/// conflicting evidence.
/// </summary>
public sealed class AllOperationsGenuineResponseTests(ITestOutputHelper output)
{
    private static readonly HashSet<string> ConflictCodes = new(StringComparer.Ordinal)
    {
        "operation-error-unrelated", "operation-error-inconsistent", "operation-error-kind-unrecognized",
        "operation-error-enum-unrecognized", "ok-with-operation-error", "result-output-unrelated",
        "result-output-missing", "result-execution-missing", "result-execution-contract-violation",
        "operation-error-trailer-malformed"
    };

    /// <summary>Whether a diagnostic code is one the CLI gives conflicting evidence.</summary>
    internal static bool IsConflict(string code) => ConflictCodes.Contains(code);

    [Fact]
    public void EveryGenuineResponseOfEveryRegisteredOperationIsItsRealOutcome()
    {
        var failures = new List<string>();
        var groups = new Dictionary<string, int>(StringComparer.Ordinal);
        var cases = new Dictionary<string, int>(StringComparer.Ordinal);
        var operations = GenuineResponses.Operations;
        Assert.Equal(1018, operations.Count);
        foreach (var operation in operations)
        {
            foreach (var (label, reply) in Replies(operation))
            {
                cases[label] = cases.GetValueOrDefault(label) + 1;
                var envelope = GenuineResponses.Classify(operation, reply);
                var expected = Expected(reply);
                var code = envelope.Failure?["diagnostic_code"]?.GetValue<string>();
                if (envelope.Outcome != expected || (code is not null && ConflictCodes.Contains(code)))
                {
                    failures.Add($"{operation.OperationId} [{operation.Tier}] {label}: {envelope.Outcome} ({code}), expected {expected}");
                    var group = $"{label} [{operation.Tier}]: {envelope.Outcome} ({code}), expected {expected}";
                    groups[group] = groups.GetValueOrDefault(group) + 1;
                }
            }
        }

        foreach (var (label, count) in cases.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            output.WriteLine($"{label}: {count}");
        }

        output.WriteLine($"operations: {operations.Count}; read-only tier: {operations.Count(o => o.Tier == BridgeTier.ReadOnly)}; " +
            $"cases: {cases.Values.Sum()}; failures: {failures.Count}");
        // An operation whose own result mapping rejects the sample values answers a
        // full MP success with result-mapping-failed, as the executor does.
        foreach (var operation in operations.Where(operation => GenuineResponses.Complete(operation, 2).Result is null))
        {
            output.WriteLine($"success mapped to result-mapping-failed: {operation.OperationId}");
        }
        foreach (var (group, count) in groups.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            output.WriteLine($"{count} x {group}");
        }

        foreach (var failure in failures.Take(60))
        {
            output.WriteLine(failure);
        }

        Assert.Empty(failures);
    }

    internal static IEnumerable<(string Label, GenuineReply Reply)> Replies(GenuineOperation operation)
    {
        var success = GenuineResponses.Complete(operation, 2);
        yield return ("success", success);
        yield return ("partial-success", GenuineResponses.Complete(operation, 4));
        if (success.Details is { } details)
        {
            yield return ("result-mapping-failed", GenuineResponses.ResultMappingFailure(operation, operation.ReplaySafety, details));
        }

        yield return ("mp-failure-0", GenuineResponses.Complete(operation, 0));
        yield return ("mp-failure-3", GenuineResponses.Complete(operation, 3));
        yield return ("outputs-unavailable-2", GenuineResponses.Map(operation, new WorkerMpOutputsUnavailable(2, 1, null)));
        yield return ("outputs-unavailable-4", GenuineResponses.Map(operation, new WorkerMpOutputsUnavailable(4, 1, null)));
        yield return ("arguments-rejected", GenuineResponses.Map(operation, new WorkerArgumentsRejected(1, "sdk-argument-rejected")));
        yield return ("execute-rejected", GenuineResponses.Map(operation, new WorkerExecuteRejected(1, "execute-step-rejected")));
        yield return ("result-unavailable", GenuineResponses.Map(operation, new WorkerMpResultUnavailable(1, null)));
        yield return ("sdk-fault-before-execute", GenuineResponses.Map(operation,
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.BeforeExecute, 1, [], "sdk-call-faulted-before-execute")));
        yield return ("sdk-fault-execute-step", GenuineResponses.Map(operation,
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.ExecuteStep, 1, [], "sdk-execute-step-faulted")));
        yield return ("sdk-fault-result-retrieval", GenuineResponses.Map(operation,
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.MpResultRetrieval, 1, [], "sdk-mp-result-retrieval-faulted")));
        yield return ("sdk-fault-output-getter-withheld", GenuineResponses.Map(operation,
            new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1, [], "sdk-output-getter-faulted", 4)));
        if (operation.Contracts.Count > 0)
        {
            yield return ("first-output-failed", GenuineResponses.FirstOutputFailed(operation));
            yield return ("sdk-fault-output-getter", GenuineResponses.Map(operation,
                new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1,
                    [GenuineResponses.Unavailable(operation.Contracts[0], WorkerUnavailableOutputReason.SdkCallFaulted),
                        .. GenuineResponses.Retrieved(operation).Skip(1)], "sdk-output-getter-faulted", 2)));
        }
    }

    // The real outcome of a reply, read from the reply itself.
    internal static BridgeOutcome Expected(GenuineReply reply)
    {
        if (reply.Result is { } result)
        {
            var details = (MpExecutionDetails)ProtocolCatalog.FindExecutionField(result.Descriptor)!.Accessor.GetValue(result);
            return details.State == MpExecutionState.PartialSuccess ? BridgeOutcome.PartialSuccess : BridgeOutcome.Succeeded;
        }

        return Expected(OperationError.Parser.ParseFrom(reply.Error!.Trailers.Single().ValueBytes));
    }

    // The real outcome of a typed error, read from its disposition and kind.
    internal static BridgeOutcome Expected(OperationError error)
    {
        return error.ExecutionDisposition switch
        {
            ExecutionDisposition.NotStarted when error.Kind == OperationFailureKind.Validation => BridgeOutcome.InvalidRequest,
            ExecutionDisposition.NotStarted => BridgeOutcome.NotStarted,
            ExecutionDisposition.StartedOutcomeUnknown => BridgeOutcome.StartedOutcomeUnknown,
            ExecutionDisposition.Completed when error.Kind == OperationFailureKind.MpFailure => BridgeOutcome.MpFailed,
            ExecutionDisposition.Completed => BridgeOutcome.OutputsIncomplete,
            _ => BridgeOutcome.CompletionEvidenceUnavailable
        };
    }
}
