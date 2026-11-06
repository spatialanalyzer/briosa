using System.Globalization;
using System.Text;
using System.Text.Json;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// Writes a session as structured JSON and as a markdown evidence draft in the
/// style of docs/testing/evidence. Neither contains argument or returned values,
/// manifest contents, paths, or raw exception text.
/// </summary>
internal static class ProbeRecorder
{
    // 2 adds the "exclusions" object and the "excluded" step classification.
    // 3 adds "interaction_mode", and per step the planned operator checkpoints, the
    // operator's observations, the client deadline, and the elapsed time.
    // 4 adds "ended_in_planned_stall", "selection", and "server_interaction_mode_setting",
    // per step "expected_stall" and "server_interaction_mode", and per outcome
    // "recovery_guidance" and "execution_state".
    public const int SchemaVersion = 4;

    public static string FileStem(ProbeSessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return $"probe-277-{ProbePlan.PhaseName(record.Phase)}-{ProbeTarget.SpatialAnalyzerTarget}{(record.DryRun ? "-dry-run" : string.Empty)}";
    }

    /// <summary>Writes both files into an empty or new directory. Never overwrites.</summary>
    public static (string JsonPath, string MarkdownPath) Write(ProbeSessionRecord record, string directory)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, FileStem(record));
        var jsonPath = stem + ".json";
        var markdownPath = stem + ".md";
        using (var json = new FileStream(jsonPath, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(json, new UTF8Encoding(false)))
        {
            writer.Write(ToJson(record));
        }

        using (var markdown = new FileStream(markdownPath, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(markdown, new UTF8Encoding(false)))
        {
            writer.Write(ToMarkdown(record));
        }

        return (jsonPath, markdownPath);
    }

    public static string ToJson(ProbeSessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteNumber("schema_version", SchemaVersion);
            json.WriteString("issue", ProbeTarget.Issue);
            json.WriteString("spatial_analyzer_target", ProbeTarget.SpatialAnalyzerTarget);
            json.WriteString("phase", ProbePlan.PhaseName(record.Phase));
            json.WriteString("mode", record.DryRun ? "dry-run" : "licensed");
            json.WriteBoolean("placeholder_fixtures", record.PlaceholderFixtures);
            json.WriteString("started_at", record.StartedAt.ToString("O", CultureInfo.InvariantCulture));
            json.WriteString("completed_at", record.CompletedAt.ToString("O", CultureInfo.InvariantCulture));
            json.WriteBoolean("completed", record.Completed);
            WriteNullable(json, "stopped_at", record.StoppedAt);
            WriteNullable(json, "stop_reason", record.StopReason);
            json.WriteBoolean("ended_in_planned_stall", record.EndedInPlannedStall);
            WriteExclusions(json, record);
            WriteSelection(json, record);
            WriteInteractionMode(json, record.InteractionMode);
            WriteNullable(json, "server_interaction_mode_setting", record.ServerInteractionModeSetting);
            json.WriteStartObject("identity");
            WriteNullable(json, "server_version", record.Identity.ServerVersion);
            WriteNullable(json, "server_source_revision", record.Identity.ServerSourceRevision);
            WriteNullable(json, "activated_sdk_version", record.Identity.ActivatedSdkVersion);
            WriteNullable(json, "activated_sdk_evidence", record.Identity.ActivatedSdkEvidence);
            WriteNullable(json, "activated_sdk_attestation_reference", record.Identity.ActivatedSdkAttestationReference);
            WriteNullable(json, "connected_sa_attested_version", record.Identity.ConnectedSpatialAnalyzerAttestedVersion);
            WriteNullable(json, "connected_sa_attestation_reference", record.Identity.ConnectedSpatialAnalyzerAttestationReference);
            WriteNullable(json, "harness_revision", record.Identity.HarnessRevision);
            json.WriteEndObject();
            json.WriteStartArray("steps");
            foreach (var step in record.Steps)
            {
                WriteStep(json, step);
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
    }

    // Always present, so a record with no exclusions says so explicitly.
    private static void WriteExclusions(Utf8JsonWriter json, ProbeSessionRecord record)
    {
        json.WriteStartObject("exclusions");
        var any = record.ExcludedProbes.Count > 0;
        WriteNullable(json, "statement", any ? ProbePlan.ExclusionStatement(record.ExcludedProbes) : null);
        WriteNullable(json, "excluded_by", any ? "operator" : null);

        // The harness never records why; the committed evidence must.
        json.WriteBoolean("reason_recorded_by_harness", false);
        json.WriteStartArray("probes");
        foreach (var probe in record.ExcludedProbes)
        {
            json.WriteNumberValue(probe);
        }

        json.WriteEndArray();
        json.WriteStartArray("steps");
        foreach (var step in record.Steps.Where(static step => step.Classification == StepClassification.Excluded))
        {
            json.WriteStringValue(step.Step.Id);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    // Null for the default plan; otherwise the probes the operator named with --probe.
    private static void WriteSelection(Utf8JsonWriter json, ProbeSessionRecord record)
    {
        if (record.SelectedProbes.Count == 0)
        {
            json.WriteNull("selection");
            return;
        }

        json.WriteStartObject("selection");
        json.WriteString("statement", ProbePlan.SelectionStatement(record.SelectedProbes));
        json.WriteString("selected_by", "operator");
        json.WriteStartArray("probes");
        foreach (var probe in record.SelectedProbes)
        {
            json.WriteNumberValue(probe);
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    // Null when the plan never changes SA's interaction mode.
    private static void WriteInteractionMode(Utf8JsonWriter json, InteractionModeSettings? mode)
    {
        if (mode is null)
        {
            json.WriteNull("interaction_mode");
            return;
        }

        json.WriteStartObject("interaction_mode");
        json.WriteString("restore_sa_interaction_mode", InteractionModeSettings.ModeName(mode.Restore));
        json.WriteBoolean("restore_supplied_by_operator", mode.Supplied);
        json.WriteString("mp_interaction_mode", "Halt on Failure or Partial Success");
        json.WriteString("mp_dialog_interaction_mode", "Allow Application Interaction");
        json.WriteEndObject();
    }

    private static void WriteStep(Utf8JsonWriter json, ProbeStepRecord record)
    {
        var step = record.Step;
        json.WriteStartObject();
        json.WriteString("id", step.Id);
        json.WriteString("probe", step.Probe);
        json.WriteString("kind", ProbePlan.KindName(step.Kind));
        json.WriteString("purpose", step.Purpose);
        json.WriteString("rpc", step.Operation.FullyQualifiedMethod);
        json.WriteString("mp_step", step.Command.StepName);
        json.WriteBoolean("mp_step_is_shipped_text", string.Equals(step.Command.StepName, step.ShippedCommand.StepName, StringComparison.Ordinal));
        json.WriteStartObject("variant");
        json.WriteString("kind", VariantName(step.Variant.Kind));
        WriteNullable(json, "argument", step.Variant.ArgumentLabel);
        WriteNullable(json, "replacement", step.Variant.Replacement);
        json.WriteEndObject();
        json.WriteStartArray("sequence");
        foreach (var line in ProbePlan.DescribeSequence(step))
        {
            json.WriteStringValue(line);
        }

        json.WriteEndArray();
        json.WriteString("acceptable", ProbePlan.AcceptanceText(step));
        if (step.ExpectedStall is { } stall)
        {
            json.WriteStartObject("expected_stall");
            json.WriteString("transport", stall.Transport);
            json.WriteString("diagnostic_code", stall.DiagnosticCode);
            WriteNullable(json, "failure_kind", stall.FailureKind);
            WriteNullable(json, "recovery_guidance", stall.RecoveryGuidance);
            json.WriteEndObject();
        }
        else
        {
            json.WriteNull("expected_stall");
        }

        WriteNullable(json, "server_interaction_mode",
            step.ServerInteractionMode is { } setting ? InteractionModeSettings.ModeName(setting) : null);
        WriteNullable(json, "destructive_target", step.DestructiveTarget);
        json.WriteBoolean("requires_operator", step.RequiresOperator);
        json.WriteStartArray("operator_checkpoints");
        foreach (var timing in new[] { OperatorCheckpoint.Before, OperatorCheckpoint.After }.Where(timing => step.Checkpoints.HasFlag(timing)))
        {
            json.WriteStringValue(FileOperatorConsole.TimingName(timing));
        }

        json.WriteEndArray();
        if (step.ClientTimeout is { } deadline)
        {
            json.WriteNumber("client_timeout_seconds", (long)deadline.TotalSeconds);
        }
        else
        {
            json.WriteNull("client_timeout_seconds");
        }

        json.WriteString("classification", ClassificationName(record.Classification));
        if (record.ElapsedMilliseconds is { } elapsed)
        {
            json.WriteNumber("elapsed_ms", elapsed);
        }
        else
        {
            json.WriteNull("elapsed_ms");
        }

        json.WriteStartArray("operator_observations");
        foreach (var observation in record.OperatorObservations)
        {
            json.WriteStartObject();
            json.WriteString("timing", FileOperatorConsole.TimingName(observation.Timing));
            json.WriteString("text", observation.Text);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        if (record.Outcome is null)
        {
            json.WriteNull("outcome");
        }
        else
        {
            WriteOutcome(json, record.Outcome);
        }

        json.WriteStartArray("hypotheses");
        foreach (var hypothesis in record.Hypotheses)
        {
            json.WriteStartObject();
            json.WriteString("label", hypothesis.Label);
            json.WriteString("observation", hypothesis.ObservationKey);
            json.WriteString("expected", hypothesis.ExpectedValue);
            json.WriteBoolean("matched", hypothesis.Matched);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        if (step.Requirement is null)
        {
            json.WriteNull("requirement");
        }
        else
        {
            json.WriteStartObject("requirement");
            json.WriteString("description", step.Requirement.Description);
            if (record.RequirementSatisfied is { } satisfied)
            {
                json.WriteBoolean("satisfied", satisfied);
            }
            else
            {
                json.WriteNull("satisfied");
            }

            json.WriteEndObject();
        }

        json.WriteEndObject();
    }

    private static void WriteOutcome(Utf8JsonWriter json, ProbeOutcome outcome)
    {
        json.WriteStartObject("outcome");
        json.WriteString("kind", outcome.Kind.ToString());
        json.WriteString("transport", outcome.Transport);
        WriteNullable(json, "execute_step_returned", outcome.ExecuteStepReturned);
        WriteNullable(json, "mp_result_retrieved", outcome.MpResultRetrieved);
        if (outcome.MpResultCode is { } code)
        {
            json.WriteNumber("mp_result_code", code);
        }
        else
        {
            json.WriteNull("mp_result_code");
        }

        json.WriteStartArray("outputs");
        foreach (var output in outcome.Outputs)
        {
            json.WriteStartObject();
            json.WriteString("name", output.Name);
            json.WriteBoolean("retrieved", output.Retrieved);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        WriteNullable(json, "diagnostic_code", outcome.DiagnosticCode);
        WriteNullable(json, "failure_kind", outcome.FailureKind);
        WriteNullable(json, "execution_disposition", outcome.ExecutionDisposition);
        WriteNullable(json, "recovery_guidance", outcome.RecoveryGuidance);
        WriteNullable(json, "execution_state", outcome.ExecutionState);
        json.WriteStartObject("observations");
        foreach (var (key, value) in outcome.Observations)
        {
            json.WriteString(key, value);
        }

        json.WriteEndObject();
        json.WriteEndObject();
    }

    public static string ToMarkdown(ProbeSessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var text = new StringBuilder();
        var date = record.StartedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        text.Append(CultureInfo.InvariantCulture,
            $"# SA {ProbeTarget.SpatialAnalyzerTarget} licensed probe observations, {ProbePlan.PhaseName(record.Phase)} phase ({ProbeTarget.Issue}) — {date}\n\n");
        if (record.DryRun)
        {
            text.Append("**Dry run.** This file lists planned calls only. Nothing was started, connected, or executed; ")
                .Append("it is not evidence and must not be committed as an observation record.\n\n");
        }
        else
        {
            text.Append(Introduction(record)).Append("\n\n");
        }

        if (!record.DryRun && record.InteractionMode is { } mode)
        {
            text.Append(CultureInfo.InvariantCulture,
                $"**Interaction mode.** The plan's Set Interaction Mode steps send SA Interaction Mode `{InteractionModeSettings.ModeName(mode.Restore)}` ")
                .Append(mode.Supplied ? "(the operator's restore mode)" : "(a placeholder)")
                .Append(", except a Silent step, which sends `Silent`. Each sends MP mode `Halt on Failure or Partial Success` and MP dialog mode ")
                .Append("`Allow Application Interaction`. SA keeps these across restarts and has no getter. ")
                .Append("The table shows which of these steps ran.\n\n");
        }

        if (record.ServerInteractionModeSetting is { } setting)
        {
            text.Append(CultureInfo.InvariantCulture,
                $"**Server setting.** The plan needed the server started with `--Briosa:SpatialAnalyzer:InteractionMode={setting}`, as the dry run printed it")
                .Append(string.Equals(setting, ProbePlan.UnchangedSetting, StringComparison.Ordinal)
                    ? ", so only the plan's own steps set SA Interaction Mode.\n\n"
                    : ". The server applies it after each Connect, before it reports ready.\n\n");
        }

        if (record.SelectedProbes.Count > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $"**{ProbePlan.SelectionStatement(record.SelectedProbes)}.** ")
                .Append("Guard and fixture setup steps always run.\n\n");
        }

        if (record.ExcludedProbes.Count > 0)
        {
            var excluded = record.Steps.Where(static step => step.Classification == StepClassification.Excluded).Select(static step => $"`{step.Step.Id}`");
            text.Append(CultureInfo.InvariantCulture,
                $"**{ProbePlan.ExclusionStatement(record.ExcludedProbes)}.** Excluded steps, never sent: {string.Join(", ", excluded)}. ")
                .Append("Guard and fixture setup steps are never excluded. ")
                .Append("The justification for each exclusion must be stated in the committed evidence.\n\n");
        }

        text.Append("| Step | # | MP step | Variant | Observed result |\n");
        text.Append("| --- | --- | --- | --- | --- |\n");
        foreach (var step in record.Steps.Where(static step => step.Step.Kind is ProbeStepKind.Probe or ProbeStepKind.Check))
        {
            text.Append(CultureInfo.InvariantCulture,
                $"| `{step.Step.Id}` | {step.Step.Probe} | {Cell(step.Step.Command.StepName)} | {Cell(VariantText(step.Step))} | {Cell(ResultText(record.Phase, step))} |\n");
        }

        var fixtures = record.Steps.Where(static step => step.Step.Kind is ProbeStepKind.Guard or ProbeStepKind.Setup).ToList();
        text.Append(CultureInfo.InvariantCulture,
            $"\nGuard and fixture steps: {fixtures.Count(static step => step.Classification == StepClassification.Observed)} of {fixtures.Count} observed as planned.\n");

        if (record.StopReason is not null)
        {
            text.Append(CultureInfo.InvariantCulture, $"\nThe session stopped{(record.StoppedAt is null ? " before any step" : $" at `{record.StoppedAt}`")} (`{record.StopReason}`). ");
            text.Append(string.Equals(record.StopReason, ProbeSession.UnknownOutcomeStop, StringComparison.Ordinal)
                ? "Completion of that step is unknown. It was not replayed and later steps were not run; follow the runbook's recovery before any further SDK use.\n"
                : "Later steps were not run and nothing was replayed.\n");
        }
        else if (record.EndedInPlannedStall)
        {
            var stall = record.Steps.Last(static step => step.Classification != StepClassification.Excluded).Step.Id;
            text.Append(CultureInfo.InvariantCulture, $"\nThe run ended as planned in the stall of its last step, `{stall}`. ")
                .Append("Completion of that step is unknown. It was not replayed and nothing was sent after it; ")
                .Append("follow the runbook's stall recovery before any further SDK use.\n");
        }

        var notRun = record.Steps.Where(static step => step.Classification == StepClassification.NotRun).Select(static step => step.Step.Id).ToList();
        if (!record.DryRun && notRun.Count > 0)
        {
            text.Append(CultureInfo.InvariantCulture, $"\nNot run: {string.Join(", ", notRun.Select(static id => $"`{id}`"))}.\n");
        }

        text.Append("\nThese are observations of one bounded session, not vendor guarantees. ")
            .Append("Maintainer follow-up tasks are recorded below before this draft is committed.\n\n## Follow-up\n\n- (maintainer)\n");
        return text.ToString();
    }

    private static string Introduction(ProbeSessionRecord record)
    {
        var identity = record.Identity;
        var client = record.Phase == ProbePhase.PublicApi
            ? "through the generated gRPC client against one packaged Briosa server and its single SDK worker"
            : "through one directly hosted, unchanged Briosa worker process (one SDK client on one STA, no Briosa server)";
        var sdk = identity.ActivatedSdkVersion is null
            ? "The activated SDK version was not runtime-observed by the harness"
            : $"The activated SDK version was `{identity.ActivatedSdkVersion}` ({identity.ActivatedSdkEvidence ?? "unclassified"} evidence)";
        var connected = identity.ConnectedSpatialAnalyzerAttestedVersion is null
            ? "the connected SpatialAnalyzer release relied on the server's own identity policy"
            : $"the connected SpatialAnalyzer release was attested as `{identity.ConnectedSpatialAnalyzerAttestedVersion}` (reference `{identity.ConnectedSpatialAnalyzerAttestationReference}`)";
        var server = identity.ServerVersion is null ? string.Empty : $" Server `{identity.ServerVersion}` from `{identity.ServerSourceRevision ?? "unknown"}`.";
        return $"A maintainer-authorized, bounded probe session ran {client} against one licensed SpatialAnalyzer " +
            $"`{ProbeTarget.SpatialAnalyzerTarget}` instance. {sdk}, and {connected}.{server} The harness created its fixtures " +
            $"in the new collection `{FixtureNames.Collection}` and destroyed points only in its own disposable clouds. " +
            "Only structural results were retained: setter, `ExecuteStep`, and `GetMPStepResult` dispositions, MP result codes, " +
            "output retrieval, and counts of harness-created objects. No application values, paths, or license data were retained.";
    }

    internal static string VariantText(ProbeStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        return step.Variant.Kind switch
        {
            VariantKind.OmitArgument => $"setter `{step.Variant.ArgumentLabel}` skipped",
            VariantKind.BlankArgument => $"`{step.Variant.ArgumentLabel}` blank",
            VariantKind.RecaseArgumentLabel => $"label `{step.Variant.Replacement}` (exact `{step.Variant.ArgumentLabel}`)",
            VariantKind.ReplaceStepText => string.Equals(step.Command.StepName, step.ShippedCommand.StepName, StringComparison.Ordinal)
                ? "step text (shipped by this target)"
                : "step text (not shipped by this target)",
            _ => step.Kind == ProbeStepKind.Check ? "check" : "shipped"
        };
    }

    internal static string ResultText(ProbePhase phase, ProbeStepRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.Classification == StepClassification.Excluded)
        {
            return "Excluded by operator; not sent.";
        }

        var outcome = record.Outcome;
        if (outcome is null)
        {
            return "Not run.";
        }

        var text = new StringBuilder();
        var code = outcome.MpResultCode?.ToString(CultureInfo.InvariantCulture) ?? "?";
        if (record.Classification == StepClassification.Observed && record.Step.ExpectedStall is not null)
        {
            text.Append("Stalled as planned. ");
        }

        if (phase == ProbePhase.PublicApi && outcome.Kind != ProbeOutcomeKind.RefusedByHarness)
        {
            text.Append(CultureInfo.InvariantCulture, $"Public RPC `{outcome.Transport}`; ");
        }

        text.Append(outcome.Kind switch
        {
            ProbeOutcomeKind.Succeeded => $"setters returned true; `ExecuteStep` returned true; `GetMPStepResult` returned true with MP code `{code}`.",
            ProbeOutcomeKind.MpFailed => $"setters returned true; `ExecuteStep` returned true; `GetMPStepResult` returned true with MP code `{code}`.",
            ProbeOutcomeKind.ArgumentRejected => "a setter returned false; `ExecuteStep` was not called.",
            ProbeOutcomeKind.ExecuteStepRejected => "setters returned true; `ExecuteStep` returned false; completion unknown; not replayed.",
            ProbeOutcomeKind.MpResultUnavailable => "`ExecuteStep` returned true; `GetMPStepResult` returned false; completion unknown; not replayed.",
            ProbeOutcomeKind.OutputRetrievalFailed => $"MP code `{code}`, but at least one getter failed.",
            ProbeOutcomeKind.NotStarted => "definitely not started.",
            ProbeOutcomeKind.RefusedByHarness => "refused by the harness before dispatch.",
            _ => "completion unknown; not replayed."
        });
        if (outcome.Outputs.Count > 0)
        {
            text.Append(CultureInfo.InvariantCulture,
                $" Outputs retrieved: {outcome.Outputs.Count(static output => output.Retrieved)} of {outcome.Outputs.Count}.");
        }

        foreach (var (key, value) in outcome.Observations)
        {
            text.Append(CultureInfo.InvariantCulture, $" `{key}` = `{value}`.");
        }

        if (outcome.ExecutionState is { } state && outcome.Kind == ProbeOutcomeKind.Succeeded &&
            !string.Equals(state, Api.MpExecutionState.Succeeded.ToString(), StringComparison.Ordinal))
        {
            text.Append(CultureInfo.InvariantCulture, $" Execution state `{state}`.");
        }

        if (outcome.DiagnosticCode is not null && outcome.Kind != ProbeOutcomeKind.Succeeded)
        {
            text.Append(CultureInfo.InvariantCulture, $" Diagnostic `{outcome.DiagnosticCode}`.");
        }

        if (outcome.Kind == ProbeOutcomeKind.Indeterminate && outcome.FailureKind is not null)
        {
            text.Append(CultureInfo.InvariantCulture, $" Failure kind `{outcome.FailureKind}`.");
        }

        if (outcome.RecoveryGuidance is { } guidance && outcome.Kind != ProbeOutcomeKind.Succeeded)
        {
            text.Append(CultureInfo.InvariantCulture, $" Recovery guidance `{guidance}`.");
        }

        if (record.Hypotheses.Count > 0)
        {
            var matched = record.Hypotheses.Where(static hypothesis => hypothesis.Matched).Select(static hypothesis => hypothesis.Label).ToList();
            text.Append(matched.Count > 0 ? $" Matches: {string.Join("; ", matched)}." : " No listed hypothesis matched.");
        }

        foreach (var observation in record.OperatorObservations)
        {
            text.Append(CultureInfo.InvariantCulture,
                $" Operator {(observation.Timing == OperatorCheckpoint.Before ? "before dispatch" : "after the result")}: `{observation.Text}`.");
        }

        if (record.Classification == StepClassification.Unexpected)
        {
            text.Append(outcome.CompletionUnknown
                ? " **Completion unknown; the session stopped here (do not replay).**"
                : " **Unexpected for this step; the session stopped here.**");
        }

        return text.ToString();
    }

    public static string VariantName(VariantKind kind) => kind switch
    {
        VariantKind.OmitArgument => "omit-argument",
        VariantKind.BlankArgument => "blank-argument",
        VariantKind.RecaseArgumentLabel => "recase-argument-label",
        VariantKind.ReplaceStepText => "replace-step-text",
        _ => "shipped"
    };

    public static string ClassificationName(StepClassification classification) => classification switch
    {
        StepClassification.Observed => "observed",
        StepClassification.Unexpected => "unexpected",
        StepClassification.Refused => "refused",
        StepClassification.Excluded => "excluded",
        _ => "not-run"
    };

    private static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);

    private static void WriteNullable(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null)
        {
            json.WriteNull(name);
        }
        else
        {
            json.WriteString(name, value);
        }
    }

    private static void WriteNullable(Utf8JsonWriter json, string name, bool? value)
    {
        if (value is { } flag)
        {
            json.WriteBoolean(name, flag);
        }
        else
        {
            json.WriteNull(name);
        }
    }
}
