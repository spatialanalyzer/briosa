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

$targetRoot = Split-Path -Parent $PSScriptRoot
$targetVersion = "2026.1.0529.7"
$contractRoot = Join-Path $targetRoot "conformance\client\v1"
$workerProject = Join-Path $targetRoot "tests\Briosa.SmokeWorker\Briosa.SmokeWorker.csproj"
$windowsPackageScript = Join-Path $PSScriptRoot "New-WindowsPackage.ps1"
$zipScript = Join-Path $PSScriptRoot "New-DeterministicZip.ps1"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-client-conformance-$([Guid]::NewGuid().ToString('N'))"
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory, $targetRoot)

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content
    )

    [IO.File]::WriteAllText($Path, $Content, [Text.UTF8Encoding]::new($false))
}

function Copy-Tree {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse |
        Where-Object Extension -NE ".pdb" |
        Sort-Object FullName) {
        $relativePath = [IO.Path]::GetRelativePath($Source, $file.FullName)
        $destinationPath = Join-Path $Destination $relativePath
        [IO.Directory]::CreateDirectory((Split-Path -Parent $destinationPath)) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destinationPath
    }
}

if ([string]::IsNullOrWhiteSpace($SourceRevision)) {
    $SourceRevision = (& git -C $targetRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $SourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
        throw "Could not determine a complete source revision."
    }
}

$artifactBase = "briosa-client-conformance-$Version-sa-$targetVersion-win-x64"
$zipPath = Join-Path $outputRoot "$artifactBase.zip"
$zipChecksumPath = "$zipPath.sha256"
$externalProvenancePath = Join-Path $outputRoot "$artifactBase.provenance.json"

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
try {
    $serverArtifactRoot = Join-Path $temporaryRoot "server-artifact"
    $serverExtractRoot = Join-Path $temporaryRoot "server-extracted"
    $workerOutput = Join-Path $temporaryRoot "fake-worker"
    $packageRoot = Join-Path $temporaryRoot "package"
    $serverRoot = Join-Path $packageRoot "server"
    $packagedWorkerRoot = Join-Path $packageRoot "fake-worker"
    $runnerRoot = Join-Path $packageRoot "runner"
    $packagedContractRoot = Join-Path $packageRoot "contract"
    foreach ($directory in @(
        $serverArtifactRoot,
        $serverExtractRoot,
        $workerOutput,
        $serverRoot,
        $packagedWorkerRoot,
        $runnerRoot,
        $packagedContractRoot)) {
        [IO.Directory]::CreateDirectory($directory) | Out-Null
    }

    $serverPackageArguments = @{
        Version = $Version
        SourceRevision = $SourceRevision
        OutputDirectory = $serverArtifactRoot
        Configuration = $Configuration
        NoRestore = $NoRestore
    }
    & $windowsPackageScript @serverPackageArguments
    $serverZip = Join-Path $serverArtifactRoot "briosa-$Version-sa-$targetVersion-win-x64.zip"
    Expand-Archive -LiteralPath $serverZip -DestinationPath $serverExtractRoot
    $serverPackageDirectories = @(Get-ChildItem -LiteralPath $serverExtractRoot -Directory)
    if ($serverPackageDirectories.Count -ne 1) {
        throw "The server package must contain exactly one top-level directory."
    }
    Copy-Tree -Source $serverPackageDirectories[0].FullName -Destination $serverRoot

    if (-not $NoRestore) {
        Invoke-DotNet @("restore", $workerProject, "--locked-mode", "-r", "win-x64")
    }
    $workerPublishArguments = @(
        "publish", $workerProject,
        "-c", $Configuration,
        "-r", "win-x64",
        "--self-contained", "true",
        "--no-restore",
        "-o", $workerOutput,
        "-p:Version=$Version",
        "-p:InformationalVersion=$Version",
        "-p:SourceRevisionId=$SourceRevision",
        "-p:IncludeSourceRevisionInInformationalVersion=false",
        "-p:ContinuousIntegrationBuild=true",
        "-p:DebugSymbols=false",
        "-p:DebugType=None",
        "-p:PublishSingleFile=false",
        "-p:PublishTrimmed=false")
    Invoke-DotNet $workerPublishArguments
    Copy-Tree -Source $workerOutput -Destination $packagedWorkerRoot

    Copy-Item -LiteralPath (Join-Path $contractRoot "Invoke-BriosaClientConformance.ps1") `
        -Destination $runnerRoot
    Copy-Item -LiteralPath (Join-Path $contractRoot "scenarios.json") `
        -Destination $packagedContractRoot
    Copy-Item -LiteralPath (Join-Path $contractRoot "scenarios.schema.json") `
        -Destination $packagedContractRoot
    Copy-Item -LiteralPath (Join-Path $targetRoot "conformance\client\README.md") `
        -Destination (Join-Path $packageRoot "README.md")

    $manifest = [ordered]@{
        schemaVersion = 1
        artifactKind = "briosa_client_conformance"
        artifactName = $artifactBase
        briosaVersion = $Version
        sourceRevision = $SourceRevision.ToLowerInvariant()
        runtimeIdentifier = "win-x64"
        spatialAnalyzerTarget = $targetVersion
        scenarioContract = "briosa.first-party-client.v1"
        scenarioContractSchemaVersion = 1
        serverArtifact = "briosa-$Version-sa-$targetVersion-win-x64"
        spatialAnalyzerBundled = $false
        spatialAnalyzerLicenseRequired = $false
        proprietaryDataBundled = $false
    }
    $manifestPath = Join-Path $packageRoot "manifest.json"
    Write-Utf8File $manifestPath (($manifest | ConvertTo-Json -Depth 10) + "`n")

    $fileChecksumPath = Join-Path $packageRoot "files.sha256"
    $checksums = Get-ChildItem -LiteralPath $packageRoot -File -Recurse |
        Where-Object FullName -NE $fileChecksumPath |
        ForEach-Object {
            $relativePath = ([IO.Path]::GetRelativePath($packageRoot, $_.FullName)).Replace('\', '/')
            $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            "$hash  $relativePath"
        } |
        Sort-Object
    Write-Utf8File $fileChecksumPath (($checksums -join "`n") + "`n")

    foreach ($outputPath in @($zipPath, $zipChecksumPath, $externalProvenancePath)) {
        if (Test-Path -LiteralPath $outputPath) {
            Remove-Item -LiteralPath $outputPath -Force
        }
    }
    & $zipScript -Source $packageRoot -Destination $zipPath -RootName $artifactBase
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
