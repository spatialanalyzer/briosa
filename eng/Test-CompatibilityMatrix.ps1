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
    $expected = if ($pair.PSObject.Properties.Name -contains 'expectedCompatibility') { $pair.expectedCompatibility } else { 'accepted' }
    $observed = if ($report.PSObject.Properties.Name -contains 'expectedCompatibility') { $report.expectedCompatibility } else { 'accepted' }
    if ($expected -cnotin @('accepted', 'rejected') -or $observed -cne $expected) {
        throw 'Compatibility acceptance and rejection must be recorded separately.'
    }
    $compatible = if ($null -eq $pair.serverContract) {
        $pair.requiredContract.major -eq 1 -and $pair.requiredContract.minimumRevision -eq 0 -and
        $pair.serverVersion -ceq $matrix.legacyBaseline.serverVersion -and
        $pair.serverSourceRevision -ceq $matrix.legacyBaseline.sourceRevision
    }
    else {
        $pair.serverContract.major -eq $pair.requiredContract.major -and
        $pair.serverContract.revision -ge $pair.requiredContract.minimumRevision
    }
    if ($expected -ceq 'rejected') {
        if ($report.validationKind -cne 'client-server-compatibility-rejection' -or
            @($report.scenarios).Count -ne 1 -or @($report.scenarios)[0] -cne 'installation-contract-rejected' -or
            $compatible) {
            throw 'A rejection claim requires incompatible coordinates and explicit rejection evidence.'
        }
    }
    elseif ($report.validationKind -cne 'packaged-client-and-server-fake-sdk' -or -not $compatible) {
        throw 'Accepted operation conformance requires compatible coordinates.'
    }
    if ($report.client.PSObject.Properties.Name -contains 'fixtureSourceRevision') {
        $fixture = @($retained.fixtureSources | Where-Object language -CEQ $pair.language)
        if ($fixture.Count -ne 1 -or $report.client.fixtureUncommittedChanges -or
            $report.client.fixtureSourceRevision -cne $fixture[0].sourceRevision) {
            throw 'Current compatibility evidence must use the pinned, committed fixture.'
        }
    }
    if (-not $report.passed -or $report.client.uncommittedChanges -or $report.licensedSpatialAnalyzer -or
        $report.validationKind -cne $pair.validationKind -or
        $report.client.package.version -cne $pair.clientVersion -or
        $report.client.sourceRevision -cne $pair.clientSourceRevision -or
        $report.client.package.sha256 -cne $pair.clientPackageSha256 -or
        $report.client.requiredContract.major -ne $pair.requiredContract.major -or
        $report.client.requiredContract.minimumRevision -ne $pair.requiredContract.minimumRevision -or
        $report.target.spatial_analyzer -cne $pair.target -or
        $report.server.briosa_version -cne $pair.serverVersion -or
        $report.server.source_revision -cne $pair.serverSourceRevision -or
        @($report.scenarios).Count -ne $pair.scenariosPassed) {
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
