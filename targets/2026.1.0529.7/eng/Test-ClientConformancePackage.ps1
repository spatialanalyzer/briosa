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
    foreach ($majorThreeScenario in @("sdk-call-faulted", "mp-partial-success")) {
        Assert-Condition (@($sourceContract.scenarios | Where-Object {
                    $_.id -eq $majorThreeScenario -and [int]$_.minimum_contract_major -eq 3 }).Count -eq 1) `
            "The $majorThreeScenario scenario must be gated on contract major 3."
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
    & $runnerPath @runnerArguments

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
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
[IO.File]::WriteAllText((Join-Path $logDirectory "server-log-marker.jsonl"), "server-log-marker")
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
        [pscustomobject]@{ Mode = "exit-nonzero"; Diagnostics = $true
            Failure = "*failed scenario 'default-ready' with exit code 1*" },
        [pscustomobject]@{ Mode = "unsuccessful-report"; Diagnostics = $true
            Failure = "*report did not satisfy scenario 'default-ready'*" },
        [pscustomobject]@{ Mode = "empty-report"; Diagnostics = $true
            Failure = "*returned no report for scenario 'default-ready'*" },
        [pscustomobject]@{ Mode = "invalid-report"; Diagnostics = $true
            Failure = "*returned an invalid JSON report for scenario 'default-ready'*" },
        [pscustomobject]@{ Mode = "timeout"; Diagnostics = $true
            Failure = "*timed out in scenario 'default-ready'*" },
        # Without a diagnostics directory the runner deletes its temporary
        # files, so the printed stderr is the only remaining evidence.
        [pscustomobject]@{ Mode = "unsuccessful-report"; Diagnostics = $false
            Failure = "*report did not satisfy scenario 'default-ready'*" })
    $previousDiagnosticsDirectory = $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY
    try {
        foreach ($case in $rejectionCases) {
            $caseName = "$($case.Mode)-$(if ($case.Diagnostics) { 'saved' } else { 'printed' })"
            $failingFixturePath = Join-Path $temporaryRoot "failing-fixture-$caseName.ps1"
            [IO.File]::WriteAllText(
                $failingFixturePath,
                $failingFixtureSource.Replace("__MODE__", $case.Mode),
                [Text.UTF8Encoding]::new($false))
            $diagnosticsRoot = Join-Path $temporaryRoot "conformance-diagnostics-$caseName"
            $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY = $(if ($case.Diagnostics) { $diagnosticsRoot } else { $null })
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
        }
    }
    finally {
        $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY = $previousDiagnosticsDirectory
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
