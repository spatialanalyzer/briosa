[CmdletBinding()]
param([string]$ConsumerCliAssembly)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$testRoot = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'Briosa.ReleaseCatalog.Tests'))
$caseRoot = [IO.Path]::GetFullPath((Join-Path $testRoot ([Guid]::NewGuid().ToString('N'))))
$null = New-Item -ItemType Directory -Path $caseRoot -Force
$artifactRoot = Join-Path $caseRoot 'artifacts'
$null = New-Item -ItemType Directory -Path $artifactRoot
$producer = Join-Path $PSScriptRoot 'New-ReleaseCatalog.ps1'

function Assert-Condition([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function New-Fixture([string]$Version, [switch]$Installer) {
    $name = if ($Installer) { "briosa-installer-$Version-win-x64" } else { "briosa-$Version-sa-2099.1.0101.1-win-x64" }
    $payload = Join-Path $caseRoot $name
    $null = New-Item -ItemType Directory -Path $payload
    [IO.File]::WriteAllText((Join-Path $payload 'README.txt'), "Invented catalog fixture $Version. No executable or vendor content.")
    $zipPath = Join-Path $artifactRoot "$name.zip"
    [IO.Compression.ZipFile]::CreateFromDirectory($payload, $zipPath)
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    [IO.File]::WriteAllText("$zipPath.sha256", "$hash  $name.zip`n")
    $manifest = [ordered]@{
        schemaVersion = 2
        artifactName = $name
        briosaVersion = $Version
        spatialAnalyzerTarget = '2099.1.0101.1'
        runtimeIdentifier = 'win-x64'
    }
    if ($Installer) {
        $manifest.schemaVersion = 1
        $manifest.Remove('spatialAnalyzerTarget')
        $manifest.component = 'installer'
    }
    [IO.File]::WriteAllText((Join-Path $artifactRoot "$name.provenance.json"), ($manifest | ConvertTo-Json))
}

try {
    New-Fixture '0.1.0-fixture.1'
    New-Fixture '0.1.1-fixture.1'
    New-Fixture '0.1.0-fixture.1' -Installer
    # This unrelated client/protocol artifact must never appear as a server.
    [IO.File]::WriteAllText((Join-Path $artifactRoot 'briosa-client-conformance-0.1.0-sa-2099.1.0101.1-win-x64.provenance.json'), '{}')
    $output = Join-Path $artifactRoot 'catalog.json'
    & $producer -ArtifactDirectory $artifactRoot -OutputPath $output
    $first = [IO.File]::ReadAllText($output)
    $parsed = $first | ConvertFrom-Json
    Assert-Condition ($parsed.packages.Count -eq 3) 'Incorrect package count.'
    $installer = @($parsed.packages | Where-Object component -EQ 'installer')
    Assert-Condition ($installer.Count -eq 1 -and -not $installer[0].PSObject.Properties['spatialAnalyzerTarget']) 'Installer identity includes an SA target.'
    foreach ($package in $parsed.packages) {
        $artifact = Join-Path $artifactRoot $package.artifact.path
        Assert-Condition ($package.artifact.sha256 -ceq (Get-FileHash -LiteralPath $artifact).Hash.ToLowerInvariant()) 'Artifact digest differs.'
    }
    & $producer -ArtifactDirectory $artifactRoot -OutputPath $output
    Assert-Condition ($first -ceq [IO.File]::ReadAllText($output)) 'Catalog generation is not deterministic.'
    $mirrorRoot = Join-Path $caseRoot 'mirror'
    $null = New-Item -ItemType Directory -Path $mirrorRoot
    Get-ChildItem -LiteralPath $artifactRoot -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $mirrorRoot }
    $mirrorCatalog = Join-Path $mirrorRoot 'catalog.json'
    & $producer -ArtifactDirectory $mirrorRoot -OutputPath $mirrorCatalog
    Assert-Condition ($first -ceq [IO.File]::ReadAllText($mirrorCatalog)) 'Relocating the mirror changes catalog bytes.'
    $key = [Security.Cryptography.RSA]::Create(3072)
    try {
        $privateFile = Join-Path $caseRoot 'temporary-fixture-private-key.pem'
        [IO.File]::WriteAllText($privateFile, $key.ExportPkcs8PrivateKeyPem())
        & (Join-Path $PSScriptRoot 'Sign-ReleaseCatalog.ps1') -CatalogPath $mirrorCatalog -PrivateKeyPath $privateFile
        $publicKey = $key.ExportSubjectPublicKeyInfoPem()
        $signatureJson = [IO.File]::ReadAllText("$mirrorCatalog.signature.json")
        Assert-Condition (Test-Json -Json $signatureJson -SchemaFile (Join-Path $PSScriptRoot '../schemas/releases/v1/catalog-signature.schema.json')) 'Invalid signature envelope.'
        $envelope = $signatureJson | ConvertFrom-Json
        $message = [Text.Encoding]::UTF8.GetBytes("Briosa release catalog signature v1`n$($envelope.catalogSha256)`n$($envelope.issuedAt)`n$($envelope.expiresAt)`n")
        Assert-Condition ($key.VerifyData($message, [Convert]::FromBase64String($envelope.signature), [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)) 'Signature verification failed.'
        $message[0] = $message[0] -bxor 1
        Assert-Condition (-not $key.VerifyData($message, [Convert]::FromBase64String($envelope.signature), [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)) 'Signature accepted modified content.'
    } finally {
        $key.Dispose()
        if (Test-Path -LiteralPath $privateFile) { Remove-Item -LiteralPath $privateFile }
    }
    if ($ConsumerCliAssembly) {
        $settingsPath = Join-Path $caseRoot 'consumer-settings.json'
        $settings = @{ schemaVersion = 1; source = @{ catalog = $mirrorCatalog } } | ConvertTo-Json -Depth 5
        [IO.File]::WriteAllText($settingsPath, $settings)
        $consumerOutput = & dotnet $ConsumerCliAssembly catalog list --component server --config $settingsPath
        Assert-Condition ($LASTEXITCODE -eq 0) 'Installer CLI rejected the generated catalog.'
        $consumer = ($consumerOutput -join "`n") | ConvertFrom-Json
        Assert-Condition ($consumer.packages.Count -eq 2 -and $consumer.publisherVerification -ceq 'notPerformed') 'Producer/consumer identity or trust interpretation differs.'
        $settings = @{ schemaVersion = 1; source = @{ catalog = $mirrorCatalog; publisherKey = $publicKey } } | ConvertTo-Json -Depth 5
        [IO.File]::WriteAllText($settingsPath, $settings)
        $consumerOutput = & dotnet $ConsumerCliAssembly catalog list --component installer --config $settingsPath
        Assert-Condition ($LASTEXITCODE -eq 0) 'Installer rejected the signed catalog.'
        $consumer = ($consumerOutput -join "`n") | ConvertFrom-Json
        Assert-Condition ($consumer.packages.Count -eq 1 -and $consumer.publisherVerification -ceq 'verified') 'Signed installer metadata interpretation differs.'
        [IO.File]::AppendAllText($mirrorCatalog, ' ')
        & dotnet $ConsumerCliAssembly catalog list --component installer --config $settingsPath 2>$null | Out-Null
        Assert-Condition ($LASTEXITCODE -eq 5) 'Installer accepted changed signed catalog bytes.'
        Write-Output 'Installer CLI consumed the generated mirror catalog without acquiring payloads.'
    }
    $rejected = $false
    try { & $producer -ArtifactDirectory $artifactRoot -OutputPath (Join-Path $artifactRoot 'nested/catalog.json') }
    catch { $rejected = $true }
    Assert-Condition $rejected 'Escaping artifact references were accepted.'
    $checksum = Get-ChildItem -LiteralPath $artifactRoot -Filter '*.sha256' -File | Select-Object -First 1
    [IO.File]::WriteAllText($checksum.FullName, ('0' * 64) + '  ' + [IO.Path]::GetFileNameWithoutExtension($checksum.Name))
    $rejected = $false
    try { & $producer -ArtifactDirectory $artifactRoot -OutputPath $output }
    catch { $rejected = $true }
    Assert-Condition $rejected 'A corrupt input checksum was accepted.'
    Assert-Condition ($first -ceq [IO.File]::ReadAllText($output)) 'A failed generation overwrote the previous catalog.'
    Write-Output 'Release catalog tests passed: server/installer identities, checksums, deterministic output, relocation, signatures/tampering, containment, and failure preservation.'
}
finally {
    $resolvedCase = [IO.Path]::GetFullPath($caseRoot)
    if (-not $resolvedCase.StartsWith($testRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the dedicated test root.'
    }
    Remove-Item -LiteralPath $resolvedCase -Recurse -Force
}
