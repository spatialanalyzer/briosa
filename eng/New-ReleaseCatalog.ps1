[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ArtifactDirectory,
    [Parameter(Mandatory)][string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReleasePackageSigning.psm1') -Force
$artifactRoot = (Resolve-Path -LiteralPath $ArtifactDirectory).Path
$catalogPath = [IO.Path]::GetFullPath($OutputPath)
$catalogDirectory = [IO.Path]::GetDirectoryName($catalogPath)
$schemaPath = Join-Path $PSScriptRoot '../schemas/releases/v1/catalog.schema.json'
$schema = Get-Content -LiteralPath $schemaPath -Raw | ConvertFrom-Json

function Get-Reference([string]$Path) {
    $file = Get-Item -LiteralPath $Path
    if ($file.PSIsContainer -or $file.Length -lt 1 -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'A catalog input must be a nonempty ordinary file.'
    }
    $relative = [IO.Path]::GetRelativePath($catalogDirectory, $file.FullName).Replace('\', '/')
    if ($relative.Length -gt 512 -or $relative -cnotmatch '^[A-Za-z0-9][A-Za-z0-9._-]*(/[A-Za-z0-9][A-Za-z0-9._-]*)*$') {
        throw 'Catalog references must stay below the output directory and use the documented mirror path syntax.'
    }
    foreach ($segment in $relative.Split('/')) {
        if ($segment.Length -gt 128 -or $segment.EndsWith('.') -or $segment.Split('.')[0] -match '^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$') {
            throw 'Catalog reference contains an unsupported Windows path segment.'
        }
    }
    if ($file.FullName -eq $catalogPath) { throw 'The catalog cannot overwrite an input artifact.' }
    return [ordered]@{
        path = $relative
        size = [long]$file.Length
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$packages = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
$provenanceFiles = @(Get-ChildItem -LiteralPath $artifactRoot -Recurse -File -Filter '*.provenance.json' |
    Where-Object Name -Match '^briosa-([0-9].*-sa-.*|installer-[0-9].*)-win-.*\.provenance\.json$')
if ($provenanceFiles.Count -eq 0) { throw 'No target-qualified server provenance files were found.' }
foreach ($file in $provenanceFiles) {
    if ($file.Length -gt 1MB) { throw 'Server provenance exceeds the 1 MiB metadata limit.' }
    $manifest = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    $isInstaller = $file.Name.StartsWith('briosa-installer-', [StringComparison]::Ordinal)
    if (($isInstaller -and ($manifest.schemaVersion -ne 1 -or $manifest.component -cne 'installer')) -or
        (-not $isInstaller -and $manifest.schemaVersion -notin @(2, 3)) -or $manifest.briosaVersion -isnot [string] -or
        $manifest.briosaVersion.Length -gt 128 -or $manifest.briosaVersion -cnotmatch $schema.'$defs'.version.pattern -or
        (-not $isInstaller -and $manifest.spatialAnalyzerTarget -cnotmatch $schema.'$defs'.package.properties.spatialAnalyzerTarget.pattern) -or
        $manifest.runtimeIdentifier -cnotmatch $schema.'$defs'.package.properties.runtimeIdentifier.pattern) {
        throw 'Server provenance does not match the supported product metadata contract.'
    }
    $artifactName = if ($isInstaller) { "briosa-installer-$($manifest.briosaVersion)-$($manifest.runtimeIdentifier)" }
        else { "briosa-$($manifest.briosaVersion)-sa-$($manifest.spatialAnalyzerTarget)-$($manifest.runtimeIdentifier)" }
    if (-not $isInstaller) { Assert-ServerCompatibilityMetadata $manifest }
    if ($manifest.artifactName -cne $artifactName -or $file.Name -cne "$artifactName.provenance.json") {
        throw 'Server provenance identity and artifact filename disagree.'
    }
    $zipPath = Join-Path $file.DirectoryName "$artifactName.zip"
    $reference = Get-Reference $zipPath
    $checksumPath = "$zipPath.sha256"
    $checksum = [IO.File]::ReadAllText($checksumPath).TrimEnd([char[]]"`r`n")
    if ($checksum -notmatch '^([0-9A-Fa-f]{64})  (.+)$' -or $Matches[2] -cne "$artifactName.zip" -or
        $Matches[1].ToLowerInvariant() -cne $reference.sha256) {
        throw 'A server ZIP does not match its adjacent checksum.'
    }
    $entry = [ordered]@{
        id = $artifactName
        component = $(if ($isInstaller) { 'installer' } else { 'server' })
        version = $manifest.briosaVersion
        runtimeIdentifier = $manifest.runtimeIdentifier
        artifact = $reference
        provenance = (Get-Reference $file.FullName)
    }
    if (-not $isInstaller) { $entry.spatialAnalyzerTarget = $manifest.spatialAnalyzerTarget }
    if (-not $packages.TryAdd($artifactName, $entry)) { throw 'Duplicate product identity in release inputs.' }
}
[string[]]$ids = @($packages.Keys)
[Array]::Sort($ids, [StringComparer]::Ordinal)
$catalog = [ordered]@{ schemaVersion = 1; packages = @($ids | ForEach-Object { $packages[$_] }) }
$json = ($catalog | ConvertTo-Json -Depth 12).Replace("`r`n", "`n") + "`n"
if ([Text.Encoding]::UTF8.GetByteCount($json) -gt 1MB) { throw 'The release catalog exceeds the 1 MiB limit.' }
if (-not (Test-Json -Json $json -SchemaFile $schemaPath)) { throw 'The generated catalog did not pass its schema.' }
$null = New-Item -ItemType Directory -Path $catalogDirectory -Force
$temporaryPath = "$catalogPath.$([Guid]::NewGuid().ToString('N')).tmp"
try {
    [IO.File]::WriteAllText($temporaryPath, $json, [Text.UTF8Encoding]::new($false))
    [IO.File]::Move($temporaryPath, $catalogPath, $true)
}
finally {
    if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath }
}
Write-Output "Created catalog with $($packages.Count) packages. Sign it before installation use."
