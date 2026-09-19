param([string]$CompatibilityRoot = (Join-Path $PSScriptRoot '../compatibility'))
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($CompatibilityRoot)
$matrix = Get-Content (Join-Path $root 'matrix.json') -Raw | ConvertFrom-Json
if ($matrix.schemaVersion -ne 1) { throw 'Unknown matrix schema.' }
$retained = Get-Content (Join-Path $root 'retained-clients.json') -Raw | ConvertFrom-Json
if (@($matrix.testedPairs).Count -eq 0) { throw 'A compatibility matrix requires tested pairs.' }
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($pair in $matrix.testedPairs) {
    if (-not $seen.Add("$($pair.language)/$($pair.target)/$($pair.clientVersion)/$($pair.clientSourceRevision)/$($pair.serverVersion)/$($pair.serverSourceRevision)")) {
        throw 'Duplicate compatibility pair.'
    }
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
    $publishedUrl = [string]$report.client.package.publishedUrl
    if ($pair.clientPublished -isnot [bool] -or $pair.clientPublished -ne (-not [string]::IsNullOrWhiteSpace($publishedUrl))) {
        throw 'Published status differs from package origin evidence.'
    }
    if ($pair.clientPublished) {
        $record = @($retained.clients | Where-Object {
            $_.language -ceq $pair.language -and $_.target -ceq $pair.target -and
            $_.version -ceq $pair.clientVersion -and $_.sourceRevision -ceq $pair.clientSourceRevision -and
            $_.sha256 -ceq $pair.clientPackageSha256 -and $_.url -ceq $publishedUrl
        })
        if ($record.Count -ne 1) { throw 'Published compatibility evidence must match one retained registry artifact.' }
    }
}
Write-Host "Verified $($matrix.testedPairs.Count) evidence-backed compatibility pairs."
