using System.Text;
using System.Text.Json.Nodes;
using Briosa.Cli.CommandLine;
using Briosa.Cli.Engine;

namespace Briosa.Cli.Tests;

/// <summary>Runs the real CLI entry point in process and captures what it wrote.</summary>
internal sealed class CliRun
{
    private CliRun(int exitCode, byte[] standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutputBytes = standardOutput;
        StandardOutput = Encoding.UTF8.GetString(standardOutput);
        StandardError = standardError;
        Envelope = JsonNode.Parse(StandardOutput)!.AsObject();
    }

    public int ExitCode { get; }

    public byte[] StandardOutputBytes { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public JsonObject Envelope { get; }

    public string Outcome => Envelope["outcome"]!.GetValue<string>();

    public string? DiagnosticCode => Envelope["failure"]?["diagnostic_code"]?.GetValue<string>();

    public JsonObject Data => Envelope["data"]!.AsObject();

    public JsonObject Execution => Envelope["execution"]!.AsObject();

    public static async Task<CliRun> RunAsync(IReadOnlyList<string> arguments, BridgeEnvironment? environment = null,
        CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        using var error = new StringWriter();
        var exitCode = await CliApplication.RunAsync(arguments,
            new CliHost(output, error, environment ?? TestEnvironment.Shared.Environment),
            cancellationToken).ConfigureAwait(true);
        return new CliRun(exitCode, output.ToArray(), error.ToString());
    }
}

/// <summary>A per-test spill directory and file system.</summary>
internal sealed class TestEnvironment : IDisposable
{
    private TestEnvironment(string root, IResultFileSystem files)
    {
        Root = root;
        SpillDirectory = Path.Combine([root, .. SpillComponents]);
        Environment = new BridgeEnvironment(files, root, SpillComponents, TimeProvider.System);
    }

    /// <summary>The managed directories below the test root, as below %LOCALAPPDATA%.</summary>
    public static IReadOnlyList<string> SpillComponents { get; } = ["Briosa", "cli", "results"];

    public string Root { get; }

    public string SpillDirectory { get; }

    public BridgeEnvironment Environment { get; }

    /// <summary>For tests that never deliver a result to a file.</summary>
    public static TestEnvironment Shared { get; } = Create();

    public static TestEnvironment Create(IResultFileSystem? files = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "briosa-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new TestEnvironment(root, files ?? ResultFileSystem.Instance);
    }

    public string[] SpilledFiles() =>
        Directory.Exists(SpillDirectory) ? Directory.GetFiles(SpillDirectory) : [];

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

/// <summary>Where a <see cref="FailingWriteFileSystem"/> makes a result file fail.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal",
    Justification = "xUnit theories take it as a parameter of their public test methods.")]
public enum WriteFault
{
    /// <summary>The write fails before any byte reaches the file.</summary>
    BeforeAnyByte,

    /// <summary>Three bytes reach the file, then the write fails.</summary>
    AfterPrefix,

    /// <summary>Three bytes reach the file and it is made read-only, so it cannot be removed, then the write fails.</summary>
    AfterPrefixUnremovable,

    /// <summary>The whole result reaches the file, then the flush fails.</summary>
    AtFlush,

    /// <summary>The whole result reaches the file and is flushed, then the close fails.</summary>
    AtClose,

    /// <summary>The whole result reaches the file, but reading it back fails.</summary>
    ReadBack
}

/// <summary>A file system whose result files fail at one point, to prove how each failure is delivered.</summary>
internal sealed class FailingWriteFileSystem(Func<string, bool> failsFor, WriteFault fault = WriteFault.BeforeAnyByte)
    : IResultFileSystem
{
    public ResultFile CreateNew(string path) => Wrap(ResultFileSystem.Instance.CreateNew(path));

    public ResultFile CreatePrivate(string baseDirectory, IReadOnlyList<string> components, string fileName) =>
        Wrap(ResultFileSystem.Instance.CreatePrivate(baseDirectory, components, fileName));

    private ResultFile Wrap(ResultFile file)
    {
        if (failsFor(file.Path))
        {
            file.WrapStream(stream => new FailingStream(stream, fault, file));
            if (fault == WriteFault.ReadBack)
            {
                file.ReplaceReadBack((_, _) => ResultContent.Unknown);
            }
        }

        return file;
    }

    private sealed class FailingStream(Stream inner, WriteFault fault, ResultFile file) : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => inner.Length;

        public override long Position { get => inner.Position; set => throw new NotSupportedException(); }

        public override void Flush()
        {
            inner.Flush();
            if (fault == WriteFault.AtFlush)
            {
                throw new IOException("The flush failed.");
            }
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            switch (fault)
            {
                case WriteFault.BeforeAnyByte:
                    throw new IOException("There is not enough space on the disk.");
                case WriteFault.AfterPrefix or WriteFault.AfterPrefixUnremovable:
                    inner.Write(buffer[..Math.Min(3, buffer.Length)]);
                    inner.Flush();
                    if (fault == WriteFault.AfterPrefixUnremovable)
                    {
                        File.SetAttributes(file.Handle, FileAttributes.ReadOnly);
                    }

                    throw new IOException("There is not enough space on the disk.");
                default:
                    inner.Write(buffer);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                if (fault == WriteFault.AtClose)
                {
                    throw new IOException("The close failed.");
                }
            }

            base.Dispose(disposing);
        }
    }
}

/// <summary>
/// Puts a new, empty file at a reservation's path the moment the CLI closes the
/// reservation's handle, as a concurrent process could. A release that deletes by
/// name after closing would delete that file; one that deletes through the handle
/// cannot.
/// </summary>
internal sealed class ReplacingFileSystem : IResultFileSystem
{
    public ResultFile CreateNew(string path)
    {
        var file = ResultFileSystem.Instance.CreateNew(path);
        file.WrapStream(stream => new ReplacingStream(stream, path));
        return file;
    }

    public ResultFile CreatePrivate(string baseDirectory, IReadOnlyList<string> components, string fileName) =>
        ResultFileSystem.Instance.CreatePrivate(baseDirectory, components, fileName);

    private sealed class ReplacingStream(Stream inner, string path) : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => inner.Length;

        public override long Position { get => inner.Position; set => throw new NotSupportedException(); }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                // The handle is closed: someone else now creates a file of the same name.
                File.WriteAllBytes(path, []);
            }

            base.Dispose(disposing);
        }
    }
}
