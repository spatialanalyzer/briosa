using System.Security.AccessControl;
using System.Security.Principal;
using Briosa.Cli.Engine;

namespace Briosa.Cli.Tests;

/// <summary>Findings of the fourth independent review: the spill directory, on real disks.</summary>
public sealed class ReviewRoundFourTests
{
    // 1: a spill changes the owner and access list of nothing already in results: not
    // an ordinary file or folder that inherited its permissions, and not an earlier
    // spill file, which has its own protected list. Only results and the new file end
    // with the intended lists.
    [Fact]
    public void ASpillChangesNothingAlreadyInTheResultsDirectory()
    {
        using var environment = TestEnvironment.Create();
        Directory.CreateDirectory(environment.SpillDirectory);
        var ordinary = Path.Combine(environment.SpillDirectory, "ordinary.txt");
        File.WriteAllText(ordinary, "kept");
        var folder = Path.Combine(environment.SpillDirectory, "folder");
        Directory.CreateDirectory(folder);
        var ordinaryBefore = Security(ordinary);
        var folderBefore = Security(folder);
        Assert.Contains(new FileInfo(ordinary).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>(), rule => rule.IsInherited);

        Spill(environment, "first.json");
        var first = Path.Combine(environment.SpillDirectory, "first.json");
        var firstAfter = Security(first);
        Spill(environment, "second.json");

        Assert.Equal(ordinaryBefore, Security(ordinary));
        Assert.Equal(folderBefore, Security(folder));
        Assert.Equal(firstAfter, Security(first));
        SpillDirectoryTests.AssertPrivate(new FileInfo(first).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner),
            InheritanceFlags.None);
        SpillDirectoryTests.AssertPrivate(new DirectoryInfo(environment.SpillDirectory).GetAccessControl(
            AccessControlSections.Access | AccessControlSections.Owner), InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit);
        SpillDirectoryTests.AssertPrivate(new FileInfo(Path.Combine(environment.SpillDirectory, "second.json")).GetAccessControl(
            AccessControlSections.Access | AccessControlSections.Owner), InheritanceFlags.None);
    }

    // 2: passing through a directory that already exists needs no right to create in
    // it, so an intermediate directory that denies new subdirectories is no obstacle
    // when nothing below it is missing.
    [Fact]
    public void AnExistingAncestorThatDeniesNewSubdirectoriesIsPassedThrough()
    {
        using var environment = TestEnvironment.Create();
        Directory.CreateDirectory(environment.SpillDirectory);
        var ancestor = Path.Combine([environment.Root, .. TestEnvironment.SpillComponents.SkipLast(1)]);
        using (DenyNewSubdirectories(ancestor))
        {
            Spill(environment, "r.json");
        }

        Assert.True(File.Exists(Path.Combine(environment.SpillDirectory, "r.json")));
    }

    // 2: in a fresh tree every level is created, relative to its parent.
    [Fact]
    public void AFreshTreeIsCreatedLevelByLevel()
    {
        using var environment = TestEnvironment.Create();
        Assert.False(Directory.Exists(Path.Combine(environment.Root, TestEnvironment.SpillComponents[0])));

        Spill(environment, "r.json");

        Assert.True(File.Exists(Path.Combine(environment.SpillDirectory, "r.json")));
        Assert.True(new DirectoryInfo(environment.SpillDirectory).GetAccessControl().AreAccessRulesProtected);
    }

    // 2: where a directory must be created, the file system decides from the parent's
    // own permissions; a parent that denies it stops the spill, and nothing is created.
    [Fact]
    public void AMissingDirectoryUnderAnAncestorThatDeniesItIsRefused()
    {
        using var environment = TestEnvironment.Create();
        var ancestor = Path.Combine([environment.Root, .. TestEnvironment.SpillComponents.SkipLast(1)]);
        Directory.CreateDirectory(ancestor);
        using (DenyNewSubdirectories(ancestor))
        {
            Assert.ThrowsAny<IOException>(() => Spill(environment, "r.json"));
        }

        Assert.False(Directory.Exists(environment.SpillDirectory));
    }

    // 3: a file the CLI created is removed through its own handle when anything fails
    // before it is handed over, here the lookup of its final path.
    [Fact]
    public void AFailedFinalPathLookupLeavesNoFile()
    {
        using var environment = TestEnvironment.Create();
        var lookups = 0;
        var files = new ResultFileSystem(finalPath: handle => ++lookups == 2
            ? throw new IOException("The file's path could not be read.")
            : NativeMethods.FinalPath(handle));

        Assert.ThrowsAny<IOException>(() => files.CreatePrivate(environment.Root, TestEnvironment.SpillComponents, "r.json"));

        Assert.Equal(2, lookups);
        Assert.Empty(Directory.EnumerateFileSystemEntries(environment.SpillDirectory));
    }

    // 3: a directory handle whose check fails is closed at once, so the directory is
    // not held, and can be renamed, after the spill fails.
    [Fact]
    public void AFailedAttributeQueryReleasesTheDirectory()
    {
        using var environment = TestEnvironment.Create();
        Directory.CreateDirectory(environment.SpillDirectory);
        var queries = 0;
        var files = new ResultFileSystem(attributes: handle => ++queries == 3
            ? throw new IOException("The attributes could not be read.")
            : File.GetAttributes(handle));

        Assert.ThrowsAny<IOException>(() => files.CreatePrivate(environment.Root, TestEnvironment.SpillComponents, "r.json"));

        var third = Path.Combine([environment.Root, .. TestEnvironment.SpillComponents.Take(2)]);
        Directory.Move(third, third + "-moved");
        Assert.True(Directory.Exists(third + "-moved"));
    }

    // 3: a spill whose write fails leaves no partial file that could pass for a result.
    [Fact]
    public async Task AFailedSpillWriteLeavesNoPartialFile()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new FailingWriteFileSystem(_ => true));
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Equal(12, run.ExitCode);
        Assert.Equal("spill-write-failed", run.DiagnosticCode);
        Assert.Empty(environment.SpilledFiles());
    }

    private static void Spill(TestEnvironment environment, string name)
    {
        using var file = ResultFileSystem.Instance.CreatePrivate(environment.Root, TestEnvironment.SpillComponents, name);
        file.Stream.Write("{}"u8);
    }

    // The owner and access list, byte for byte.
    private static byte[] Security(string path) =>
        (Directory.Exists(path)
            ? (FileSystemSecurity)new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner)
            : new FileInfo(path).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner))
        .GetSecurityDescriptorBinaryForm();

    // Denies the current user the right to create subdirectories in one directory,
    // inherited by nothing, until disposed.
    private static Restore DenyNewSubdirectories(string directory)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var rule = new FileSystemAccessRule(identity.User!, FileSystemRights.CreateDirectories, InheritanceFlags.None,
            PropagationFlags.None, AccessControlType.Deny);
        var info = new DirectoryInfo(directory);
        var security = info.GetAccessControl();
        security.AddAccessRule(rule);
        info.SetAccessControl(security);
        return new Restore(() =>
        {
            var current = info.GetAccessControl();
            current.RemoveAccessRule(rule);
            info.SetAccessControl(current);
        });
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
