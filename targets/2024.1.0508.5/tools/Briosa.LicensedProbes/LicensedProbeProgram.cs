using System.Globalization;
using System.Text.Json;

namespace Briosa.LicensedProbes;

internal static class LicensedProbeProgram
{
    public const string Usage = """
        Usage (exact target only; see docs/testing/licensed-probe-session.md):
          Briosa.LicensedProbes --phase public-api|worker --dry-run [--fixtures <manifest.json>] [--output-directory <empty dir>]
          Briosa.LicensedProbes --phase public-api --confirm-licensed-sa2024 --fixtures <manifest.json>
              --output-directory <empty dir> [--address http://127.0.0.1:50051]
          Briosa.LicensedProbes --phase worker --confirm-licensed-sa2024 --fixtures <manifest.json>
              --output-directory <empty dir> --worker-path <package>\Briosa.Worker.exe
              --connected-sa-attested-version 2024.1.0508.5 --connected-sa-attestation-reference <id>
          Optional: --activated-sdk-attestation-reference <id> --harness-revision <sha>
              --step-timeout-seconds <5-600> --operator-timeout-seconds <30-1800>
        """;

    public static IProbeTransport CreateTransport(ProbeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Phase == ProbePhase.PublicApi
            ? new PublicApiTransport(options.Address!, options.StepTimeout, options.OperatorTimeout, ProcessCensus.Instance)
            : new WorkerTransport(options.WorkerPath!, options.StepTimeout, ProcessCensus.Instance);
    }

    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        TextWriter error,
        Func<ProbeOptions, IProbeTransport> transportFactory,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(transportFactory);
        ArgumentNullException.ThrowIfNull(clock);

        ProbeOptions options;
        FixtureManifest manifest;
        try
        {
            options = ProbeOptions.Parse(arguments);
            manifest = options.FixturesPath is null
                ? FixtureManifest.Placeholder
                : FixtureManifest.Parse(await File.ReadAllTextAsync(options.FixturesPath, cancellationToken).ConfigureAwait(false));
            if (!options.DryRun && !File.Exists(manifest.UiProfileFile))
                throw new ProbeUsageException("The manifest's ui_profile_file does not exist.");
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

        var plan = ProbePlan.Create(options.Phase, manifest);
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
        var transport = transportFactory(options);
        await using (transport.ConfigureAwait(false))
        {
            record = await ProbeSession.RunAsync(plan, transport, clock, operatorIdentity, cancellationToken).ConfigureAwait(false);
        }

        var written = ProbeRecorder.Write(record, options.OutputDirectory!);
        var observed = record.Steps.Count(static step => step.Classification == StepClassification.Observed);
        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"Phase {ProbePlan.PhaseName(record.Phase)}: {observed} of {record.Steps.Count} steps observed; " +
            $"completed={record.Completed}; stopped_at={record.StoppedAt ?? "none"}; stop_reason={record.StopReason ?? "none"}.")).ConfigureAwait(false);
        await output.WriteLineAsync($"Records: {Path.GetFileName(written.JsonPath)}, {Path.GetFileName(written.MarkdownPath)}").ConfigureAwait(false);
        return record.Completed ? 0 : 1;
    }
}
