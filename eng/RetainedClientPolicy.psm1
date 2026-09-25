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
    if ($policy.schemaVersion -ne 2) { throw 'Unsupported retained-client policy.' }
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

    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $retainedCoverage = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $acceptedCoverage = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $publishedAcceptedCoverage = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $matrix = [Collections.Generic.List[object]]::new()
    foreach ($entry in @($policy.clients) + @($policy.candidates)) {
        $published = $entry.PSObject.Properties.Name -contains 'url'
        if ($entry.language -cnotin $languages -or
            $entry.repository -cne "spatialanalyzer/briosa-$($entry.language)" -or
            $entry.target -cnotin $targets -or
            $entry.version -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$' -or
            $entry.sourceRevision -cnotmatch '^[0-9a-f]{40}$' -or $entry.sourceRevision -eq ('0' * 40) -or
            $entry.requiredContract.major -lt 1 -or $entry.requiredContract.minimumRevision -lt 0) {
            throw 'Invalid client identity or compatibility requirement.'
        }
        $pair = "$($entry.language)/$($entry.target)"
        if (-not $seen.Add("$pair/$($entry.version)")) { throw 'Duplicate client release.' }
        $contract = $contracts[$entry.target]
        $accepted = $entry.requiredContract.major -eq $contract.major -and
            $entry.requiredContract.minimumRevision -le $contract.revision
        if ($published) {
            $hosts = @{ dotnet = 'api.nuget.org'; js = 'registry.npmjs.org'; py = 'files.pythonhosted.org' }
            $url = [Uri]$entry.url
            if ($entry.sha256 -cnotmatch '^[0-9a-f]{64}$' -or
                -not $url.IsAbsoluteUri -or $url.Scheme -cne 'https' -or $url.Host -cne $hosts[$entry.language] -or
                $url.UserInfo -or $url.Query -or $url.Fragment -or
                $entry.fileName -cnotmatch '^[A-Za-z0-9_.-]+\.(nupkg|tgz|whl)$') {
                throw 'Retained packages must identify a hash-pinned public registry artifact.'
            }
            $retainedCoverage.Add($pair) | Out-Null
            if ($accepted) { $publishedAcceptedCoverage.Add($pair) | Out-Null }
        }
        elseif ($entry.sourceRevision -cne $fixtures[$entry.language] -or -not $accepted) {
            throw 'Source candidates must match the tested fixture revision and server contract.'
        }
        if ($accepted) { $acceptedCoverage.Add($pair) | Out-Null }
        $item = @{}
        foreach ($property in $entry.PSObject.Properties) { $item[$property.Name] = $property.Value }
        $item.fixtureSourceRevision = $fixtures[$entry.language]
        $item.published = $published
        $item.expectIncompatible = -not $accepted
        $matrix.Add($item)
    }
    if ($retainedCoverage.Count -ne 6) { throw 'Retain published clients for all languages and exact targets.' }
    if ($acceptedCoverage.Count -ne 6) { throw 'Test a compatible client for every language and exact target.' }
    if (-not $AllowSourceCandidates -and $publishedAcceptedCoverage.Count -ne 6) {
        throw "Server $ServerVersion cannot release until compatible clients are published for every language and target. Source candidates prove development compatibility only."
    }
    return [pscustomobject]@{ include = @($matrix.ToArray()) }
}
Export-ModuleMember -Function Get-RetainedClientMatrix