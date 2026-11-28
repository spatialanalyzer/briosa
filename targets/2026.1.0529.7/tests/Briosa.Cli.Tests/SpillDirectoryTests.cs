using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
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

    // D: what is on disk afterwards admits the current user and SYSTEM only. The
    // directory is protected and passes its two entries on; the file has its own two,
    // inherits nothing, and belongs to the user.
    [Fact]
    public void TheSpillDirectoryAndFileAdmitOnlyTheUserAndSystem()
    {
        using var environment = TestEnvironment.Create();
        // A directory that already exists, with the permissions it inherited, is reused.
        Directory.CreateDirectory(environment.SpillDirectory);

        using (var created = ResultFileSystem.Instance.CreatePrivate(environment.Root, TestEnvironment.SpillComponents, "r.json"))
        {
            created.Stream.Write("{}"u8);
        }

        var directory = new DirectoryInfo(environment.SpillDirectory).GetAccessControl(
            AccessControlSections.Access | AccessControlSections.Owner);
        AssertPrivate(directory, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit);
        var file = new FileInfo(Path.Combine(environment.SpillDirectory, "r.json")).GetAccessControl(
            AccessControlSections.Access | AccessControlSections.Owner);
        AssertPrivate(file, InheritanceFlags.None);
    }

    // C (round 3): the base is the user's folder and may be redirected at any time.
    // Redirected after the base, after each managed directory, or after all of them
    // are held, the spill still lands in the tree the base led to when it was opened,
    // and nothing outside that tree changes: no permission, no entry.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ARedirectedBaseNeverChangesAnythingOutsideTheTreeItFirstLedTo(int heldWhenRedirected)
    {
        using var environment = TestEnvironment.Create();
        var verified = Path.Combine(environment.Root, "verified");
        var elsewhere = Path.Combine(environment.Root, "elsewhere");
        Directory.CreateDirectory(verified);
        Directory.CreateDirectory(Path.Combine([elsewhere, .. TestEnvironment.SpillComponents]));
        var link = Path.Combine(environment.Root, "base");
        CreateJunction(link, verified);
        var before = Snapshot(environment.Root);
        var files = new ResultFileSystem(afterHeld: held =>
        {
            if (held == heldWhenRedirected)
            {
                Directory.Delete(link);
                CreateJunction(link, elsewhere);
            }
        });

        string? created = null;
        IOException? refused = null;
        try
        {
            using var file = files.CreatePrivate(link, TestEnvironment.SpillComponents, "r.json");
            created = file.Path;
        }
        catch (IOException exception)
        {
            refused = exception;
        }

        // Every entry that existed before, other than the base link that the test
        // re-pointed, keeps its owner and access list, and nothing new appears
        // outside the verified tree.
        var after = Snapshot(environment.Root);
        var verifiedResults = Path.Combine([verified, .. TestEnvironment.SpillComponents]);
        foreach (var (path, security) in before.Where(entry => !string.Equals(entry.Key, link, StringComparison.OrdinalIgnoreCase)))
        {
            Assert.True(after.TryGetValue(path, out var now) && now == security, path);
        }

        Assert.All(after.Keys.Except(before.Keys, StringComparer.OrdinalIgnoreCase), path =>
            Assert.StartsWith(verified + Path.DirectorySeparatorChar, path, StringComparison.OrdinalIgnoreCase));
        Assert.Null(refused);
        Assert.Equal(Path.Combine(verifiedResults, "r.json"), created, StringComparer.OrdinalIgnoreCase);
        Assert.True(new DirectoryInfo(verifiedResults).GetAccessControl().AreAccessRulesProtected);
    }

    // The owner and access list of the root and of every entry below it, without
    // following junctions.
    private static Dictionary<string, string> Snapshot(string root)
    {
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Walk(string directory)
        {
            entries[directory] = new DirectoryInfo(directory)
                .GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner)
                .GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner);
            foreach (var child in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                if (child is DirectoryInfo { Attributes: var attributes } && (attributes & FileAttributes.ReparsePoint) == 0)
                {
                    Walk(child.FullName);
                }
                else
                {
                    entries[child.FullName] = child.Attributes.HasFlag(FileAttributes.ReparsePoint) ? "link"
                        : new FileInfo(child.FullName).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner)
                            .GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner);
                }
            }
        }

        Walk(root);
        return entries;
    }

    // D: on a volume that keeps no permissions, nothing is spilled or created, and the
    // envelope says the result is gone. Exit 12 only when completion was established.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AVolumeWithoutPermissionsIsNeverUsedForASpill(bool completionEstablished)
    {
        using var environment = TestEnvironment.Create(new ResultFileSystem(keepsPermissions: _ => false));
        await using var server = await FakeBriosaServer.StartAsync();
        server.Reply = _ => FakeReply.Ok(completionEstablished
            ? CallOutcomeTests.Result(MpExecutionState.Succeeded, 2)
            : new GetIthPointFromGroupResult { Execution = new MpExecutionDetails { State = MpExecutionState.Succeeded, MpResultCode = 2 } });

        var run = await CliRun.RunAsync(CallOutcomeTests.CallArguments(server, "--max-inline-bytes", "0"),
            environment.Environment);

        Assert.Equal(completionEstablished ? 12 : 10, run.ExitCode);
        Assert.Equal("lost", run.Data["result_delivery"]!.GetValue<string>());
        Assert.Equal(Codes.SpillPermissionsUnsupported, run.Data["result_delivery_error"]!.GetValue<string>());
        Assert.Null(run.Data["result_file"]);
        Assert.Null(run.Data["result"]);
        var warning = run.Envelope["warnings"]!.AsArray()
            .Single(entry => entry!["code"]!.GetValue<string>() == "spill-refused")!["message"]!.GetValue<string>();
        Assert.Contains("discarded", warning, StringComparison.Ordinal);
        Assert.Equal(completionEstablished, warning.StartsWith("The operation completed", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(environment.Root, TestEnvironment.SpillComponents[0])));
    }

    internal static void AssertPrivate(FileSystemSecurity security, InheritanceFlags inheritance)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        Assert.True(security.AreAccessRulesProtected);
        Assert.Equal(identity.User, security.GetOwner(typeof(SecurityIdentifier)));
        var rules = security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>().ToArray();
        Assert.Equal(2, rules.Length);
        Assert.All(rules, rule =>
        {
            Assert.False(rule.IsInherited);
            Assert.Equal(AccessControlType.Allow, rule.AccessControlType);
            Assert.Equal(FileSystemRights.FullControl, rule.FileSystemRights);
            Assert.Equal(inheritance, rule.InheritanceFlags);
            Assert.Equal(PropagationFlags.None, rule.PropagationFlags);
        });
        Assert.Equal(new IdentityReference[] { identity.User!, system }.Select(sid => sid.Value).Order(StringComparer.Ordinal),
            rules.Select(rule => rule.IdentityReference.Value).Order(StringComparer.Ordinal));
    }

    internal static void CreateJunction(string link, string target)
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
