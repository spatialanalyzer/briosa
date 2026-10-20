using System.Globalization;
using System.Text;

namespace Briosa.LicensedProbes;

/// <summary>
/// Operator checkpoints through files, so a licensed run can execute in a
/// background shell while the operator watches SpatialAnalyzer. At each
/// checkpoint the harness writes a request file into the checkpoint directory,
/// prints its name, and sends nothing until the operator writes the matching
/// response file. The response is one line: <c>none</c>, or <c>dialog:</c>
/// followed by the dialog's exact title and text and what the operator did.
/// Console lines name files only, never their directory.
/// </summary>
internal sealed class FileOperatorConsole : IOperatorConsole
{
    public const string DirectoryName = "checkpoints";
    public const string RequestSuffix = ".request.txt";
    public const string ResponseSuffix = ".response.txt";

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
    }

    public void Dispatching(ProbeStep step, int ordinal, int count)
    {
        ArgumentNullException.ThrowIfNull(step);
        Write(string.Create(CultureInfo.InvariantCulture,
            $"[{ordinal:D3}/{count:D3}] {step.Id} (probe {step.Probe}): dispatched at {_clock.GetUtcNow():HH:mm:ss}Z."));
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
            $"Write one line to {name}{ResponseSuffix}: none, or dialog: <exact title and text>.");
        var deadline = _clock.GetUtcNow() + _timeout;
        var refused = 0;
        while (true)
        {
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
        text.Append(timing == OperatorCheckpoint.Before
            ? "Before this step is sent: confirm that no SpatialAnalyzer dialog is open. If one is, note its exact title and text and dismiss it with OK.\n"
            : "This step has returned. Note any SpatialAnalyzer dialog it raised, including one you dismissed while it ran, with its exact title and text. Dismiss any open dialog with OK.\n");
        text.Append(CultureInfo.InvariantCulture, $"Then write one line to the matching {ResponseSuffix} file: none, or dialog: <title and text>. {OperatorObservationText.Rule}\n");
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
/// Validates operator checkpoint text before it can reach a record: one short
/// line of printable ASCII that starts with <c>none</c> or <c>dialog:</c>, and
/// no backslash, so no Windows path can be recorded by mistake.
/// </summary>
internal static class OperatorObservationText
{
    public const int MaximumLength = 300;

    public const string Rule =
        "Use one line of at most 300 printable ASCII characters, starting with 'none' or 'dialog:', with no backslash, backtick, or pipe.";

    public static bool TryNormalize(string raw, out string observation)
    {
        observation = string.Empty;
        ArgumentNullException.ThrowIfNull(raw);
        var lines = raw.Replace("\r", string.Empty, StringComparison.Ordinal).Trim().Split('\n');
        if (lines.Length != 1)
        {
            return false;
        }

        var text = string.Join(' ', lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (text.Length is 0 or > MaximumLength || !text.All(static c => c is >= ' ' and <= '~' and not '\\' and not '`' and not '|'))
        {
            return false;
        }

        // "none", optionally followed by a separator and a note ("nonexistent" is not "none").
        if (text.StartsWith("none", StringComparison.OrdinalIgnoreCase) && (text.Length == 4 || text[4] is ';' or ',' or ' ' or '.'))
        {
            observation = "none" + text[4..];
            return true;
        }

        if (text.StartsWith("dialog:", StringComparison.OrdinalIgnoreCase) && text[7..].Trim().Length > 0)
        {
            observation = "dialog: " + text[7..].Trim();
            return true;
        }

        return false;
    }
}
