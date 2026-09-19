Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '../compatibility'
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryBase "briosa-matrix-policy-$([Guid]::NewGuid().ToString('N'))"
[IO.Directory]::CreateDirectory((Join-Path $fixture 'evidence/2000-01-01')) | Out-Null
$matrixPath = Join-Path $fixture 'matrix.json'
$reportPath = Join-Path $fixture 'evidence/2000-01-01/fixture.json'
$retainedPath = Join-Path $fixture 'retained-clients.json'
function Save-Fixture {
    $script:report | ConvertTo-Json -Depth 20 | Set-Content $reportPath -Encoding utf8
    $script:pair.evidenceSha256 = (Get-FileHash $reportPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $script:matrix | ConvertTo-Json -Depth 20 | Set-Content $matrixPath -Encoding utf8
    $script:retained | ConvertTo-Json -Depth 20 | Set-Content $retainedPath -Encoding utf8
}
function Reset-Fixture {
    $script:matrix = Get-Content (Join-Path $source 'matrix.json') -Raw | ConvertFrom-Json
    $script:pair = $script:matrix.testedPairs[0]
    $script:report = Get-Content (Join-Path $source $script:pair.evidence) -Raw | ConvertFrom-Json
    $script:matrix.testedPairs = @($script:pair)
    $script:pair.evidence = 'evidence/2000-01-01/fixture.json'
    $script:pair.clientPublished = $false
    $script:report.client.package.publishedUrl = ''
    $script:retained = @{ schemaVersion = 1; bootstrapServerVersion = '0.7.0'; clients = @() }
}
function Assert-Rejected([string]$Name, [scriptblock]$Change) {
    Reset-Fixture
    & $Change
    Save-Fixture
    $rejected = $false
    try { & (Join-Path $PSScriptRoot 'Test-CompatibilityMatrix.ps1') -CompatibilityRoot $fixture }
    catch { $rejected = $true }
    if (-not $rejected) { throw "Expected rejection: $Name" }
}
function Set-Published {
    $script:pair.clientPublished = $true
    $script:report.client.package.publishedUrl = 'https://api.nuget.org/v3-flatcontainer/test/0.2.0/test.0.2.0.nupkg'
    $script:retained.clients = @(@{
        language = $script:pair.language; target = $script:pair.target
        version = $script:pair.clientVersion; sourceRevision = $script:pair.clientSourceRevision
        sha256 = $script:pair.clientPackageSha256; url = $script:report.client.package.publishedUrl
    })
}
try {
    Reset-Fixture
    Save-Fixture
    & (Join-Path $PSScriptRoot 'Test-CompatibilityMatrix.ps1') -CompatibilityRoot $fixture
    Set-Published
    Save-Fixture
    & (Join-Path $PSScriptRoot 'Test-CompatibilityMatrix.ps1') -CompatibilityRoot $fixture
    Assert-Rejected 'development package claimed published' { $script:pair.clientPublished = $true }
    Assert-Rejected 'published origin claimed local' { $script:report.client.package.publishedUrl = 'https://example.invalid/package' }
    Assert-Rejected 'missing retained artifact' { Set-Published; $script:retained.clients = @() }
    Assert-Rejected 'different retained artifact' { Set-Published; $script:retained.clients[0].sha256 = ('a' * 64) }
    Assert-Rejected 'ambiguous retained artifact' { Set-Published; $script:retained.clients += $script:retained.clients[0] }
    Assert-Rejected 'duplicate pair' { $script:matrix.testedPairs += $script:pair }
    Assert-Rejected 'empty matrix' { $script:matrix.testedPairs = @() }
    Write-Host 'Compatibility matrix policy passed: local and published evidence, origin claims, retained identity, duplicates, and empty input.'
}
finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    if (-not $resolved.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -or $resolved -eq $temporaryBase) {
        throw 'Refusing cleanup outside the temporary fixture directory.'
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
