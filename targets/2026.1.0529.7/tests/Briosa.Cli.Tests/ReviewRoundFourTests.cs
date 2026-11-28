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
    // with the intended lists. The test's own tree gets a known, protected access list
    // first, so that the result does not depend on the temporary folder or the account:
    // a BUILTIN\Users entry that any propagation from results would visibly remove, and
    // a CREATOR OWNER entry, which resolves to the user under an ordinary token and to
    // the Administrators group under an elevated one.
    [Fact]
    public void ASpillChangesNothingAlreadyInTheResultsDirectory()
    {
        using var environment = TestEnvironment.Create();
        SeedTestTree(environment.Root);
        Directory.CreateDirectory(environment.SpillDirectory);
        var ordinary = Path.Combine(environment.SpillDirectory, "ordinary.txt");
        File.WriteAllText(ordinary, "kept");
        var folder = Path.Combine(environment.SpillDirectory, "folder");
        Directory.CreateDirectory(folder);
        var first = Path.Combine(environment.SpillDirectory, "first.json");
        var second = Path.Combine(environment.SpillDirectory, "second.json");
        var stages = new List<string>();
        void Record(string stage)
        {
            foreach (var (label, path) in new[] { ("root", environment.Root), ("results", environment.SpillDirectory),
                         ("ordinary.txt", ordinary), ("folder", folder), ("first.json", first), ("second.json", second) })
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    stages.Add($"{stage} {label}: {Sddl(path)}");
                }
            }
        }

        Record("before");
        var ordinaryBefore = Security(ordinary);
        var folderBefore = Security(folder);
        Assert.Contains(Rules(ordinary), rule => rule.IsInherited && rule.IdentityReference.Value == BuiltinUsers.Value);
        Assert.Contains(Rules(folder), rule => rule.IsInherited && rule.IdentityReference.Value == BuiltinUsers.Value);

        Spill(environment, "first.json");
        Record("after the first spill");
        var firstAfter = Security(first);
        Spill(environment, "second.json");
        Record("after the second spill");

        var report = string.Join(Environment.NewLine, stages);
        Assert.True(ordinaryBefore.SequenceEqual(Security(ordinary)), "ordinary.txt changed." + Environment.NewLine + report);
        Assert.True(folderBefore.SequenceEqual(Security(folder)), "folder changed." + Environment.NewLine + report);
        Assert.True(firstAfter.SequenceEqual(Security(first)), "first.json changed." + Environment.NewLine + report);
        SpillDirectoryTests.AssertPrivate(new FileInfo(first).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner),
            InheritanceFlags.None);
        SpillDirectoryTests.AssertPrivate(new DirectoryInfo(environment.SpillDirectory).GetAccessControl(
            AccessControlSections.Access | AccessControlSections.Owner), InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit);
        SpillDirectoryTests.AssertPrivate(new FileInfo(second).GetAccessControl(
            AccessControlSections.Access | AccessControlSections.Owner), InheritanceFlags.None);
    }

    private static readonly SecurityIdentifier BuiltinUsers = new(WellKnownSidType.BuiltinUsersSid, null);

    // A protected access list for the test's root that every account produces alike:
    // the current user and SYSTEM in full, BUILTIN\Users to read, and CREATOR OWNER in
    // full on what is created below, all inherited by files and folders.
    private static void SeedTestTree(string root)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var both = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        security.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.FullControl, both,
            PropagationFlags.None, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl, both, PropagationFlags.None, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(BuiltinUsers, FileSystemRights.ReadAndExecute, both,
            PropagationFlags.None, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null),
            FileSystemRights.FullControl, both, PropagationFlags.InheritOnly, AccessControlType.Allow));
        new DirectoryInfo(root).SetAccessControl(security);
    }

    private static FileSystemAccessRule[] Rules(string path) =>
        [.. (Directory.Exists(path)
                ? (FileSystemSecurity)new DirectoryInfo(path).GetAccessControl()
                : new FileInfo(path).GetAccessControl())
            .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>()];

    private static string Sddl(string path) =>
        (Directory.Exists(path)
            ? (FileSystemSecurity)new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner)
            : new FileInfo(path).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner))
        .GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner);

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

    // Round 5, items 1 and 2: a spill file is removed only when it is known not to hold
    // the whole result; a complete copy is kept and reported, whatever fails after it.
    [Theory]
    [InlineData(WriteFault.BeforeAnyByte, "lost", "spill-write-failed", false)]
    [InlineData(WriteFault.AfterPrefix, "lost", "spill-write-failed", false)]
    [InlineData(WriteFault.AtFlush, "file_unconfirmed", "spill-finalize-failed", true)]
    [InlineData(WriteFault.AtClose, "file_unconfirmed", "spill-finalize-failed", true)]
    [InlineData(WriteFault.ReadBack, "file_unverified", "spill-verify-failed", true)]
    public async Task AFailedSpillKeepsAFileOnlyWhenItMayHoldTheResult(WriteFault fault, string delivery, string error, bool kept)
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new FailingWriteFileSystem(_ => true, fault));
        var result = CallOutcomeTests.Result(MpExecutionState.Succeeded, 2);
        server.Reply = _ => FakeReply.Ok(result);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        // Exit 12 asserts only that the operation completed and the delivery failed.
        Assert.Equal(12, run.ExitCode);
        Assert.Equal(delivery, run.Data["result_delivery"]!.GetValue<string>());
        Assert.Equal(error, run.Data["result_delivery_error"]!.GetValue<string>());
        if (kept)
        {
            var path = run.Data["result_file"]!["path"]!.GetValue<string>();
            Assert.Equal(environment.SpilledFiles().Single(), path, StringComparer.OrdinalIgnoreCase);
            CallOutcomeTests.AssertResultFile(result, await File.ReadAllBytesAsync(path));
        }
        else
        {
            Assert.Null(run.Data["result_file"]);
            Assert.Empty(environment.SpilledFiles());
        }
    }

    // Round 5, item 3: an incomplete spill file that cannot be removed is named, so no
    // one takes it for a result.
    [Fact]
    public async Task AnIncompleteSpillFileThatCannotBeRemovedIsReported()
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var environment = TestEnvironment.Create(new FailingWriteFileSystem(_ => true, WriteFault.AfterPrefixUnremovable));
        server.Reply = _ => FakeReply.Ok(CallOutcomeTests.Result(MpExecutionState.Succeeded, 2));

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        var left = environment.SpilledFiles().Single();
        try
        {
            Assert.Equal(12, run.ExitCode);
            Assert.Equal("lost", run.Data["result_delivery"]!.GetValue<string>());
            Assert.Equal("spill-write-failed", run.Data["result_delivery_error"]!.GetValue<string>());
            var file = run.Data["result_file"]!;
            Assert.Equal(left, file["path"]!.GetValue<string>(), StringComparer.OrdinalIgnoreCase);
            Assert.Equal("incomplete_not_removed", file["reason"]!.GetValue<string>());
            Assert.Equal(3, file["bytes"]!.GetValue<long>());
            Assert.Null(file["sha256"]);
            Assert.Contains("incomplete-file-left", CallOutcomeTests.Warnings(run));
        }
        finally
        {
            File.SetAttributes(left, FileAttributes.Normal);
        }
    }

    // Round 5, item 1: the --output file the caller named keeps a complete copy, and is
    // reported as such, when only its flush or close fails. Round 6: an incomplete one is
    // removed through its handle, as a spill file is, and the result goes to the spill
    // directory.
    [Theory]
    [InlineData(WriteFault.AtFlush, "file_unconfirmed", "output-finalize-failed", false)]
    [InlineData(WriteFault.AtClose, "file_unconfirmed", "output-finalize-failed", false)]
    [InlineData(WriteFault.AfterPrefix, "file", "output-write-failed", true)]
    public async Task AFailedOutputWriteIsReportedForWhatIsOnDisk(WriteFault fault, string delivery, string error, bool spilled)
    {
        await using var server = await FakeBriosaServer.StartAsync();
        using var probe = TestEnvironment.Create();
        var output = Path.Combine(probe.Root, "result.json");
        using var environment = TestEnvironment.Create(new FailingWriteFileSystem(path =>
            string.Equals(path, output, StringComparison.OrdinalIgnoreCase), fault));
        var result = CallOutcomeTests.Result(MpExecutionState.Succeeded, 2);
        server.Reply = _ => FakeReply.Ok(result);

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--output", output), environment.Environment);

        Assert.Equal(12, run.ExitCode);
        Assert.Equal(delivery, run.Data["result_delivery"]!.GetValue<string>());
        Assert.Equal(error, run.Data["result_delivery_error"]!.GetValue<string>());
        var path = run.Data["result_file"]!["path"]!.GetValue<string>();
        CallOutcomeTests.AssertResultFile(result, await File.ReadAllBytesAsync(path));
        Assert.Equal(spilled ? 1 : 0, environment.SpilledFiles().Length);
        if (!spilled)
        {
            Assert.Equal(output, path, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            Assert.False(File.Exists(output));
        }
    }

    private static void Spill(TestEnvironment environment, string name)
    {
        using var file = ResultFileSystem.Instance.CreatePrivate(environment.Root, TestEnvironment.SpillComponents, name);
        file.Write("{}"u8);
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
