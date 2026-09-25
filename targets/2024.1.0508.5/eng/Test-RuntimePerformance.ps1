[CmdletBinding()]
param(
    [string]$Configuration = "Release",

    [string]$OutputDirectory = "artifacts/ci-metrics/runtime-performance",

    [switch]$IncludeGrpc,

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
$grpcVariableName = "BRIOSA_GENERATED_CLIENT_PERFORMANCE_DIRECTORY"
$previousGrpcDirectory = [Environment]::GetEnvironmentVariable($grpcVariableName)
$filter = "FullyQualifiedName~RuntimePerformanceEvidenceTests"
if ($IncludeGrpc) {
    $filter += "|FullyQualifiedName~GeneratedClientPerformanceEvidenceTests"
}

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
    $filter)
if ($NoBuild) {
    $arguments += "--no-build"
}

try {
    [Environment]::SetEnvironmentVariable($variableName, $evidencePath)
    [Environment]::SetEnvironmentVariable($grpcVariableName, $(if ($IncludeGrpc) { $outputRoot } else { $null }))
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "The vendor-independent runtime performance harness failed with exit code $LASTEXITCODE."
    }
}
finally {
    [Environment]::SetEnvironmentVariable($variableName, $previousEvidencePath)
    [Environment]::SetEnvironmentVariable($grpcVariableName, $previousGrpcDirectory)
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

if ($IncludeGrpc) {
    foreach ($name in @("grpc-no-logging.json", "grpc-logging.json")) {
        $grpcEvidence = Get-Content -Raw -LiteralPath (Join-Path $outputRoot $name) | ConvertFrom-Json -Depth 20
        if ($grpcEvidence.schema_version -ne 1 -or
            $grpcEvidence.harness -cne "generated-client-http2-named-pipe-fake-worker" -or
            $grpcEvidence.rows.Count -ne 10 -or
            $grpcEvidence.sample_requests -ne 400 -or
            $grpcEvidence.overload.submitted -ne 256 -or
            $grpcEvidence.overload.rejected -le 0 -or
            $grpcEvidence.execution.PeakQueuedRequests -gt 64 -or
            $grpcEvidence.execution.AdmittedRequests -ne $grpcEvidence.execution.TerminalRequests -or
            $grpcEvidence.execution.QueuedRequests -ne 0 -or
            $grpcEvidence.execution.WaitingForAdmission -ne 0 -or
            $grpcEvidence.execution.ActiveExecutions -ne 0 -or
            $grpcEvidence.execution.WorkerFailures -ne 0 -or
            $grpcEvidence.execution.WatchdogTimeouts -ne 0 -or
            $grpcEvidence.log_health.Dropped -ne 0 -or
            $grpcEvidence.log_health.Failures -ne 0 -or
            $grpcEvidence.log_health.Queued -ne 0 -or
            $grpcEvidence.worker.executed -ne $grpcEvidence.execution.TerminalRequests -or
            $grpcEvidence.worker.pings -lt 1) {
            throw "The generated-client evidence has an invalid or incomplete state contract: $name."
        }
    }
    Write-Host "Generated HTTP/2 client evidence passed for scalar, nested and list payloads, concurrency, typed overload, idle heartbeat and logging on/off."
}

Write-Host ((
    "Vendor-independent runtime evidence passed for {0} measured requests after {1} warmups. " +
    "No SpatialAnalyzer process or SDK was used.") -f
    $evidence.sample_requests,
    $evidence.warmup_requests)
