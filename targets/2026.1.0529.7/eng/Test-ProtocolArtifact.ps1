[CmdletBinding()]
param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version = "0.2.0-ci",

    [string]$OutputDirectory = "artifacts\protocol-smoke",

    [string]$BufPath = "buf"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$artifactScript = Join-Path $PSScriptRoot "New-ProtocolArtifact.ps1"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-protocol-test-$([Guid]::NewGuid().ToString('N'))"
$firstOutput = [IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot)
$secondOutput = Join-Path $temporaryRoot "second"
$extractRoot = Join-Path $temporaryRoot "extracted"
$bufCommand = Get-Command -Name $BufPath -CommandType Application -ErrorAction Stop
$artifactBase = "briosa-protocol-$Version-sa-2026.1.0529.7"
$zipName = "$artifactBase.zip"
$firstZip = Join-Path $firstOutput $zipName
$secondZip = Join-Path $secondOutput $zipName

function Assert-Condition {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-AggregateFingerprint {
    param([Parameter(Mandatory)][object[]]$Files)

    $canonical = (($Files | ForEach-Object {
        "$($_.sha256)  $($_.path)"
    }) -join "`n") + "`n"
    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($canonical))).ToLowerInvariant()
}

$safeRepositoryRoot = $repositoryRoot.Replace('\', '/')
$sourceRevision = (& git -c "safe.directory=$safeRepositoryRoot" `
    -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Could not determine a complete source revision."
}

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    & $artifactScript `
        -Version $Version `
        -SourceRevision $sourceRevision `
        -OutputDirectory $firstOutput `
        -BufPath $bufCommand.Source
    & $artifactScript `
        -Version $Version `
        -SourceRevision $sourceRevision `
        -OutputDirectory $secondOutput `
        -BufPath $bufCommand.Source

    $firstHash = (Get-FileHash -LiteralPath $firstZip -Algorithm SHA256).Hash
    $secondHash = (Get-FileHash -LiteralPath $secondZip -Algorithm SHA256).Hash
    Assert-Condition `
        -Condition ($firstHash -eq $secondHash) `
        -Message "Two protocol artifact builds produced different SHA-256 hashes."

    $externalChecksum = Get-Content -LiteralPath "$firstZip.sha256" -Raw
    Assert-Condition `
        -Condition ($externalChecksum.Trim() -eq
            "$($firstHash.ToLowerInvariant())  $zipName") `
        -Message "The external protocol ZIP checksum does not match."

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipArchive = [IO.Compression.ZipFile]::OpenRead($firstZip)
    try {
        [string[]]$entryNames = @($zipArchive.Entries.FullName)
        [string[]]$sortedEntryNames = @($entryNames)
        [Array]::Sort($sortedEntryNames, [StringComparer]::Ordinal)
        Assert-Condition `
            -Condition (-not (Compare-Object `
                $entryNames $sortedEntryNames -SyncWindow 0)) `
            -Message "Protocol ZIP entries are not in ordinal path order."
        foreach ($entry in $zipArchive.Entries) {
            Assert-Condition `
                -Condition ($entry.CompressedLength -eq $entry.Length) `
                -Message "Protocol ZIP entries must use the stored representation."
            Assert-Condition `
                -Condition ($entry.LastWriteTime.DateTime -eq
                    [DateTime]::new(
                        1980,
                        1,
                        1,
                        0,
                        0,
                        0,
                        [DateTimeKind]::Unspecified)) `
                -Message "A protocol ZIP entry does not use the fixed timestamp."
        }
    }
    finally {
        $zipArchive.Dispose()
    }

    Expand-Archive -LiteralPath $firstZip -DestinationPath $extractRoot
    $bundleRoot = Join-Path $extractRoot $artifactBase
    $manifestPath = Join-Path $bundleRoot "manifest.json"
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    Assert-Condition -Condition ($manifest.schema_version -eq 2) `
        -Message "The protocol manifest schema version is incorrect."
    Assert-Condition -Condition ($manifest.artifact_kind -eq "briosa_protocol") `
        -Message "The protocol artifact kind is incorrect."
    Assert-Condition -Condition ($manifest.artifact_name -eq $artifactBase) `
        -Message "The protocol artifact name is incorrect."
    Assert-Condition -Condition ($manifest.briosa_version -eq $Version) `
        -Message "The protocol Briosa version is incorrect."
    Assert-Condition `
        -Condition ($manifest.source_revision -eq
            $sourceRevision.ToLowerInvariant()) `
        -Message "The protocol source revision is incorrect."
    Assert-Condition `
        -Condition ($manifest.spatial_analyzer_target -eq "2026.1.0529.7") `
        -Message "The protocol exact SA target is incorrect."
    Assert-Condition -Condition ($manifest.protocol_package -eq "briosa") `
        -Message "The protocol package is incorrect."
    Assert-Condition `
        -Condition ($null -eq $manifest.PSObject.Properties["core_protocol_package"] -and
            $null -eq $manifest.PSObject.Properties["target_protocol_package"]) `
        -Message "A retired versioned protocol package leaked into the artifact."
    Assert-Condition `
        -Condition ($manifest.client_generation_contract -eq
            "standard-protobuf-grpc") `
        -Message "The client generation contract is incorrect."
    Assert-Condition `
        -Condition ($null -eq $manifest.PSObject.Properties["catalog_revision"]) `
        -Message "The retired catalog revision leaked into the protocol artifact."

    $manifestFiles = @($manifest.files | Sort-Object path)
    $actualFiles = @(
        Get-ChildItem -LiteralPath $bundleRoot -File -Recurse |
            Where-Object Name -NotIn @("manifest.json", "files.sha256") |
            ForEach-Object {
                [pscustomobject]@{
                    path = ([IO.Path]::GetRelativePath(
                        $bundleRoot,
                        $_.FullName)).Replace('\', '/')
                    sha256 = (Get-FileHash `
                        -LiteralPath $_.FullName `
                        -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            } |
            Sort-Object path)
    Assert-Condition `
        -Condition ($manifestFiles.Count -eq $actualFiles.Count) `
        -Message "The protocol manifest file count is incorrect."
    for ($index = 0; $index -lt $manifestFiles.Count; $index++) {
        Assert-Condition `
            -Condition (
                $manifestFiles[$index].path -eq $actualFiles[$index].path -and
                $manifestFiles[$index].sha256 -eq $actualFiles[$index].sha256) `
            -Message "The protocol manifest file list or hash is stale."
    }

    $protocolFiles = @($manifestFiles | Where-Object {
        $_.path -eq "buf.yaml" -or
        $_.path.StartsWith("proto/", [StringComparison]::Ordinal)
    })
    Assert-Condition `
        -Condition ($manifest.protocol_schema_sha256 -eq
            (Get-AggregateFingerprint -Files $protocolFiles)) `
        -Message "The protocol schema fingerprint is stale."

    foreach ($line in Get-Content -LiteralPath (
        Join-Path $bundleRoot "files.sha256")) {
        $match = [regex]::Match($line, '^([0-9A-Fa-f]{64})  (.+)$')
        Assert-Condition -Condition $match.Success `
            -Message "Malformed protocol files.sha256 entry."
        $filePath = Join-Path $bundleRoot $match.Groups[2].Value
        Assert-Condition `
            -Condition ((Get-FileHash `
                -LiteralPath $filePath `
                -Algorithm SHA256).Hash -eq $match.Groups[1].Value) `
            -Message "A protocol internal checksum does not match."
    }

    $rebuiltDescriptor = Join-Path $temporaryRoot "rebuilt.protoset"
    Push-Location $bundleRoot
    try {
        & $bufCommand.Source build `
            --as-file-descriptor-set `
            --output $rebuiltDescriptor
        if ($LASTEXITCODE -ne 0) {
            throw "Bundled protocol descriptor rebuild failed."
        }
    }
    finally {
        Pop-Location
    }
    $descriptorPath = Join-Path $bundleRoot "descriptor\briosa.protoset"
    $descriptorHash = (Get-FileHash `
        -LiteralPath $descriptorPath `
        -Algorithm SHA256).Hash
    $rebuiltHash = (Get-FileHash `
        -LiteralPath $rebuiltDescriptor `
        -Algorithm SHA256).Hash
    Assert-Condition -Condition ($descriptorHash -eq $rebuiltHash) `
        -Message "The bundled descriptor does not match its sources."
    Assert-Condition `
        -Condition ($manifest.descriptor_set_sha256 -eq
            $descriptorHash.ToLowerInvariant()) `
        -Message "The descriptor manifest hash is stale."

    $externalProvenancePath =
        Join-Path $firstOutput "$artifactBase.provenance.json"
    Assert-Condition `
        -Condition ((Get-FileHash `
            -LiteralPath $manifestPath `
            -Algorithm SHA256).Hash -eq
            (Get-FileHash `
                -LiteralPath $externalProvenancePath `
                -Algorithm SHA256).Hash) `
        -Message "The external protocol provenance does not match the archive."

    Write-Host "Protocol artifact reproducibility, descriptors, manifests, and checksums passed."
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith(
            $temporaryBase,
            [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
