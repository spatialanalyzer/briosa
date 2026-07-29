[CmdletBinding()]
param(
    [string]$SpatialAnalyzerTarget = "2026.1.0529.7",
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$verificationArguments = @{
    SpatialAnalyzerTarget = $SpatialAnalyzerTarget
    Configuration = $Configuration
}
if ($NoBuild) {
    $verificationArguments.NoBuild = $true
}
& (Join-Path $PSScriptRoot "Verify-ReleaseEvidence.ps1") @verificationArguments

$auditPath = Join-Path $repositoryRoot "generated\release\sa\$SpatialAnalyzerTarget\release-audit.json"
$audit = Get-Content -Raw -LiteralPath $auditPath | ConvertFrom-Json -Depth 100
if (-not $audit.release_ready) {
    $blockers = @(
        $audit.criteria |
            Where-Object status -CEQ "blocked" |
            ForEach-Object {
                $references = if (@($_.blocker_references).Count -eq 0) {
                    "no external reference"
                }
                else {
                    @($_.blocker_references) -join ", "
                }
                "$($_.criterion_id) ($references)"
            })
    throw (
        "Release publication is blocked by the generated exact-target audit: " +
        ($blockers -join "; "))
}

Write-Host "Generated release audit permits publication for SA $SpatialAnalyzerTarget."
