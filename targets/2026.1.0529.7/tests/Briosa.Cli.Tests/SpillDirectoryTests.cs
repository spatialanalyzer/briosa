using System.Diagnostics;
using Briosa.Cli.Engine;

namespace Briosa.Cli.Tests;

/// <summary>
/// The spill directory is checked at every managed level and the file is bound to
/// the directory that was checked. Directory junctions need no elevation on Windows,
/// so these run for real.
/// </summary>
public sealed class SpillDirectoryTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task AJunctionAtAnyManagedLevelIsRefusedAndNothingIsWrittenThroughIt(int level)
    {
        using var environment = TestEnvironment.Create();
        var elsewhere = Path.Combine(environment.Root, "elsewhere");
        Directory.CreateDirectory(elsewhere);
        var components = TestEnvironment.SpillComponents.Take(level).ToArray();
        var parent = Path.Combine([environment.Root, .. components[..^1]]);
        Directory.CreateDirectory(parent);
        CreateJunction(Path.Combine(parent, components[^1]), elsewhere);
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Equal(12, run.ExitCode);
        Assert.Equal("spill-write-failed", run.DiagnosticCode);
        Assert.Empty(Directory.EnumerateFileSystemEntries(elsewhere));
    }

    [Fact]
    public void APrivateFileIsCreatedOnlyBelowTheVerifiedDirectories()
    {
        using var environment = TestEnvironment.Create();

        using var file = ResultFileSystem.Instance.CreatePrivate(environment.Root, TestEnvironment.SpillComponents, "r.json");

        Assert.Equal(Path.Combine(environment.SpillDirectory, "r.json"), file.Path);
        Assert.True(File.Exists(file.Path));
        Assert.False(new DirectoryInfo(environment.SpillDirectory).Attributes.HasFlag(FileAttributes.ReparsePoint));
    }

    [Fact]
    public async Task ReleasingAReservationNeverDeletesAFileThatReplacedIt()
    {
        using var environment = TestEnvironment.Create(new ReplacingFileSystem());
        await using var server = await FakeBriosaServer.StartAsync();
        server.ServerInfo = FakeBriosaServer.DefaultServerInfo(revision: 1);
        var output = Path.Combine(environment.Root, "result.json");

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--output", output), environment.Environment);

        // The call was refused, the reservation was released, and the file that
        // appeared at the same path after release is still there.
        Assert.Equal(5, run.ExitCode);
        Assert.True(File.Exists(output));
    }

    [Fact]
    public void AReservedFileCannotBeReplacedWhileItIsHeld()
    {
        using var environment = TestEnvironment.Create();
        var path = Path.Combine(environment.Root, "held.json");

        using (var file = ResultFileSystem.Instance.CreateNew(path))
        {
            Assert.ThrowsAny<IOException>(() => File.Move(path, path + ".moved"));
            Assert.ThrowsAny<IOException>(() => File.Delete(path));
            file.DiscardIfEmpty();
        }

        Assert.False(File.Exists(path));
    }

    private static void CreateJunction(string link, string target)
    {
        using var process = Process.Start(new ProcessStartInfo("cmd.exe")
        {
            ArgumentList = { "/c", "mklink", "/J", link, target },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        })!;
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        Assert.True(new DirectoryInfo(link).Attributes.HasFlag(FileAttributes.ReparsePoint));
    }
}
