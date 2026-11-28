using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Briosa.Cli.Engine;
using Grpc.Core;
using Microsoft.Win32.SafeHandles;

namespace Briosa.Cli.Tests;

/// <summary>One point where delivering a result to a file can fail.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal",
    Justification = "xUnit theories take it as a parameter of their public test methods.")]
public enum DeliveryFault
{
    None,

    // A spill, before its file exists.
    DirectoryOpen,
    Permissions,
    VolumeRefused,
    FileCreate,

    // A spill, after its file exists and before it is handed over.
    HandOverFinalPath,
    HandOverPathMismatch,

    // Either destination: writing the file and finishing it.
    WriteBeforeAnyByte,
    WriteAfterPrefix,
    WriteAfterPayload,
    ContentMismatch,
    ReadBackLength,
    ReadBackRead,
    FlushToDisk,
    Close
}

/// <summary>What the real handle operations did to one file, as the test saw them.</summary>
internal sealed class FileRecord(string path)
{
    public string Path { get; } = path;

    /// <summary>The bytes that actually reached the file, or null when nothing was written.</summary>
    public byte[]? Written { get; set; }

    public bool WriteFailed { get; set; }

    public int BytesRead { get; set; }

    public bool ReadFailed { get; set; }

    public int Flushes { get; set; }

    public bool FlushFailed { get; set; }

    public bool Closed { get; set; }

    public bool CloseFailed { get; set; }

    public bool RemovalRefused { get; set; }

    public override string ToString() =>
        $"written {Written?.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "nothing"}" +
        $"{(WriteFailed ? " (failed)" : "")}, read {BytesRead}{(ReadFailed ? " (failed)" : "")}, flushes {Flushes}" +
        $"{(FlushFailed ? " (failed)" : "")}, closed {Closed}{(CloseFailed ? " (failed)" : "")}" +
        $"{(RemovalRefused ? ", removal refused" : "")}";
}

/// <summary>Every file the handle operations touched, keyed by handle, with its real path.</summary>
internal sealed class FaultLog
{
    private readonly Dictionary<SafeFileHandle, FileRecord> _records = new(ReferenceEqualityComparer.Instance);

    public List<FileRecord> Records { get; } = [];

    public FileRecord For(SafeFileHandle file)
    {
        if (!_records.TryGetValue(file, out var record))
        {
            record = new FileRecord(NativeMethods.FinalPath(file));
            _records.Add(file, record);
            Records.Add(record);
        }

        return record;
    }
}

/// <summary>
/// The real handle operations with one fault injected, and a log of what really
/// happened. A removal that fails is refused before the real call, so the file stays.
/// </summary>
internal sealed class FaultingIo(DeliveryFault fault, bool removalFails, FaultLog log) : IResultFileIo
{
    private static SystemResultFileIo Real => SystemResultFileIo.Instance;

    public void Write(SafeFileHandle file, ReadOnlySpan<byte> bytes, long offset)
    {
        var record = log.For(file);
        switch (fault)
        {
            case DeliveryFault.WriteBeforeAnyByte:
                record.WriteFailed = true;
                throw new IOException("There is not enough space on the disk.");
            case DeliveryFault.WriteAfterPrefix:
                Real.Write(file, bytes[..3], offset);
                record.Written = bytes[..3].ToArray();
                record.WriteFailed = true;
                throw new IOException("There is not enough space on the disk.");
            case DeliveryFault.WriteAfterPayload:
                Real.Write(file, bytes, offset);
                record.Written = bytes.ToArray();
                record.WriteFailed = true;
                throw new IOException("The write reported an error after its last byte.");
            case DeliveryFault.ContentMismatch:
                // The right length, one different byte, and no error.
                var wrong = bytes.ToArray();
                wrong[wrong.Length / 2] ^= 0x01;
                Real.Write(file, wrong, offset);
                record.Written = wrong;
                return;
            default:
                Real.Write(file, bytes, offset);
                record.Written = bytes.ToArray();
                return;
        }
    }

    public long GetLength(SafeFileHandle file)
    {
        var record = log.For(file);
        if (fault == DeliveryFault.ReadBackLength)
        {
            record.ReadFailed = true;
            throw new IOException("The file's length could not be read.");
        }

        return Real.GetLength(file);
    }

    public int Read(SafeFileHandle file, Span<byte> buffer, long offset)
    {
        var record = log.For(file);
        if (fault == DeliveryFault.ReadBackRead)
        {
            record.ReadFailed = true;
            throw new IOException("The read failed.");
        }

        var read = Real.Read(file, buffer, offset);
        record.BytesRead += read;
        return read;
    }

    public void FlushToDisk(SafeFileHandle file)
    {
        var record = log.For(file);
        record.Flushes++;
        Real.FlushToDisk(file);
        if (fault == DeliveryFault.FlushToDisk)
        {
            record.FlushFailed = true;
            throw new IOException("The flush to disk failed.");
        }
    }

    public void DeleteOnClose(SafeFileHandle file)
    {
        var record = log.For(file);
        if (removalFails)
        {
            record.RemovalRefused = true;
            throw new IOException("Access is denied.");
        }

        Real.DeleteOnClose(file);
    }

    public void Close(SafeFileHandle file)
    {
        var record = log.For(file);
        Real.Close(file);
        record.Closed = true;
        if (fault == DeliveryFault.Close)
        {
            record.CloseFailed = true;
            throw new IOException("The close failed.");
        }
    }
}

/// <summary>
/// The real file system with one fault armed for the --output file and one for the
/// spill, and every removal refused or not.
/// </summary>
internal sealed class MatrixFileSystem(DeliveryFault output, DeliveryFault spill, bool removalFails, FaultLog log)
    : IResultFileSystem
{
    public ResultFile CreateNew(string path) =>
        new ResultFileSystem(io: new FaultingIo(output, removalFails, log)).CreateNew(path);

    public ResultFile CreatePrivate(string baseDirectory, IReadOnlyList<string> components, string fileName)
    {
        var lookups = 0;
        var files = new ResultFileSystem(
            keepsPermissions: spill == DeliveryFault.VolumeRefused ? _ => false : null,
            finalPath: handle =>
            {
                var path = NativeMethods.FinalPath(handle);
                // The first lookup is the base's, the second the new file's.
                return ++lookups != 2 ? path : spill switch
                {
                    DeliveryFault.HandOverFinalPath => throw new IOException("The file's path could not be read."),
                    DeliveryFault.HandOverPathMismatch => path + ".elsewhere",
                    _ => path
                };
            },
            io: new FaultingIo(spill, removalFails, log),
            beforeStep: step =>
            {
                if ((step, spill) is (SpillStep.OpenDirectory, DeliveryFault.DirectoryOpen) or
                    (SpillStep.Protect, DeliveryFault.Permissions) or (SpillStep.CreateFile, DeliveryFault.FileCreate))
                {
                    throw new IOException("Injected failure before " + step + ".");
                }
            });
        return files.CreatePrivate(baseDirectory, components, fileName);
    }
}

/// <summary>
/// Every combination of where delivery fails, checked against the delivery invariants
/// on the operations page, from what is on disk after the call and what the handle
/// operations really did, never from the CLI's own account of it.
/// </summary>
public sealed class DeliveryFaultMatrixTests
{
    internal enum Destination
    {
        // No --output and a result within the cap.
        Inline,

        // No --output and a cap of 0.
        Spill,

        // --output.
        Output,

        // --output, and the call returns no result.
        OutputUnusedNoResult,

        // --output, and the call is refused before the operation is found.
        OutputUnusedRefused
    }

    internal sealed record Cell(Destination Destination, DeliveryFault OutputFault, DeliveryFault SpillFault,
        bool RemovalFails, bool CompletionEstablished)
    {
        public override string ToString() =>
            $"{Destination} output={OutputFault} spill={SpillFault} removal={(RemovalFails ? "fails" : "works")} " +
            $"completion={(CompletionEstablished ? "established" : "not established")}";
    }

    // The faults a file can meet once it exists, on either destination.
    internal static readonly DeliveryFault[] FileFaults =
    [
        DeliveryFault.None, DeliveryFault.WriteBeforeAnyByte, DeliveryFault.WriteAfterPrefix,
        DeliveryFault.WriteAfterPayload, DeliveryFault.ContentMismatch, DeliveryFault.ReadBackLength,
        DeliveryFault.ReadBackRead, DeliveryFault.FlushToDisk, DeliveryFault.Close
    ];

    internal static IEnumerable<Cell> Cells()
    {
        bool[] both = [false, true];
        foreach (var destination in new[] { Destination.Inline, Destination.Spill })
        {
            foreach (var spill in Enum.GetValues<DeliveryFault>())
            {
                foreach (var removal in both)
                {
                    foreach (var completion in both)
                    {
                        yield return new Cell(destination, DeliveryFault.None, spill, removal, completion);
                    }
                }
            }
        }

        foreach (var output in FileFaults)
        {
            foreach (var spill in Enum.GetValues<DeliveryFault>())
            {
                foreach (var removal in both)
                {
                    foreach (var completion in both)
                    {
                        yield return new Cell(Destination.Output, output, spill, removal, completion);
                    }
                }
            }
        }

        foreach (var destination in new[] { Destination.OutputUnusedNoResult, Destination.OutputUnusedRefused })
        {
            foreach (var removal in both)
            {
                yield return new Cell(destination, DeliveryFault.None, DeliveryFault.None, removal, false);
            }
        }
    }

    [Fact]
    public async Task EveryCellOfTheDeliveryFaultMatrixKeepsTheInvariants()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var cells = Cells().ToList();
        // 2 x 15 x 2 x 2 without --output, 9 x 15 x 2 x 2 with it, and 2 x 2 unused reservations.
        Assert.Equal(664, cells.Count);

        var violations = new List<string>();
        foreach (var cell in cells)
        {
            violations.AddRange((await RunAsync(server, cell)).Select(problem => cell + ": " + problem));
        }

        Assert.True(violations.Count == 0, Summary(cells.Count, violations));
    }

    internal static string Summary(int cells, IReadOnlyList<string> violations)
    {
        var byInvariant = violations.GroupBy(violation => violation.Split(": ")[1].Split(' ')[0])
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key} {group.Count()}");
        var failedCells = violations.Select(violation => violation.Split(": ")[0]).Distinct(StringComparer.Ordinal).Count();
        return $"{failedCells} of {cells} cells broke an invariant ({string.Join(", ", byInvariant)}):" +
            Environment.NewLine + string.Join(Environment.NewLine, violations.Take(40));
    }

    private static readonly string[] MayHoldTheResult = ["matched", "matched_unconfirmed", "not_read_back"];

    // The path the file system resolves an existing file to, so that a short name in the
    // temporary folder's path compares equal to the long name a final path reports.
    private static string? Canonical(string? path)
    {
        if (path is null || !File.Exists(path))
        {
            return path;
        }

        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return NativeMethods.FinalPath(handle);
    }

    internal static async Task<List<string>> RunAsync(FakeBriosaServer server, Cell cell)
    {
        var log = new FaultLog();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(cell.OutputFault, cell.SpillFault,
            cell.RemovalFails, log));
        var result = CallOutcomeTests.Result(MpExecutionState.Succeeded, cell.CompletionEstablished ? 2 : 4);
        var expected = Encoding.UTF8.GetBytes(ProtocolJson.Format(result));
        var hash = Convert.ToHexStringLower(SHA256.HashData(expected));
        server.Reply = cell.Destination == Destination.OutputUnusedNoResult
            ? _ => FakeReply.Status(StatusCode.Internal)
            : _ => FakeReply.Ok(result);
        Directory.CreateDirectory(Path.Combine(environment.Root, "out"));
        var output = Path.Combine(environment.Root, "out", "result.json");
        string[] arguments = cell.Destination switch
        {
            Destination.Inline => CallOutcomeTests.CallArguments(server),
            Destination.Spill => CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            Destination.OutputUnusedRefused =>
                ["call", "analysis_operations.no_such_operation", "--json", "{}", "--endpoint", server.Endpoint, "--output", output],
            _ => CallOutcomeTests.CallArguments(server, "--output", output)
        };

        var run = await CliRun.RunAsync(arguments, environment.Environment);

        var problems = new List<string>();
        var data = run.Envelope["data"] as JsonObject;
        var warnings = CallOutcomeTests.Warnings(run);
        var onDisk = Directory.EnumerateFiles(environment.Root, "*", SearchOption.AllDirectories).ToList();
        var primary = data?["result_file"] as JsonObject;
        var entries = primary is null ? [] : new[] { primary }.Concat(
            (primary["other_files"] as JsonArray ?? []).Select(entry => (JsonObject)entry!)).ToList();
        var delivery = data?["result_delivery"]?.GetValue<string>();

        static bool Same(string? left, string? right) =>
            string.Equals(Canonical(left), Canonical(right), StringComparison.OrdinalIgnoreCase);
        static string Name(string path) => Path.GetFileName(path);
        static string PathOf(JsonObject entry) => entry["path"]!.GetValue<string>();
        static string Label(JsonObject entry) => entry["verification"]?.GetValue<string>() ?? "(none)";
        bool Complete(string path) => File.ReadAllBytes(path).AsSpan().SequenceEqual(expected);
        JsonObject? EntryFor(string path) => entries.FirstOrDefault(entry => Same(PathOf(entry), path));
        FileRecord? RecordFor(string path) => log.Records.LastOrDefault(record => Same(record.Path, path));

        var messages = run.Envelope["warnings"]!.AsArray().Select(warning => warning!["message"]!.GetValue<string>()).ToList();
        bool IsOutput(string path) => string.Equals(Name(path), Name(output), StringComparison.OrdinalIgnoreCase);
        var diskOutput = onDisk.Where(IsOutput).ToList();
        var diskSpill = onDisk.Where(path => !IsOutput(path)).ToList();

        if (cell.Destination is Destination.OutputUnusedNoResult or Destination.OutputUnusedRefused)
        {
            // The model: the reservation is the only file, it stays only when its removal
            // is refused, and then it is named, with its path.
            var left = cell.RemovalFails;
            if (!log.Records.Any(record => IsOutput(record.Path)))
            {
                problems.Add("M-attempts the reservation was never inspected");
            }

            if (diskOutput.Count != (left ? 1 : 0) || diskSpill.Count != 0)
            {
                problems.Add($"M-disk {diskOutput.Count} --output and {diskSpill.Count} spill files on disk");
            }

            var wantedExit = cell.Destination == Destination.OutputUnusedRefused ? 3 : 10;
            if (run.ExitCode != wantedExit)
            {
                problems.Add($"I5 exit code {run.ExitCode}, not {wantedExit}");
            }

            var warning = messages.Where((_, index) => warnings[index] == "output-reservation-left").ToList();
            if (warning.Count != (left ? 1 : 0))
            {
                problems.Add($"I3 output-reservation-left is {(left ? "missing" : "present")}");
            }
            else if (left && !warning[0].Contains(output, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add("I3 output-reservation-left does not give the file's path");
            }

            if ((cell.Destination == Destination.OutputUnusedRefused) != (data is null))
            {
                problems.Add("M-data data is " + (data is null ? "null" : "present"));
            }
            else if (data is not null && (left
                ? EntryFor(output) is not { } entry || Label(entry) != "incomplete" ||
                  entry["reason"]?.GetValue<string>() != "reservation_not_removed"
                : primary is not null))
            {
                problems.Add("I3 data.result_file does not name the reservation that is left, or names one that is not");
            }

            return problems;
        }

        // The model, from the injected faults alone: never from the envelope.
        var model = Predict(cell, expected.Length);

        // The write attempts the faults call for, in the handle log: the --output file
        // when there is one, and a spill file whenever the faults say one is created,
        // including the fallback behind an --output file that is not matched.
        if (log.Records.Any(record => IsOutput(record.Path)) != model.OutputTried)
        {
            problems.Add($"M-attempts the --output file was {(model.OutputTried ? "not " : "")}written");
        }

        var spillAttempts = log.Records.Count(record => !IsOutput(record.Path));
        if (spillAttempts != (model.SpillCreated ? 1 : 0))
        {
            problems.Add($"M-attempts {spillAttempts} spill files were created, not {(model.SpillCreated ? 1 : 0)}");
        }

        // What is on disk is what the faults leave: each file, its size, and whether it
        // holds the whole result.
        var wantedOutput = model.OnDisk.Where(file => file.Output).ToList();
        var wantedSpill = model.OnDisk.Where(file => !file.Output).ToList();
        if (diskOutput.Count != wantedOutput.Count || diskSpill.Count != wantedSpill.Count)
        {
            problems.Add($"M-disk {diskOutput.Count} --output and {diskSpill.Count} spill files on disk, not " +
                $"{wantedOutput.Count} and {wantedSpill.Count}");
        }
        else
        {
            foreach (var (path, wanted) in diskOutput.Zip(wantedOutput).Concat(diskSpill.Zip(wantedSpill)))
            {
                if (new FileInfo(path).Length != wanted.Bytes || Complete(path) != (wanted.Outcome != Expected.Incomplete))
                {
                    problems.Add($"M-disk {Name(path)} holds {new FileInfo(path).Length} bytes, " +
                        $"{(Complete(path) ? "the result" : "not the result")}, not what its fault leaves");
                }
            }
        }

        // The envelope names exactly those files, by the primary-file rule (the best
        // verification first, the --output file first on a tie), each with its label.
        var named = entries.Select(entry => $"{(IsOutput(PathOf(entry)) ? "output" : "spill")} {Label(entry)}").ToList();
        var wantedNamed = model.Named.Select(file => $"{(file.Output ? "output" : "spill")} {LabelOf(file.Outcome)}").ToList();
        if (named.FirstOrDefault() != wantedNamed.FirstOrDefault())
        {
            problems.Add($"M-primary data.result_file is {named.FirstOrDefault() ?? "null"}, not {wantedNamed.FirstOrDefault() ?? "null"}");
        }
        else if (!named.SequenceEqual(wantedNamed))
        {
            problems.Add($"M-named the files named are [{string.Join(", ", named)}], not [{string.Join(", ", wantedNamed)}]");
        }

        if (delivery != model.State)
        {
            problems.Add($"M-state result_delivery is {delivery}, not {model.State}");
        }

        var error = data?["result_delivery_error"]?.GetValue<string>();
        if (error != model.Error)
        {
            problems.Add($"M-error result_delivery_error is {error ?? "null"}, not {model.Error ?? "null"}");
        }

        if (cell.Destination != Destination.Inline)
        {
            void Expect(string code, bool present)
            {
                if (warnings.Contains(code) != present)
                {
                    problems.Add($"M-warnings {code} is {(present ? "missing" : "present")}");
                }
            }

            Expect("result-lost", model.State == "lost");
            Expect("spill-refused", model.SpillTried && cell.SpillFault == DeliveryFault.VolumeRefused);
            Expect("result-spilled", cell.Destination == Destination.Spill && model.State == "file");
            Expect("incomplete-file-left", model.OnDisk.Any(file => file.Outcome == Expected.Incomplete));
            if (diskSpill.Count > 0 && messages.Any(message => message.Contains("none is left there", StringComparison.Ordinal)))
            {
                problems.Add("M-warnings a warning says no spill file is left, but one is");
            }
        }

        // I3: every file on disk is named in the envelope, and every named file is on disk.
        foreach (var path in onDisk.Where(path => EntryFor(path) is null))
        {
            problems.Add($"I3 {Name(path)} is on disk but not named ({(Complete(path) ? "complete" : "incomplete")})");
        }

        foreach (var entry in entries.Where(entry => !File.Exists(PathOf(entry))))
        {
            problems.Add($"I3 {Name(PathOf(entry))} is named ({Label(entry)}) but not on disk");
        }

        // I1: a file that holds the whole result is named as one that may hold it, with
        // the result's size and SHA-256, is never reported absent, and is never deleted.
        foreach (var path in onDisk.Where(Complete))
        {
            if (EntryFor(path) is not { } entry)
            {
                continue;
            }

            if (!MayHoldTheResult.Contains(Label(entry)))
            {
                problems.Add($"I1 {Name(path)} holds the whole result but is labeled {Label(entry)}");
            }

            if (entry["sha256"]?.GetValue<string>() != hash || entry["bytes"]?.GetValue<long>() != expected.Length)
            {
                problems.Add($"I1 {Name(path)} holds the whole result but is named without its size and SHA-256");
            }
        }

        if (onDisk.Any(Complete) && (delivery == "lost" || warnings.Contains("result-lost")))
        {
            problems.Add($"I1 the result is reported as lost while a file holds it ({delivery})");
        }

        foreach (var record in log.Records.Where(record => record.Written?.AsSpan().SequenceEqual(expected) == true))
        {
            if (!File.Exists(record.Path))
            {
                problems.Add($"I1 {Name(record.Path)} received the whole result and was deleted");
            }
        }

        // I2: matched only when the content read back in full and every step succeeded.
        foreach (var entry in entries.Where(entry => Label(entry) is "matched" or "matched_unconfirmed"))
        {
            var path = PathOf(entry);
            if (!File.Exists(path) || !Complete(path))
            {
                problems.Add($"I2 {Name(path)} is labeled {Label(entry)} but does not hold the whole result");
            }

            var record = RecordFor(path);
            var read = record is { ReadFailed: false } && record.BytesRead >= expected.Length;
            if (!read)
            {
                problems.Add($"I2 {Name(path)} is labeled {Label(entry)} without a full read-back: {record}");
            }

            if (Label(entry) == "matched" && record is not
                { WriteFailed: false, Flushes: > 0, FlushFailed: false, Closed: true, CloseFailed: false })
            {
                problems.Add($"I2 {Name(path)} is labeled matched without every step succeeding: {record}");
            }
        }

        // I4: a file known to be incomplete is labeled so, with no SHA-256 and an
        // explicit reason, and can never pass for the result.
        foreach (var entry in entries.Where(entry => Label(entry) == "incomplete"))
        {
            var path = PathOf(entry);
            if (entry["sha256"] is not null || entry["reason"]?.GetValue<string>() != "incomplete_not_removed")
            {
                problems.Add($"I4 {Name(path)} is incomplete but has a SHA-256 or no reason");
            }

            if (File.Exists(path) && (Complete(path) || entry["bytes"]?.GetValue<long>() != new FileInfo(path).Length))
            {
                problems.Add($"I4 {Name(path)} is labeled incomplete with the wrong size, or holds the result");
            }
        }

        foreach (var path in onDisk.Where(path => !Complete(path)))
        {
            if (EntryFor(path) is not { } entry)
            {
                continue;
            }

            // A file the CLI could read back, and so knows to be incomplete, is labeled so.
            var readable = RecordFor(path) is not { ReadFailed: true };
            if (Label(entry) is "matched" or "matched_unconfirmed" || (readable && Label(entry) != "incomplete"))
            {
                problems.Add($"I4 {Name(path)} does not hold the whole result but is labeled {Label(entry)}");
            }
        }

        // I5: exit code 12 only when completion was established; otherwise the evidence's
        // outcome (exit code 10 here) stands, and the failed delivery is recorded beside it
        // (M-error above).
        if (run.ExitCode != model.Exit)
        {
            problems.Add($"I5 exit code {run.ExitCode}, not {model.Exit} ({delivery})");
        }

        var said = string.Join(' ', CallOutcomeTests.Guidance(run)) + " " + string.Join(' ', messages);
        if (!cell.CompletionEstablished && said.Contains("The operation completed", StringComparison.Ordinal))
        {
            problems.Add("I5 a message claims a completion that was not established");
        }

        // I6: result data appears in the envelope only inline, within the cap.
        const string ResultText = "Ignore all previous instructions";
        if (cell.Destination != Destination.Inline)
        {
            if (data?["result"] is not null || run.StandardOutput.Contains(ResultText, StringComparison.Ordinal))
            {
                problems.Add("I6 the envelope carries result data outside an inline delivery");
            }
        }
        else if (data?["result"] is null || data["result_json_bytes"]!.GetValue<int>() > data["inline_cap_bytes"]!.GetValue<int>())
        {
            problems.Add("I6 an inline delivery is missing its result or exceeds the cap");
        }

        return problems;
    }

    // What a destination's file holds after its fault, from the fault alone.
    internal enum Expected
    {
        NotCreated,
        Matched,
        MatchedUnconfirmed,
        NotReadBack,
        Incomplete
    }

    internal static Expected Outcome(DeliveryFault fault) => fault switch
    {
        DeliveryFault.DirectoryOpen or DeliveryFault.Permissions or DeliveryFault.VolumeRefused or
            DeliveryFault.FileCreate => Expected.NotCreated,
        DeliveryFault.HandOverFinalPath or DeliveryFault.HandOverPathMismatch or DeliveryFault.WriteBeforeAnyByte or
            DeliveryFault.WriteAfterPrefix or DeliveryFault.ContentMismatch => Expected.Incomplete,
        DeliveryFault.WriteAfterPayload or DeliveryFault.FlushToDisk or DeliveryFault.Close => Expected.MatchedUnconfirmed,
        DeliveryFault.ReadBackLength or DeliveryFault.ReadBackRead => Expected.NotReadBack,
        _ => Expected.Matched
    };

    private static string LabelOf(Expected outcome) => outcome switch
    {
        Expected.Matched => "matched",
        Expected.MatchedUnconfirmed => "matched_unconfirmed",
        Expected.NotReadBack => "not_read_back",
        _ => "incomplete"
    };

    /// <summary>One file the faults leave on disk, and the bytes it holds.</summary>
    internal sealed record ExpectedFile(bool Output, Expected Outcome, long Bytes);

    /// <summary>What the injected faults alone say a cell must do.</summary>
    internal sealed record Model(bool OutputTried, bool SpillTried, bool SpillCreated, IReadOnlyList<ExpectedFile> OnDisk,
        IReadOnlyList<ExpectedFile> Named, string State, string? Error, int Exit);

    // The delivery rules of the operations page, applied to the faults of one cell.
    internal static Model Predict(Cell cell, int length)
    {
        bool Stays(Expected outcome) => outcome is Expected.Matched or Expected.MatchedUnconfirmed or Expected.NotReadBack ||
            (outcome == Expected.Incomplete && cell.RemovalFails);
        long Bytes(Expected outcome, DeliveryFault fault) => outcome != Expected.Incomplete ? length : fault switch
        {
            DeliveryFault.WriteAfterPrefix => 3,
            DeliveryFault.ContentMismatch => length,
            _ => 0
        };

        Expected? output = cell.Destination == Destination.Output ? Outcome(cell.OutputFault) : null;
        // A spill is tried without --output when the result is over the cap, and behind
        // an --output file that is neither matched nor matched_unconfirmed.
        var spillTried = cell.Destination == Destination.Spill || output is Expected.NotReadBack or Expected.Incomplete;
        Expected? spill = spillTried ? Outcome(cell.SpillFault) : null;
        var onDisk = new List<ExpectedFile>();
        if (output is { } kept && Stays(kept))
        {
            onDisk.Add(new ExpectedFile(true, kept, Bytes(kept, cell.OutputFault)));
        }

        if (spill is { } spilled && spilled != Expected.NotCreated && Stays(spilled))
        {
            onDisk.Add(new ExpectedFile(false, spilled, Bytes(spilled, cell.SpillFault)));
        }

        // The primary-file rule: matched, then matched_unconfirmed, then not_read_back,
        // then incomplete; on a tie, the --output file, which is listed first.
        var named = onDisk.OrderBy(file => file.Outcome).ToList();
        var state = cell.Destination == Destination.Inline ? "inline" : named.FirstOrDefault()?.Outcome switch
        {
            Expected.Matched => "file",
            Expected.MatchedUnconfirmed => "file_unconfirmed",
            Expected.NotReadBack => "file_unverified",
            _ => "lost"
        };
        var target = cell.Destination == Destination.Output ? output : spill;
        var delivered = cell.Destination == Destination.Inline || target == Expected.Matched;
        var error = delivered ? null
            : cell.Destination == Destination.Output ? output switch
            {
                Expected.MatchedUnconfirmed => "output-finalize-failed",
                Expected.NotReadBack => "output-verify-failed",
                _ => "output-write-failed"
            }
            : cell.SpillFault == DeliveryFault.VolumeRefused ? "spill-permissions-unsupported"
            : spill switch
            {
                Expected.MatchedUnconfirmed => "spill-finalize-failed",
                Expected.NotReadBack => "spill-verify-failed",
                _ => "spill-write-failed"
            };
        var exit = !cell.CompletionEstablished ? 10 : delivered ? 0 : 12;
        return new Model(output is not null, spillTried, spill is { } created && created != Expected.NotCreated, onDisk,
            named, state, error, exit);
    }
}
