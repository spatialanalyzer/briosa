using System.Globalization;
using System.Text;

namespace Briosa.LicensedProbes;

/// <summary>
/// Operator checkpoints through files, so a licensed run can execute in a
/// background shell while the operator watches SpatialAnalyzer. At each
/// checkpoint the harness writes a request file into the checkpoint directory,
/// prints its name, and sends nothing until the operator writes the matching
/// response file. The response is one line from the closed vocabulary of
/// <see cref="OperatorObservationText"/>; no dialog text is ever written.
/// Console lines name files only, never their directory.
/// </summary>
internal sealed class FileOperatorConsole : IOperatorConsole
{
    public const string DirectoryName = "checkpoints";
    public const string RequestSuffix = ".request.txt";
    public const string ResponseSuffix = ".response.txt";

    // Created by the operator at any time, in the checkpoint directory, when SpatialAnalyzer
    // shows a license prompt (#402). Only its existence is read, never its content.
    public const string LicenseAbortFileName = "license-prompt-abort.signal";

    private readonly string _directory;
    private readonly TextWriter _output;
    private readonly TimeSpan _timeout;
    private readonly TimeProvider _clock;
    private readonly TimeSpan _pollInterval;
    private int _sequence;

    public FileOperatorConsole(string directory, TextWriter output, TimeSpan timeout, TimeProvider clock, TimeSpan? pollInterval = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(clock);
        _directory = directory;
        _output = output;
        _timeout = timeout;
        _clock = clock;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(1);

        // The directory exists from the start, so the license-abort signal can be created at once.
        Directory.CreateDirectory(_directory);
        LicenseAbort = LicenseAbortSignal.ForFile(Path.Combine(_directory, LicenseAbortFileName));
        Write($"LICENSE PROMPT: if SpatialAnalyzer shows one at any time, create {DirectoryName}\\{LicenseAbortFileName} " +
            "inside the --output-directory folder. From then on nothing more is sent, started or terminated, not even a " +
            "graceful stop. Ctrl+C instead cancels and lets the run clean up.");
    }

    public LicenseAbortSignal LicenseAbort { get; }

    /// <summary>Where the operator creates the license-abort signal for a run writing to this output directory.</summary>
    public static string LicenseAbortPath(string outputDirectory) => Path.Combine(outputDirectory, DirectoryName, LicenseAbortFileName);

    public void Dispatching(ProbeStep step, int ordinal, int count)
    {
        ArgumentNullException.ThrowIfNull(step);
        Write(string.Create(CultureInfo.InvariantCulture,
            $"[{ordinal:D3}/{count:D3}] {step.Id} (probe {step.Probe}): dispatched at {_clock.GetUtcNow():HH:mm:ss}Z."));
        if (step.ExpectedStall is not null)
        {
            Write(string.Create(CultureInfo.InvariantCulture,
                $"PLANNED STALL {step.Id}: do not dismiss the SpatialAnalyzer dialog this step opens. Leave it open until this step returns " +
                $"(its deadline is {step.ClientTimeout?.TotalSeconds ?? 0:0} s; in the public phase the server's quick budget ends it first)."));
        }
    }

    public void Completed(ProbeStep step, int ordinal, int count, ProbeOutcome outcome, long elapsedMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(outcome);
        var code = outcome.MpResultCode?.ToString(CultureInfo.InvariantCulture) ?? "none";
        Write(string.Create(CultureInfo.InvariantCulture,
            $"[{ordinal:D3}/{count:D3}] {step.Id}: {outcome.Kind}, MP code {code}, {elapsedMilliseconds} ms."));
    }

    public async Task<string?> CheckpointAsync(ProbeStep step, OperatorCheckpoint timing, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        Directory.CreateDirectory(_directory);
        var name = string.Create(CultureInfo.InvariantCulture, $"{++_sequence:D3}-{step.Id}-{TimingName(timing)}");
        var response = Path.Combine(_directory, name + ResponseSuffix);
        var request = new StreamWriter(
            new FileStream(Path.Combine(_directory, name + RequestSuffix), FileMode.CreateNew, FileAccess.Write), new UTF8Encoding(false));
        await using (request.ConfigureAwait(false))
        {
            await request.WriteAsync(Instruction(step, timing)).ConfigureAwait(false);
        }

        Write($"OPERATOR CHECKPOINT {name}: {(timing == OperatorCheckpoint.Before ? "before dispatch of" : "after the result of")} {step.Id}. " +
            $"Write one line to {name}{ResponseSuffix}: none, dialog-seen, dialog-dismissed or license-prompt-abort.");
        var deadline = _clock.GetUtcNow() + _timeout;
        var refused = 0;
        while (true)
        {
            if (LicenseAbort.Raised)
            {
                Write($"OPERATOR CHECKPOINT {name}: license prompt signalled; nothing more is sent.");
                return OperatorObservationText.LicensePromptAbort;
            }

            if (TryRead(response, out var text))
            {
                if (OperatorObservationText.TryNormalize(text, out var observation))
                {
                    Write($"OPERATOR CHECKPOINT {name}: recorded.");
                    return observation;
                }

                // Keep the refused answer for the operator; never record it. While another
                // process still holds the file, it stays in place and is read again later.
                var setAside = Path.Combine(_directory, string.Create(CultureInfo.InvariantCulture, $"{name}.refused-{refused + 1}.txt"));
                if (TryMove(response, setAside))
                {
                    refused++;
                    Write($"OPERATOR CHECKPOINT {name}: response refused. {OperatorObservationText.Rule} Write it again.");
                }
            }

            if (_clock.GetUtcNow() >= deadline)
            {
                Write($"OPERATOR CHECKPOINT {name}: not answered in time; nothing more is sent.");
                return null;
            }

            try
            {
                await Task.Delay(_pollInterval, _clock, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Write($"OPERATOR CHECKPOINT {name}: cancelled; nothing more is sent.");
                return null;
            }
        }
    }

    internal static string TimingName(OperatorCheckpoint timing) => timing == OperatorCheckpoint.Before ? "before" : "after";

    private static string Instruction(ProbeStep step, OperatorCheckpoint timing)
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"Step {step.Id} (probe {step.Probe}): {step.Purpose}\n");
        text.Append((timing, step.ExpectedStall is not null) switch
        {
            (OperatorCheckpoint.Before, false) =>
                "Before this step is sent: confirm that no SpatialAnalyzer dialog is open. If one is, dismiss it with OK and answer dialog-dismissed.\n",
            (OperatorCheckpoint.Before, true) =>
                "Before this step is sent: confirm that no SpatialAnalyzer dialog is open. If one is, dismiss it with OK and answer dialog-dismissed. " +
                "This step should open a dialog: do not dismiss that one until the step has returned.\n",
            (_, false) =>
                "This step has returned. If it raised a SpatialAnalyzer dialog, dismiss it with OK and answer dialog-dismissed, or dialog-seen " +
                "if it was already gone; otherwise answer none.\n",
            (_, true) =>
                "This planned stall has returned. The dialog it opened should still be open: dismiss it with OK and answer dialog-dismissed, " +
                "or none if no dialog opened.\n"
        });
        text.Append("Never copy any text from a dialog. If SpatialAnalyzer shows a license prompt, do not dismiss it: answer ")
            .Append("license-prompt-abort, and nothing more is sent.\n");
        text.Append(CultureInfo.InvariantCulture, $"Then write one line to the matching {ResponseSuffix} file. {OperatorObservationText.Rule}\n");
        return text.ToString();
    }

    private static bool TryRead(string path, out string text)
    {
        text = string.Empty;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            // Sharing read only fails while any writer still holds the file, so a partly
            // written answer is never read.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            text = reader.ReadToEnd();
        }
        catch (IOException)
        {
            // Still being written; read it on the next poll.
            return false;
        }

        // An empty file is still being written.
        return text.Trim().Length > 0;
    }

    private static bool TryMove(string source, string destination)
    {
        try
        {
            File.Move(source, destination);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void Write(string line)
    {
        _output.WriteLine(line);
        _output.Flush();
    }
}

/// <summary>
/// The operator's license-abort signal (#402). Once seen it stays raised for the rest of the
/// run, whatever happens to its file afterwards, and the session, the transport and the command
/// consult this one object before anything is sent, started or terminated, and in disposal.
/// </summary>
internal sealed class LicenseAbortSignal
{
    private readonly Func<bool> _probe;
    private int _raised;

    public LicenseAbortSignal(Func<bool> probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        _probe = probe;
    }

    /// <summary>A signal that is never raised, for a transport started without one.</summary>
    public static LicenseAbortSignal Never => new(static () => false);

    /// <summary>The signal file; only its existence is read, never its content.</summary>
    public static LicenseAbortSignal ForFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new(() => File.Exists(path));
    }

    public bool Raised
    {
        get
        {
            if (Volatile.Read(ref _raised) != 0)
            {
                return true;
            }

            if (!_probe())
            {
                return false;
            }

            Interlocked.Exchange(ref _raised, 1);
            return true;
        }
    }

    /// <summary>Latches the signal without its file (tests, and an answer of license-prompt-abort).</summary>
    public void Raise() => Interlocked.Exchange(ref _raised, 1);
}

/// <summary>
/// The closed vocabulary of operator checkpoint answers (#402). An answer is one of four words,
/// optionally followed by up to two detail words, separated by single spaces. Nothing else is
/// accepted, so no dialog text, name, path, process id or license text can reach a record.
/// </summary>
internal static class OperatorObservationText
{
    public const string None = "none";
    public const string DialogSeen = "dialog-seen";
    public const string DialogDismissed = "dialog-dismissed";

    // A license prompt appeared. The operator does not copy or dismiss it, and the run sends
    // nothing more.
    public const string LicensePromptAbort = "license-prompt-abort";

    public const string LinesShown = "lines-shown";
    public const string LinesNotShown = "lines-not-shown";

    public static IReadOnlyList<string> Answers { get; } = [None, DialogSeen, DialogDismissed, LicensePromptAbort];

    // The callout lines of probe 35, and the InteractionMode registry digit of probe 25.
    public static IReadOnlyList<string> Details { get; } =
        [LinesShown, LinesNotShown, .. Enumerable.Range(0, 10).Select(static digit => "registry-" + digit.ToString(CultureInfo.InvariantCulture))];

    public const string Rule =
        "Write one of none, dialog-seen, dialog-dismissed or license-prompt-abort, optionally followed by one or two of " +
        "lines-shown, lines-not-shown or registry-<digit>, separated by single spaces. Nothing else is accepted.";

    public static bool TryNormalize(string raw, out string observation)
    {
        observation = string.Empty;
        ArgumentNullException.ThrowIfNull(raw);
        var lines = raw.Replace("\r", string.Empty, StringComparison.Ordinal).Trim().Split('\n');
        if (lines.Length != 1)
        {
            return false;
        }

        // Each word is matched case-insensitively and recorded in its canonical form.
        var words = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(static word => Answers.Concat(Details).FirstOrDefault(known => string.Equals(known, word, StringComparison.OrdinalIgnoreCase)) ?? word)
            .ToList();
        if (words.Count is 0 or > 3 || !Answers.Contains(words[0], StringComparer.Ordinal) ||
            !words.Skip(1).All(word => Details.Contains(word, StringComparer.Ordinal)) ||
            words.Skip(1).Distinct(StringComparer.Ordinal).Count() != words.Count - 1 ||
            (words[0] == LicensePromptAbort && words.Count > 1))
        {
            return false;
        }

        observation = string.Join(' ', words);
        return true;
    }

    /// <summary>The operator reported a license prompt: nothing more may be sent.</summary>
    public static bool IsLicensePromptAbort(string? observation) => string.Equals(observation, LicensePromptAbort, StringComparison.Ordinal);
}
