[CmdletBinding()]
param(
    [string]$Configuration = "Release",

    [string]$OutputDirectory = "artifacts/ci-metrics/runtime-performance",

    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repositoryRoot "tests/Briosa.Server.Tests/Briosa.Server.Tests.csproj"
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot)
$evidencePath = Join-Path $outputRoot "runtime-performance-evidence.json"
$variableName = "BRIOSA_RUNTIME_PERFORMANCE_EVIDENCE_PATH"
$previousEvidencePath = [Environment]::GetEnvironmentVariable($variableName)

[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
if (Test-Path -LiteralPath $evidencePath) {
    Remove-Item -LiteralPath $evidencePath -Force
}

$arguments = @(
    "test",
    $testProject,
    "-c",
    $Configuration,
    "--no-restore",
    "--filter",
    "FullyQualifiedName~RuntimePerformanceEvidenceTests")
if ($NoBuild) {
    $arguments += "--no-build"
}

try {
    [Environment]::SetEnvironmentVariable($variableName, $evidencePath)
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "The vendor-independent runtime performance harness failed with exit code $LASTEXITCODE."
    }
}
finally {
    [Environment]::SetEnvironmentVariable($variableName, $previousEvidencePath)
}

if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
    throw "The runtime performance harness did not write '$evidencePath'."
}

$evidence = Get-Content -Raw -LiteralPath $evidencePath | ConvertFrom-Json -Depth 20
if ($evidence.schema_version -ne 1 -or
    $evidence.harness -cne "named-pipe-fake-worker" -or
    $evidence.warmup_requests -ne 64 -or
    $evidence.sample_requests -ne 512 -or
    $evidence.dispatch_p95_milliseconds -lt 0 -or
    $evidence.request_mapping_p95_milliseconds -lt 0 -or
    $evidence.discovery_p95_milliseconds -lt 0 -or
    $evidence.retained_managed_memory_bytes -lt 0 -or
    $evidence.execution.AdmittedRequests -ne 576 -or
    $evidence.execution.TerminalRequests -ne 576 -or
    $evidence.execution.QueuedRequests -ne 0 -or
    $evidence.execution.WaitingForAdmission -ne 0 -or
    $evidence.execution.ActiveExecutions -ne 0) {
    throw "The runtime performance evidence has an invalid or incomplete state contract."
}

& (Join-Path $PSScriptRoot "Measure-CiBudget.ps1") `
    -Metric dispatch-p95 `
    -ObservedValue ([double]$evidence.dispatch_p95_milliseconds) `
    -OutputDirectory $outputRoot
& (Join-Path $PSScriptRoot "Measure-CiBudget.ps1") `
    -Metric retained-managed-memory `
    -ObservedValue ([double]$evidence.retained_managed_memory_bytes) `
    -OutputDirectory $outputRoot
& (Join-Path $PSScriptRoot "Measure-CiBudget.ps1") `
    -Metric request-mapping-p95 `
    -ObservedValue ([double]$evidence.request_mapping_p95_milliseconds) `
    -OutputDirectory $outputRoot
& (Join-Path $PSScriptRoot "Measure-CiBudget.ps1") `
    -Metric discovery-p95 `
    -ObservedValue ([double]$evidence.discovery_p95_milliseconds) `
    -OutputDirectory $outputRoot

Write-Host ((
    "Vendor-independent runtime evidence passed for {0} measured requests after {1} warmups. " +
    "No SpatialAnalyzer process or SDK was used.") -f
    $evidence.sample_requests,
    $evidence.warmup_requests)
