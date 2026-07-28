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
$deterministicZipScript = Join-Path $PSScriptRoot "New-DeterministicZip.ps1"
$coveragePath = Join-Path $repositoryRoot "generated\catalog\sa\2026.1.0529.7\coverage.json"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-protocol-test-$([Guid]::NewGuid().ToString('N'))"
$firstOutput = [IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot)
$secondOutput = Join-Path $temporaryRoot "second"
$extractRoot = Join-Path $temporaryRoot "extracted"
$bufCommand = Get-Command -Name $BufPath -CommandType Application -ErrorAction Stop

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
    $bytes = [Text.Encoding]::UTF8.GetBytes($canonical)
    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

$sourceRevision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Could not determine a complete source revision."
}

$coverage = Get-Content -LiteralPath $coveragePath -Raw | ConvertFrom-Json
$artifactBase = "briosa-protocol-$Version-sa-$($coverage.spatial_analyzer_target)-catalog-$($coverage.catalog_revision)"
$zipName = "$artifactBase.zip"
$firstZip = Join-Path $firstOutput $zipName
$secondZip = Join-Path $secondOutput $zipName

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

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipArchive = [IO.Compression.ZipFile]::OpenRead($firstZip)
    try {
        [string[]]$entryNames = @($zipArchive.Entries.FullName)
        [string[]]$sortedEntryNames = @($entryNames)
        [Array]::Sort($sortedEntryNames, [StringComparer]::Ordinal)
        Assert-Condition `
            -Condition (-not (Compare-Object $entryNames $sortedEntryNames -SyncWindow 0)) `
            -Message "Protocol ZIP entries are not in ordinal path order."
        foreach ($entry in $zipArchive.Entries) {
            Assert-Condition `
                -Condition ($entry.CompressedLength -eq $entry.Length) `
                -Message "Protocol ZIP entries must use the stored representation."
            Assert-Condition `
                -Condition ($entry.LastWriteTime.Year -eq 1980 -and
                    $entry.LastWriteTime.Month -eq 1 -and
                    $entry.LastWriteTime.Day -eq 1 -and
                    $entry.LastWriteTime.TimeOfDay -eq [TimeSpan]::Zero) `
                -Message "A protocol ZIP entry does not use the fixed timestamp."
        }
    }
    finally {
        $zipArchive.Dispose()
    }

    $externalChecksumPath = "$firstZip.sha256"
    $externalChecksum = Get-Content -LiteralPath $externalChecksumPath -Raw
    Assert-Condition `
        -Condition ($externalChecksum.Trim() -eq "$($firstHash.ToLowerInvariant())  $zipName") `
        -Message "The external protocol ZIP checksum does not match."

    Expand-Archive -LiteralPath $firstZip -DestinationPath $extractRoot
    $bundleRoot = Join-Path $extractRoot $artifactBase
    Assert-Condition `
        -Condition (Test-Path -LiteralPath $bundleRoot -PathType Container) `
        -Message "The protocol archive does not contain the expected root."

    $manifestPath = Join-Path $bundleRoot "manifest.json"
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    Assert-Condition -Condition ($manifest.schema_version -eq 1) -Message "The protocol manifest schema version is incorrect."
    Assert-Condition -Condition ($manifest.artifact_kind -eq "briosa_protocol") -Message "The protocol artifact kind is incorrect."
    Assert-Condition -Condition ($manifest.artifact_name -eq $artifactBase) -Message "The protocol artifact name is incorrect."
    Assert-Condition -Condition ($manifest.briosa_version -eq $Version) -Message "The protocol Briosa version is incorrect."
    Assert-Condition -Condition ($manifest.source_revision -eq $sourceRevision.ToLowerInvariant()) -Message "The protocol source revision is incorrect."
    Assert-Condition -Condition ($manifest.spatial_analyzer_target -eq "2026.1.0529.7") -Message "The protocol exact SA target is incorrect."
    Assert-Condition -Condition ($manifest.core_protocol_package -eq "briosa.core.v1alpha1") -Message "The core protocol package is incorrect."
    Assert-Condition -Condition ($manifest.target_protocol_package -eq $coverage.target_protocol_package) -Message "The target protocol package is incorrect."
    Assert-Condition -Condition ($manifest.catalog_id -eq $coverage.catalog_id) -Message "The catalog ID is incorrect."
    Assert-Condition -Condition ($manifest.catalog_revision -eq $coverage.catalog_revision) -Message "The catalog revision is incorrect."
    Assert-Condition -Condition ($manifest.client_generation_contract_version -eq 1) -Message "The client-generation contract version is incorrect."
    Assert-Condition -Condition ($manifest.conformance_fixture_sets.Count -eq 2) -Message "The protocol artifact must identify both fixture sets."

    $manifestFiles = @($manifest.files | Sort-Object path)
    $actualContentFiles = @(
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
        -Condition ($manifestFiles.Count -eq $actualContentFiles.Count) `
        -Message "The protocol manifest file count is incorrect."
    for ($index = 0; $index -lt $manifestFiles.Count; $index++) {
        Assert-Condition `
            -Condition ($manifestFiles[$index].path -eq $actualContentFiles[$index].path -and
                $manifestFiles[$index].sha256 -eq $actualContentFiles[$index].sha256) `
            -Message "The protocol manifest file list or hash is stale."
    }

    $protocolFiles = @($manifestFiles | Where-Object {
        $_.path -eq "buf.yaml" -or $_.path.StartsWith("proto/", [StringComparison]::Ordinal)
    })
    $conformanceFiles = @($manifestFiles | Where-Object {
        $_.path.StartsWith("conformance/", [StringComparison]::Ordinal)
    })
    Assert-Condition `
        -Condition ($manifest.protocol_schema_sha256 -eq
            (Get-AggregateFingerprint -Files $protocolFiles)) `
        -Message "The protocol schema fingerprint is stale."
    Assert-Condition `
        -Condition ($manifest.conformance_fixture_sha256 -eq
            (Get-AggregateFingerprint -Files $conformanceFiles)) `
        -Message "The conformance fixture fingerprint is stale."

    $checksumRoot = Join-Path $bundleRoot "files.sha256"
    foreach ($line in Get-Content -LiteralPath $checksumRoot) {
        $match = [regex]::Match($line, '^([0-9A-Fa-f]{64})  (.+)$')
        Assert-Condition -Condition $match.Success -Message "Malformed protocol files.sha256 entry."
        $filePath = Join-Path $bundleRoot $match.Groups[2].Value
        Assert-Condition -Condition (Test-Path -LiteralPath $filePath -PathType Leaf) -Message "A protocol checksum entry is missing."
        $actualHash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
        Assert-Condition -Condition ($actualHash -eq $match.Groups[1].Value) -Message "A protocol internal checksum does not match."
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
    $descriptorHash = (Get-FileHash -LiteralPath $descriptorPath -Algorithm SHA256).Hash
    $rebuiltHash = (Get-FileHash -LiteralPath $rebuiltDescriptor -Algorithm SHA256).Hash
    Assert-Condition -Condition ($descriptorHash -eq $rebuiltHash) -Message "The bundled descriptor does not match the bundled sources."
    Assert-Condition -Condition ($manifest.descriptor_set_sha256 -eq $descriptorHash.ToLowerInvariant()) -Message "The descriptor manifest hash is stale."

    $liveFixturePath = Join-Path $bundleRoot "conformance\v1\live-scenarios.json"
    $liveFixtures = Get-Content -LiteralPath $liveFixturePath -Raw | ConvertFrom-Json
    $expectedLiveIds = @(
        "cancellation",
        "deadline",
        "mp-failure",
        "output-failure",
        "policy-denied",
        "ready",
        "unavailable",
        "unsupported-version",
        "watchdog-recovery")
    $actualLiveIds = @($liveFixtures.scenarios.id | Sort-Object)
    Assert-Condition `
        -Condition (-not (Compare-Object $expectedLiveIds $actualLiveIds)) `
        -Message "The live client conformance scenarios are incomplete."

    $errorFixturePath = Join-Path $bundleRoot "conformance\v1\operation-error-cases.json"
    $errorFixtures = Get-Content -LiteralPath $errorFixturePath -Raw | ConvertFrom-Json
    Assert-Condition `
        -Condition ($errorFixtures.cases.Count -ge 7) `
        -Message "The typed-error fixture set is incomplete."
    Assert-Condition `
        -Condition ($errorFixtures.cases.client_behavior.automatic_replay -notcontains $true) `
        -Message "A client conformance case permits automatic replay."

    $externalProvenancePath = Join-Path $firstOutput "$artifactBase.provenance.json"
    $manifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash
    $provenanceHash = (Get-FileHash -LiteralPath $externalProvenancePath -Algorithm SHA256).Hash
    Assert-Condition -Condition ($manifestHash -eq $provenanceHash) -Message "The external protocol provenance manifest does not match the archive."

    $windowsPowerShell = Get-Command -Name "powershell.exe" -CommandType Application -ErrorAction SilentlyContinue
    if ($null -ne $windowsPowerShell) {
        $windowsPowerShellZip = Join-Path $temporaryRoot "windows-powershell.zip"
        & $windowsPowerShell.Source `
            -NoLogo `
            -NoProfile `
            -NonInteractive `
            -ExecutionPolicy Bypass `
            -File $deterministicZipScript `
            -Source $bundleRoot `
            -Destination $windowsPowerShellZip `
            -RootName $artifactBase
        Assert-Condition `
            -Condition ($LASTEXITCODE -eq 0) `
            -Message "Windows PowerShell could not rebuild the deterministic protocol ZIP."

        $windowsPowerShellHash = (Get-FileHash `
            -LiteralPath $windowsPowerShellZip `
            -Algorithm SHA256).Hash
        Assert-Condition `
            -Condition ($firstHash -eq $windowsPowerShellHash) `
            -Message "PowerShell runtimes produced different protocol ZIP SHA-256 hashes."
    }

    Write-Host "Protocol artifact cross-runtime reproducibility, descriptors, manifests, checksums, and fixtures passed."
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
