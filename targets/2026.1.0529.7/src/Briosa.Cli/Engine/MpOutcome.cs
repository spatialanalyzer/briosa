using System.Text.Json.Nodes;
using Google.Protobuf;
using Grpc.Core;

namespace Briosa.Cli.Engine;

/// <summary>
/// Reads one MP call's outcome without collapsing its dimensions. On gRPC OK it
/// checks <c>execution</c>; on failure it decodes the typed <c>OperationError</c>
/// trailer and never infers a disposition the server did not send.
/// </summary>
internal static class MpOutcome
{
    public const string ResultExecutionMissing = "result-execution-missing";
    public const string ResultExecutionContractViolation = "result-execution-contract-violation";
    public const string ResultOutputNotRetrieved = "result-output-not-retrieved";
    public const string TrailerMalformed = "operation-error-trailer-malformed";
    public const string DispositionUnrecognized = "operation-error-disposition-unrecognized";
    public const string ClientCancelled = "client-cancelled";
    public const string ClientDeadlineExceeded = "client-deadline-exceeded";

    private const string ReconcileGuidance =
        "Check SpatialAnalyzer's state before trying again. The CLI never retries or replays a call.";

    /// <summary>
    /// Fills the envelope's transport, failure, and execution members and returns the
    /// outcome. The result payload, if any, is delivered separately.
    /// </summary>
    public static BridgeOutcome Classify(
        MpCallObservation observation,
        string fullyQualifiedMethod,
        string callerRestriction,
        BridgeEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(envelope);
        envelope.Transport = new JsonObject
        {
            ["method"] = fullyQualifiedMethod,
            ["status"] = ProtocolJson.StatusName(observation.Status)
        };
        var execution = Members(requestSent: true, callerRestriction);
        envelope.Execution = execution;
        return observation.Response is { } response
            ? ClassifySuccess(response, execution, envelope)
            : ClassifyFailure(observation, execution, envelope);
    }

    /// <summary>The execution member for a call the CLI never sent: provably not started.</summary>
    public static JsonObject NotSent()
    {
        var execution = Members(requestSent: false, callerRestriction: null);
        execution["disposition"] = "EXECUTION_DISPOSITION_NOT_STARTED";
        execution["disposition_source"] = "cli";
        execution["completion_evidence"] = "available";
        return execution;
    }

    // Every execution member, always in this order; classification only fills values.
    private static JsonObject Members(bool requestSent, string? callerRestriction) => new()
    {
        ["request_sent"] = requestSent,
        ["caller_restriction"] = callerRestriction,
        ["disposition"] = null,
        ["disposition_source"] = null,
        ["completion_evidence"] = null,
        ["recovery_guidance"] = null,
        ["replay_guidance"] = null,
        ["replay_safety"] = null,
        ["worker_generation"] = null,
        ["mp_execution"] = null
    };

    private static BridgeOutcome ClassifySuccess(IMessage response, JsonObject execution, BridgeEnvelope envelope)
    {
        var field = ProtocolCatalog.FindExecutionField(response.Descriptor);
        var details = field is null ? null : field.Accessor.GetValue(response) as MpExecutionDetails;
        execution["disposition"] = null;
        execution["disposition_source"] = null;
        execution["recovery_guidance"] = null;
        execution["replay_guidance"] = null;
        execution["replay_safety"] = null;
        execution["worker_generation"] = null;
        execution["mp_execution"] = ProtocolJson.Optional(details);
        if (details is null)
        {
            return EvidenceUnavailable(execution, envelope, "cli", ResultExecutionMissing);
        }

        var succeeded = details.State == MpExecutionState.Succeeded && details is { HasMpResultCode: true, MpResultCode: 2 };
        var partial = details.State == MpExecutionState.PartialSuccess && details is { HasMpResultCode: true, MpResultCode: 4 };
        if (!succeeded && !partial)
        {
            // A missing code, or a state that disagrees with its code, is a
            // protocol-contract failure: the MP result cannot be trusted.
            return EvidenceUnavailable(execution, envelope, "cli", ResultExecutionContractViolation);
        }

        // gRPC OK with a consistent MP result is the server's statement that the
        // operation completed (execution outcomes guide).
        execution["disposition"] = "EXECUTION_DISPOSITION_COMPLETED";
        execution["disposition_source"] = "grpc_ok";
        execution["completion_evidence"] = "available";
        var allRetrieved = details.OutputRetrievals.All(output => output.State == OutputRetrievalState.Retrieved);
        if (partial)
        {
            envelope.Warn("partial-success",
                "SpatialAnalyzer returned MP code 4 (partial success): the command did its work but not its full " +
                "intended effect. The call is complete. Do not run it again to change the outcome.");
        }

        if (!allRetrieved)
        {
            envelope.Warn("output-not-retrieved",
                "At least one output was not retrieved. See execution.mp_execution.output_retrievals. The call is " +
                "complete; running it again to recover an output is not safe in general.");
            envelope.Failure = new JsonObject
            {
                ["source"] = "cli",
                ["kind"] = null,
                ["diagnostic_code"] = ResultOutputNotRetrieved,
                ["policy_denial"] = null,
                ["request_validation"] = null,
                ["input"] = null
            };
            return BridgeOutcome.OutputsIncomplete;
        }

        return partial ? BridgeOutcome.PartialSuccess : BridgeOutcome.Succeeded;
    }

    private static BridgeOutcome ClassifyFailure(MpCallObservation observation, JsonObject execution, BridgeEnvelope envelope)
    {
        var (error, malformed) = ReadOperationError(observation.Trailers);
        if (error is null)
        {
            execution["disposition"] = null;
            execution["disposition_source"] = null;
            execution["recovery_guidance"] = null;
            execution["replay_guidance"] = null;
            execution["replay_safety"] = null;
            execution["worker_generation"] = null;
            execution["mp_execution"] = null;
            // Without a typed trailer nothing proves where the call stopped, and no
            // disposition is invented for it.
            var code = malformed ? TrailerMalformed
                : observation.CallerCancelled ? ClientCancelled
                : observation.CallerDeadlineReached && observation.Status == StatusCode.DeadlineExceeded
                    ? ClientDeadlineExceeded
                    : ProtocolJson.TransportCode(observation.Status);
            return EvidenceUnavailable(execution, envelope, malformed ? "server" : "transport", code);
        }

        execution["disposition"] = ProtocolJson.EnumNode(error.ExecutionDisposition);
        execution["disposition_source"] = "operation_error";
        execution["recovery_guidance"] = ProtocolJson.EnumNode(error.RecoveryGuidance);
        execution["replay_guidance"] = ProtocolJson.EnumNode(error.ReplayGuidance);
        execution["replay_safety"] = ProtocolJson.EnumNode(error.ReplaySafety);
        execution["worker_generation"] = error.HasWorkerGeneration ? error.WorkerGeneration : null;
        execution["mp_execution"] = ProtocolJson.Optional(error.MpExecution);
        envelope.Failure = new JsonObject
        {
            ["source"] = "server",
            ["kind"] = ProtocolJson.EnumNode(error.Kind),
            ["diagnostic_code"] = error.DiagnosticCode,
            ["policy_denial"] = ProtocolJson.Optional(error.PolicyDenial),
            ["request_validation"] = ProtocolJson.Optional(error.RequestValidation),
            ["input"] = null
        };

        switch (error.ExecutionDisposition)
        {
            case ExecutionDisposition.NotStarted:
                execution["completion_evidence"] = "available";
                if (error.Kind == OperationFailureKind.Validation)
                {
                    envelope.Guidance.Add(error.RequestValidation is null
                        ? "The server rejected the request before running it. Correct the request and send it again."
                        : "The server rejected the request before running it. Correct the field named in " +
                          "failure.request_validation, which is the first invalid field the server found, and send it again.");
                    return BridgeOutcome.InvalidRequest;
                }

                if (error.PolicyDenial?.Rule is PolicyDenialRule.CallerProfile or PolicyDenialRule.CallerFlag)
                {
                    envelope.Warn("caller-restriction-denied",
                        "The server's own configuration admits this request, but the read-only restriction this CLI " +
                        "sends denied it. call cannot run it.");
                }

                envelope.Guidance.Add("The server did not start the operation. See failure.kind and " +
                    "execution.recovery_guidance for why.");
                return BridgeOutcome.NotStarted;
            case ExecutionDisposition.Completed:
                execution["completion_evidence"] = "available";
                if (error.MpExecution?.State is MpExecutionState.Succeeded or MpExecutionState.PartialSuccess)
                {
                    if (error.MpExecution.State == MpExecutionState.PartialSuccess)
                    {
                        envelope.Warn("partial-success",
                            "SpatialAnalyzer returned MP code 4 (partial success) before an output failed.");
                    }

                    envelope.Guidance.Add("The MP command ran and succeeded, but at least one output was not " +
                        "delivered. The call is complete; running it again to recover an output is not safe in general.");
                    return BridgeOutcome.OutputsIncomplete;
                }

                envelope.Guidance.Add("The operation ran and did not succeed. The call is complete; do not replay it.");
                return BridgeOutcome.MpFailed;
            case ExecutionDisposition.StartedOutcomeUnknown:
                execution["completion_evidence"] = "available";
                envelope.Warn("outcome-unknown",
                    "The server reports that the operation may have started and that its outcome is unknown.");
                envelope.Guidance.Add(ReconcileGuidance);
                return BridgeOutcome.StartedOutcomeUnknown;
            default:
                // Missing or unspecified disposition is never read as not started.
                // The server's own codes stay as it sent them.
                execution["completion_evidence"] = "unavailable";
                envelope.Warn(DispositionUnrecognized,
                    "The server's error did not state whether the operation ran (see execution.disposition). The " +
                    "operation may have run.");
                envelope.Guidance.Add(ReconcileGuidance);
                return BridgeOutcome.CompletionEvidenceUnavailable;
        }
    }

    private static BridgeOutcome EvidenceUnavailable(JsonObject execution, BridgeEnvelope envelope, string source, string code)
    {
        execution["completion_evidence"] = "unavailable";
        envelope.Failure = new JsonObject
        {
            ["source"] = source,
            ["kind"] = null,
            ["diagnostic_code"] = code,
            ["policy_denial"] = null,
            ["request_validation"] = null,
            ["input"] = null
        };
        envelope.Warn("completion-evidence-unavailable",
            "The request was sent, and no trustworthy evidence of how it ended came back. The operation may have run.");
        envelope.Guidance.Add(ReconcileGuidance);
        return BridgeOutcome.CompletionEvidenceUnavailable;
    }

    /// <summary>
    /// Exactly one binary <c>briosa-operation-error-bin</c> entry that decodes is a
    /// typed error. None is "no typed detail"; several, a text entry, or bytes that do
    /// not decode are malformed.
    /// </summary>
    internal static (OperationError? Error, bool Malformed) ReadOperationError(Metadata? trailers)
    {
        if (trailers is null)
        {
            return (null, false);
        }

        var entries = trailers.Where(entry => string.Equals(entry.Key, BridgeContract.OperationErrorTrailer,
            StringComparison.OrdinalIgnoreCase)).ToArray();
        if (entries.Length == 0)
        {
            return (null, false);
        }

        if (entries.Length > 1 || !entries[0].IsBinary)
        {
            return (null, true);
        }

        try
        {
            return (OperationError.Parser.ParseFrom(entries[0].ValueBytes), false);
        }
        catch (InvalidProtocolBufferException)
        {
            return (null, true);
        }
    }
}
