Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RetainedClientPolicy.psm1') -Force
$policyPath = Join-Path $PSScriptRoot '../compatibility/retained-clients.json'
$initial = Get-RetainedClientMatrix -Path $policyPath -ServerVersion '0.7.0'
if (-not $initial.bootstrap) { Write-Host 'Published-client records are active.'; return }
$rejected = $false
try { Get-RetainedClientMatrix -Path $policyPath -ServerVersion '0.7.1' | Out-Null }
catch { $rejected = $true }
if (-not $rejected) { throw 'A subsequent release incorrectly bypassed retained-client validation.' }
if (-not (Get-RetainedClientMatrix -Path $policyPath -ServerVersion '0.2.0-ci' -PullRequest).bootstrap) {
    throw 'PR bootstrap verification failed.'
}
Write-Host 'Initial release bootstrap is bounded; later releases require retained published clients.'

$temporary = Join-Path ([IO.Path]::GetTempPath()) "briosa-retained-policy-$([Guid]::NewGuid().ToString('N')).json"
$clients = @(
    foreach ($language in @('dotnet', 'js', 'py')) {
        foreach ($target in @('2026.1.0529.7', '2024.1.0508.5')) {
            $hostName = @{ dotnet = 'api.nuget.org'; js = 'registry.npmjs.org'; py = 'files.pythonhosted.org' }[$language]
            @{
                language = $language; repository = "spatialanalyzer/briosa-$language"; target = $target
                version = '0.2.0'; sourceRevision = ('a' * 40); sha256 = ('b' * 64)
                url = "https://$hostName/test-package"; fileName = 'test-package.nupkg'
            }
        }
    }
)
function Assert-Rejected {
    param([object[]]$Records)
    @{ schemaVersion = 1; bootstrapServerVersion = '0.7.0'; clients = $Records } |
        ConvertTo-Json -Depth 8 | Set-Content $temporary -Encoding utf8
    $failed = $false
    try { Get-RetainedClientMatrix -Path $temporary -ServerVersion '0.7.1' | Out-Null }
    catch { $failed = $true }
    if (-not $failed) { throw 'Invalid retained-client policy was accepted.' }
}
try {
    @{ schemaVersion = 1; bootstrapServerVersion = '0.7.0'; clients = $clients } |
        ConvertTo-Json -Depth 8 | Set-Content $temporary -Encoding utf8
    $complete = Get-RetainedClientMatrix -Path $temporary -ServerVersion '0.7.1'
    if ($complete.bootstrap -or $complete.include.Count -ne 6) { throw 'Complete retained matrix rejected.' }
    Assert-Rejected @($clients[0..4])
    Assert-Rejected @($clients + $clients[0])
    $clients[0].sha256 = 'invalid'
    Assert-Rejected $clients
    $clients[0].sha256 = 'b' * 64
    $clients[0].url = 'http://api.nuget.org/test-package'
    Assert-Rejected $clients
    $clients[0].url = 'https://api.nuget.org/test-package'
    $clients[0].sourceRevision = 'main'
    Assert-Rejected $clients
    Write-Host 'Complete matrix accepted; missing targets, duplicates, bad hashes, insecure origins, and mutable source refs rejected.'
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
}
