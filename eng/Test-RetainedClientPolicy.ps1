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
function Get-Plan {
    param([hashtable]$Value, [switch]$Release)
    Write-Policy $Value
    return Get-RetainedClientMatrix -Path $policyPath -ServerVersion '0.10.0' -RepositoryRoot $temporary -AllowSourceCandidates:(-not $Release)
}
function Assert-Rejected {
    param([hashtable]$Value, [string]$Message, [switch]$Release)
    $failure = $null
    try { Get-Plan $Value -Release:$Release | Out-Null }
    catch { $failure = $_.Exception.Message }
    if ($null -eq $failure) { throw "An invalid compatibility policy was accepted. Expected: $Message" }
    if (-not $failure.StartsWith($Message, [StringComparison]::Ordinal)) { throw "Expected the rejection '$Message', got '$failure'." }
}
function Copy-Policy {
    param([hashtable]$Value)
    return ($Value | ConvertTo-Json -Depth 12 | ConvertFrom-Json -AsHashtable)
}
function New-PublishedClient {
    param([string]$Language, [string]$Target, [string]$Version, [int]$Major)
    $hostName = @{ dotnet = 'api.nuget.org'; js = 'registry.npmjs.org'; py = 'files.pythonhosted.org' }[$Language]
    return @{
        language = $Language; repository = "spatialanalyzer/briosa-$Language"; target = $Target
        version = $Version; sourceRevision = 'a' * 40; sha256 = 'b' * 64
        url = "https://$hostName/test-package-$Version"; fileName = "test-package-$Version.nupkg"
        requiredContract = @{ major = $Major; minimumRevision = 0 }
    }
}
try {
    $languages = @('dotnet', 'js', 'py')
    $targets = @('2026.1.0529.7', '2024.1.0508.5')
    foreach ($target in $targets) {
        $directory = Join-Path $temporary "targets/$target"
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        '{"schemaVersion":1,"major":3,"revision":0}' | Set-Content (Join-Path $directory 'compatibility.json')
    }
    # The current fixture builds the major-3 source candidates. The earlier fixture is the one
    # that tested the published previous-major packages, so they keep it as their own pin.
    $current = @{ dotnet = '1' * 40; js = '2' * 40; py = '3' * 40 }
    $earlier = @{ dotnet = '4' * 40; js = '5' * 40; py = '6' * 40 }
    $clients = @()
    $retired = @()
    $candidates = @()
    foreach ($language in $languages) {
        foreach ($target in $targets) {
            foreach ($version in @('0.2.0', '0.3.0')) { $retired += New-PublishedClient $language $target $version 1 }
            $previous = New-PublishedClient $language $target '0.4.0' 2
            $previous.fixtureSourceRevision = $earlier[$language]
            $clients += $previous
            $candidates += @{
                language = $language; repository = "spatialanalyzer/briosa-$language"; target = $target
                version = '0.5.0'; sourceRevision = $current[$language]
                requiredContract = @{ major = 3; minimumRevision = 0 }
            }
        }
    }
    $valid = @{
        schemaVersion = 3; clients = $clients; retiredClients = $retired; candidates = $candidates
        fixtureSources = @($languages | ForEach-Object { @{ language = $_; sourceRevision = $current[$_] } })
        historicalFixtureSources = @($languages | ForEach-Object { @{ language = $_; sourceRevision = $earlier[$_] } })
    }

    # Development: six previous-major rejections on their own fixture pins and six source
    # candidates on the current fixture. Retired clients never run.
    $plan = Get-Plan $valid
    $rejections = @($plan.include | Where-Object expectIncompatible)
    $compatible = @($plan.include | Where-Object { -not $_.expectIncompatible })
    if ($plan.include.Count -ne 12 -or $rejections.Count -ne 6 -or $compatible.Count -ne 6 -or
        @($rejections | Where-Object { $_.version -cne '0.4.0' -or -not $_.published -or $_.fixtureSourceRevision -cne $earlier[$_.language] }).Count -ne 0 -or
        @($compatible | Where-Object { $_.version -cne '0.5.0' -or $_.published -or $_.fixtureSourceRevision -cne $current[$_.language] }).Count -ne 0) {
        throw 'The development matrix must run six pinned previous-major rejections and six current-fixture candidates, and no retired client.'
    }
    Assert-Rejected $valid 'Server 0.10.0 cannot release until compatible clients are published' -Release

    $invalidRegistry = 'Retained packages must identify a hash-pinned public registry artifact.'
    $invalidIdentity = 'Invalid client identity or compatibility requirement.'
    $invalidCandidate = 'Source candidates must match the tested fixture revision and server contract.'
    $duplicate = 'Duplicate client release.'
    $outsideWindow = 'Live published clients must require the current or the previous contract major.'
    $notRetirable = 'Only clients of an older contract major can be retired.'
    $unknownPin = 'Published client fixture pins must name a current or historical fixture source of the same language.'
    $candidatePin = 'Source candidates use the current fixture source and cannot pin another.'
    $invalidHistory = 'Historical fixture sources must pin distinct immutable revisions.'
    $cases = @(
        # Schema 3 only.
        @{ message = 'Unsupported retained-client policy.'; change = { param($p) $p.schemaVersion = 2 } }
        @{ message = 'Unsupported retained-client policy.'; change = { param($p) $p.Remove('retiredClients') } }
        # Previous-major rejections alone prove no compatible client.
        @{ message = 'Test a compatible client for every language and exact target.'; change = { param($p) $p.candidates = @() } }
        @{ message = 'Retain published clients for all languages and exact targets.'; change = { param($p) $p.clients = @($p.clients[0..4]) } }
        # Immutable identities and pins.
        @{ message = $duplicate; change = { param($p) $p.clients += $p.clients[0] } }
        @{ message = $invalidRegistry; change = { param($p) $p.clients[0].sha256 = 'invalid' } }
        @{ message = $invalidRegistry; change = { param($p) $p.clients[0].url = 'http://api.nuget.org/test-package' } }
        @{ message = $invalidIdentity; change = { param($p) $p.clients[0].sourceRevision = 'main' } }
        @{ message = $invalidIdentity; change = { param($p) $p.clients[0].requiredContract.major = 0 } }
        @{ message = 'Fixture sources must pin one immutable revision per language.'; change = { param($p) $p.fixtureSources[0].sourceRevision = 'main' } }
        @{ message = $invalidCandidate; change = { param($p) $p.candidates[0].sourceRevision = 'd' * 40 } }
        @{ message = $invalidCandidate; change = { param($p) $p.candidates[0].requiredContract.minimumRevision = 1 } }
        @{ message = $invalidCandidate; change = { param($p) $p.candidates[0].requiredContract.major = 2 } }
        # Retired records keep the published identity rules.
        @{ message = $invalidRegistry; change = { param($p) $p.retiredClients[0].sha256 = 'invalid' } }
        @{ message = $invalidRegistry; change = { param($p) $p.retiredClients[0].url = 'https://example.org/test-package' } }
        @{ message = $invalidRegistry; change = { param($p) $p.retiredClients[0].fileName = 'test-package.zip' } }
        @{ message = $invalidRegistry; change = { param($p) $p.retiredClients[0].Remove('url') } }
        @{ message = $invalidIdentity; change = { param($p) $p.retiredClients[0].sourceRevision = 'main' } }
        # A release appears in one list only.
        @{ message = $duplicate; change = { param($p) $p.retiredClients += $p.clients[0] } }
        @{ message = $duplicate; change = { param($p) $p.retiredClients += $p.retiredClients[0] } }
        # The #250 support window: live clients require the current or the previous major, and
        # only older clients can be retired.
        @{ message = $outsideWindow; change = { param($p) $p.clients += $p.retiredClients[0]; $p.retiredClients = @($p.retiredClients | Select-Object -Skip 1) } }
        @{ message = $outsideWindow; change = { param($p) $p.clients[0].requiredContract.major = 4 } }
        @{ message = $notRetirable; change = { param($p) $p.retiredClients[0].requiredContract.major = 3 } }
        @{ message = $notRetirable; change = { param($p) $p.retiredClients[0].requiredContract.major = 4 } }
        # A per-entry fixture pin names a known fixture of the same language. Candidates have none.
        @{ message = $unknownPin; change = { param($p) $p.clients[0].fixtureSourceRevision = 'd' * 40 } }
        @{ message = $unknownPin; change = { param($p) $p.clients[0].fixtureSourceRevision = $earlier.js } }
        @{ message = $unknownPin; change = { param($p) $p.retiredClients[0].fixtureSourceRevision = 'd' * 40 } }
        @{ message = $candidatePin; change = { param($p) $p.candidates[0].fixtureSourceRevision = $current.dotnet } }
        @{ message = $candidatePin; change = { param($p) $p.candidates[0].fixtureSourceRevision = $earlier.dotnet } }
        @{ message = $invalidHistory; change = { param($p) $p.historicalFixtureSources[0].sourceRevision = 'main' } }
        @{ message = $invalidHistory; change = { param($p) $p.historicalFixtureSources += $p.fixtureSources[0] } }
    )
    foreach ($case in $cases) {
        $changed = Copy-Policy $valid
        & $case.change $changed
        Assert-Rejected $changed $case.message
    }

    # A published client may also pin the current fixture of its language.
    $changed = Copy-Policy $valid
    $changed.clients[0].fixtureSourceRevision = $current.dotnet
    $item = @((Get-Plan $changed).include | Where-Object { $_.language -ceq 'dotnet' -and $_.target -ceq $changed.clients[0].target -and $_.version -ceq '0.4.0' })
    if ($item.Count -ne 1 -or $item[0].fixtureSourceRevision -cne $current.dotnet) { throw 'A published client must run with its own fixture pin.' }

    # Release: the published major-3 packages replace the candidates. Without a pin they run on
    # the current fixture, beside the pinned previous-major rejections.
    $published = Copy-Policy $valid
    foreach ($candidate in $published.candidates) {
        $client = New-PublishedClient $candidate.language $candidate.target '0.5.0' 3
        $client.sourceRevision = $candidate.sourceRevision
        $published.clients += $client
    }
    $published.candidates = @()
    $release = Get-Plan $published -Release
    if ($release.include.Count -ne 12 -or @($release.include | Where-Object { -not $_.published }).Count -ne 0 -or
        @($release.include | Where-Object { $_.version -ceq '0.4.0' -and $_.expectIncompatible -and $_.fixtureSourceRevision -ceq $earlier[$_.language] }).Count -ne 6 -or
        @($release.include | Where-Object { $_.version -ceq '0.5.0' -and -not $_.expectIncompatible -and $_.fixtureSourceRevision -ceq $current[$_.language] }).Count -ne 6) {
        throw 'Published positive and negative pairs must both remain covered.'
    }
    # After that release, the previous-major clients may be retired too.
    $published.retiredClients += @($published.clients | Where-Object { $_.version -ceq '0.4.0' })
    $published.clients = @($published.clients | Where-Object { $_.version -cne '0.4.0' })
    $release = Get-Plan $published -Release
    if ($release.include.Count -ne 6 -or @($release.include | Where-Object expectIncompatible).Count -ne 0) {
        throw 'Retiring the previous-major clients must leave only the current-major release pairs.'
    }
    Write-Host 'Verified the #250 support window, retired-client identities, per-entry fixture pins, compatible coverage, and the published-client release gate.'
}
finally {
    $resolved = [IO.Path]::GetFullPath($temporary)
    if (-not $resolved.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -or $resolved -eq $temporaryBase) {
        throw 'Refusing cleanup outside the temporary policy directory.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}