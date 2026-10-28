Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RetainedClientMatrix {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$ServerVersion,
        [switch]$AllowSourceCandidates,
        [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
    )
    $policy = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $lists = @('clients', 'retiredClients', 'candidates', 'fixtureSources', 'historicalFixtureSources')
    if ($policy.schemaVersion -ne 3 -or @($lists | Where-Object { $policy.PSObject.Properties.Name -cnotcontains $_ }).Count -ne 0) {
        throw 'Unsupported retained-client policy.'
    }
    $languages = @('dotnet', 'js', 'py')
    $targets = @('2026.1.0529.7', '2024.1.0508.5')
    $contracts = @{}
    foreach ($target in $targets) {
        $contract = Get-Content (Join-Path $RepositoryRoot "targets/$target/compatibility.json") -Raw | ConvertFrom-Json
        if ($contract.major -lt 1 -or $contract.revision -lt 0) { throw 'Invalid server compatibility contract.' }
        $contracts[$target] = $contract
    }
    $fixtures = @{}
    foreach ($fixture in $policy.fixtureSources) {
        if ($fixture.language -cnotin $languages -or $fixtures.ContainsKey($fixture.language) -or
            $fixture.sourceRevision -cnotmatch '^[0-9a-f]{40}$' -or $fixture.sourceRevision -eq ('0' * 40)) {
            throw 'Fixture sources must pin one immutable revision per language.'
        }
        $fixtures[$fixture.language] = $fixture.sourceRevision
    }
    if ($fixtures.Count -ne 3) { throw 'Missing client fixture source.' }
    # A published client may pin any current or historical fixture of its language, and the
    # workflow checks that revision out, so every historical pin must be immutable as well.
    $fixturePins = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($fixture in @($policy.fixtureSources) + @($policy.historicalFixtureSources)) {
        if ($fixture.language -cnotin $languages -or
            $fixture.sourceRevision -cnotmatch '^[0-9a-f]{40}$' -or $fixture.sourceRevision -eq ('0' * 40) -or
            -not $fixturePins.Add("$($fixture.language)/$($fixture.sourceRevision)")) {
            throw 'Historical fixture sources must pin distinct immutable revisions.'
        }
    }

    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $retainedCoverage = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $acceptedCoverage = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $publishedAcceptedCoverage = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $matrix = [Collections.Generic.List[object]]::new()
    foreach ($list in @('clients', 'retiredClients', 'candidates')) {
        $retired = $list -ceq 'retiredClients'
        foreach ($entry in @($policy.$list)) {
            $published = $entry.PSObject.Properties.Name -contains 'url'
            $pinned = $entry.PSObject.Properties.Name -contains 'fixtureSourceRevision'
            if ($entry.language -cnotin $languages -or
                $entry.repository -cne "spatialanalyzer/briosa-$($entry.language)" -or
                $entry.target -cnotin $targets -or
                $entry.version -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$' -or
                $entry.sourceRevision -cnotmatch '^[0-9a-f]{40}$' -or $entry.sourceRevision -eq ('0' * 40) -or
                $entry.requiredContract.major -lt 1 -or $entry.requiredContract.minimumRevision -lt 0) {
                throw 'Invalid client identity or compatibility requirement.'
            }
            $pair = "$($entry.language)/$($entry.target)"
            # The release identity spans all three lists.
            if (-not $seen.Add("$pair/$($entry.version)")) { throw 'Duplicate client release.' }
            $contract = $contracts[$entry.target]
            $major = $entry.requiredContract.major
            $accepted = $major -eq $contract.major -and $entry.requiredContract.minimumRevision -le $contract.revision
            if ($published) {
                $hosts = @{ dotnet = 'api.nuget.org'; js = 'registry.npmjs.org'; py = 'files.pythonhosted.org' }
                $url = [Uri]$entry.url
                if ($entry.sha256 -cnotmatch '^[0-9a-f]{64}$' -or
                    -not $url.IsAbsoluteUri -or $url.Scheme -cne 'https' -or $url.Host -cne $hosts[$entry.language] -or
                    $url.UserInfo -or $url.Query -or $url.Fragment -or
                    $entry.fileName -cnotmatch '^[A-Za-z0-9_.-]+\.(nupkg|tgz|whl)$') {
                    throw 'Retained packages must identify a hash-pinned public registry artifact.'
                }
                if ($pinned -and -not $fixturePins.Contains("$($entry.language)/$($entry.fixtureSourceRevision)")) {
                    throw 'Published client fixture pins must name a current or historical fixture source of the same language.'
                }
            }
            elseif ($retired) { throw 'Retained packages must identify a hash-pinned public registry artifact.' }
            elseif ($pinned) { throw 'Source candidates use the current fixture source and cannot pin another.' }
            elseif ($entry.sourceRevision -cne $fixtures[$entry.language] -or -not $accepted) {
                throw 'Source candidates must match the tested fixture revision and server contract.'
            }
            # The support window (#250): the live gate runs the clients of the current and the
            # previous contract major. Older clients stay recorded in retiredClients, so their
            # published evidence remains verifiable, but they never run.
            if ($retired) {
                if ($major -ge $contract.major) { throw 'Only clients of an older contract major can be retired.' }
                continue
            }
            if ($published -and ($major -gt $contract.major -or $major -lt $contract.major - 1)) {
                throw 'Live published clients must require the current or the previous contract major. Move older clients to retiredClients.'
            }
            if ($published) {
                $retainedCoverage.Add($pair) | Out-Null
                if ($accepted) { $publishedAcceptedCoverage.Add($pair) | Out-Null }
            }
            if ($accepted) { $acceptedCoverage.Add($pair) | Out-Null }
            $item = @{}
            foreach ($property in $entry.PSObject.Properties) { $item[$property.Name] = $property.Value }
            $item.fixtureSourceRevision = if ($pinned) { $entry.fixtureSourceRevision } else { $fixtures[$entry.language] }
            $item.published = $published
            $item.expectIncompatible = -not $accepted
            $matrix.Add($item)
        }
    }
    if ($retainedCoverage.Count -ne 6) { throw 'Retain published clients for all languages and exact targets.' }
    if ($acceptedCoverage.Count -ne 6) { throw 'Test a compatible client for every language and exact target.' }
    if (-not $AllowSourceCandidates -and $publishedAcceptedCoverage.Count -ne 6) {
        throw "Server $ServerVersion cannot release until compatible clients are published for every language and target. Source candidates prove development compatibility only."
    }
    return [pscustomobject]@{ include = @($matrix.ToArray()) }
}
Export-ModuleMember -Function Get-RetainedClientMatrix