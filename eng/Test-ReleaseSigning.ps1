[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$root = Join-Path $temporaryBase ('briosa-signing-tests-' + [Guid]::NewGuid().ToString('N'))
$null = [IO.Directory]::CreateDirectory($root)
$global:BriosaSigningFixtureKey = [Security.Cryptography.RSA]::Create(3072)
$global:BriosaSigningWrongKey = [Security.Cryptography.RSA]::Create(3072)
$global:BriosaSigningFixtureKeyId = 'https://fixture.vault.azure.net/keys/catalog/11111111111111111111111111111111'
$global:BriosaSigningFixtureFailure = $false
$global:BriosaSigningFixtureWrongKey = $false
function Assert-Fails([scriptblock]$Action, [string]$Message) {
    $failed = $false
    try { & $Action | Out-Null } catch { $failed = $true }
    if (-not $failed) { throw $Message }
}
# This process-local fake exercises the actual CLI adapter without Azure credentials.
function global:az {
    $arguments = @($args)
    if ($global:BriosaSigningFixtureFailure) { $global:LASTEXITCODE = 1; return }
    if ($arguments[0] -ne 'keyvault' -or $arguments[1] -ne 'key' -or $arguments[2] -ne 'sign' -or
        $arguments[[Array]::IndexOf($arguments, '--algorithm') + 1] -cne 'PS256' -or
        $arguments[[Array]::IndexOf($arguments, '--id') + 1] -cne $global:BriosaSigningFixtureKeyId) { throw 'Incorrect vault signing request.' }
    $digest = [Convert]::FromBase64String($arguments[[Array]::IndexOf($arguments, '--digest') + 1])
    $signer = if ($global:BriosaSigningFixtureWrongKey) { $global:BriosaSigningWrongKey } else { $global:BriosaSigningFixtureKey }
    $signature = $signer.SignHash($digest, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)
    $global:LASTEXITCODE = 0
    @{keyId=$global:BriosaSigningFixtureKeyId;algorithm='PS256';signature=[Convert]::ToBase64String($signature).TrimEnd('=').Replace('+','-').Replace('/','_')} | ConvertTo-Json -Compress
}
try {
    $catalog = Join-Path $root 'catalog.json'
    $publicKey = Join-Path $root 'public.pem'
    [IO.File]::WriteAllText($catalog, '{"schemaVersion":1,"packages":[]}')
    [IO.File]::WriteAllText($publicKey, $global:BriosaSigningFixtureKey.ExportSubjectPublicKeyInfoPem())
    & "$PSScriptRoot/Sign-ReleaseCatalog.ps1" -CatalogPath $catalog -AzureKeyId $global:BriosaSigningFixtureKeyId -PublicKeyPath $publicKey -ValidDays 30
    & "$PSScriptRoot/Test-CatalogSignature.ps1" -CatalogPath $catalog -PublicKeyPath $publicKey
    $prior = [IO.File]::ReadAllText("$catalog.signature.json")
    $global:BriosaSigningFixtureFailure = $true
    Assert-Fails { & "$PSScriptRoot/Sign-ReleaseCatalog.ps1" -CatalogPath $catalog -AzureKeyId $global:BriosaSigningFixtureKeyId -PublicKeyPath $publicKey } 'Vault failure was accepted.'
    $global:BriosaSigningFixtureFailure = $false
    $global:BriosaSigningFixtureWrongKey = $true
    Assert-Fails { & "$PSScriptRoot/Sign-ReleaseCatalog.ps1" -CatalogPath $catalog -AzureKeyId $global:BriosaSigningFixtureKeyId -PublicKeyPath $publicKey } 'Wrong signing key was accepted.'
    $global:BriosaSigningFixtureWrongKey = $false
    if ([IO.File]::ReadAllText("$catalog.signature.json") -cne $prior) { throw 'Failed signing overwrote the previous envelope.' }
    Assert-Fails { & "$PSScriptRoot/Sign-ReleaseCatalog.ps1" -CatalogPath $catalog -AzureKeyId 'https://fixture.vault.azure.net/keys/catalog' -PublicKeyPath $publicKey } 'Unversioned vault key was accepted.'
    [IO.File]::AppendAllText($catalog, "`n")
    Assert-Fails { & "$PSScriptRoot/Test-CatalogSignature.ps1" -CatalogPath $catalog -PublicKeyPath $publicKey } 'Changed catalog bytes were accepted.'
    & "$PSScriptRoot/Sign-ReleaseCatalog.ps1" -CatalogPath $catalog -AzureKeyId $global:BriosaSigningFixtureKeyId -PublicKeyPath $publicKey -ValidDays 1 -IssuedAt ([DateTimeOffset]::UtcNow.AddDays(-2).ToUnixTimeSeconds())
    Assert-Fails { & "$PSScriptRoot/Test-CatalogSignature.ps1" -CatalogPath $catalog -PublicKeyPath $publicKey } 'Expired signature was accepted.'

    $name = 'briosa-0.0.0-signing-test-sa-2099.1.0101.1-win-x64'
    $tree = Join-Path $root "source/$name"
    $null = [IO.Directory]::CreateDirectory($tree)
    foreach ($leaf in @('Briosa.Server.exe','Briosa.Worker.exe','ThirdParty.dll')) { [IO.File]::WriteAllText((Join-Path $tree $leaf), 'Inert signing test fixture.') }
    $null = [IO.Directory]::CreateDirectory((Join-Path $tree 'support files'))
    [IO.File]::WriteAllText((Join-Path $tree 'support files/readme.txt'), 'Nested archive fixture.')
    $manifest = @{schemaVersion=2;artifactName=$name;briosaVersion='0.0.0-signing-test';spatialAnalyzerTarget='2099.1.0101.1';runtimeIdentifier='win-x64'} | ConvertTo-Json
    [IO.File]::WriteAllText((Join-Path $tree 'manifest.json'), $manifest)
    $lines = Get-ChildItem $tree -File -Recurse | ForEach-Object { "$((Get-FileHash -LiteralPath $_.FullName).Hash)  $([IO.Path]::GetRelativePath($tree, $_.FullName).Replace('\', '/'))" }
    [IO.File]::WriteAllText((Join-Path $tree 'files.sha256'), ($lines -join "`n") + "`n")
    $zip = Join-Path $root "$name.zip"
    [IO.Compression.ZipFile]::CreateFromDirectory((Split-Path -Parent $tree), $zip)
    [IO.File]::WriteAllText("$zip.sha256", "$((Get-FileHash -LiteralPath $zip).Hash)  $name.zip`n")
    [IO.File]::WriteAllText((Join-Path $root "$name.provenance.json"), $manifest)
    $staging = Join-Path $root 'staging'
    & "$PSScriptRoot/Prepare-SignedPackage.ps1" -PackagePath $zip -OutputDirectory $staging
    $selected = @(Get-Content (Join-Path $staging 'authenticode-files.txt'))
    if ($selected.Count -ne 2 -or ($selected -match 'ThirdParty')) { throw 'Signing included third-party content or omitted entry points.' }
    Assert-Fails { & "$PSScriptRoot/Complete-SignedPackage.ps1" -PackageRoot (Join-Path $staging $name) -OutputDirectory (Join-Path $root 'signed') -ExpectedPublisher 'Fixture' } 'Unsigned product could be finalized.'
    # Exercise archive finalization separately from production certificate access.
    Import-Module "$PSScriptRoot/ReleasePackageSigning.psm1" -Force
    $repacked = Join-Path $root "repacked/$name.zip"
    $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $repacked))
    New-ReleaseArchive -PackageRoot $tree -ArchivePath $repacked
    $archive = [IO.Compression.ZipFile]::OpenRead($repacked)
    try {
        if ($archive.Entries.Count -ne 6) { throw 'Repacked archive omitted files.' }
        foreach ($entry in $archive.Entries) {
            if ($entry.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss') -cne '1980-01-01 00:00:00') { throw 'Archive timestamp is not normalized.' }
        }
        if (-not $archive.GetEntry("$name/support files/readme.txt")) { throw 'Nested archive path was not preserved.' }
    } finally { $archive.Dispose() }
    $repackedHash = (Get-FileHash -LiteralPath $repacked).Hash
    Assert-Fails { New-ReleaseArchive -PackageRoot $tree -ArchivePath $repacked } 'Existing archive was overwritten.'
    if ((Get-FileHash -LiteralPath $repacked).Hash -cne $repackedHash) { throw 'Existing archive changed after rejected overwrite.' }
    [IO.File]::WriteAllText("$repacked.sha256", "$repackedHash  $name.zip`n")
    [IO.File]::WriteAllText((Join-Path (Split-Path -Parent $repacked) "$name.provenance.json"), $manifest)
    & "$PSScriptRoot/Prepare-SignedPackage.ps1" -PackagePath $repacked -OutputDirectory (Join-Path $root 'repacked-verification')
    [IO.File]::AppendAllText((Join-Path $staging "$name/Briosa.Server.exe"), 'corruption')
    Import-Module "$PSScriptRoot/ReleasePackageSigning.psm1" -Force
    Assert-Fails { Assert-ReleaseChecksums (Join-Path $staging $name) } 'Corrupt internal payload accepted.'
    $malicious = Join-Path $root "bad/$name.zip"
    $null = [IO.Directory]::CreateDirectory((Split-Path -Parent $malicious))
    $archive = [IO.Compression.ZipFile]::Open($malicious, [IO.Compression.ZipArchiveMode]::Create)
    try { $null = $archive.CreateEntry("$name/../../escape.txt") } finally { $archive.Dispose() }
    [IO.File]::WriteAllText("$malicious.sha256", "$((Get-FileHash -LiteralPath $malicious).Hash)  $name.zip`n")
    Assert-Fails { & "$PSScriptRoot/Prepare-SignedPackage.ps1" -PackagePath $malicious -OutputDirectory (Join-Path $root 'bad-staging') } 'Archive traversal was accepted.'
    if (Test-Path -LiteralPath (Join-Path $root 'escape.txt')) { throw 'Unsafe archive wrote outside staging.' }
    Write-Host 'Signing adapter, pinned-key verification, failure preservation, expiry, checksums, archive round-trip and containment, and unsigned-release rejection passed.'
} finally {
    Remove-Item Function:\az
    $global:BriosaSigningFixtureKey.Dispose()
    $global:BriosaSigningWrongKey.Dispose()
    $resolvedRoot = [IO.Path]::GetFullPath($root)
    if (-not $resolvedRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
}
