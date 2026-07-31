[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$SourceRevision,

    [string]$OutputDirectory = "artifacts",

    [string]$BufPath = "buf"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$monorepoRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot "..\.."))
$deterministicZipScript = Join-Path $PSScriptRoot "New-DeterministicZip.ps1"
$protoRoot = Join-Path $repositoryRoot "proto"
$targetVersion = "2026.1.0529.7"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-protocol-$([Guid]::NewGuid().ToString('N'))"
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot)
$bufCommand = Get-Command -Name $BufPath -CommandType Application -ErrorAction Stop

function Write-Utf8File {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content
    )

    [IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $Content, [Text.UTF8Encoding]::new($false))
}

function Copy-NormalizedTextFile {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    Write-Utf8File `
        -Path $Destination `
        -Content ([IO.File]::ReadAllText($Source).Replace("`r`n", "`n"))
}

function Copy-NormalizedTextTree {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse |
        Sort-Object FullName) {
        $relativePath = [IO.Path]::GetRelativePath($Source, $file.FullName)
        Copy-NormalizedTextFile `
            -Source $file.FullName `
            -Destination (Join-Path $Destination $relativePath)
    }
}

function Get-ContentFiles {
    param([Parameter(Mandatory)][string]$Root)

    return @(
        Get-ChildItem -LiteralPath $Root -File -Recurse |
            Where-Object Name -NotIn @("manifest.json", "files.sha256") |
            ForEach-Object {
                [ordered]@{
                    path = ([IO.Path]::GetRelativePath(
                        $Root,
                        $_.FullName)).Replace('\', '/')
                    sha256 = (Get-FileHash `
                        -LiteralPath $_.FullName `
                        -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            } |
            Sort-Object { $_.path })
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

if ([string]::IsNullOrWhiteSpace($SourceRevision)) {
    $SourceRevision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $SourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
        throw "Could not determine a complete source revision."
    }
}

$artifactBase = "briosa-protocol-$Version-sa-$targetVersion"
$zipPath = Join-Path $outputRoot "$artifactBase.zip"
$zipChecksumPath = "$zipPath.sha256"
$externalProvenancePath = Join-Path $outputRoot "$artifactBase.provenance.json"

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
try {
    $bundleRoot = Join-Path $temporaryRoot "bundle"
    [IO.Directory]::CreateDirectory($bundleRoot) | Out-Null
    Copy-NormalizedTextFile `
        -Source (Join-Path $repositoryRoot "buf.yaml") `
        -Destination (Join-Path $bundleRoot "buf.yaml")
    Copy-NormalizedTextTree `
        -Source $protoRoot `
        -Destination (Join-Path $bundleRoot "proto")
    Copy-NormalizedTextFile `
        -Source (Join-Path $repositoryRoot "docs\operations\protocol-artifacts.md") `
        -Destination (Join-Path $bundleRoot "README.md")
    Copy-NormalizedTextFile `
        -Source (Join-Path $monorepoRoot "LICENSE") `
        -Destination (Join-Path $bundleRoot "LICENSE.txt")

    $descriptorPath = Join-Path $bundleRoot "descriptor\briosa.protoset"
    [IO.Directory]::CreateDirectory((Split-Path -Parent $descriptorPath)) | Out-Null
    Push-Location $repositoryRoot
    try {
        & $bufCommand.Source build `
            --as-file-descriptor-set `
            --output $descriptorPath
        if ($LASTEXITCODE -ne 0) {
            throw "Buf descriptor generation failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }

    $contentFiles = Get-ContentFiles -Root $bundleRoot
    $protocolFiles = @($contentFiles | Where-Object {
        $_.path -eq "buf.yaml" -or
        $_.path.StartsWith("proto/", [StringComparison]::Ordinal)
    })
    $descriptorFile = $contentFiles |
        Where-Object path -EQ "descriptor/briosa.protoset"
    $manifest = [ordered]@{
        schema_version = 2
        artifact_kind = "briosa_protocol"
        artifact_name = $artifactBase
        briosa_version = $Version
        source_revision = $SourceRevision.ToLowerInvariant()
        spatial_analyzer_target = $targetVersion
        protocol_package = "briosa"
        protocol_schema_sha256 = Get-AggregateFingerprint -Files $protocolFiles
        descriptor_set_sha256 = [string]$descriptorFile.sha256
        client_generation_contract = "standard-protobuf-grpc"
        files = @($contentFiles)
    }
    $manifestPath = Join-Path $bundleRoot "manifest.json"
    Write-Utf8File `
        -Path $manifestPath `
        -Content (($manifest | ConvertTo-Json -Depth 10) + "`n")

    $fileChecksumPath = Join-Path $bundleRoot "files.sha256"
    $fileChecksums = Get-ChildItem -LiteralPath $bundleRoot -File -Recurse |
        Where-Object FullName -NE $fileChecksumPath |
        ForEach-Object {
            $relativePath = ([IO.Path]::GetRelativePath(
                $bundleRoot,
                $_.FullName)).Replace('\', '/')
            $hash = (Get-FileHash `
                -LiteralPath $_.FullName `
                -Algorithm SHA256).Hash.ToLowerInvariant()
            "$hash  $relativePath"
        } |
        Sort-Object
    Write-Utf8File `
        -Path $fileChecksumPath `
        -Content (($fileChecksums -join "`n") + "`n")

    foreach ($outputPath in @(
        $zipPath,
        $zipChecksumPath,
        $externalProvenancePath)) {
        if (Test-Path -LiteralPath $outputPath) {
            Remove-Item -LiteralPath $outputPath -Force
        }
    }

    & $deterministicZipScript `
        -Source $bundleRoot `
        -Destination $zipPath `
        -RootName $artifactBase
    $zipHash = (Get-FileHash `
        -LiteralPath $zipPath `
        -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8File `
        -Path $zipChecksumPath `
        -Content "$zipHash  $([IO.Path]::GetFileName($zipPath))`n"
    Copy-Item -LiteralPath $manifestPath -Destination $externalProvenancePath

    Write-Host "Created $zipPath"
    Write-Host "Created $zipChecksumPath"
    Write-Host "Created $externalProvenancePath"
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
