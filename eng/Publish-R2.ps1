[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('publish', 'renew', 'check')][string]$Mode,
    [string]$WorkDirectory = 'artifacts/r2-publication',
    [string]$Python = 'python'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$work = [IO.Path]::GetFullPath($WorkDirectory)
if (Test-Path -LiteralPath $work) { throw 'Use a new publication working directory.' }
$null = New-Item -ItemType Directory -Path (Join-Path $work 'public') -Force
$catalogSource = Join-Path $PSScriptRoot 'publishing/catalog.json'
$catalogPath = Join-Path $work 'public/catalog.json'
$publicKey = Join-Path $PSScriptRoot 'signing/catalog-public.pem'
$schema = Join-Path $repository 'schemas/releases/v1/catalog.schema.json'
$config = Get-Content (Join-Path $PSScriptRoot 'signing/azure.json') -Raw | ConvertFrom-Json
if (-not (Test-Json -Json (Get-Content $catalogSource -Raw) -SchemaFile $schema)) { throw 'Invalid reviewed catalog.' }
Copy-Item -LiteralPath $catalogSource -Destination $catalogPath
$catalog = Get-Content $catalogPath -Raw | ConvertFrom-Json
if ($catalog.packages.Count -eq 0) { throw 'Refusing to publish an empty catalog.' }
function Invoke-Publisher([string]$Command) {
    & $Python (Join-Path $PSScriptRoot 'publishing/r2_publish.py') $Command --work $work --mode $(if ($Mode -eq 'renew') { 'renew' } else { 'publish' })
    if ($LASTEXITCODE -ne 0) { throw "R2 publication stage failed: $Command" }
}
if ($Mode -eq 'check') {
    Invoke-Publisher check
    & "$PSScriptRoot/Test-CatalogSignature.ps1" -CatalogPath $catalogPath -PublicKeyPath $publicKey
    $envelope = Get-Content "$catalogPath.signature.json" -Raw | ConvertFrom-Json
    if ($envelope.expiresAt - [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() -lt 7 * 86400) {
        throw 'Public catalog signature has fewer than seven days remaining.'
    }
    Write-Host 'Public catalog matches the reviewed source, verifies, bypasses cache, and has at least seven days remaining.'
    exit 0
}
Invoke-Publisher snapshot
$priorCatalog = Join-Path $work 'prior/catalog.json'
if (Test-Path -LiteralPath $priorCatalog) {
    if (-not (Test-Json -Json (Get-Content $priorCatalog -Raw) -SchemaFile $schema)) { throw 'Invalid existing catalog.' }
    # Identical reviewed bytes can recover an interrupted first publication or expired signature.
    # Different bytes must have a current valid signature before their entries are retained.
    if ($Mode -eq 'renew' -or (Get-FileHash $priorCatalog).Hash -cne (Get-FileHash $catalogPath).Hash) {
        & "$PSScriptRoot/Test-CatalogSignature.ps1" -CatalogPath $priorCatalog -PublicKeyPath $publicKey
    }
}
Invoke-Publisher guard
if ($Mode -eq 'publish') {
    foreach ($package in $catalog.packages) {
        $repo = if ($package.component -eq 'server') { 'spatialanalyzer/briosa' } else { 'spatialanalyzer/briosa-installer' }
        $tag = "v$($package.version)"
        $releaseText = & gh release view $tag --repo $repo --json tagName,isDraft,isPrerelease,publishedAt
        if ($LASTEXITCODE -ne 0) { throw 'Approved GitHub release is unavailable.' }
        $release = ($releaseText -join [Environment]::NewLine) | ConvertFrom-Json
        if ($release.tagName -cne $tag -or $release.isDraft -or $release.isPrerelease -or -not $release.publishedAt) {
            throw 'R2 publication requires a published stable GitHub release.'
        }
        $directory = Join-Path $work ("public/" + [IO.Path]::GetDirectoryName($package.artifact.path))
        $null = New-Item -ItemType Directory -Path $directory -Force
        & gh release download $tag --repo $repo --pattern "$($package.id).*" --dir $directory
        if ($LASTEXITCODE -ne 0) { throw 'Release asset download failed.' }
        $zip = Join-Path $work ("public/" + $package.artifact.path)
        $stage = Join-Path $work ("verification/" + $package.id)
        & "$PSScriptRoot/Prepare-SignedPackage.ps1" -PackagePath $zip -OutputDirectory $stage
        Import-Module "$PSScriptRoot/ReleasePackageSigning.psm1" -Force
        Assert-ReleaseSignatures -PackageRoot (Join-Path $stage $package.id) -ExpectedPublisher $config.expectedPublisher
    }
    # Prove catalog coordinates and hashes from the final downloaded release assets.
    $regenerated = Join-Path $work 'public/regenerated.json'
    & "$PSScriptRoot/New-ReleaseCatalog.ps1" -ArtifactDirectory (Join-Path $work 'public/packages') -OutputPath $regenerated
    if ((Get-FileHash $regenerated).Hash -cne (Get-FileHash $catalogPath).Hash) {
        throw 'Released assets do not reproduce the reviewed canonical catalog.'
    }
    Invoke-Publisher upload
}
if ($env:AZURE_CATALOG_KEY_ID -cne $config.catalogKeyId) { throw 'Unexpected catalog signing key version.' }
& "$PSScriptRoot/Sign-ReleaseCatalog.ps1" -CatalogPath $catalogPath -AzureKeyId $env:AZURE_CATALOG_KEY_ID -PublicKeyPath $publicKey -ValidDays 30
& "$PSScriptRoot/Test-CatalogSignature.ps1" -CatalogPath $catalogPath -PublicKeyPath $publicKey
Invoke-Publisher commit
Write-Host 'R2 publication and public metadata verification completed.'
