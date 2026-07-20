[CmdletBinding()]
param(
    [string] $SpatialAnalyzerVersion = '2026.1.0529.7',
    [switch] $NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$generatorProject = Join-Path $repositoryRoot 'tools\Briosa.Generator\Briosa.Generator.csproj'
$interopDirectory = Join-Path $repositoryRoot "interop\SpatialAnalyzer\$SpatialAnalyzerVersion"
$artifactPath = Join-Path $interopDirectory 'Briosa.SpatialAnalyzer.Interop.dll'
$apiPath = Join-Path $interopDirectory 'Briosa.SpatialAnalyzer.Interop.PublicApi.txt'
$provenancePath = Join-Path $interopDirectory 'Briosa.SpatialAnalyzer.Interop.provenance.json'

foreach ($requiredPath in @($artifactPath, $apiPath, $provenancePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required committed interop artifact is missing: $requiredPath"
    }
}

$provenance = Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json
$artifactHash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash
if ($artifactHash -ne $provenance.artifact.sha256) {
    throw 'The interop assembly SHA-256 does not match its provenance manifest.'
}

$apiHash = (Get-FileHash -LiteralPath $apiPath -Algorithm SHA256).Hash
if ($apiHash -ne $provenance.artifact.canonicalApiSha256) {
    throw 'The canonical API SHA-256 does not match its provenance manifest.'
}

$assemblyIdentity = [Reflection.AssemblyName]::GetAssemblyName($artifactPath)
if ($assemblyIdentity.Name -ne $provenance.artifact.assemblyName -or
    $assemblyIdentity.Version.ToString() -ne $provenance.artifact.assemblyVersion) {
    throw 'The interop assembly identity does not match its provenance manifest.'
}

if (-not $NoBuild) {
    dotnet build $generatorProject -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "The Briosa generator failed to build with exit code $LASTEXITCODE."
    }
}

$verificationDirectory = Join-Path $repositoryRoot 'artifacts\interop-verification'
$generatedApiPath = Join-Path $verificationDirectory 'Briosa.SpatialAnalyzer.Interop.PublicApi.txt'
New-Item -ItemType Directory -Force -Path $verificationDirectory | Out-Null

try {
    dotnet run --project $generatorProject -c Release --no-build -- interop-api $artifactPath $generatedApiPath
    if ($LASTEXITCODE -ne 0) {
        throw 'The canonical API verifier failed.'
    }

    $expectedApi = [IO.File]::ReadAllText($apiPath)
    $generatedApi = [IO.File]::ReadAllText($generatedApiPath)
    if ($expectedApi -cne $generatedApi) {
        throw 'The committed public API manifest is stale.'
    }

    $allowedBinary = "interop/SpatialAnalyzer/$SpatialAnalyzerVersion/Briosa.SpatialAnalyzer.Interop.dll"
    $trackedFiles = git -C $repositoryRoot ls-files
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to enumerate tracked repository files.'
    }

    $forbiddenFiles = $trackedFiles |
        Where-Object { [IO.Path]::GetExtension($_) -in @('.dll', '.exe', '.tlb', '.ocx') } |
        Where-Object { $_ -cne $allowedBinary }
    if ($forbiddenFiles) {
        throw "Unapproved binary artifacts are tracked:`n$($forbiddenFiles -join [Environment]::NewLine)"
    }
}
finally {
    if (Test-Path -LiteralPath $generatedApiPath) {
        Remove-Item -LiteralPath $generatedApiPath -Force
    }
}

Write-Host "Verified $($provenance.artifact.assemblyName) $($provenance.artifact.assemblyVersion)."
