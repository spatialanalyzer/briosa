using System.Security.Cryptography;
using System.Text;
using Briosa.Cli.Engine;
using Microsoft.Win32.SafeHandles;

namespace Briosa.Cli.Tests;

/// <summary>Findings of the sixth re-check: what delivery reports when a later step fails.</summary>
public sealed class ReviewRoundSevenTests
{
    // 1: a SHA-256 that cannot be computed never hides a file: the file is still written,
    // read back, and named, with the SHA-256 left empty and a warning that says so.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AFileIsStillNamedWhenItsSha256CannotBeComputed(bool toOutput)
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(sha256: _ => throw new CryptographicException("The provider failed."));
        var output = Path.Combine(environment.Root, "result.json");
        var result = CallOutcomeTests.Result(MpExecutionState.Succeeded, 2);
        server.Reply = _ => FakeReply.Ok(result);

        var run = await CliRun.RunAsync(toOutput
            ? CallOutcomeTests.CallArguments(server, "--output", output)
            : CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"), environment.Environment);

        var file = run.Data["result_file"]!;
        var path = file["path"]!.GetValue<string>();
        CallOutcomeTests.AssertResultFile(result, await File.ReadAllBytesAsync(path));
        Assert.Equal(0, run.ExitCode);
        Assert.Equal("file", run.Data["result_delivery"]!.GetValue<string>());
        Assert.Null(run.Data["result_delivery_error"]);
        Assert.Equal("matched", file["verification"]!.GetValue<string>());
        Assert.Null(file["sha256"]);
        Assert.Equal(Encoding.UTF8.GetByteCount(ProtocolJson.Format(result)), file["bytes"]!.GetValue<long>());
        Assert.Contains("result-hash-unavailable", CallOutcomeTests.Warnings(run));
        Assert.DoesNotContain("result-lost", CallOutcomeTests.Warnings(run));
        if (toOutput)
        {
            Assert.Equal(output, path, StringComparer.OrdinalIgnoreCase);
        }
    }

    // 2: the real flush reports a flush Windows refuses. A handle without write access is
    // the way to make FlushFileBuffers fail with ERROR_ACCESS_DENIED on a real file, which
    // .NET's own helper reports as success.
    [Fact]
    public void TheRealFlushReportsAFlushWindowsRefuses()
    {
        using var environment = TestEnvironment.Create();
        var path = Path.Combine(environment.Root, "read-only-handle.json");
        File.WriteAllBytes(path, "{}"u8.ToArray());
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read);

        var refused = Assert.ThrowsAny<IOException>(() => SystemResultFileIo.Instance.FlushToDisk(handle));
        Assert.Equal(NativeMethods.ErrorAccessDenied, refused.HResult & 0xFFFF);
    }

    // 2: and lets a flush that succeeds pass.
    [Fact]
    public void TheRealFlushPassesOnAWritableHandle()
    {
        using var environment = TestEnvironment.Create();
        var path = Path.Combine(environment.Root, "writable-handle.json");
        File.WriteAllBytes(path, "{}"u8.ToArray());
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite);

        SystemResultFileIo.Instance.FlushToDisk(handle);
    }

    // 4: an unused --output reservation that cannot be removed is named by its path, also
    // when the call stops before it finds the operation and data is null.
    [Fact]
    public async Task AnUnremovableReservationIsNamedByItsPath()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(DeliveryFault.None, DeliveryFault.None,
            removalFails: true, new FaultLog()));
        var output = Path.Combine(environment.Root, "never.json");

        var run = await CliRun.RunAsync(["call", "analysis_operations.no_such_operation", "--json", "{}", "--endpoint",
            server.Endpoint, "--output", output], environment.Environment);

        Assert.Equal(3, run.ExitCode);
        Assert.Null(run.Envelope["data"]);
        Assert.True(File.Exists(output));
        var warning = Assert.Single(run.Envelope["warnings"]!.AsArray(),
            entry => entry!["code"]!.GetValue<string>() == "output-reservation-left")!;
        Assert.Contains(output, warning["message"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
    }

    // 4: a spill file that failed its hand-over checks and could not be removed is not
    // reported as one that could not be created.
    [Fact]
    public async Task AFailedHandOverIsNotReportedAsAFailedCreation()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(DeliveryFault.None,
            DeliveryFault.HandOverFinalPath, removalFails: true, new FaultLog()));
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Single(environment.SpilledFiles());
        var message = run.Envelope["warnings"]!.AsArray()
            .Single(entry => entry!["code"]!.GetValue<string>() == "spill-write-failed")!["message"]!.GetValue<string>();
        Assert.Contains("failed its checks", message, StringComparison.Ordinal);
        Assert.Contains("could not be removed", message, StringComparison.Ordinal);
        Assert.DoesNotContain("none is left", message, StringComparison.Ordinal);
        Assert.Contains("incomplete-file-left", CallOutcomeTests.Warnings(run));
    }

    // 5: an incomplete file that cannot be removed keeps the size its read-back measured,
    // even when a later query of its length fails.
    [Fact]
    public async Task AnIncompleteFileKeepsTheSizeItsReadBackMeasured()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new LengthOnceFileSystem());
        var output = Path.Combine(environment.Root, "result.json");
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--output", output), environment.Environment);

        Assert.Equal(3, new FileInfo(output).Length);
        var entry = Assert.Single(run.Data["result_file"]!["other_files"]!.AsArray())!;
        Assert.Equal(output, entry["path"]!.GetValue<string>(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("incomplete", entry["verification"]!.GetValue<string>());
        Assert.Equal(3, entry["bytes"]!.GetValue<long>());
    }

    // The --output file gets three bytes and a failed write, then its removal is refused,
    // and every query of its length after the first fails. The spill works.
    private sealed class LengthOnceFileSystem : IResultFileSystem
    {
        public ResultFile CreateNew(string path) => new ResultFileSystem(io: new LengthOnce(
            new FaultingIo(DeliveryFault.WriteAfterPrefix, removalFails: true, new FaultLog()))).CreateNew(path);

        public ResultFile CreatePrivate(string baseDirectory, IReadOnlyList<string> components, string fileName) =>
            ResultFileSystem.Instance.CreatePrivate(baseDirectory, components, fileName);

        private sealed class LengthOnce(IResultFileIo inner) : IResultFileIo
        {
            private int _lengths;

            public void Write(SafeFileHandle file, ReadOnlySpan<byte> bytes, long offset) => inner.Write(file, bytes, offset);

            public long GetLength(SafeFileHandle file) =>
                ++_lengths == 1 ? inner.GetLength(file) : throw new IOException("The file's length could not be read.");

            public int Read(SafeFileHandle file, Span<byte> buffer, long offset) => inner.Read(file, buffer, offset);

            public void FlushToDisk(SafeFileHandle file) => inner.FlushToDisk(file);

            public void DeleteOnClose(SafeFileHandle file) => inner.DeleteOnClose(file);

            public void Close(SafeFileHandle file) => inner.Close(file);
        }
    }
}
