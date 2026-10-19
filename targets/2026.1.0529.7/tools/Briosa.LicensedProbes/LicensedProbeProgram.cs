using System.Globalization;
using System.Text.Json;

namespace Briosa.LicensedProbes;

internal static class LicensedProbeProgram
{
    public const string Usage = """
        Usage (exact target only; see docs/testing/licensed-probe-session.md):
          Briosa.LicensedProbes --phase public-api|worker --dry-run [--fixtures <manifest.json>] [--output-directory <empty dir>]
          Briosa.LicensedProbes --phase public-api --confirm-licensed-sa2026 --fixtures <manifest.json>
              --output-directory <empty dir> --restore-sa-interaction-mode manual|automatic [--address http://127.0.0.1:50051]
          Briosa.LicensedProbes --phase worker --confirm-licensed-sa2026 --fixtures <manifest.json>
              --output-directory <empty dir> --restore-sa-interaction-mode manual|automatic
              --worker-path <package>\Briosa.Worker.exe
              --connected-sa-attested-version 2026.1.0529.7 --connected-sa-attestation-reference <id>
          Recovery after a session stopped inside probe 25: add --interaction-mode-restore-only to either form.
          Optional: --activated-sdk-attestation-reference <id> --harness-revision <sha>
              --step-timeout-seconds <5-600> --operator-timeout-seconds <30-1800>
              --exclude-probe <n> (repeatable; a probe number of the selected phase, e.g. 11)
          A licensed run pauses at operator checkpoints; answer each by writing its response file
          in the checkpoints folder of the output directory.
        """;

    public static IProbeTransport CreateTransport(ProbeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Phase == ProbePhase.PublicApi
            ? new PublicApiTransport(options.Address!, options.StepTimeout, options.OperatorTimeout, ProcessCensus.Instance)
            : new WorkerTransport(options.WorkerPath!, options.StepTimeout, ProcessCensus.Instance);
    }

    /// <summary>The licensed-run operator console: progress on standard output, checkpoints through files.</summary>
    public static IOperatorConsole CreateOperatorConsole(ProbeOptions options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new FileOperatorConsole(Path.Combine(options.OutputDirectory!, FileOperatorConsole.DirectoryName),
            output, options.OperatorTimeout, TimeProvider.System);
    }

    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        TextWriter error,
        Func<ProbeOptions, IProbeTransport> transportFactory,
        Func<ProbeOptions, TextWriter, IOperatorConsole> consoleFactory,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(transportFactory);
        ArgumentNullException.ThrowIfNull(consoleFactory);
        ArgumentNullException.ThrowIfNull(clock);

        ProbeOptions options;
        ProbePlan plan;
        try
        {
            options = ProbeOptions.Parse(arguments);
            var manifest = options.FixturesPath is null
                ? FixtureManifest.Placeholder
                : FixtureManifest.Parse(await File.ReadAllTextAsync(options.FixturesPath, cancellationToken).ConfigureAwait(false));
            if (!options.DryRun && !File.Exists(manifest.UiProfileFile))
                throw new ProbeUsageException("The manifest's ui_profile_file does not exist.");
            if (options.OutputDirectory is not null && Directory.Exists(options.OutputDirectory) &&
                Directory.EnumerateFileSystemEntries(options.OutputDirectory).Any())
                throw new ProbeUsageException("--output-directory must be new or empty; records are never overwritten.");

            // An exclusion outside this phase or of a prerequisite is a usage error.
            plan = options.InteractionModeRestoreOnly
                ? ProbePlan.CreateInteractionModeRestore(options.Phase, options.InteractionMode)
                : ProbePlan.Create(options.Phase, manifest, options.ExcludedProbes, options.InteractionMode);

            // SA keeps the mode across restarts and has no getter: only the operator knows what to restore.
            if (!options.DryRun && plan.SetsInteractionMode && !options.InteractionMode.Supplied)
                throw new ProbeUsageException(
                    $"Probe 25 changes SA Interaction Mode; a licensed run requires {ProbeOptions.RestoreModeOption} manual|automatic " +
                    "(or --exclude-probe 25).");
        }
        catch (ProbeUsageException exception)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            await error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 2;
        }
        catch (InvalidDataException exception)
        {
            // Manifest validation messages are harness-authored and contain no manifest values.
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            return 2;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync("The fixture manifest could not be read.").ConfigureAwait(false);
            return 2;
        }

        if (options.DryRun)
        {
            await output.WriteAsync(plan.RenderDryRun()).ConfigureAwait(false);
            if (options.OutputDirectory is not null)
            {
                var files = ProbeRecorder.Write(ProbeSession.DryRun(plan, clock), options.OutputDirectory);
                await output.WriteLineAsync($"Plan records: {Path.GetFileName(files.JsonPath)}, {Path.GetFileName(files.MarkdownPath)}").ConfigureAwait(false);
            }

            return 0;
        }

        // Fail before any client starts if the record cannot be written.
        Directory.CreateDirectory(options.OutputDirectory!);
        var operatorIdentity = new SessionIdentity
        {
            ConnectedSpatialAnalyzerAttestedVersion = options.ConnectedSaAttestedVersion,
            ConnectedSpatialAnalyzerAttestationReference = options.ConnectedSaAttestationReference,
            ActivatedSdkAttestationReference = options.ActivatedSdkAttestationReference,
            HarnessRevision = options.HarnessRevision
        };

        ProbeSessionRecord record;
        var console = consoleFactory(options, output);
        var transport = transportFactory(options);
        await using (transport.ConfigureAwait(false))
        {
            record = await ProbeSession.RunAsync(plan, transport, clock, operatorIdentity, console, cancellationToken).ConfigureAwait(false);
        }

        var written = ProbeRecorder.Write(record, options.OutputDirectory!);
        var observed = record.Steps.Count(static step => step.Classification == StepClassification.Observed);
        var excluded = record.Steps.Count(static step => step.Classification == StepClassification.Excluded);
        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"Phase {ProbePlan.PhaseName(record.Phase)}: {observed} of {record.Steps.Count - excluded} steps observed; " +
            $"excluded_steps={excluded}; " +
            $"completed={record.Completed}; stopped_at={record.StoppedAt ?? "none"}; stop_reason={record.StopReason ?? "none"}.")).ConfigureAwait(false);
        if (record.ExcludedProbes.Count > 0)
        {
            await output.WriteLineAsync(ProbePlan.ExclusionStatement(record.ExcludedProbes) + ".").ConfigureAwait(false);
        }

        if (plan.SetsInteractionMode && !record.Completed && !plan.InteractionModeRestoreOnly)
        {
            await output.WriteLineAsync(
                "If probe 25 set Silent before the stop, SA is still in Silent: after recovery, run this phase again with " +
                $"{ProbeOptions.RestoreOnlyFlag} on a clean SA.").ConfigureAwait(false);
        }

        await output.WriteLineAsync($"Records: {Path.GetFileName(written.JsonPath)}, {Path.GetFileName(written.MarkdownPath)}").ConfigureAwait(false);
        return record.Completed ? 0 : 1;
    }
}
