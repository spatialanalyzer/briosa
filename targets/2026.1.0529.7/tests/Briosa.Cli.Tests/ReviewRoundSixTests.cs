using System.Security.Cryptography;
using System.Text;
using Briosa.Cli.Engine;

namespace Briosa.Cli.Tests;

/// <summary>Findings of the fifth re-check: what result delivery reports when several steps fail.</summary>
public sealed class ReviewRoundSixTests
{
    // 1: an --output file that may hold the whole result, here because it could not be
    // read back, is kept and named with the result's size and SHA-256 when the spill
    // that should back it is refused; the result is never reported as in no file.
    [Fact]
    public async Task AnOutputFileThatCouldNotBeReadBackIsNamedWhenTheSpillIsRefused()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var log = new FaultLog();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(DeliveryFault.ReadBackRead,
            DeliveryFault.VolumeRefused, removalFails: false, log));
        var output = Path.Combine(environment.Root, "my-result.json");
        var result = CallOutcomeTests.Result(MpExecutionState.Succeeded, 2);
        var expected = Encoding.UTF8.GetBytes(ProtocolJson.Format(result));
        server.Reply = _ => FakeReply.Ok(result);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--output", output), environment.Environment);

        CallOutcomeTests.AssertResultFile(result, await File.ReadAllBytesAsync(output));
        Assert.Equal(12, run.ExitCode);
        Assert.Equal("file_unverified", run.Data["result_delivery"]!.GetValue<string>());
        Assert.Equal("output-verify-failed", run.Data["result_delivery_error"]!.GetValue<string>());
        var file = run.Data["result_file"]!;
        Assert.Equal(output, file["path"]!.GetValue<string>(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal("not_read_back", file["verification"]!.GetValue<string>());
        Assert.Equal(expected.Length, file["bytes"]!.GetValue<long>());
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(expected)), file["sha256"]!.GetValue<string>());
        Assert.Contains("result-file-unverified", CallOutcomeTests.Warnings(run));
        Assert.Contains("spill-refused", CallOutcomeTests.Warnings(run));
        Assert.DoesNotContain("result-lost", CallOutcomeTests.Warnings(run));
        Assert.DoesNotContain("in any file", run.StandardOutput, StringComparison.Ordinal);
    }

    // 2: a file is reported as delivered only after the operating system was asked to
    // write it to the storage device and said it did; a flush that fails leaves it
    // unconfirmed.
    [Theory]
    [InlineData(DeliveryFault.None, "file")]
    [InlineData(DeliveryFault.FlushToDisk, "file_unconfirmed")]
    public async Task AFileIsDeliveredOnlyOnceItIsFlushedToDisk(DeliveryFault fault, string delivery)
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var log = new FaultLog();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(DeliveryFault.None, fault, removalFails: false, log));
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Equal(delivery, run.Data["result_delivery"]!.GetValue<string>());
        var record = Assert.Single(log.Records);
        Assert.Equal(1, record.Flushes);
        Assert.True(record.Closed);
        Assert.Equal(fault == DeliveryFault.None ? 0 : 12, run.ExitCode);
    }

    // 3: the read-back compares every byte, so a file of the right length with one
    // different byte is not the result.
    [Fact]
    public void AFileOfTheRightLengthWithDifferentBytesIsNotTheResult()
    {
        using var environment = TestEnvironment.Create();
        using (var file = ResultFileSystem.Instance.CreateNew(Path.Combine(environment.Root, "one.json")))
        {
            file.Write("123"u8);
            Assert.Equal(ResultContent.Incomplete, file.ReadBack("124"u8.ToArray()));
            Assert.Equal(ResultContent.Complete, file.ReadBack("123"u8.ToArray()));
        }
    }

    // 3, through the CLI: a spill file whose bytes differ from the result is removed and
    // the result reported lost.
    [Fact]
    public async Task ASpillFileWithDifferentBytesIsRemoved()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var log = new FaultLog();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(DeliveryFault.None,
            DeliveryFault.ContentMismatch, removalFails: false, log));
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Equal(12, run.ExitCode);
        Assert.Equal("lost", run.Data["result_delivery"]!.GetValue<string>());
        Assert.Equal("spill-write-failed", run.Data["result_delivery_error"]!.GetValue<string>());
        Assert.Null(run.Data["result_file"]);
        Assert.Empty(environment.SpilledFiles());
        Assert.Contains("result-lost", CallOutcomeTests.Warnings(run));
    }

    // 4: a new spill file that can be neither handed over nor removed, here because its
    // final-path lookup fails after it was made read-only, is named, empty and labeled
    // incomplete. The operating system itself refuses the removal.
    [Fact]
    public async Task AnEmptySpillFileThatCannotBeRemovedAtHandOverIsNamed()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var lookups = 0;
        var files = new ResultFileSystem(finalPath: handle =>
        {
            if (++lookups == 2)
            {
                File.SetAttributes(handle, FileAttributes.ReadOnly);
                throw new IOException("The file's path could not be read.");
            }

            return NativeMethods.FinalPath(handle);
        });
        using var environment = TestEnvironment.Create(files);
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        var left = Assert.Single(environment.SpilledFiles());
        try
        {
            Assert.Equal(0, new FileInfo(left).Length);
            Assert.Equal(12, run.ExitCode);
            Assert.Equal("lost", run.Data["result_delivery"]!.GetValue<string>());
            Assert.Equal("spill-write-failed", run.Data["result_delivery_error"]!.GetValue<string>());
            var file = run.Data["result_file"]!;
            Assert.Equal(left, file["path"]!.GetValue<string>(), StringComparer.OrdinalIgnoreCase);
            Assert.Equal("incomplete", file["verification"]!.GetValue<string>());
            Assert.Equal("incomplete_not_removed", file["reason"]!.GetValue<string>());
            Assert.Equal(0, file["bytes"]!.GetValue<long>());
            Assert.Null(file["sha256"]);
            Assert.Contains("incomplete-file-left", CallOutcomeTests.Warnings(run));
            Assert.Contains("result-lost", CallOutcomeTests.Warnings(run));
        }
        finally
        {
            File.SetAttributes(left, FileAttributes.Normal);
        }
    }
}
