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
