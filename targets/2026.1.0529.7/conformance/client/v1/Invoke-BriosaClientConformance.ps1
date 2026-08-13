[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$FixtureCommand,

    [string[]]$FixtureArguments = @(),

    [string[]]$Scenario = @(),

    [ValidateRange(5, 600)]
    [int]$FixtureTimeoutSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw "The Briosa client conformance host requires 64-bit Windows."
}

$packageRoot = Split-Path -Parent $PSScriptRoot
$contractPath = Join-Path $packageRoot "contract\scenarios.json"
$manifestPath = Join-Path $packageRoot "manifest.json"
$serverPath = Join-Path $packageRoot "server\Briosa.Server.exe"
$workerPath = Join-Path $packageRoot "fake-worker\Briosa.SmokeWorker.exe"
$applicationPath = $null
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-conformance-$([Guid]::NewGuid().ToString('N'))"

function Get-PackageProcesses {
    $expectedPaths = @($serverPath, $workerPath, $applicationPath) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    return @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        try {
            $_.Path -in $expectedPaths
        }
        catch {
            $false
        }
    })
}

function Stop-PackageProcesses {
    foreach ($process in @(Get-PackageProcesses)) {
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction Stop
            $process.WaitForExit(5000) | Out-Null
        }
        catch {
        }
    }
}

function New-ProcessStartInfo {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [hashtable]$Environment = @{},
        [switch]$RedirectOutput
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }
    foreach ($entry in $Environment.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            $startInfo.Environment.Remove($entry.Key) | Out-Null
        }
        else {
            $startInfo.Environment[$entry.Key] = [string]$entry.Value
        }
    }
    if ($RedirectOutput) {
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
    }
    return $startInfo
}

function Invoke-Fixture {
    param(
        [Parameter(Mandatory)][object]$ScenarioDefinition,
        [Parameter(Mandatory)][hashtable]$Environment
    )

    $arguments = @($FixtureArguments) + @(
        "--scenario", [string]$ScenarioDefinition.id,
        "--contract", $contractPath)
    $startInfo = New-ProcessStartInfo `
        -FilePath $FixtureCommand `
        -Arguments $arguments `
        -WorkingDirectory (Get-Location).Path `
        -Environment $Environment `
        -RedirectOutput
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "The client fixture could not be started."
        }
        $outputTask = $process.StandardOutput.ReadToEndAsync()
        $errorTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($FixtureTimeoutSeconds * 1000)) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            throw "The client fixture timed out in scenario '$($ScenarioDefinition.id)'."
        }
        $output = $outputTask.GetAwaiter().GetResult().Trim()
        $errorOutput = $errorTask.GetAwaiter().GetResult().Trim()
        if ($process.ExitCode -ne 0) {
            $safeError = $errorOutput.Replace($temporaryRoot, "<temporary-root>")
            throw "The client fixture failed scenario '$($ScenarioDefinition.id)' with exit code $($process.ExitCode): $safeError"
        }
        if ([string]::IsNullOrWhiteSpace($output)) {
            throw "The client fixture returned no report for scenario '$($ScenarioDefinition.id)'."
        }
        try {
            return $output | ConvertFrom-Json
        }
        catch {
            throw "The client fixture returned an invalid JSON report for scenario '$($ScenarioDefinition.id)'."
        }
    }
    finally {
        $process.Dispose()
    }
}

foreach ($requiredPath in @(
    $contractPath,
    $manifestPath,
    $serverPath,
    $workerPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "The conformance package is missing '$requiredPath'."
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
if ($manifest.artifactKind -ne "briosa_client_conformance" -or
    $manifest.scenarioContract -ne $contract.contract_id -or
    $manifest.spatialAnalyzerTarget -ne $contract.spatial_analyzer_target) {
    throw "The conformance package manifest and scenario contract do not agree."
}

$requestedScenarios = @($Scenario | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$selectedScenarios = @($contract.scenarios | Where-Object {
    $requestedScenarios.Count -eq 0 -or $_.id -in $requestedScenarios
})
if ($selectedScenarios.Count -eq 0) {
    throw "No matching conformance scenarios were selected."
}
foreach ($requested in $requestedScenarios) {
    if ($requested -notin @($selectedScenarios.id)) {
        throw "Unknown conformance scenario '$requested'."
    }
}

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    foreach ($scenarioDefinition in $selectedScenarios) {
        Stop-PackageProcesses
        $scenarioRoot = Join-Path $temporaryRoot $scenarioDefinition.id
        [IO.Directory]::CreateDirectory($scenarioRoot) | Out-Null
        $statePath = Join-Path $scenarioRoot "worker-state"
        $exitSignalPath = Join-Path $scenarioRoot "worker-exit"
        $applicationRoot = Join-Path $scenarioRoot "fake-application"
        [IO.Directory]::CreateDirectory($applicationRoot) | Out-Null
        Copy-Item -Path (Join-Path $packageRoot "fake-worker\*") `
            -Destination $applicationRoot -Recurse
        $applicationPath = Join-Path $applicationRoot "Spatial Analyzer64.exe"
        Move-Item -LiteralPath (Join-Path $applicationRoot "Briosa.SmokeWorker.exe") `
            -Destination $applicationPath
        $externalApplication = $null
        $environment = @{
            "BRIOSA_SERVER_PATH" = $serverPath
            "BRIOSA_CONFORMANCE_SCENARIO" = [string]$scenarioDefinition.id
            "BRIOSA_CONFORMANCE_CONTRACT_PATH" = $contractPath
            "BRIOSA_CONFORMANCE_WORKER_EXIT_SIGNAL_PATH" = $exitSignalPath
            "BRIOSA_TEST_WORKER_SCENARIO" = [string]$scenarioDefinition.worker_scenario
            "BRIOSA_TEST_WORKER_STATE_PATH" = $statePath
            "BRIOSA_TEST_WORKER_EXIT_SIGNAL_PATH" = $exitSignalPath
            "Briosa__Worker__ExecutablePath" = $workerPath
            "Briosa__Worker__ExecutionWatchdogTimeout" = $scenarioDefinition.watchdog_timeout
            "Briosa__SpatialAnalyzer__ExecutablePath" = $applicationPath
            "Briosa__Security__Operations__Deny__0" = $(if ($scenarioDefinition.deny_get_working_directory) { "file_operations.get_working_directory" } else { $null })
            "Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Version" = [string]$scenarioDefinition.activated_sdk_version
            "Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Reference" = "portable-conformance-host"
            "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Version" = [string]$scenarioDefinition.connected_sa_version
            "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Reference" = "portable-conformance-host"
        }
        try {
            if ($scenarioDefinition.start_external_application) {
                $applicationStartInfo = New-ProcessStartInfo `
                    -FilePath $applicationPath `
                    -Arguments @("--hold") `
                    -WorkingDirectory (Split-Path -Parent $applicationPath)
                $externalApplication = [Diagnostics.Process]::Start($applicationStartInfo)
            }

            $report = Invoke-Fixture `
                -ScenarioDefinition $scenarioDefinition `
                -Environment $environment
            if ($report.schema_version -ne 1 -or
                $report.contract_id -ne $contract.contract_id -or
                $report.scenario -ne $scenarioDefinition.id -or
                $report.success -ne $true) {
                throw "The client fixture report did not satisfy scenario '$($scenarioDefinition.id)'."
            }

            if ($null -ne $externalApplication -and $externalApplication.HasExited) {
                throw "The client terminated an externally owned fake application in scenario '$($scenarioDefinition.id)'."
            }
            $unexpectedProcesses = @(Get-PackageProcesses | Where-Object {
                $null -eq $externalApplication -or $_.Id -ne $externalApplication.Id
            })
            if ($unexpectedProcesses.Count -ne 0) {
                throw "The client left package-owned processes running after scenario '$($scenarioDefinition.id)'."
            }

            Write-Host "Passed client conformance scenario: $($scenarioDefinition.id)"
        }
        finally {
            if ($null -ne $externalApplication -and -not $externalApplication.HasExited) {
                Stop-Process -Id $externalApplication.Id -Force -ErrorAction SilentlyContinue
                $externalApplication.WaitForExit(5000) | Out-Null
            }
            if ($null -ne $externalApplication) {
                $externalApplication.Dispose()
            }
            Stop-PackageProcesses
        }
    }
}
finally {
    Stop-PackageProcesses
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}

Write-Host "All selected Briosa client conformance scenarios passed."
