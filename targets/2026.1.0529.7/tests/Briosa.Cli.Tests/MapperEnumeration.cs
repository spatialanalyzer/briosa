using System.Globalization;
using Briosa.Cli.Engine;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Xunit.Abstractions;

namespace Briosa.Cli.Tests;

/// <summary>
/// Every reply the server's outcome mapper and executor produce over finite domains
/// of their inputs. Each worker outcome is built through its own constructor, which
/// rejects the combinations the server cannot produce, and is then mapped by the
/// server's real code. What the domains cannot cover is listed in
/// <see cref="Limits"/>.
/// </summary>
internal static class MapperEnumeration
{
    /// <summary>The input domains this enumeration does not cover exhaustively.</summary>
    public const string Limits =
        "Worker diagnostic codes: every code the mapper writes or falls back to, one ordinary code, and one the " +
        "mapper must replace, not every string. MP result codes: a sample of nine. Operations: six representative " +
        "ones (no outputs, several outputs, nested and dotted output names, repeated output names, and one whose MP " +
        "code 4 is a failure); AllOperationsGenuineResponseTests covers the completed paths of every operation. " +
        "Worker generations: 0 and 3. Policy denials: the server's own rules from its denial factory, over every " +
        "profile, every risk flag, and with and without a request option; the caller rules from the real policy with " +
        "this CLI's restriction, over every operation and reviewed request option; a denial is not crossed with " +
        "connections or deadlines, which the mapper does not read for it. Output values: one typed value per kind.";

    /// <summary>Each code the mapper writes itself or falls back to, and one ordinary worker code.</summary>
    public static IReadOnlyList<string> WorkerCodes { get; } =
    [
        "probe-worker-code", "sa-modal-dialog-open", "worker-output-shape-invalid", "request-violation-after-mapping",
        "result-mapping-failed", "request-validation-failed", "caller-restriction-invalid", "worker-execution-failed",
        "worker-result-missing", "internal-operation-failure", "operation-unsupported", "sdk-argument-rejected",
        "execute-step-rejected", "sdk-mp-result-retrieval-failed", "mp-command-failed", "worker-output-encoding-rejected",
        "sdk-output-retrieval-failed", WorkerSdkFaultDiagnosticCodes.BeforeExecute, WorkerSdkFaultDiagnosticCodes.ExecuteStep,
        WorkerSdkFaultDiagnosticCodes.MpResultRetrieval, WorkerSdkFaultDiagnosticCodes.OutputGetter
    ];

    /// <summary>A worker code the mapper must replace with its fallback.</summary>
    public const string UnnormalizedCode = "Not A Code";

    public static IReadOnlyList<int> MpCodes { get; } = [int.MinValue, -1, 0, 1, 2, 3, 4, 5, int.MaxValue];

    public static IReadOnlyList<ReplaySafety> ReplaySafeties { get; } = [ReplaySafety.Safe, ReplaySafety.Unsafe, ReplaySafety.Unknown];

    public static IReadOnlyList<GenuineOperation> Operations { get; } =
    [
        GenuineResponses.Operations.First(operation => operation.Contracts.Count == 0 && operation.Tier == BridgeTier.ReadOnly),
        GenuineResponses.Operation("analysis_operations.get_ith_point_from_group"),
        GenuineResponses.Operation("relationship_operations.get_relationship_status"),
        GenuineResponses.Operation("gdt_operations.get_gdt_options"),
        GenuineResponses.Operation("instrument_operations.get_observation_info"),
        GenuineResponses.Operation(GenuineResponses.PartialSuccessFailures.Order(StringComparer.Ordinal).First())
    ];

    private static readonly Lazy<Enumerated> LazyAll = new(Build);

    public static Enumerated All => LazyAll.Value;

    private static Enumerated Build()
    {
        var produced = new List<Produced>();
        var attempted = 0;
        var rejected = 0;
        void Add(GenuineOperation operation, ReplaySafety safety, string path, GenuineReply? reply)
        {
            attempted++;
            if (reply is null)
            {
                rejected++;
                return;
            }

            produced.Add(new Produced(operation, safety, path, reply));
            if (reply.Details is { } details)
            {
                // The executor's answer when the result mapping rejects these values.
                produced.Add(new Produced(operation, safety, "result-mapping-failed",
                    GenuineResponses.ResultMappingFailure(operation, safety, details)));
            }
        }

        // Outcomes that did not complete carry no MP details, so one operation shows them.
        var transport = Operations[1];
        string[] statuses =
            ["PolicyDenied", "Unsupported", "Unavailable", "ClientCancelled", "WatchdogTimeout", "WorkerFailure", "RequestRejected", "Overloaded", "Completed"];
        string[] dispositions = ["NotStarted", "StartedOutcomeUnknown", "Completed"];
        foreach (var safety in ReplaySafeties)
        {
            foreach (var status in statuses)
            {
                // A denial is mapped from the status, disposition, code, and denial
                // alone, so its many payloads are not crossed with connections and
                // deadlines, which the mapper reads only for other statuses.
                var denied = status == "PolicyDenied";
                var denials = denied ? GenuineResponses.PolicyDenials.Cast<object?>().ToArray() : new object?[] { null };
                var connections = denied ? [null] : Connections().ToArray();
                var deadlines = denied ? new[] { false } : [false, true];
                foreach (var disposition in dispositions)
                {
                    foreach (var connection in connections)
                    {
                        foreach (var code in WorkerCodes.Append(UnnormalizedCode))
                        {
                            foreach (var deadline in deadlines)
                            {
                                foreach (var denial in denials)
                                {
                                    Add(transport, safety, $"transport:{status}", GenuineResponses.MapTransport(transport,
                                        status, disposition, connection, code, deadline, denial, safety));
                                }
                            }
                        }
                    }
                }
            }
        }

        foreach (var operation in Operations)
        {
            foreach (var safety in ReplaySafeties)
            {
                foreach (var (path, execution) in Executions(operation))
                {
                    Add(operation, safety, path, GenuineResponses.Map(operation, execution, safety));
                }

                Add(operation, safety, "caller-restriction-invalid",
                    GenuineResponses.Validation(operation, "caller-restriction-invalid", safety, null));
                Add(operation, safety, "request-validation",
                    GenuineResponses.Validation(operation, "request-validation-failed", safety, null));
                Add(operation, safety, "request-validation-with-field",
                    GenuineResponses.Validation(operation, "request-validation-failed", safety,
                        new RequestValidationDetails { FieldPath = "probe", Constraint = RequestFieldConstraint.Required }));
                Add(operation, safety, "request-violation-after-mapping",
                    GenuineResponses.RequestViolationAfterMapping(operation, safety));
            }
        }

        return new Enumerated(produced, attempted, rejected);
    }

    private static IEnumerable<WorkerConnectionSnapshot?> Connections()
    {
        yield return null;
        foreach (var state in new[] { WorkerConnectionState.Disconnected, WorkerConnectionState.Connected, WorkerConnectionState.Faulted })
        {
            foreach (var readiness in Enum.GetValues<WorkerExecutionReadinessState>())
            {
                yield return new WorkerConnectionSnapshot(state, readiness, null, 1, 1, "probe-connection", DateTimeOffset.UnixEpoch);
            }
        }
    }

    private static IEnumerable<(string Path, WorkerMpExecutionResult Execution)> Executions(GenuineOperation operation)
    {
        foreach (var code in WorkerCodes.Append(UnnormalizedCode).Prepend(null))
        {
            yield return ("arguments-rejected", new WorkerArgumentsRejected(1, code));
            yield return ("execute-rejected", new WorkerExecuteRejected(1, code));
            yield return ("result-unavailable", new WorkerMpResultUnavailable(1, code));
            foreach (var mpCode in MpCodes)
            {
                if (!WorkerMpResultCodes.PermitsOutputRetrieval(mpCode))
                {
                    yield return ($"result-{mpCode}", new WorkerMpResultAvailable(mpCode, 1, [], code));
                    continue;
                }

                foreach (var (label, outputs) in OutputVariants(operation))
                {
                    yield return ($"result-{mpCode}-{label}", new WorkerMpResultAvailable(mpCode, 1, outputs, code));
                }

                yield return ($"outputs-unavailable-{mpCode}", new WorkerMpOutputsUnavailable(mpCode, 1, code));
            }
        }

        foreach (var code in WorkerCodes.Append(UnnormalizedCode))
        {
            foreach (var phase in new[] { WorkerSdkCallPhase.BeforeExecute, WorkerSdkCallPhase.ExecuteStep, WorkerSdkCallPhase.MpResultRetrieval })
            {
                yield return ($"fault-{phase}", new WorkerSdkCallFaulted(phase, 1, [], code));
            }

            foreach (var mpCode in new[] { 2, 4 })
            {
                foreach (var (label, outputs) in OutputVariants(operation).Where(variant =>
                    variant.Outputs.Any(output => !output.Retrieved)).Append(("withheld", Array.Empty<WorkerMpOutputValue>())))
                {
                    yield return ($"fault-getter-{mpCode}-{label}",
                        new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1, outputs, code, mpCode));
                }
            }
        }
    }

    // The outputs a worker can return after MP code 2 or 4: all retrieved, each
    // unavailable reason on the first, last, or every output, and outputs that do
    // not match the operation's contracts.
    private static IEnumerable<(string Label, IReadOnlyList<WorkerMpOutputValue> Outputs)> OutputVariants(GenuineOperation operation)
    {
        var retrieved = GenuineResponses.Retrieved(operation);
        var contracts = operation.Contracts;
        yield return ("all-retrieved", retrieved);
        var stranger = new WorkerUnavailableOutput("Another Output", WorkerMpValueKind.Text);
        yield return ("extra", [.. retrieved, stranger]);
        if (contracts.Count == 0)
        {
            yield break;
        }

        foreach (var reason in Enum.GetValues<WorkerUnavailableOutputReason>())
        {
            yield return ($"first-unavailable-{reason}", [GenuineResponses.Unavailable(contracts[0], reason), .. retrieved.Skip(1)]);
            yield return ($"last-unavailable-{reason}", [.. retrieved.SkipLast(1), GenuineResponses.Unavailable(contracts[^1], reason)]);
            yield return ($"all-unavailable-{reason}", contracts.Select(contract => GenuineResponses.Unavailable(contract, reason)).ToArray());
        }

        yield return ("missing-last", retrieved.SkipLast(1).ToArray());
        yield return ("first-mismatched", [stranger, .. retrieved.Skip(1)]);
    }
}

internal sealed record Produced(GenuineOperation Operation, ReplaySafety ReplaySafety, string Path, GenuineReply Reply);

internal sealed record Enumerated(IReadOnlyList<Produced> Produced, int Attempted, int Rejected);

/// <summary>
/// The CLI against everything <see cref="MapperEnumeration"/> produces: every genuine
/// reply is its real outcome, and every single change to a genuine typed error is
/// accepted exactly when the mapper also produces the changed error.
/// </summary>
public sealed class MapperEnumerationTests(ITestOutputHelper output)
{
    [Fact]
    public void EveryReplyTheMapperProducesIsItsRealOutcome()
    {
        var all = MapperEnumeration.All;
        var failures = new List<string>();
        foreach (var produced in all.Produced)
        {
            var envelope = GenuineResponses.Classify(produced.Operation, produced.Reply, produced.ReplaySafety);
            var expected = AllOperationsGenuineResponseTests.Expected(produced.Reply);
            var code = envelope.Failure?["diagnostic_code"]?.GetValue<string>();
            if (envelope.Outcome != expected || code is not null && AllOperationsGenuineResponseTests.IsConflict(code))
            {
                failures.Add($"{produced.Operation.OperationId} {produced.ReplaySafety} {produced.Path}: {envelope.Outcome} ({code}), expected {expected}");
            }
        }

        var errors = all.Produced.Where(produced => produced.Reply.Error is not null).ToArray();
        output.WriteLine($"attempted outcomes: {all.Attempted}; rejected by their constructors: {all.Rejected}; " +
            $"replies: {all.Produced.Count} ({all.Produced.Count - errors.Length} results, {errors.Length} typed errors); " +
            $"distinct error signatures: {errors.Select(Signature).Distinct(StringComparer.Ordinal).Count()}; " +
            $"failure kinds: {errors.Select(produced => Parse(produced).Kind).Distinct().Count()}; failures: {failures.Count}");
        output.WriteLine("Not covered: " + MapperEnumeration.Limits);
        foreach (var failure in failures.Take(40))
        {
            output.WriteLine(failure);
        }

        Assert.Empty(failures);
        Assert.Equal(Enum.GetValues<OperationFailureKind>().Length - 1,
            errors.Select(produced => Parse(produced).Kind).Distinct().Count());
    }

    [Fact]
    public void EverySingleChangeToAGenuineErrorIsAcceptedOnlyWhenTheMapperProducesIt()
    {
        var errors = MapperEnumeration.All.Produced.Where(produced => produced.Reply.Error is not null).ToArray();
        var producible = errors.Select(Signature).ToHashSet(StringComparer.Ordinal);
        var sources = errors.DistinctBy(SourceKey, StringComparer.Ordinal).ToArray();
        var failures = new List<string>();
        int mutations = 0, accepted = 0, refused = 0, gaps = 0;
        foreach (var source in sources)
        {
            var original = Parse(source);
            var signature = Signature(source);
            foreach (var (label, status, error) in Mutations(source.Operation, source.Reply.Error!.StatusCode, original))
            {
                var changed = Signature(source.Operation, source.ReplaySafety, status, error);
                if (changed == signature)
                {
                    continue;
                }

                mutations++;
                var envelope = GenuineResponses.Classify(source.Operation, status, error, source.ReplaySafety);
                var code = envelope.Failure?["diagnostic_code"]?.GetValue<string>();
                var conflict = envelope.Outcome == BridgeOutcome.CompletionEvidenceUnavailable &&
                    code is not null && AllOperationsGenuineResponseTests.IsConflict(code);
                var describe = $"{source.Operation.OperationId} {source.ReplaySafety} {source.Path} {label}: {envelope.Outcome} ({code})";
                if (producible.Contains(changed))
                {
                    accepted++;
                    if (conflict || envelope.Outcome != AllOperationsGenuineResponseTests.Expected(error))
                    {
                        failures.Add("producible but " + describe);
                    }
                }
                else if (conflict)
                {
                    refused++;
                }
                else if (IsKnownGap(source, status, error, producible))
                {
                    gaps++;
                }
                else
                {
                    failures.Add("never produced but " + describe);
                }
            }
        }

        output.WriteLine($"genuine typed errors: {errors.Length}; mutation sources: {sources.Length}; single changes: {mutations}; " +
            $"producible and accepted: {accepted}; never produced and refused: {refused}; known gaps accepted: {gaps}; " +
            $"failures: {failures.Count}");
        output.WriteLine("Known gap: the CLI has no per-operation table of the operations whose MP code 4 the server " +
            "reports as an MP failure, so it accepts code 4 as an MP failure of any operation, and as a partial success " +
            "followed by an output or mapping failure of those operations too.");
        foreach (var failure in failures.Take(40))
        {
            output.WriteLine(failure);
        }

        Assert.Empty(failures);
        Assert.True(refused > 0 && accepted > 0);
    }

    private static OperationError Parse(Produced produced) =>
        OperationError.Parser.ParseFrom(produced.Reply.Error!.Trailers.Single().ValueBytes);

    private static string Signature(Produced produced) =>
        Signature(produced.Operation, produced.ReplaySafety, produced.Reply.Error!.StatusCode, Parse(produced));

    // Everything the mapper's output depends on, with free-form detail reduced to the
    // facts the mapper fixes: which row states occur, whether an MP code is 2, 4, or
    // another value, and a denial's rule with which of its fields are set, blank, or
    // absent, and the exact caller values, which name the restriction this CLI sent.
    private static string Signature(GenuineOperation operation, ReplaySafety replaySafety, StatusCode status, OperationError error)
    {
        var mp = error.MpExecution is { } details
            ? string.Create(CultureInfo.InvariantCulture,
                $"{(int)details.State}/{(details.HasMpResultCode ? details.MpResultCode is 2 or 4 ? details.MpResultCode.ToString(CultureInfo.InvariantCulture) : "other" : "none")}/" +
                $"{(details.OutputRetrievals.Count == 0 ? "no-rows" : string.Join(',', details.OutputRetrievals.Select(row => (int)row.State).Distinct().Order()))}")
            : "none";
        static string Field(bool present, string value) =>
            !present ? "absent" : string.IsNullOrWhiteSpace(value) ? "blank" : "set";
        var rule = error.PolicyDenial is not { } denial ? "absent"
            : string.Create(CultureInfo.InvariantCulture,
                $"{(int)denial.Rule}:{Field(denial.HasDeniedFlag, denial.DeniedFlag)}:{Field(denial.HasAdmissionProfile, denial.AdmissionProfile)}:" +
                $"{Field(denial.HasRequestOptionField, denial.RequestOptionField)}:" +
                $"{(denial.HasCallerAdmissionProfile ? "=" + denial.CallerAdmissionProfile : "absent")}:" +
                $"{(denial.HasCallerDeniedFlag ? "=" + denial.CallerDeniedFlag : "absent")}");
        var generation = !error.HasWorkerGeneration ? "none" : error.WorkerGeneration >= 0 ? "present" : "negative";
        // Only MP details depend on the operation's own outputs; every other error the
        // mapper builds is the same for every operation, apart from its ID.
        var operationKey = error.MpExecution is null ? "any-operation" : operation.OperationId;
        return string.Create(CultureInfo.InvariantCulture,
            $"{operationKey}|{(int)replaySafety}|{(int)status}|{error.OperationId == operation.OperationId}|{(int)error.Kind}|" +
            $"{error.DiagnosticCode}|{(int)error.ExecutionDisposition}|{(int)error.RecoveryGuidance}|{(int)error.ReplayGuidance}|" +
            $"{(int)error.ReplaySafety}|{generation}|{rule}|{error.RequestValidation is not null}|{mp}");
    }

    // One source per shape, and one more for each code the mapper writes itself.
    private static string SourceKey(Produced produced)
    {
        var error = Parse(produced);
        string[] fixedCodes = ["sa-modal-dialog-open", "request-validation-failed", "worker-output-shape-invalid",
            "request-violation-after-mapping", "result-mapping-failed"];
        var code = fixedCodes.Contains(error.DiagnosticCode, StringComparer.Ordinal) ? error.DiagnosticCode : "*";
        error.DiagnosticCode = code;
        return Signature(produced.Operation, produced.ReplaySafety, produced.Reply.Error!.StatusCode, error);
    }

    // The CLI cannot tell the operations whose MP code 4 the server reports as an MP
    // failure from the others: it accepts code 4 as an MP failure of any operation,
    // and as a partial success, after which an output or the mapping failed, of
    // those operations too. Each is accepted here only when the same error with the
    // other reading of code 4 is one the mapper produces.
    private static bool IsKnownGap(Produced source, StatusCode status, OperationError error, HashSet<string> producible)
    {
        var failsOnFour = GenuineResponses.PartialSuccessFailures.Contains(source.Operation.OperationId);
        var other = error.Clone();
        // Outside the read-only tier only: a served read never reports code 4 as a failure.
        if (!failsOnFour && source.Operation.Context.CodeFourMayBeFailure &&
            error is { Kind: OperationFailureKind.MpFailure, MpExecution: { HasMpResultCode: true, MpResultCode: 4 } })
        {
            other.MpExecution.MpResultCode = 0;
        }
        else if (failsOnFour && error is { MpExecution: { State: MpExecutionState.PartialSuccess, HasMpResultCode: true, MpResultCode: 4 } })
        {
            other.MpExecution.State = MpExecutionState.Succeeded;
            other.MpExecution.MpResultCode = 2;
        }
        else
        {
            return false;
        }

        return producible.Contains(Signature(source.Operation, source.ReplaySafety, status, other));
    }

    // Each detail field cleared, blank, or set, and each caller field set to a value
    // this CLI's restriction does or does not name.
    private static IEnumerable<(string Label, Action<PolicyDenialDetails> Change)> DenialFieldChanges(PolicyDenialDetails denial)
    {
        yield return (denial.HasDeniedFlag ? "no-denied-flag" : "denied-flag", denial.HasDeniedFlag
            ? details => details.ClearDeniedFlag()
            : details => details.DeniedFlag = "physical_motion");
        yield return ("blank-denied-flag", details => details.DeniedFlag = "");
        yield return (denial.HasAdmissionProfile ? "no-admission-profile" : "admission-profile", denial.HasAdmissionProfile
            ? details => details.ClearAdmissionProfile()
            : details => details.AdmissionProfile = "standard");
        yield return ("blank-admission-profile", details => details.AdmissionProfile = "");
        yield return (denial.HasRequestOptionField ? "no-option" : "option", denial.HasRequestOptionField
            ? details => details.ClearRequestOptionField()
            : details => details.RequestOptionField = "show_results_dialog");
        yield return ("blank-option", details => details.RequestOptionField = "");
        yield return ("no-caller-profile", details => details.ClearCallerAdmissionProfile());
        foreach (var profile in new[] { "read-only", "standard", "full", "" })
        {
            yield return ($"caller-profile={profile}", details => details.CallerAdmissionProfile = profile);
        }

        yield return ("no-caller-flag", details => details.ClearCallerDeniedFlag());
        foreach (var flag in new[] { "interactive_ui", "physical_motion", "filesystem_read", "" })
        {
            yield return ($"caller-flag={flag}", details => details.CallerDeniedFlag = flag);
        }
    }

    private static IEnumerable<(string Label, StatusCode Status, OperationError Error)> Mutations(
        GenuineOperation operation, StatusCode status, OperationError original)
    {
        OperationError Change(Action<OperationError> change)
        {
            var error = original.Clone();
            change(error);
            return error;
        }

        foreach (var value in Enum.GetValues<StatusCode>())
        {
            yield return ($"status={value}", value, original);
        }

        foreach (var value in Enum.GetValues<OperationFailureKind>().Append((OperationFailureKind)99))
        {
            yield return ($"kind={value}", status, Change(error => error.Kind = value));
        }

        foreach (var value in Enum.GetValues<ExecutionDisposition>().Append((ExecutionDisposition)9))
        {
            yield return ($"disposition={value}", status, Change(error => error.ExecutionDisposition = value));
        }

        foreach (var value in Enum.GetValues<RecoveryGuidance>().Append((RecoveryGuidance)9))
        {
            yield return ($"recovery={value}", status, Change(error => error.RecoveryGuidance = value));
        }

        foreach (var value in Enum.GetValues<ReplayGuidance>().Append((ReplayGuidance)9))
        {
            yield return ($"replay={value}", status, Change(error => error.ReplayGuidance = value));
        }

        foreach (var value in Enum.GetValues<ReplaySafety>().Append((ReplaySafety)9))
        {
            yield return ($"replay-safety={value}", status, Change(error => error.ReplaySafety = value));
        }

        foreach (var code in MapperEnumeration.WorkerCodes)
        {
            yield return ($"code={code}", status, Change(error => error.DiagnosticCode = code));
        }

        yield return ("another-operation", status, Change(error => error.OperationId = "file_operations.get_working_directory"));
        yield return ("no-generation", status, Change(error => error.ClearWorkerGeneration()));
        yield return ("negative-generation", status, Change(error => error.WorkerGeneration = -1));
        yield return (original.PolicyDenial is null ? "add-policy-denial" : "remove-policy-denial", status,
            Change(error => error.PolicyDenial = error.PolicyDenial is null ? new PolicyDenialDetails { Rule = PolicyDenialRule.Override } : null));
        if (original.PolicyDenial is not null)
        {
            foreach (var value in Enum.GetValues<PolicyDenialRule>().Append((PolicyDenialRule)99))
            {
                yield return ($"rule={value}", status, Change(error => error.PolicyDenial.Rule = value));
            }

            foreach (var (label, change) in DenialFieldChanges(original.PolicyDenial))
            {
                yield return (label, status, Change(error => change(error.PolicyDenial)));
            }
        }

        yield return (original.RequestValidation is null ? "add-request-validation" : "remove-request-validation", status,
            Change(error => error.RequestValidation = error.RequestValidation is null
                ? new RequestValidationDetails { FieldPath = "probe", Constraint = RequestFieldConstraint.Required }
                : null));
        if (original.MpExecution is null)
        {
            yield return ("add-mp-details", status, Change(error =>
            {
                error.MpExecution = new MpExecutionDetails { State = MpExecutionState.Failed, MpResultCode = 0 };
                error.MpExecution.OutputRetrievals.AddRange(operation.Contracts.Select(contract =>
                    new OutputRetrievalDetails { FieldName = contract.FieldName, State = OutputRetrievalState.NotAttempted }));
            }));
            yield break;
        }

        yield return ("remove-mp-details", status, Change(error => error.MpExecution = null));
        foreach (var value in Enum.GetValues<MpExecutionState>().Append((MpExecutionState)99))
        {
            yield return ($"mp-state={value}", status, Change(error => error.MpExecution.State = value));
        }

        yield return ("no-mp-code", status, Change(error => error.MpExecution.ClearMpResultCode()));
        foreach (var value in new[] { 0, 2, 4 })
        {
            yield return ($"mp-code={value}", status, Change(error => error.MpExecution.MpResultCode = value));
        }

        for (var index = 0; index < Math.Min(3, original.MpExecution.OutputRetrievals.Count); index++)
        {
            var row = index;
            foreach (var value in Enum.GetValues<OutputRetrievalState>().Append((OutputRetrievalState)9))
            {
                yield return ($"row{row}={value}", status, Change(error => error.MpExecution.OutputRetrievals[row].State = value));
            }
        }
    }
}
