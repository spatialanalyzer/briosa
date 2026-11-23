using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Briosa.LicensedProbes;

/// <summary>The <c>--ownership</c> command: parse, dry run or one bounded probe, then the records.</summary>
internal static class OwnershipProgram
{
    public static string Usage { get; } = $"""
        Usage (exact target only; see docs/testing/licensed-probe-session.md, licensed session 4):
          Briosa.LicensedProbes --ownership <scenario> --dry-run
          Briosa.LicensedProbes --ownership <scenario> {ProbeTarget.ConfirmFlag} --output-directory <empty dir>
              [--address http://127.0.0.1:50051] [--observe-seconds <10-1800>] [--harness-revision <sha>]
          scenario: release | worker-crash | parent-death | watchdog-kill | competing-client | observe
          watchdog-kill also needs --restore-sa-interaction-mode manual; competing-client needs
              --worker-path <package>\Briosa.Worker.exe. observe needs no server.
        """;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The environment owns the server client; RunAsync disposes it after the probe.")]
    public static OwnershipEnvironment CreateEnvironment(OwnershipOptions options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new OwnershipEnvironment(
            WindowsProcessTable.Instance,
            options.Scenario == OwnershipScenario.Observe ? null : new OwnershipServer(options.Address, options.StepTimeout),
            ProcessTerminator.Instance,
            options.WorkerPath is { } path ? () => new SecondWorker(path) : null,
            new FileOperatorConsole(Path.Combine(options.OutputDirectory!, FileOperatorConsole.DirectoryName), output,
                options.OperatorTimeout, TimeProvider.System),
            SystemPause.Instance,
            TimeProvider.System,
            Environment.ProcessId);
    }

    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        TextWriter error,
        Func<OwnershipOptions, TextWriter, OwnershipEnvironment> environmentFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(environmentFactory);
        OwnershipOptions options;
        try
        {
            options = OwnershipOptions.Parse(arguments);
            if (options.OutputDirectory is not null && Directory.Exists(options.OutputDirectory) &&
                Directory.EnumerateFileSystemEntries(options.OutputDirectory).Any())
                throw new ProbeUsageException("--output-directory must be new or empty; records are never overwritten.");
        }
        catch (ProbeUsageException exception)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            await error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }

        if (options.DryRun)
        {
            await output.WriteAsync(RenderDryRun(options)).ConfigureAwait(false);
            return 0;
        }

        Directory.CreateDirectory(options.OutputDirectory!);
        var environment = environmentFactory(options, output);
        OwnershipRecord record;
        try
        {
            record = await OwnershipRunner.RunAsync(options, environment, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (environment.Server is not null)
            {
                await environment.Server.DisposeAsync().ConfigureAwait(false);
            }
        }

        var stem = Path.Combine(options.OutputDirectory!,
            $"ownership-{OwnershipOptions.ScenarioName(options.Scenario)}-{ProbeTarget.SpatialAnalyzerTarget}");
        await WriteNewAsync(stem + ".json", OwnershipRecorder.ToJson(record), cancellationToken).ConfigureAwait(false);
        await WriteNewAsync(stem + ".md", OwnershipRecorder.ToMarkdown(record), cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"Ownership {OwnershipOptions.ScenarioName(record.Scenario)}: completed={record.Completed}; stop_reason={record.StopReason ?? "none"}; " +
            $"residual={(record.Residual.Count == 0 ? "none" : string.Join(",", record.Residual.Select(static process => process.Label)))}.")).ConfigureAwait(false);
        if (record.Residual.Count > 0)
        {
            await output.WriteLineAsync(
                "Residual processes remain. Do not stop an SDK process without the agreed rule in the runbook; record what the operator stopped.")
                .ConfigureAwait(false);
        }

        await output.WriteLineAsync($"Records: {Path.GetFileName(stem)}.json, {Path.GetFileName(stem)}.md").ConfigureAwait(false);
        return record.Completed ? 0 : 1;
    }

    internal static string RenderDryRun(OwnershipOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var text = new StringBuilder();
        var name = OwnershipOptions.ScenarioName(options.Scenario);
        text.Append(CultureInfo.InvariantCulture,
            $"Briosa SDK-ownership probe {name} ({ProbeTarget.Session4Issue}, #252) for SpatialAnalyzer {ProbeTarget.SpatialAnalyzerTarget}\n");
        text.Append("DRY RUN: nothing was started, connected, or terminated.\n");
        text.Append(CultureInfo.InvariantCulture,
            $"Observation windows: up to {options.ObserveWindow.TotalSeconds:0} s each, a process snapshot every {OwnershipRunner.PollInterval.TotalMilliseconds:0} ms; ")
            .Append("a window ends early once the processes it watches are gone.\n");
        if (options.Scenario != OwnershipScenario.Observe)
        {
            text.Append("Preconditions: exactly one Spatial Analyzer, one Briosa server with no SDK started, no worker and no SDK process.\n");
            text.Append("Script: census; describe; get SDK state; start SDK; census; connect; census; then\n");
        }

        text.Append(options.Scenario switch
        {
            OwnershipScenario.Release => "  stop SDK; watch until the worker and the SDK process are gone.\n",
            OwnershipScenario.WorkerCrash => "  terminate the worker (that process only); wait up to 30 s for the server to report Faulted; watch; then stop the SDK generation and watch.\n",
            OwnershipScenario.ParentDeath => "  terminate the server (that process only); watch until the worker and the SDK process are gone.\n",
            OwnershipScenario.WatchdogKill =>
                "  set SA Interaction Mode Manual; operator checkpoint (no dialog open); a wildcard selection that matches nothing, which stalls on its\n" +
                "  dialog until the server's watchdog terminates the worker; watch with the dialog open; operator checkpoint (record and dismiss the\n" +
                "  dialog); watch; then stop the SDK generation and watch.\n",
            OwnershipScenario.CompetingClient =>
                "  one read-only MP through the first client; start a second worker (SDK activation); census; its ConnectEx; census;\n" +
                "  a read-only MP through the first client; stop the second worker; watch; a read-only MP; then stop the SDK and watch.\n" +
                "  The second worker never runs an MP.\n",
            _ => "Script: census, then watch until no SDK, worker or server process remains.\n"
        });

        var steps = OwnershipRunner.MpSteps(options.Scenario, options.InteractionMode);
        if (steps.Count > 0)
        {
            text.Append("Server admission: start Briosa.Server.exe with\n");
            foreach (var argument in ProbePlan.CreateServerAdmissionArguments([.. steps.Select(static step => (step.Operation.FullyQualifiedMethod, (Google.Protobuf.IMessage?)step.Request))]))
            {
                text.Append("  ").Append(argument).Append('\n');
            }

            text.Append(CultureInfo.InvariantCulture, $"  --{Briosa.Server.Workers.SpatialAnalyzerConnectionOptions.InteractionModeKey}={ProbePlan.UnchangedSetting}\n");
            foreach (var step in steps)
            {
                text.Append(CultureInfo.InvariantCulture, $"MP step {step.Id}: RPC {step.Operation.FullyQualifiedMethod}\n");
                foreach (var line in ProbePlan.DescribeSequence(step))
                {
                    text.Append("  ").Append(line).Append('\n');
                }
            }
        }

        text.Append("The record holds process labels (sdk-1, worker-1), roles, parent roles, start times and offsets, lifecycle states and codes. ")
            .Append("No process id, path, argument, or value is recorded.\n");
        return text.ToString();
    }

    private static async Task WriteNewAsync(string path, string content, CancellationToken cancellationToken)
    {
        var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await using (stream.ConfigureAwait(false))
        {
            await stream.WriteAsync(new UTF8Encoding(false).GetBytes(content), cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>Writes an ownership record as JSON and as a markdown draft. Neither holds a process id, path or value.</summary>
internal static class OwnershipRecorder
{
    public const int SchemaVersion = 1;

    public static string ToJson(OwnershipRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteNumber("schema_version", SchemaVersion);
            json.WriteString("issue", ProbeTarget.Session4Issue);
            json.WriteString("spatial_analyzer_target", ProbeTarget.SpatialAnalyzerTarget);
            json.WriteString("scenario", OwnershipOptions.ScenarioName(record.Scenario));
            json.WriteString("started_at", record.StartedAt.ToString("O", CultureInfo.InvariantCulture));
            json.WriteString("completed_at", record.CompletedAt.ToString("O", CultureInfo.InvariantCulture));
            json.WriteBoolean("completed", record.Completed);
            Nullable(json, "stop_reason", record.StopReason);
            json.WriteNumber("observe_window_seconds", (long)record.ObserveWindow.TotalSeconds);
            json.WriteNumber("poll_interval_ms", (long)OwnershipRunner.PollInterval.TotalMilliseconds);
            Nullable(json, "harness_revision", record.HarnessRevision);
            if (record.Server is { } server)
            {
                json.WriteStartObject("server");
                json.WriteBoolean("reachable", server.Reachable);
                json.WriteBoolean("target_matches", server.TargetMatches);
                Nullable(json, "server_version", server.ServerVersion);
                Nullable(json, "server_source_revision", server.SourceRevision);
                json.WriteStartObject("lifecycle_bounds_ms");
                foreach (var (name, value) in server.LifecycleBoundsMilliseconds)
                {
                    json.WriteNumber(name, value);
                }

                json.WriteEndObject();
                json.WriteEndObject();
            }
            else
            {
                json.WriteNull("server");
            }

            json.WriteStartArray("steps");
            foreach (var step in record.Steps)
            {
                json.WriteStartObject();
                json.WriteString("id", step.Id);
                json.WriteString("action", step.Action);
                json.WriteNumber("started_ms", step.StartedMilliseconds);
                json.WriteNumber("elapsed_ms", step.ElapsedMilliseconds);
                json.WriteString("result", step.Result);
                Nullable(json, "target", step.Target);
                Nullable(json, "operator_answer", step.OperatorAnswer);
                if (step.Lifecycle is { } lifecycle)
                {
                    json.WriteStartObject("lifecycle");
                    json.WriteString("transport", lifecycle.Transport);
                    Nullable(json, "sdk_state", lifecycle.SdkState);
                    if (lifecycle.Generation is { } generation)
                        json.WriteNumber("sdk_generation", generation);
                    else
                        json.WriteNull("sdk_generation");
                    json.WriteBoolean("ready_for_mp", lifecycle.ReadyForMp);
                    Nullable(json, "connection_state", lifecycle.ConnectionState);
                    Nullable(json, "diagnostic_code", lifecycle.DiagnosticCode);
                    Nullable(json, "last_incident_termination", lifecycle.IncidentTermination);
                    json.WriteEndObject();
                }

                if (step.Outcome is { } outcome)
                {
                    json.WriteStartObject("outcome");
                    json.WriteString("kind", outcome.Kind.ToString());
                    json.WriteString("transport", outcome.Transport);
                    if (outcome.MpResultCode is { } code)
                        json.WriteNumber("mp_result_code", code);
                    else
                        json.WriteNull("mp_result_code");
                    Nullable(json, "diagnostic_code", outcome.DiagnosticCode);
                    Nullable(json, "failure_kind", outcome.FailureKind);
                    Nullable(json, "execution_disposition", outcome.ExecutionDisposition);
                    Nullable(json, "recovery_guidance", outcome.RecoveryGuidance);
                    json.WriteEndObject();
                }

                if (step.SecondWorker is { } second)
                {
                    json.WriteStartObject("second_worker");
                    json.WriteString("result", second.Result);
                    Nullable(json, "connection_state", second.ConnectionState);
                    Nullable(json, "diagnostic_code", second.DiagnosticCode);
                    if (second.ActivatedSdkMatchesTarget is { } matches)
                        json.WriteBoolean("activated_sdk_matches_target", matches);
                    else
                        json.WriteNull("activated_sdk_matches_target");
                    json.WriteEndObject();
                }

                json.WriteStartArray("alive");
                foreach (var label in step.Alive)
                {
                    json.WriteStringValue(label);
                }

                json.WriteEndArray();
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteStartArray("processes");
            foreach (var process in record.Processes)
            {
                json.WriteStartObject();
                json.WriteString("label", process.Label);
                json.WriteString("role", process.Role);
                json.WriteString("parent_role", process.ParentRole);
                Nullable(json, "started_at", process.StartedAt?.ToString("O", CultureInfo.InvariantCulture));
                json.WriteBoolean("present_at_start", process.PresentAtStart);
                json.WriteNumber("first_seen_ms", process.FirstSeenMilliseconds);
                if (process.ExitedMilliseconds is { } exited)
                    json.WriteNumber("exited_ms", exited);
                else
                    json.WriteNull("exited_ms");
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteStartArray("residual");
            foreach (var process in record.Residual)
            {
                json.WriteStringValue(process.Label);
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray()) + "\n";
    }

    public static string ToMarkdown(OwnershipRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var text = new StringBuilder();
        var date = record.StartedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        text.Append(CultureInfo.InvariantCulture,
            $"# SA {ProbeTarget.SpatialAnalyzerTarget} SDK-ownership probe `{OwnershipOptions.ScenarioName(record.Scenario)}` ({ProbeTarget.Session4Issue}) — {date}\n\n");
        text.Append(CultureInfo.InvariantCulture,
            $"Completed: {(record.Completed ? "yes" : "no")}; stop reason: `{record.StopReason ?? "none"}`. ")
            .Append(CultureInfo.InvariantCulture,
                $"Each observation window lasted up to {record.ObserveWindow.TotalSeconds:0} s with a snapshot every {OwnershipRunner.PollInterval.TotalMilliseconds:0} ms.\n\n");
        text.Append("| Step | Action | Started (ms) | Elapsed (ms) | Result | Detail | Alive after |\n| --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var step in record.Steps)
        {
            text.Append(CultureInfo.InvariantCulture,
                $"| `{step.Id}` | {step.Action} | {step.StartedMilliseconds} | {step.ElapsedMilliseconds} | `{step.Result}` | {Detail(step)} | {string.Join(", ", step.Alive)} |\n");
        }

        text.Append("\n| Process | Role | Parent | Present at start | First seen (ms) | Exited (ms) |\n| --- | --- | --- | --- | --- | --- |\n");
        foreach (var process in record.Processes)
        {
            text.Append(CultureInfo.InvariantCulture,
                $"| `{process.Label}` | {process.Role} | {process.ParentRole} | {(process.PresentAtStart ? "yes" : "no")} | {process.FirstSeenMilliseconds} | {process.ExitedMilliseconds?.ToString(CultureInfo.InvariantCulture) ?? "alive at end"} |\n");
        }

        text.Append(record.Residual.Count == 0
            ? "\nResidual: none. No SDK or worker process from this run was alive when it ended.\n"
            : $"\nResidual: {string.Join(", ", record.Residual.Select(static process => $"`{process.Label}`"))} alive when the run ended. " +
              "Record whether each exited on its own later or which the operator stopped, with the maintainer's agreement.\n");
        text.Append("\nThese are observations of one bounded run, not vendor guarantees.\n");
        return text.ToString();
    }

    private static string Detail(OwnershipStepRecord step)
    {
        var parts = new List<string>();
        if (step.Target is { } target)
            parts.Add($"target `{target}`");
        if (step.Lifecycle is { } lifecycle)
        {
            parts.Add($"`{lifecycle.Transport}`");
            if (lifecycle.SdkState is { } state)
                parts.Add($"SDK `{state}`");
            if (lifecycle.Generation is { } generation)
                parts.Add(string.Create(CultureInfo.InvariantCulture, $"generation {generation}"));
            if (lifecycle.ReadyForMp)
                parts.Add("ready for MP");
            if (lifecycle.DiagnosticCode is { } code)
                parts.Add($"`{code}`");
            if (lifecycle.IncidentTermination is { } termination)
                parts.Add($"incident `{termination}`");
        }

        if (step.Outcome is { } outcome)
        {
            parts.Add($"`{outcome.Kind}` via `{outcome.Transport}`");
            if (outcome.MpResultCode is { } code)
                parts.Add(string.Create(CultureInfo.InvariantCulture, $"MP code `{code}`"));
            if (outcome.DiagnosticCode is { } diagnostic)
                parts.Add($"`{diagnostic}`");
        }

        if (step.SecondWorker is { } second)
        {
            if (second.ConnectionState is { } state)
                parts.Add($"connection `{state}`");
            if (second.DiagnosticCode is { } code)
                parts.Add($"`{code}`");
            if (second.ActivatedSdkMatchesTarget is { } matches)
                parts.Add(matches ? "activated SDK is the target" : "activated SDK not verified as the target");
        }

        if (step.OperatorAnswer is { } answer)
            parts.Add($"operator: `{answer}`");
        return string.Join("; ", parts);
    }

    private static void Nullable(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null)
            json.WriteNull(name);
        else
            json.WriteString(name, value);
    }
}
