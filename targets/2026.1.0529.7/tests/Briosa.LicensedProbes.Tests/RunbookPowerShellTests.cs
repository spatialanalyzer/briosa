using System.ComponentModel;
using System.Diagnostics;

namespace Briosa.LicensedProbes.Tests;

// The session 4 runbook's own PowerShell, read from the committed runbook and run in 64-bit
// PowerShell 7 (pwsh): the state-check wrappers against the real eng/Test-LicensedRunnerState.ps1,
// with its two process queries replaced by functions so that no real process is listed, and the
// license-signal command against a temporary folder. xUnit 2 cannot skip a test at run time, so
// where pwsh is not on PATH these tests fail with that message instead of passing; the Windows
// CI runners have it.
public sealed class RunbookPowerShellTests
{
    // Functions take precedence over cmdlets, so the real script's two queries reach these.
    private const string Driver = """
        param([string]$WrapperFile, [string]$Target, [string]$Canary, [string]$Scenarios)

        function global:Get-CimInstance {
            param([Parameter(Position = 0)][string]$ClassName)
            switch ($global:Scenario) {
                'error-then-report' { Write-Error -Message 'mocked query error' -ErrorAction Continue }
                'canary-then-throw' {
                    Write-Error -Message $global:CanaryText -ErrorAction Continue
                    throw $global:CanaryText
                }
            }
            [pscustomobject]@{ Name = 'Spatial Analyzer64.exe'; ExecutablePath = "C:\Program Files\SpatialAnalyzer $global:TargetText\x64\Spatial Analyzer64.exe" }
        }

        function global:Get-Process {
            [CmdletBinding()]
            param([string[]]$Name)
            if ($global:Scenario -eq 'reported-failure' -and $Name -contains 'SpatialAnalyzerSDK') {
                [pscustomobject]@{ Name = 'SpatialAnalyzerSDK' }
            }
        }

        $global:TargetText = $Target
        $global:CanaryText = $Canary
        $wrapper = [scriptblock]::Create((Get-Content -LiteralPath $WrapperFile -Raw))
        foreach ($scenario in $Scenarios.Split(',')) {
            $global:Scenario = $scenario
            "=== $scenario"
            & $wrapper
        }
        """;

    [Fact]
    public async Task TheStateCheckWrappersPrintOnlyACheckedReportAndFailOnAnyErrorRecord()
    {
        var root = TestSupport.TargetRoot();
        var section = Session4Section(root);
        var folder = Directory.CreateTempSubdirectory("briosa-runbook-");
        try
        {
            var driver = Save(folder, "driver.ps1", Driver);
            var preflight = Save(folder, "preflight.ps1", Block(section, "Preflight:\n"));
            var postflight = Save(folder, "postflight.ps1", Block(section, "Postflight:\n"));
            var canary = "{C:\\briosa-canary-" + Guid.NewGuid().ToString("N") + "\\private}";

            var pre = await PowerShellAsync(root, driver, "-WrapperFile", preflight, "-Target", ProbeTarget.SpatialAnalyzerTarget,
                "-Canary", canary, "-Scenarios", "clean,reported-failure,error-then-report,canary-then-throw");
            var post = await PowerShellAsync(root, driver, "-WrapperFile", postflight, "-Target", ProbeTarget.SpatialAnalyzerTarget,
                "-Canary", canary, "-Scenarios", "clean");

            Assert.Equal(
            [
                "=== clean", Report("preflight", true, 0, ""), "State check (Preflight): passed",
                "=== reported-failure", Report("preflight", false, 1, "\"RESIDUAL_SDK_PROCESS\""), "State check (Preflight): failed",

                // A non-terminating error before a valid report still fails the check.
                "=== error-then-report", Report("preflight", true, 0, ""), "State check (Preflight): failed",

                // A braced path in an error record, then a throw: the status line alone.
                "=== canary-then-throw", "State check (Preflight): failed"
            ], pre.Lines);
            Assert.Equal(["=== clean", Report("postflight", true, 0, ""), "State check (Postflight): passed"], post.Lines);
            Assert.DoesNotContain("briosa-canary", pre.Output + pre.Error + post.Output + post.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Equal((0, 0), (pre.ExitCode, post.ExitCode));
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task TheLicenseSignalCommandCreatesTheEmptyMarkerAndPrintsOnlyAFixedLine()
    {
        var root = TestSupport.TargetRoot();
        var command = Block(Session4Section(root), "with this command, `<output-directory>`");
        var folder = Directory.CreateTempSubdirectory("briosa-signal-");
        try
        {
            var output = Directory.CreateDirectory(Path.Combine(folder.FullName, "run", "checkpoints")).Parent!.FullName;
            var missing = Path.Combine(folder.FullName, "missing");
            var create = Save(folder, "create.ps1", command.Replace("<output-directory>", output, StringComparison.Ordinal));
            var refuse = Save(folder, "refuse.ps1", command.Replace("<output-directory>", missing, StringComparison.Ordinal));

            var created = await PowerShellAsync(root, create);
            var again = await PowerShellAsync(root, create);
            var refused = await PowerShellAsync(root, refuse);
            var marker = new FileInfo(Path.Combine(output, "checkpoints", "license-prompt-abort.signal"));

            Assert.Equal(["License signal: created."], created.Lines);
            Assert.Equal(["License signal: created."], again.Lines);
            Assert.Equal(["License signal: not confirmed."], refused.Lines);
            Assert.True(marker.Exists);
            Assert.Equal(0, marker.Length);
            Assert.False(Directory.Exists(missing));
            Assert.DoesNotContain("briosa-signal-", created.Output + created.Error + again.Output + again.Error + refused.Output + refused.Error,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private static string Report(string phase, bool success, int sdk, string codes) =>
        $"{{\"phase\":\"{phase}\",\"success\":{(success ? "true" : "false")},\"exact_sa_instance_count\":1," +
        $"\"briosa_process_count\":0,\"sdk_process_count\":{sdk},\"issue_codes\":[{codes}]}}";

    private static string Session4Section(string root)
    {
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "testing", "licensed-probe-session.md"));
        var start = runbook.IndexOf("## Licensed session 4 (#402)", StringComparison.Ordinal);
        var end = runbook.IndexOf("## Recording\n", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "The runbook has no Licensed session 4 section.");
        return runbook[start..end];
    }

    // The first PowerShell block after the given text.
    private static string Block(string section, string after)
    {
        var marker = section.IndexOf(after, StringComparison.Ordinal);
        Assert.True(marker >= 0, $"The runbook has no '{after}'.");
        const string Fence = "```powershell\n";
        var start = section.IndexOf(Fence, marker, StringComparison.Ordinal) + Fence.Length;
        return section[start..section.IndexOf("```", start, StringComparison.Ordinal)];
    }

    private static string Save(DirectoryInfo folder, string name, string content)
    {
        var path = Path.Combine(folder.FullName, name);
        File.WriteAllText(path, content);
        return path;
    }

    private sealed record PowerShellRun(string Output, string Error, int ExitCode)
    {
        public IReadOnlyList<string> Lines =>
            [.. Output.Split('\n').Select(static line => line.TrimEnd('\r')).Where(static line => line.Length > 0)];
    }

    // Runs one script in a pwsh process this test starts and waits for; on a hang, that process
    // alone is ended and the test fails.
    private static async Task<PowerShellRun> PowerShellAsync(string workingDirectory, string script, params string[] arguments)
    {
        var start = new ProcessStartInfo("pwsh")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-File", script }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(start) ?? throw new InvalidOperationException("pwsh did not start.");
        }
        catch (Win32Exception)
        {
            Assert.Fail("PowerShell 7 (pwsh) is not on PATH. These tests run the runbook's PowerShell and fail without it rather than pass.");
            throw;
        }

        using (process)
        {
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                throw;
            }

            return new PowerShellRun(await output, await error, process.ExitCode);
        }
    }
}
