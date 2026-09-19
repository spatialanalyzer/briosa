Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '../compatibility'
$matrix = Get-Content (Join-Path $root 'matrix.json') -Raw | ConvertFrom-Json
if ($matrix.schemaVersion -ne 1) { throw 'Unknown matrix schema.' }
foreach ($pair in $matrix.testedPairs) {
    if ($pair.evidence -notmatch '^evidence/[0-9-]+/[a-z0-9.-]+\.json$') { throw 'Invalid evidence path.' }
    $path = Join-Path $root $pair.evidence
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $pair.evidenceSha256) {
        throw 'Compatibility evidence digest mismatch.'
    }
    $report = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    if (-not $report.passed -or $report.client.uncommittedChanges -or $report.licensedSpatialAnalyzer -or
        $report.validationKind -cne $pair.validationKind -or
        $report.client.version -cne $pair.clientVersion -or
        $report.client.sourceRevision -cne $pair.clientSourceRevision -or
        $report.client.package.sha256 -cne $pair.clientPackageSha256 -or
        $report.target.spatial_analyzer -cne $pair.target -or
        $report.server.briosa_version -cne $pair.serverVersion -or
        $report.server.source_revision -cne $pair.serverSourceRevision -or
        $report.scenarios.Count -ne $pair.scenariosPassed) {
        throw 'Matrix claim differs from its packaged fake-SDK evidence.'
    }
}
Write-Host "Verified $($matrix.testedPairs.Count) evidence-backed compatibility pairs."

