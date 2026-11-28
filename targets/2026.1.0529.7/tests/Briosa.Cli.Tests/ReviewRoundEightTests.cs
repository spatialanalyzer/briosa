using System.Security.Cryptography;
using Briosa.Cli.Engine;
using Grpc.Core;

namespace Briosa.Cli.Tests;

/// <summary>Findings of the seventh re-check: what the delivery warnings may claim.</summary>
public sealed class ReviewRoundEightTests
{
    private static byte[] NoSha256(byte[] bytes) => throw new CryptographicException("The provider failed.");

    // 1: with the SHA-256 unavailable and a spill file that could not be read back, no
    // warning claims a comparison or a SHA-256 the records do not show.
    [Fact]
    public async Task NoWarningClaimsAComparisonOrASha256ThatDidNotHappen()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        var log = new FaultLog();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(DeliveryFault.None,
            DeliveryFault.ReadBackLength, removalFails: false, log), NoSha256);
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Equal(0, Assert.Single(log.Records).BytesRead);
        var file = run.Data["result_file"]!;
        Assert.Equal("not_read_back", file["verification"]!.GetValue<string>());
        Assert.Null(file["sha256"]);
        var hash = Message(run, "result-hash-unavailable");
        Assert.DoesNotContain("compared", hash, StringComparison.Ordinal);
        Assert.DoesNotContain("read back", hash, StringComparison.Ordinal);
        var unverified = Message(run, "result-file-unverified");
        Assert.Contains("size alone cannot verify it", unverified, StringComparison.Ordinal);
        Assert.DoesNotContain("check that its size and SHA-256", unverified, StringComparison.Ordinal);
    }

    // 1: the SHA-256 warning is given whatever the delivery state, here lost, with an
    // incomplete spill file whose removal was refused.
    [Fact]
    public async Task AMissingSha256IsReportedWhenTheResultIsLost()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(DeliveryFault.None,
            DeliveryFault.WriteAfterPrefix, removalFails: true, new FaultLog()), NoSha256);
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Equal("lost", run.Data["result_delivery"]!.GetValue<string>());
        Assert.Equal(3, run.Data["result_file"]!["bytes"]!.GetValue<long>());
        Assert.Contains("result-hash-unavailable", CallOutcomeTests.Warnings(run));
    }

    // 1: an unused --output reservation whose size could not be read is not called empty,
    // and its entry gives no size.
    [Fact]
    public async Task AReservationOfUnknownSizeIsNotCalledEmpty()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new MatrixFileSystem(DeliveryFault.ReadBackLength,
            DeliveryFault.None, removalFails: false, new FaultLog()));
        server.Reply = _ => FakeReply.Status(StatusCode.Internal);
        var output = Path.Combine(environment.Root, "never.json");

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--output", output), environment.Environment);

        Assert.True(File.Exists(output));
        var file = run.Data["result_file"]!;
        Assert.Equal("reservation_not_removed", file["reason"]!.GetValue<string>());
        Assert.Null(file["bytes"]);
        var left = Message(run, "output-reservation-left");
        Assert.DoesNotContain("It is empty", left, StringComparison.Ordinal);
        Assert.Contains("the CLI wrote nothing to it", left, StringComparison.Ordinal);
    }

    private static string Message(CliRun run, string code) =>
        run.Envelope["warnings"]!.AsArray().Single(entry => entry!["code"]!.GetValue<string>() == code)!["message"]!
            .GetValue<string>();
}
