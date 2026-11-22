[CmdletBinding()]
param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version = "0.2.0-ci",

    [string]$OutputDirectory = "artifacts\client-conformance-smoke"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetRoot = Split-Path -Parent $PSScriptRoot
$packageScript = Join-Path $PSScriptRoot "New-ClientConformancePackage.ps1"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-client-conformance-test-$([Guid]::NewGuid().ToString('N'))"
$firstOutput = [IO.Path]::GetFullPath($OutputDirectory, $targetRoot)
$secondOutput = Join-Path $temporaryRoot "second"
$extractRoot = Join-Path $temporaryRoot "extracted"

function Assert-Condition {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$safeTargetRoot = $targetRoot.Replace('\', '/')
$sourceRevision = (& git -c "safe.directory=$safeTargetRoot" -C $targetRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Could not determine a complete source revision."
}

$artifactBase = "briosa-client-conformance-$Version-sa-2026.1.0529.7-win-x64"
$zipName = "$artifactBase.zip"
$firstZip = Join-Path $firstOutput $zipName
$secondZip = Join-Path $secondOutput $zipName

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    & $packageScript `
        -Version $Version `
        -SourceRevision $sourceRevision `
        -OutputDirectory $firstOutput
    & $packageScript `
        -Version $Version `
        -SourceRevision $sourceRevision `
        -OutputDirectory $secondOutput `
        -NoRestore

    $firstHash = (Get-FileHash -LiteralPath $firstZip -Algorithm SHA256).Hash
    $secondHash = (Get-FileHash -LiteralPath $secondZip -Algorithm SHA256).Hash
    Assert-Condition ($firstHash -eq $secondHash) `
        "Two conformance package builds produced different SHA-256 hashes."
    $externalChecksum = (Get-Content -LiteralPath "$firstZip.sha256" -Raw).Trim()
    Assert-Condition ($externalChecksum -eq "$firstHash  $zipName") `
        "The external conformance package checksum is incorrect."

    Expand-Archive -LiteralPath $firstZip -DestinationPath $extractRoot
    $packageRoot = Join-Path $extractRoot $artifactBase
    Assert-Condition (Test-Path -LiteralPath $packageRoot -PathType Container) `
        "The conformance archive does not contain the expected root."

    $manifestPath = Join-Path $packageRoot "manifest.json"
    $contractPath = Join-Path $packageRoot "contract\scenarios.json"
    $schemaPath = Join-Path $packageRoot "contract\scenarios.schema.json"
    $runnerPath = Join-Path $packageRoot "runner\Invoke-BriosaClientConformance.ps1"
    foreach ($requiredPath in @(
        $manifestPath,
        $contractPath,
        $schemaPath,
        $runnerPath,
        (Join-Path $packageRoot "server\Briosa.Server.exe"),
        (Join-Path $packageRoot "fake-worker\Briosa.SmokeWorker.exe"))) {
        Assert-Condition (Test-Path -LiteralPath $requiredPath -PathType Leaf) `
            "The conformance package is missing '$requiredPath'."
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    Assert-Condition ($manifest.schemaVersion -eq 1) `
        "The conformance manifest schema version is incorrect."
    Assert-Condition ($manifest.artifactKind -eq "briosa_client_conformance") `
        "The conformance manifest artifact kind is incorrect."
    Assert-Condition ($manifest.sourceRevision -eq $sourceRevision.ToLowerInvariant()) `
        "The conformance manifest source revision is incorrect."
    Assert-Condition ($manifest.spatialAnalyzerTarget -eq "2026.1.0529.7") `
        "The conformance manifest target is incorrect."
    Assert-Condition ($manifest.scenarioContract -eq "briosa.first-party-client.v1") `
        "The conformance manifest contract identity is incorrect."
    Assert-Condition (-not $manifest.spatialAnalyzerBundled -and
        -not $manifest.spatialAnalyzerLicenseRequired -and
        -not $manifest.proprietaryDataBundled) `
        "The portable conformance package must not require or bundle vendor assets."

    $contractText = Get-Content -LiteralPath $contractPath -Raw
    Assert-Condition ($contractText | Test-Json -SchemaFile $schemaPath) `
        "The conformance scenario contract does not satisfy its schema."
    $contract = $contractText | ConvertFrom-Json
    $scenarioIds = @($contract.scenarios.id)
    Assert-Condition ($scenarioIds.Count -eq @($scenarioIds | Select-Object -Unique).Count) `
        "Conformance scenario identifiers must be unique."
    foreach ($requiredScenario in @(
        "control-plane-only",
        "default-ready",
        "attach-existing",
        "identity-mismatch",
        "capability-denied",
        "mp-failure",
        "output-failure",
        "deadline",
        "cancellation",
        "watchdog-recovery",
        "sdk-loss-recovery",
        "owned-cleanup")) {
        Assert-Condition ($requiredScenario -in $scenarioIds) `
            "The conformance contract is missing scenario '$requiredScenario'."
    }

    # A scripted hang never returns, so a watchdog budget only has to exceed a
    # cold worker's first execution. 250 ms expired on a legitimate first call
    # on a slow runner (#302); keep generous headroom.
    foreach ($watchdogScenario in @($contract.scenarios | Where-Object { $null -ne $_.watchdog_timeout })) {
        $watchdogBudget = [TimeSpan]::ParseExact(
            [string]$watchdogScenario.watchdog_timeout,
            "hh\:mm\:ss\.fff",
            [Globalization.CultureInfo]::InvariantCulture)
        Assert-Condition ($watchdogBudget -ge [TimeSpan]::FromSeconds(2)) `
            "Scenario '$($watchdogScenario.id)' has a watchdog budget too short for a cold worker's first execution."
    }

    # Contract-gated scenarios ship exactly when the target contract reaches
    # their major, without the gate field.
    $contractMajor = [int](Get-Content -LiteralPath (Join-Path $targetRoot "compatibility.json") -Raw |
        ConvertFrom-Json).major
    $sourceContractText = Get-Content -LiteralPath (Join-Path $targetRoot "conformance\client\v1\scenarios.json") -Raw
    Assert-Condition ($sourceContractText | Test-Json -SchemaFile (Join-Path $targetRoot "conformance\client\v1\scenarios.schema.json")) `
        "The source conformance scenario contract does not satisfy its schema."
    $sourceContract = $sourceContractText | ConvertFrom-Json
    Assert-Condition (@($contract.scenarios | Where-Object {
                $_.PSObject.Properties.Name -contains "minimum_contract_major" }).Count -eq 0) `
        "The packaged conformance contract must not contain contract gates."
    foreach ($sourceScenario in @($sourceContract.scenarios)) {
        $gated = $sourceScenario.PSObject.Properties.Name -contains "minimum_contract_major"
        $active = -not $gated -or [int]$sourceScenario.minimum_contract_major -le $contractMajor
        Assert-Condition ($active -eq ($sourceScenario.id -in $scenarioIds)) `
            "Scenario '$($sourceScenario.id)' is packaged contrary to its contract gate."
    }
    # The scenarios that contract major 3 activates are gated on it and ship with it.
    foreach ($majorThreeScenario in @("sdk-call-faulted", "mp-partial-success", "lifecycle-cancellation")) {
        Assert-Condition (@($sourceContract.scenarios | Where-Object {
                    $_.id -eq $majorThreeScenario -and [int]$_.minimum_contract_major -eq 3 }).Count -eq 1) `
            "The $majorThreeScenario scenario must be gated on contract major 3."
        if ($contractMajor -ge 3) {
            Assert-Condition ($majorThreeScenario -in $scenarioIds) `
                "The conformance contract is missing scenario '$majorThreeScenario'."
        }
    }

    $tokens = $null
    $parseErrors = $null
    [Management.Automation.Language.Parser]::ParseFile(
        $runnerPath,
        [ref]$tokens,
        [ref]$parseErrors) | Out-Null
    Assert-Condition ($parseErrors.Count -eq 0) `
        "The packaged conformance runner has PowerShell syntax errors."

    $checksumPath = Join-Path $packageRoot "files.sha256"
    foreach ($line in Get-Content -LiteralPath $checksumPath) {
        $match = [regex]::Match($line, '^([0-9A-Fa-f]{64})  (.+)$')
        Assert-Condition $match.Success "Malformed entry in files.sha256."
        $filePath = Join-Path $packageRoot $match.Groups[2].Value
        Assert-Condition (Test-Path -LiteralPath $filePath -PathType Leaf) `
            "A file listed in files.sha256 is missing."
        $actualHash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
        Assert-Condition ($actualHash -eq $match.Groups[1].Value) `
            "An internal conformance package checksum does not match."
    }

    $fixturePath = Join-Path $temporaryRoot "fixture.ps1"
    $fixtureSource = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Scenario,
    [Parameter(Mandatory)][string]$Contract
)
$definition = Get-Content -LiteralPath $Contract -Raw | ConvertFrom-Json
[ordered]@{
    schema_version = 1
    contract_id = $definition.contract_id
    scenario = $Scenario
    success = $true
} | ConvertTo-Json -Compress
'@
    [IO.File]::WriteAllText(
        $fixturePath,
        $fixtureSource,
        [Text.UTF8Encoding]::new($false))
    $runnerArguments = @{
        FixtureCommand = (Get-Process -Id $PID).Path
        FixtureArguments = @("-NoProfile", "-File", $fixturePath)
        FixtureTimeoutSeconds = 30
    }
    $passedOutput = [Collections.Generic.List[string]]::new()
    & $runnerPath @runnerArguments *>&1 | ForEach-Object {
        $passedOutput.Add([string]$_)
        Write-Host $_
    }
    # Each passed scenario reports its phase durations; this fixture starts no
    # server, so only the fixture's own time is known (#371).
    $passedLines = @($passedOutput | Where-Object { $_ -like "Passed client conformance scenario: *" })
    Assert-Condition ($passedLines.Count -eq $scenarioIds.Count -and
        @($passedLines | Where-Object { $_ -notmatch '^Passed client conformance scenario: [a-z0-9-]+ \(fixture [0-9]+\.[0-9] s\)$' }).Count -eq 0) `
        "The conformance runner did not report each passed scenario with its fixture time."

    # Every rejected scenario prints the fixture's complete stderr and, when a
    # diagnostics directory is configured, keeps it with the scenario's server
    # logs (#302). Rejection by exit code and by report validation must behave
    # alike. The gate supplies the directory through the environment.
    $failingFixtureSource = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Scenario,
    [Parameter(Mandatory)][string]$Contract
)
$logDirectory = $env:Briosa__Logging__File__Directory
if ([string]::IsNullOrWhiteSpace($logDirectory) -or
    -not $logDirectory.EndsWith("\$Scenario\server-logs", [StringComparison]::OrdinalIgnoreCase)) {
    [Console]::Error.WriteLine("server-log-directory-not-scenario-scoped")
    exit 3
}
if ($env:Briosa__Worker__StartupTimeout -ne "__STARTUP__") {
    [Console]::Error.WriteLine("worker-startup-timeout-not-applied")
    exit 4
}
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
[IO.File]::WriteAllText((Join-Path $logDirectory "server-log-marker.jsonl"), "server-log-marker")
# A server log in the real record shape: three early warnings, more lower-severity
# events than the digest keeps, one property outside the digest allow-list, and the
# truncated last line of a killed writer.
$started = [DateTimeOffset]::UtcNow
$records = [Collections.Generic.List[string]]::new()
function Add-ServerRecord([int]$Offset, [string]$Level, [string]$Name, [hashtable]$Properties) {
    $Properties["ServerInstanceId"] = "0123456789abcdef0123456789abcdef"
    $Properties["EventId"] = @{ Id = 1; Name = $Name }
    $records.Add((@{
        Timestamp = $started.AddMilliseconds($Offset).ToString("o")
        Level = $Level
        MessageTemplate = $Name
        Properties = $Properties
    } | ConvertTo-Json -Compress -Depth 5))
}
foreach ($index in 1..3) {
    Add-ServerRecord $index "Warning" "WorkerStartPhase" @{
        Phase = "startup-bound-expired"; ElapsedMilliseconds = 10000; Generation = $index }
}
foreach ($index in 1..250) {
    Add-ServerRecord (10 + $index) "Information" "ReadinessNotReady" @{}
}
Add-ServerRecord 300 "Information" "LifecycleRpcStarted" @{
    Rpc = "LaunchSpatialAnalyzer"; Path = "secret-geometry-path" }
$records.Add('{"Timestamp":"2026-11-22T12:00:00')
[IO.File]::WriteAllLines(
    (Join-Path $logDirectory "briosa-0123456789abcdef0123456789abcdef-20261122.jsonl"), $records)
foreach ($index in 1..200) {
    [Console]::Error.WriteLine("__MODE__-trace-line-$index")
}
switch ("__MODE__") {
    "unsuccessful-report" {
        $definition = Get-Content -LiteralPath $Contract -Raw | ConvertFrom-Json
        [Console]::Out.WriteLine(([ordered]@{
            schema_version = 1
            contract_id = $definition.contract_id
            scenario = $Scenario
            success = $false
        } | ConvertTo-Json -Compress))
        exit 0
    }
    "empty-report" { exit 0 }
    "invalid-report" {
        [Console]::Out.WriteLine("{not-json")
        exit 0
    }
    "timeout" {
        Start-Sleep -Seconds 120
        exit 0
    }
    default { exit 1 }
}
'@

    $rejectionCases = @(
        # A caller's own worker startup bound reaches the server unchanged.
        [pscustomobject]@{ Mode = "exit-nonzero"; Diagnostics = $true; StartupTimeout = "00:00:12"
            Failure = "*failed scenario 'default-ready' with exit code 1*" },
        [pscustomobject]@{ Mode = "unsuccessful-report"; Diagnostics = $true
            Failure = "*report did not satisfy scenario 'default-ready'*" },
        [pscustomobject]@{ Mode = "empty-report"; Diagnostics = $true
            Failure = "*returned no report for scenario 'default-ready'*" },
        [pscustomobject]@{ Mode = "invalid-report"; Diagnostics = $true
            Failure = "*returned an invalid JSON report for scenario 'default-ready'*" },
        [pscustomobject]@{ Mode = "timeout"; Diagnostics = $true; Dump = $true
            Failure = "*timed out after 5 s in scenario 'default-ready'*" },
        # Without a diagnostics directory the runner deletes its temporary
        # files, so the printed stderr is the only remaining evidence.
        [pscustomobject]@{ Mode = "unsuccessful-report"; Diagnostics = $false
            Failure = "*report did not satisfy scenario 'default-ready'*" })
    $previousDiagnosticsDirectory = $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY
    $previousStartupTimeout = $env:Briosa__Worker__StartupTimeout
    $previousDumpOnTimeout = $env:BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT
    try {
        foreach ($case in $rejectionCases) {
            $caseName = "$($case.Mode)-$(if ($case.Diagnostics) { 'saved' } else { 'printed' })"
            $callerStartupTimeout = $(if ($case.PSObject.Properties.Name -contains "StartupTimeout") { $case.StartupTimeout })
            $dump = $case.PSObject.Properties.Name -contains "Dump" -and $case.Dump
            $failingFixturePath = Join-Path $temporaryRoot "failing-fixture-$caseName.ps1"
            # The runner gives the fake worker start 30 s unless the caller set a bound.
            [IO.File]::WriteAllText(
                $failingFixturePath,
                $failingFixtureSource.Replace("__MODE__", $case.Mode).Replace(
                    "__STARTUP__", $(if ($null -ne $callerStartupTimeout) { $callerStartupTimeout } else { "00:00:30" })),
                [Text.UTF8Encoding]::new($false))
            $diagnosticsRoot = Join-Path $temporaryRoot "conformance-diagnostics-$caseName"
            $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY = $(if ($case.Diagnostics) { $diagnosticsRoot } else { $null })
            $env:Briosa__Worker__StartupTimeout = $callerStartupTimeout
            $env:BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT = $(if ($dump) { "1" } else { $null })
            $failure = $null
            $runnerOutput = [Collections.Generic.List[string]]::new()
            try {
                $failingRunnerArguments = @{
                    FixtureCommand = (Get-Process -Id $PID).Path
                    FixtureArguments = @("-NoProfile", "-File", $failingFixturePath)
                    Scenario = @("default-ready")
                    FixtureTimeoutSeconds = $(if ($case.Mode -eq "timeout") { 5 } else { 30 })
                }
                & $runnerPath @failingRunnerArguments *>&1 |
                    ForEach-Object { $runnerOutput.Add([string]$_) }
            }
            catch {
                $failure = $_
            }
            Assert-Condition ($null -ne $failure -and "$failure" -like $case.Failure) `
                "The conformance runner did not reject the $caseName fixture as expected: $failure"
            $printedLines = @($runnerOutput | Where-Object { $_ -like "$($case.Mode)-trace-line-*" })
            Assert-Condition ($printedLines.Count -eq 200 -and
                $printedLines[0] -eq "$($case.Mode)-trace-line-1" -and
                $printedLines[-1] -eq "$($case.Mode)-trace-line-200") `
                "The conformance runner did not print the complete fixture stderr for $caseName."

            # The runner's timing marks and the server log digest follow the stderr
            # (#371). The digest keeps the latest 200 events and every warning, and
            # prints only allow-listed properties.
            $timingLine = @($runnerOutput | Where-Object { $_ -like "Runner timing for default-ready (UTC): *" })
            Assert-Condition ($timingLine.Count -eq 1 -and
                $timingLine[0] -like "*scenario start *, fixture start * (+*), fixture end * (+*)*" -and
                ($case.Mode -ne "timeout" -or $timingLine[0] -like "*, fixture timed out.")) `
                "The conformance runner did not print its timing marks for $caseName."
            $digestStart = [Array]::IndexOf([string[]]$runnerOutput,
                "--- server log digest for default-ready (times from fixture start) ---")
            $digestEnd = [Array]::IndexOf([string[]]$runnerOutput, "--- end server log digest for default-ready ---")
            Assert-Condition ($digestStart -ge 0 -and $digestEnd -gt $digestStart -and
                $digestStart -gt [Array]::IndexOf([string[]]$runnerOutput, "$($case.Mode)-trace-line-200")) `
                "The conformance runner did not print the server log digest after the fixture stderr for $caseName."
            $digest = @($runnerOutput[($digestStart + 1)..($digestEnd - 1)])
            $digestEvents = @($digest | Where-Object { $_ -match '^[+-][0-9]+\.[0-9]{3} s ' -and $_ -notmatch ' s Runner ' })
            $runnerMarks = @($digest | Where-Object { $_ -match ' s Runner ' })
            $offsets = @($digest | Where-Object { $_ -match '^[+-][0-9]+\.[0-9]{3} s ' } | ForEach-Object {
                    [double]::Parse($_.Substring(0, $_.IndexOf(' s ')), [Globalization.CultureInfo]::InvariantCulture)
                })
            Assert-Condition ($runnerMarks.Count -eq 2 -and
                $runnerMarks[0] -eq "+0.000 s Runner FixtureStart" -and
                $runnerMarks[1] -like "+* s Runner $(if ($case.Mode -eq 'timeout') { 'FixtureTimedOut' } else { 'FixtureEnd' })" -and
                @(for ($index = 1; $index -lt $offsets.Count; $index++) {
                        if ($offsets[$index] -lt $offsets[$index - 1]) { $index }
                    }).Count -eq 0) `
                "The conformance runner did not place its fixture marks in the server log digest's timeline for $caseName."
            Assert-Condition ($digestEvents.Count -eq 200 -and
                @($digestEvents | Where-Object {
                    $_ -like "* Warning WorkerStartPhase Phase=startup-bound-expired ElapsedMilliseconds=10000 Generation=*" }).Count -eq 3 -and
                $digestEvents[-1] -like "+* s Information LifecycleRpcStarted Rpc=LaunchSpatialAnalyzer" -and
                $digest -contains "(54 earlier lower-severity events omitted; warnings and errors are always kept)" -and
                $digest -contains "(2 unreadable server log lines skipped)") `
                "The conformance runner printed an unexpected server log digest for $caseName."
            Assert-Condition (@($runnerOutput | Where-Object { $_ -like "*secret-geometry-path*" }).Count -eq 0) `
                "The conformance runner printed a server log property outside the digest allow-list for $caseName."
            if ($case.Mode -eq "timeout") {
                Assert-Condition ($runnerOutput -contains
                    "--- processes alive at the fixture timeout in default-ready (start times from fixture start) ---" -and
                    @($runnerOutput | Where-Object { $_ -match '^fixture pid [0-9]+: started [+-][0-9]+\.[0-9]{3} s, CPU [0-9]+\.[0-9]{2} s$' }).Count -eq 1) `
                    "The conformance runner did not record the processes alive at the fixture timeout."
            }
            if (-not $case.Diagnostics) {
                Assert-Condition (-not (Test-Path -LiteralPath $diagnosticsRoot)) `
                    "The conformance runner wrote diagnostics without a configured directory."
                continue
            }
            $scenarioDiagnostics = Join-Path $diagnosticsRoot "default-ready"
            $savedError = @(Get-Content -LiteralPath (Join-Path $scenarioDiagnostics "fixture.stderr.txt"))
            Assert-Condition ($savedError.Count -eq 200 -and
                $savedError[0] -eq "$($case.Mode)-trace-line-1" -and
                $savedError[-1] -eq "$($case.Mode)-trace-line-200") `
                "The conformance runner did not keep the complete fixture stderr for $caseName."
            Assert-Condition ((Get-Content -LiteralPath (Join-Path $scenarioDiagnostics "server-logs\server-log-marker.jsonl") -Raw) -eq
                "server-log-marker") `
                "The conformance runner did not keep the scenario's server logs for $caseName."
            Assert-Condition ((Test-Path -LiteralPath (Join-Path $scenarioDiagnostics "failure.txt") -PathType Leaf) -and
                (Test-Path -LiteralPath (Join-Path $scenarioDiagnostics "fixture.stdout.txt") -PathType Leaf) -and
                -not (Test-Path -LiteralPath (Join-Path $scenarioDiagnostics "fake-application"))) `
                "The conformance runner diagnostics have an unexpected shape for $caseName."
            $savedDigest = @(Get-Content -LiteralPath (Join-Path $scenarioDiagnostics "server-log-digest.txt"))
            Assert-Condition ($savedDigest.Count -eq $digestEnd - $digestStart + 2 -and
                $savedDigest[0] -eq $timingLine[0] -and
                $savedDigest[1] -eq $runnerOutput[$digestStart]) `
                "The conformance runner did not keep the server log digest for $caseName."
            if ($dump) {
                # The opt-in heap dump of a .NET fixture, taken before the kill. This
                # fixture is PowerShell, which hosts the .NET runtime.
                $fixtureRuntime = @((Get-Process -Id $PID).Modules | Where-Object ModuleName -eq "coreclr.dll")
                $dumpAvailable = $fixtureRuntime.Count -eq 1 -and
                    (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $fixtureRuntime[0].FileName) "createdump.exe"))
                $dumpLines = @($runnerOutput | Where-Object { $_ -like "*heap dump of the fixture*" })
                $dumpFiles = @(Get-ChildItem -LiteralPath (Join-Path $scenarioDiagnostics "dumps") -Filter "fixture-*.dmp" -File -ErrorAction SilentlyContinue)
                Assert-Condition ($dumpLines.Count -eq 1 -and $(if ($dumpAvailable) {
                            $dumpLines[0] -like "Wrote a heap dump of the fixture (pid *) to dumps/fixture-*.dmp (* MiB)." -and
                            $dumpFiles.Count -eq 1 -and $dumpFiles[0].Length -gt 0
                        } else {
                            $dumpLines[0] -like "No heap dump of the fixture (pid *)*" -and $dumpFiles.Count -eq 0
                        })) `
                    "The conformance runner did not handle the opt-in fixture heap dump as expected: $($dumpLines -join ' | ')"
                $dumpFiles | Remove-Item -Force
            }
        }
    }
    finally {
        $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY = $previousDiagnosticsDirectory
        $env:Briosa__Worker__StartupTimeout = $previousStartupTimeout
        $env:BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT = $previousDumpOnTimeout
    }

    $applicationRoot = Join-Path $temporaryRoot "launchable-fake-application"
    [IO.Directory]::CreateDirectory($applicationRoot) | Out-Null
    Copy-Item -Path (Join-Path $packageRoot "fake-worker\*") `
        -Destination $applicationRoot -Recurse
    $applicationPath = Join-Path $applicationRoot "Spatial Analyzer64.exe"
    Move-Item -LiteralPath (Join-Path $applicationRoot "Briosa.SmokeWorker.exe") `
        -Destination $applicationPath
    $applicationProcess = $null
    try {
        $applicationProcess = Start-Process `
            -FilePath $applicationPath `
            -WorkingDirectory $applicationRoot `
            -PassThru
        $windowDeadline = [DateTimeOffset]::UtcNow.AddSeconds(5)
        do {
            Start-Sleep -Milliseconds 100
            Assert-Condition (-not $applicationProcess.HasExited) `
                "The packaged fake application did not survive a normal zero-argument launch."
            $applicationProcess.Refresh()
        } while (($applicationProcess.MainWindowHandle -eq [IntPtr]::Zero -or
            [string]::IsNullOrWhiteSpace($applicationProcess.MainWindowTitle)) -and
            [DateTimeOffset]::UtcNow -lt $windowDeadline)
        Assert-Condition ($applicationProcess.MainWindowHandle -ne [IntPtr]::Zero -and
            -not [string]::IsNullOrWhiteSpace($applicationProcess.MainWindowTitle)) `
            "The packaged fake application did not expose the real host's required ready window."
    }
    finally {
        if ($null -ne $applicationProcess -and -not $applicationProcess.HasExited) {
            Stop-Process -Id $applicationProcess.Id -Force
            $applicationProcess.WaitForExit()
        }
        if ($null -ne $applicationProcess) {
            $applicationProcess.Dispose()
        }
    }

    Write-Host "Client conformance package reproducibility, contract, checksums, and runner tests passed."
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
