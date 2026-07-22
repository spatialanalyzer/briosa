[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$SourceRevision,

    [string]$OutputDirectory = "artifacts",

    [string]$Configuration = "Release",

    [switch]$NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$serverProject = Join-Path $repositoryRoot "src\Briosa.Server\Briosa.Server.csproj"
$workerProject = Join-Path $repositoryRoot "src\Briosa.Worker\Briosa.Worker.csproj"
$coveragePath = Join-Path $repositoryRoot "generated\catalog\sa\2026.1.0529.7\coverage.json"
$interopRoot = Join-Path $repositoryRoot "interop\SpatialAnalyzer\2026.1.0529.7"
$interopProvenancePath = Join-Path $interopRoot "Briosa.SpatialAnalyzer.Interop.provenance.json"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-package-$([Guid]::NewGuid().ToString('N'))"
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot)

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Copy-PublishTree {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse |
        Where-Object Extension -NE ".pdb" |
        Sort-Object FullName) {
        $relativePath = [IO.Path]::GetRelativePath($Source, $file.FullName)
        $targetPath = Join-Path $Destination $relativePath
        $targetDirectory = Split-Path -Parent $targetPath
        [IO.Directory]::CreateDirectory($targetDirectory) | Out-Null
        if (Test-Path -LiteralPath $targetPath) {
            $sourceHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            $targetHash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash
            if ($sourceHash -ne $targetHash) {
                throw "Publish outputs disagree for shared file '$relativePath'."
            }

            continue
        }

        Copy-Item -LiteralPath $file.FullName -Destination $targetPath
    }
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content
    )

    [IO.File]::WriteAllText(
        $Path,
        $Content,
        [Text.UTF8Encoding]::new($false))
}

function New-DeterministicZip {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [Parameter(Mandatory)][string]$RootName
    )

    Add-Type -AssemblyName System.IO.Compression
    $stream = [IO.File]::Create($Destination)
    try {
        $archive = [IO.Compression.ZipArchive]::new(
            $stream,
            [IO.Compression.ZipArchiveMode]::Create,
            $false)
        try {
            $fixedTimestamp = [DateTimeOffset]::new(
                1980,
                1,
                1,
                0,
                0,
                0,
                [TimeSpan]::Zero)
            foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse |
                Sort-Object { [IO.Path]::GetRelativePath($Source, $_.FullName) }) {
                $relativePath = ([IO.Path]::GetRelativePath($Source, $file.FullName)).Replace('\', '/')
                $entry = $archive.CreateEntry(
                    "$RootName/$relativePath",
                    [IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = $fixedTimestamp
                $input = [IO.File]::OpenRead($file.FullName)
                try {
                    $output = $entry.Open()
                    try {
                        $input.CopyTo($output)
                    }
                    finally {
                        $output.Dispose()
                    }
                }
                finally {
                    $input.Dispose()
                }
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

if ([string]::IsNullOrWhiteSpace($SourceRevision)) {
    $SourceRevision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $SourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
        throw "Could not determine a complete source revision."
    }
}

$coverage = Get-Content -LiteralPath $coveragePath -Raw | ConvertFrom-Json
$interopProvenance = Get-Content -LiteralPath $interopProvenancePath -Raw | ConvertFrom-Json
$targetVersion = [string]$coverage.spatial_analyzer_target
$artifactBase = "briosa-$Version-sa-$targetVersion-win-x64"
$zipPath = Join-Path $outputRoot "$artifactBase.zip"
$zipChecksumPath = "$zipPath.sha256"
$externalProvenancePath = Join-Path $outputRoot "$artifactBase.provenance.json"

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
try {
    $serverOutput = Join-Path $temporaryRoot "server"
    $workerOutput = Join-Path $temporaryRoot "worker"
    $packageRoot = Join-Path $temporaryRoot "package"
    [IO.Directory]::CreateDirectory($serverOutput) | Out-Null
    [IO.Directory]::CreateDirectory($workerOutput) | Out-Null
    [IO.Directory]::CreateDirectory($packageRoot) | Out-Null

    if (-not $NoRestore) {
        Invoke-DotNet @(
            "restore", $serverProject, "--locked-mode", "-r", "win-x64")
        Invoke-DotNet @(
            "restore", $workerProject, "--locked-mode", "-r", "win-x64")
    }

    $publishProperties = @(
        "-p:Version=$Version",
        "-p:InformationalVersion=$Version",
        "-p:SourceRevisionId=$SourceRevision",
        "-p:IncludeSourceRevisionInInformationalVersion=false",
        "-p:ContinuousIntegrationBuild=true",
        "-p:DebugSymbols=false",
        "-p:DebugType=None",
        "-p:PublishSingleFile=false",
        "-p:PublishTrimmed=false"
    )
    $serverPublishArguments = @(
        "publish", $serverProject,
        "-c", $Configuration,
        "-r", "win-x64",
        "--self-contained", "true",
        "--no-restore",
        "-o", $serverOutput
    ) + $publishProperties
    Invoke-DotNet $serverPublishArguments
    $workerPublishArguments = @(
        "publish", $workerProject,
        "-c", $Configuration,
        "-r", "win-x64",
        "--self-contained", "true",
        "--no-restore",
        "-o", $workerOutput
    ) + $publishProperties
    Invoke-DotNet $workerPublishArguments

    Copy-PublishTree $serverOutput $packageRoot
    Copy-PublishTree $workerOutput $packageRoot
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "LICENSE") `
        -Destination (Join-Path $packageRoot "LICENSE.txt")
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "docs\operations\windows-package.md") `
        -Destination (Join-Path $packageRoot "README.md")
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "docs\operations\health-and-discovery.md") `
        -Destination (Join-Path $packageRoot "HEALTH-AND-DISCOVERY.md")

    $metadataRoot = Join-Path $packageRoot "metadata"
    [IO.Directory]::CreateDirectory($metadataRoot) | Out-Null
    Copy-Item -LiteralPath $coveragePath `
        -Destination (Join-Path $metadataRoot "catalog-coverage.json")
    Copy-Item -LiteralPath $interopProvenancePath `
        -Destination (Join-Path $metadataRoot "interop-provenance.json")

    $manifest = [ordered]@{
        schemaVersion = 1
        artifactName = $artifactBase
        briosaVersion = $Version
        sourceRevision = $SourceRevision.ToLowerInvariant()
        runtimeIdentifier = "win-x64"
        selfContained = $true
        trimmed = $false
        catalogId = [string]$coverage.catalog_id
        catalogRevision = [string]$coverage.catalog_revision
        supportedSpatialAnalyzerReleases = @($targetVersion)
        coreProtocolPackage = "briosa.core.v1alpha1"
        targetProtocolPackage = [string]$coverage.target_protocol_package
        interopFingerprint = "sha256:$($interopProvenance.artifact.canonicalApiSha256)"
        spatialAnalyzerBundled = $false
        spatialAnalyzerLicenseRequired = $true
    }
    $manifestPath = Join-Path $packageRoot "manifest.json"
    Write-Utf8File $manifestPath (($manifest | ConvertTo-Json -Depth 10) + "`n")

    $fileChecksumPath = Join-Path $packageRoot "files.sha256"
    $fileChecksums = Get-ChildItem -LiteralPath $packageRoot -File -Recurse |
        Where-Object FullName -NE $fileChecksumPath |
        ForEach-Object {
            $relativePath = ([IO.Path]::GetRelativePath($packageRoot, $_.FullName)).Replace('\', '/')
            $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            "$hash  $relativePath"
        } |
        Sort-Object
    Write-Utf8File $fileChecksumPath (($fileChecksums -join "`n") + "`n")

    foreach ($outputPath in @($zipPath, $zipChecksumPath, $externalProvenancePath)) {
        if (Test-Path -LiteralPath $outputPath) {
            Remove-Item -LiteralPath $outputPath -Force
        }
    }

    New-DeterministicZip $packageRoot $zipPath $artifactBase
    $zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    Write-Utf8File $zipChecksumPath "$zipHash  $([IO.Path]::GetFileName($zipPath))`n"
    Copy-Item -LiteralPath $manifestPath -Destination $externalProvenancePath

    Write-Host "Created $zipPath"
    Write-Host "Created $zipChecksumPath"
    Write-Host "Created $externalProvenancePath"
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
