using System.Text.Json.Nodes;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace Briosa.Cli.Engine;

/// <summary>
/// Reads one MP call's outcome without collapsing its dimensions. On gRPC OK it
/// checks <c>execution</c> and that no error trailer contradicts the OK; on failure
/// it decodes the typed <c>OperationError</c> trailer and uses it only when
/// <see cref="EvidenceConsistency"/> accepts it as evidence about this call. It
/// never infers a disposition the server did not send.
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
    public const string ResendBlocked = "request-resend-blocked";
    public const string ServerConnectionReplaced = "server-connection-replaced";
    public const string CallerRestrictionInvalid = "caller-restriction-invalid";

    private const string ReconcileGuidance =
        "Check SpatialAnalyzer's state before trying again. The CLI never retries or replays a call.";

    /// <summary>
    /// Fills the envelope's transport, failure, and execution members and returns the
    /// outcome. The result payload, if any, is delivered separately.
    /// </summary>
    public static BridgeOutcome Classify(MpCallObservation observation, MpCallContext call, BridgeEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(envelope);
        envelope.Transport = new JsonObject
        {
            ["method"] = call.FullyQualifiedMethod,
            ["status"] = ProtocolJson.StatusName(observation.Status)
        };
        var execution = Members(requestSent: true, call.CallerRestriction);
        envelope.Execution = execution;
        if (observation.ResendBlocked)
        {
            // The HTTP stack tried to submit the request a second time; the CLI
            // stopped it, so whatever else arrived is not evidence of one call.
            envelope.Warn(ResendBlocked,
                "The connection gave up on the first attempt (the server refused the stream, or closed the " +
                "connection, before processing it), and the HTTP client then tried to send the request again. The " +
                "CLI blocked the second attempt, so the request was submitted at most once.");
            return EvidenceUnavailable(execution, envelope, "cli", ResendBlocked);
        }

        return observation.Response is { } response
            ? ClassifySuccess(response, observation.Trailers, call, execution, envelope)
            : ClassifyFailure(observation, call, execution, envelope);
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

    private static BridgeOutcome ClassifySuccess(IMessage response, Metadata? trailers, MpCallContext call,
        JsonObject execution, BridgeEnvelope envelope)
    {
        var (error, malformed) = ReadOperationError(trailers);
        if (error is not null || malformed)
        {
            // An error trailer on a successful response contradicts the success.
            return Conflict(execution, envelope, EvidenceConsistency.OkWithError);
        }

        var field = ProtocolCatalog.FindExecutionField(response.Descriptor);
        var details = field is null ? null : field.Accessor.GetValue(response) as MpExecutionDetails;
        if (details is null)
        {
            return Conflict(execution, envelope, ResultExecutionMissing);
        }

        var succeeded = details.State == MpExecutionState.Succeeded && details is { HasMpResultCode: true, MpResultCode: 2 };
        var partial = details.State == MpExecutionState.PartialSuccess && details is { HasMpResultCode: true, MpResultCode: 4 };
        if (!succeeded && !partial)
        {
            // A missing code, or a state that disagrees with its code, is a
            // protocol-contract failure: the MP result cannot be trusted.
            execution["mp_execution"] = ProtocolJson.ToNode(details);
            return Conflict(execution, envelope, ResultExecutionContractViolation);
        }

        if (EvidenceConsistency.CheckResult(details, call.ResultDescriptor) is { } unrelated)
        {
            return Conflict(execution, envelope, unrelated);
        }

        // gRPC OK with a consistent MP result is the server's statement that the
        // operation completed (execution outcomes guide).
        execution["disposition"] = "EXECUTION_DISPOSITION_COMPLETED";
        execution["disposition_source"] = "grpc_ok";
        execution["completion_evidence"] = "available";
        execution["mp_execution"] = ProtocolJson.ToNode(details);
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
            envelope.Failure = BridgeEngine.Failure("cli", ResultOutputNotRetrieved);
            return BridgeOutcome.OutputsIncomplete;
        }

        return partial ? BridgeOutcome.PartialSuccess : BridgeOutcome.Succeeded;
    }

    private static BridgeOutcome ClassifyFailure(MpCallObservation observation, MpCallContext call, JsonObject execution,
        BridgeEnvelope envelope)
    {
        var (error, malformed) = ReadOperationError(observation.Trailers);
        if (error is null)
        {
            // Without a typed trailer nothing proves where the call stopped, and no
            // disposition is invented for it.
            var code = malformed ? TrailerMalformed
                : observation.CallerCancelled ? ClientCancelled
                : observation.CallerDeadlineReached && observation.Status == StatusCode.DeadlineExceeded
                    ? ClientDeadlineExceeded
                : observation.ReconnectRefused ? ServerConnectionReplaced
                : ProtocolJson.TransportCode(observation.Status);
            return EvidenceUnavailable(execution, envelope, malformed ? "cli" : "transport", code);
        }

        if (EvidenceConsistency.CheckError(error, observation.Status, call.OperationId, call.ResultDescriptor) is { } conflict)
        {
            return Conflict(execution, envelope, conflict);
        }

        execution["disposition"] = ProtocolJson.EnumNode(error.ExecutionDisposition);
        execution["disposition_source"] = "operation_error";
        execution["recovery_guidance"] = ProtocolJson.EnumNode(error.RecoveryGuidance);
        execution["replay_guidance"] = ProtocolJson.EnumNode(error.ReplayGuidance);
        execution["replay_safety"] = ProtocolJson.EnumNode(error.ReplaySafety);
        execution["worker_generation"] = error.HasWorkerGeneration ? error.WorkerGeneration : null;
        execution["mp_execution"] = ProtocolJson.Optional(error.MpExecution);
        var failure = BridgeEngine.Failure("server", error.DiagnosticCode);
        failure["kind"] = ProtocolJson.EnumNode(error.Kind);
        failure["policy_denial"] = ProtocolJson.Optional(error.PolicyDenial);
        failure["request_validation"] = ProtocolJson.Optional(error.RequestValidation);
        envelope.Failure = failure;

        switch (error.ExecutionDisposition)
        {
            case ExecutionDisposition.NotStarted:
                execution["completion_evidence"] = "available";
                if (error.Kind == OperationFailureKind.Validation)
                {
                    envelope.Guidance.Add(string.Equals(error.DiagnosticCode, CallerRestrictionInvalid, StringComparison.Ordinal)
                        ? "The server rejected the restriction header this CLI sent, which is a defect in the CLI, not in " +
                          "the request. Nothing ran. Do not retry; report the defect."
                        : error.RequestValidation is null
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
                if (error.Kind == OperationFailureKind.MpFailure)
                {
                    envelope.Guidance.Add("The operation ran and did not succeed. The call is complete; do not replay it.");
                    return BridgeOutcome.MpFailed;
                }

                // Every other completed failure follows a terminal MP result whose
                // outputs, or whole result, the server could not deliver.
                if (error.MpExecution?.State == MpExecutionState.PartialSuccess)
                {
                    envelope.Warn("partial-success",
                        "SpatialAnalyzer returned MP code 4 (partial success) before an output failed.");
                }

                envelope.Guidance.Add("The operation completed, but its result was not fully delivered. The call is " +
                    "complete; running it again to recover the result is not safe in general.");
                return BridgeOutcome.OutputsIncomplete;
            case ExecutionDisposition.StartedOutcomeUnknown:
                execution["completion_evidence"] = "available";
                envelope.Warn("outcome-unknown",
                    "The server reports that the operation may have started and that its outcome is unknown.");
                envelope.Guidance.Add(ReconcileGuidance);
                return BridgeOutcome.StartedOutcomeUnknown;
            default:
                // An internal failure whose disposition the server could not state.
                // The server's own codes stay as it sent them.
                execution["completion_evidence"] = "unavailable";
                envelope.Warn(DispositionUnrecognized,
                    "The server's error did not state whether the operation ran (see execution.disposition). The " +
                    "operation may have run.");
                envelope.Guidance.Add(ReconcileGuidance);
                return BridgeOutcome.CompletionEvidenceUnavailable;
        }
    }

    // Conflicting evidence: nothing from the trailer or result is relied on.
    private static BridgeOutcome Conflict(JsonObject execution, BridgeEnvelope envelope, string code)
    {
        envelope.Warn("operation-evidence-conflict",
            "What the server returned is not consistent evidence about this call (see failure.diagnostic_code), so " +
            "the CLI does not rely on any of it.");
        return EvidenceUnavailable(execution, envelope, "cli", code);
    }

    private static BridgeOutcome EvidenceUnavailable(JsonObject execution, BridgeEnvelope envelope, string source, string code)
    {
        execution["disposition"] = null;
        execution["disposition_source"] = null;
        execution["recovery_guidance"] = null;
        execution["replay_guidance"] = null;
        execution["replay_safety"] = null;
        execution["worker_generation"] = null;
        execution["completion_evidence"] = "unavailable";
        envelope.Failure = BridgeEngine.Failure(source, code);
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

/// <summary>What the classifier needs to know about the call it reads.</summary>
internal sealed record MpCallContext(
    string OperationId,
    string FullyQualifiedMethod,
    string CallerRestriction,
    MessageDescriptor ResultDescriptor);
