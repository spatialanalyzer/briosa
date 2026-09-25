Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RetainedClientPolicy.psm1') -Force

$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporary = Join-Path $temporaryBase "briosa-retained-policy-$([Guid]::NewGuid().ToString('N'))"
$policyPath = Join-Path $temporary 'policy.json'
function Write-Policy {
    param([hashtable]$Value)
    $Value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $policyPath -Encoding utf8
}
function Assert-Rejected {
    param([hashtable]$Value, [switch]$Release)
    Write-Policy $Value
    $failed = $false
    try { Get-RetainedClientMatrix -Path $policyPath -ServerVersion '0.9.0' -RepositoryRoot $temporary -AllowSourceCandidates:(-not $Release) | Out-Null }
    catch { $failed = $true }
    if (-not $failed) { throw 'An invalid compatibility policy was accepted.' }
}
function Copy-Policy {
    param([hashtable]$Value)
    return ($Value | ConvertTo-Json -Depth 12 | ConvertFrom-Json -AsHashtable)
}
try {
    $clients = @()
    $candidates = @()
    $fixtures = @()
    foreach ($language in @('dotnet', 'js', 'py')) {
        $fixtures += @{ language = $language; sourceRevision = 'c' * 40 }
        foreach ($target in @('2026.1.0529.7', '2024.1.0508.5')) {
            $directory = Join-Path $temporary "targets/$target"
            [IO.Directory]::CreateDirectory($directory) | Out-Null
            '{"schemaVersion":1,"major":2,"revision":0}' | Set-Content (Join-Path $directory 'compatibility.json')
            $hostName = @{ dotnet = 'api.nuget.org'; js = 'registry.npmjs.org'; py = 'files.pythonhosted.org' }[$language]
            $clients += @{
                language = $language; repository = "spatialanalyzer/briosa-$language"; target = $target
                version = '0.3.0'; sourceRevision = 'a' * 40; sha256 = 'b' * 64
                url = "https://$hostName/test-package"; fileName = 'test-package.nupkg'
                requiredContract = @{ major = 1; minimumRevision = 0 }
            }
            $candidates += @{
                language = $language; repository = "spatialanalyzer/briosa-$language"; target = $target
                version = '0.4.0'; sourceRevision = 'c' * 40
                requiredContract = @{ major = 2; minimumRevision = 0 }
            }
        }
    }
    $valid = @{ schemaVersion = 2; fixtureSources = $fixtures; clients = $clients; candidates = $candidates }
    Write-Policy $valid
    $plan = Get-RetainedClientMatrix -Path $policyPath -ServerVersion '0.9.0' -AllowSourceCandidates -RepositoryRoot $temporary
    if ($plan.include.Count -ne 12 -or
        @($plan.include | Where-Object expectIncompatible).Count -ne 6 -or
        @($plan.include | Where-Object { -not $_.expectIncompatible }).Count -ne 6) {
        throw 'The development matrix must exercise six rejections and six compatible candidates.'
    }
    Assert-Rejected $valid -Release
    $changed = Copy-Policy $valid
    $changed.candidates = @()
    Assert-Rejected $changed
    $changed = Copy-Policy $valid
    $changed.clients = @($changed.clients[0..4])
    Assert-Rejected $changed
    $changed = Copy-Policy $valid
    $changed.clients += $changed.clients[0]
    Assert-Rejected $changed
    foreach ($mutation in @(
        { param($p) $p.clients[0].sha256 = 'invalid' },
        { param($p) $p.clients[0].url = 'http://api.nuget.org/test-package' },
        { param($p) $p.clients[0].sourceRevision = 'main' },
        { param($p) $p.fixtureSources[0].sourceRevision = 'main' },
        { param($p) $p.candidates[0].sourceRevision = 'd' * 40 },
        { param($p) $p.candidates[0].requiredContract.minimumRevision = 1 },
        { param($p) $p.candidates[0].requiredContract.major = 1 },
        { param($p) $p.clients[0].requiredContract.major = 0 }
    )) {
        $changed = Copy-Policy $valid
        & $mutation $changed
        Assert-Rejected $changed
    }

    $published = Copy-Policy $valid
    foreach ($candidate in $published.candidates) {
        $original = $published.clients | Where-Object { $_.language -eq $candidate.language -and $_.target -eq $candidate.target }
        $candidate.url = $original.url
        $candidate.fileName = $original.fileName
        $candidate.sha256 = 'd' * 64
        $published.clients += $candidate
    }
    $published.candidates = @()
    Write-Policy $published
    $release = Get-RetainedClientMatrix -Path $policyPath -ServerVersion '0.9.0' -RepositoryRoot $temporary
    if ($release.include.Count -ne 12) { throw 'Published positive and negative pairs must both remain covered.' }
    Write-Host 'Verified positive and negative compatibility coverage, immutable pins, and the published-client release gate.'
}
finally {
    $resolved = [IO.Path]::GetFullPath($temporary)
    if (-not $resolved.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -or $resolved -eq $temporaryBase) {
        throw 'Refusing cleanup outside the temporary policy directory.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}