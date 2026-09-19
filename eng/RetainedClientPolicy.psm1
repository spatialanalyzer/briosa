Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RetainedClientMatrix {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$ServerVersion, [switch]$PullRequest)
    $policy = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if ($policy.schemaVersion -ne 1 -or $policy.bootstrapServerVersion -cne '0.7.0') {
        throw 'Unsupported retained-client policy.'
    }
    $clients = @($policy.clients)
    if ($clients.Count -eq 0) {
        if (-not $PullRequest -and $ServerVersion -cne $policy.bootstrapServerVersion) {
            throw 'Only the initial contract-aware server 0.7.0 may bootstrap before clients are published.'
        }
        return [pscustomobject]@{ bootstrap = $true; include = @() }
    }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $coverage = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $repositories = @{ dotnet = 'briosa-dotnet'; js = 'briosa-js'; py = 'briosa-py' }
    foreach ($client in $clients) {
        if (-not $repositories.ContainsKey([string]$client.language) -or
            $client.repository -cne "spatialanalyzer/$($repositories[$client.language])" -or
            $client.target -cnotin @('2026.1.0529.7', '2024.1.0508.5') -or
            $client.version -cnotmatch '^[0-9]+\.[0-9]+\.[0-9]+$' -or
            $client.version -cmatch '^0\.[01]\.' -or
            $client.sourceRevision -cnotmatch '^[0-9a-f]{40}$' -or
            $client.sha256 -cnotmatch '^[0-9a-f]{64}$') {
            throw 'Invalid retained published-client identity.'
        }
        $url = [Uri]$client.url
        $hosts = @{ dotnet = 'api.nuget.org'; js = 'registry.npmjs.org'; py = 'files.pythonhosted.org' }
        if (-not $url.IsAbsoluteUri -or $url.Scheme -cne 'https' -or $url.Host -cne $hosts[$client.language] -or
            $url.UserInfo -or $url.Query -or $url.Fragment -or $client.fileName -cnotmatch '^[A-Za-z0-9_.-]+\.(nupkg|tgz|whl)$') {
            throw 'Retained packages must identify a hash-pinned public registry artifact.'
        }
        if (-not $seen.Add("$($client.language)/$($client.target)/$($client.version)")) {
            throw 'Duplicate retained client release.'
        }
        $coverage.Add("$($client.language)/$($client.target)") | Out-Null
    }
    if ($coverage.Count -ne 6) { throw 'Retain published clients for all three languages and both exact targets.' }
    return [pscustomobject]@{ bootstrap = $false; include = $clients }
}
Export-ModuleMember -Function Get-RetainedClientMatrix

