[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts/ci-metrics/observability",
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$targetRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory, $targetRoot)
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$variable = "BRIOSA_OBSERVABILITY_EVIDENCE_DIRECTORY"
$previous = [Environment]::GetEnvironmentVariable($variable)
$arguments = @("test", (Join-Path $targetRoot "tests/Briosa.Server.Tests/Briosa.Server.Tests.csproj"),
    "-c", $Configuration, "--no-restore", "--filter", "FullyQualifiedName~ObservabilityPerformanceTests")
if ($NoBuild) { $arguments += "--no-build" }
try {
    [Environment]::SetEnvironmentVariable($variable, $outputRoot)
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "The portable observability benchmark failed." }
    foreach ($mode in @("disabled", "normal", "slow", "failed")) {
        $result = Get-Content -LiteralPath (Join-Path $outputRoot "$mode.json") -Raw | ConvertFrom-Json
        if ($result.schema_version -ne 1 -or $result.mode -cne $mode -or $result.requests -ne 512) {
            throw "The observability benchmark produced incomplete evidence."
        }
    }
}
finally {
    [Environment]::SetEnvironmentVariable($variable, $previous)
}
Write-Host "Portable logging-pipeline evidence complete. SpatialAnalyzer was not used."
