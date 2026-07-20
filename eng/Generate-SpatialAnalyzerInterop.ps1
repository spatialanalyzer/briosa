[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $TypeLibraryPath,
    [string] $SpatialAnalyzerVersion,
    [string] $TlbImpPath,
    [string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$generatorProject = Join-Path $repositoryRoot 'tools\Briosa.Generator\Briosa.Generator.csproj'
$resolvedTypeLibrary = (Resolve-Path -LiteralPath $TypeLibraryPath).Path

if ([string]::IsNullOrWhiteSpace($SpatialAnalyzerVersion)) {
    $SpatialAnalyzerVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($resolvedTypeLibrary).FileVersion
}

if ([string]::IsNullOrWhiteSpace($SpatialAnalyzerVersion)) {
    throw 'The SpatialAnalyzer version could not be inferred. Pass -SpatialAnalyzerVersion explicitly.'
}

$parsedVersion = [Version]::Parse($SpatialAnalyzerVersion)
$assemblyVersion = '{0}.{1}.{2}.{3}' -f $parsedVersion.Major, $parsedVersion.Minor, $parsedVersion.Build, $parsedVersion.Revision

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot "interop\SpatialAnalyzer\$SpatialAnalyzerVersion"
}

if ([string]::IsNullOrWhiteSpace($TlbImpPath)) {
    $command = Get-Command TlbImp.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        $TlbImpPath = $command.Source
    }
    else {
        $sdkToolsRoot = Join-Path ${env:ProgramFiles(x86)} 'Microsoft SDKs\Windows\v10.0A\bin'
        $candidateTool = Get-ChildItem -LiteralPath $sdkToolsRoot -Filter TlbImp.exe -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like '*\x64\TlbImp.exe' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($null -ne $candidateTool) {
            $TlbImpPath = $candidateTool.FullName
        }
    }
}

if ([string]::IsNullOrWhiteSpace($TlbImpPath) -or -not (Test-Path -LiteralPath $TlbImpPath -PathType Leaf)) {
    throw 'TlbImp.exe was not found. Run this script from Visual Studio Developer PowerShell or pass -TlbImpPath.'
}

$resolvedTlbImp = (Resolve-Path -LiteralPath $TlbImpPath).Path
$resolvedOutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$stagingParent = Join-Path $repositoryRoot 'artifacts\interop-generation'
$stagingRoot = Join-Path $stagingParent ([Guid]::NewGuid().ToString('N'))
$firstDirectory = Join-Path $stagingRoot 'first'
$secondDirectory = Join-Path $stagingRoot 'second'
$artifactName = 'Briosa.SpatialAnalyzer.Interop.dll'
$publicApiName = 'Briosa.SpatialAnalyzer.Interop.PublicApi.txt'
$provenanceName = 'Briosa.SpatialAnalyzer.Interop.provenance.json'

New-Item -ItemType Directory -Force -Path $firstDirectory, $secondDirectory, $resolvedOutputDirectory | Out-Null

try {
    dotnet build $generatorProject -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "The Briosa generator failed to build with exit code $LASTEXITCODE."
    }

    $fixedArguments = @(
        '/namespace:Briosa.SpatialAnalyzer.Interop',
        "/asmversion:$assemblyVersion",
        '/machine:Agnostic',
        '/strictref:nopia',
        '/nologo'
    )

    $firstAssembly = Join-Path $firstDirectory $artifactName
    $secondAssembly = Join-Path $secondDirectory $artifactName
    $firstArguments = @($resolvedTypeLibrary, "/out:$firstAssembly") + $fixedArguments
    $secondArguments = @($resolvedTypeLibrary, "/out:$secondAssembly") + $fixedArguments

    & $resolvedTlbImp @firstArguments
    if ($LASTEXITCODE -ne 0) {
        throw "TlbImp.exe failed with exit code $LASTEXITCODE."
    }

    & $resolvedTlbImp @secondArguments
    if ($LASTEXITCODE -ne 0) {
        throw "The second TlbImp.exe run failed with exit code $LASTEXITCODE."
    }

    $firstApi = Join-Path $firstDirectory $publicApiName
    $secondApi = Join-Path $secondDirectory $publicApiName
    dotnet run --project $generatorProject -c Release --no-build -- interop-api $firstAssembly $firstApi
    if ($LASTEXITCODE -ne 0) {
        throw 'The first canonical API manifest failed to generate.'
    }

    dotnet run --project $generatorProject -c Release --no-build -- interop-api $secondAssembly $secondApi
    if ($LASTEXITCODE -ne 0) {
        throw 'The second canonical API manifest failed to generate.'
    }

    $firstApiHash = (Get-FileHash -LiteralPath $firstApi -Algorithm SHA256).Hash
    $secondApiHash = (Get-FileHash -LiteralPath $secondApi -Algorithm SHA256).Hash
    if ($firstApiHash -ne $secondApiHash) {
        throw 'Interop generation is not semantically deterministic: the canonical API manifests differ.'
    }

    $firstAssemblyHash = (Get-FileHash -LiteralPath $firstAssembly -Algorithm SHA256).Hash
    $secondAssemblyHash = (Get-FileHash -LiteralPath $secondAssembly -Algorithm SHA256).Hash
    $byteForByteDeterministic = $firstAssemblyHash -eq $secondAssemblyHash

    $committedAssembly = Join-Path $resolvedOutputDirectory $artifactName
    $committedApi = Join-Path $resolvedOutputDirectory $publicApiName
    $committedProvenance = Join-Path $resolvedOutputDirectory $provenanceName

    $replaceAssembly = -not (Test-Path -LiteralPath $committedAssembly -PathType Leaf)
    if (-not $replaceAssembly) {
        $existingApi = Join-Path $stagingRoot 'existing-api.txt'
        dotnet run --project $generatorProject -c Release --no-build -- interop-api $committedAssembly $existingApi
        if ($LASTEXITCODE -ne 0) {
            throw 'The existing committed interop assembly could not be inspected.'
        }

        $existingApiHash = (Get-FileHash -LiteralPath $existingApi -Algorithm SHA256).Hash
        $replaceAssembly = $existingApiHash -ne $firstApiHash
    }

    if ($replaceAssembly) {
        Copy-Item -LiteralPath $firstAssembly -Destination $committedAssembly -Force
    }

    dotnet run --project $generatorProject -c Release --no-build -- interop-api $committedAssembly $committedApi
    if ($LASTEXITCODE -ne 0) {
        throw 'The committed canonical API manifest failed to generate.'
    }

    $typeLibraryJson = dotnet run --project $generatorProject -c Release --no-build -- typelib-info $resolvedTypeLibrary
    if ($LASTEXITCODE -ne 0) {
        throw 'The type-library metadata could not be inspected.'
    }

    $typeLibrary = ($typeLibraryJson -join [Environment]::NewLine) | ConvertFrom-Json
    $committedAssemblyHash = (Get-FileHash -LiteralPath $committedAssembly -Algorithm SHA256).Hash
    $committedApiHash = (Get-FileHash -LiteralPath $committedApi -Algorithm SHA256).Hash
    $assemblyIdentity = [Reflection.AssemblyName]::GetAssemblyName($committedAssembly)
    $tlbImpVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($resolvedTlbImp).FileVersion

    $provenance = [ordered]@{
        schemaVersion = 1
        source = [ordered]@{
            fileName = [IO.Path]::GetFileName($resolvedTypeLibrary)
            sha256 = (Get-FileHash -LiteralPath $resolvedTypeLibrary -Algorithm SHA256).Hash
            productVersion = $SpatialAnalyzerVersion
            typeLibrary = [ordered]@{
                name = $typeLibrary.Name
                id = $typeLibrary.TypeLibraryId
                majorVersion = $typeLibrary.MajorVersion
                minorVersion = $typeLibrary.MinorVersion
                lcid = $typeLibrary.Lcid
                systemKind = $typeLibrary.SystemKind
                flags = $typeLibrary.Flags
            }
        }
        importer = [ordered]@{
            fileName = [IO.Path]::GetFileName($resolvedTlbImp)
            fileVersion = $tlbImpVersion
            sha256 = (Get-FileHash -LiteralPath $resolvedTlbImp -Algorithm SHA256).Hash
            arguments = @('<installed-type-library>', '/out:<staging-output>') + $fixedArguments
        }
        artifact = [ordered]@{
            fileName = $artifactName
            assemblyName = $assemblyIdentity.Name
            assemblyVersion = $assemblyIdentity.Version.ToString()
            sha256 = $committedAssemblyHash
            canonicalApiFileName = $publicApiName
            canonicalApiSha256 = $committedApiHash
        }
        determinism = [ordered]@{
            byteForByteAcrossProbeRuns = $byteForByteDeterministic
            canonicalApiSha256AcrossProbeRuns = $firstApiHash
            excludedVolatileFields = @('PE headers', 'module MVID')
        }
    }

    $json = ($provenance | ConvertTo-Json -Depth 8) + [Environment]::NewLine
    [IO.File]::WriteAllText($committedProvenance, $json, [Text.UTF8Encoding]::new($false))

    Write-Host "Generated interop artifacts in $resolvedOutputDirectory"
    Write-Host "Canonical API SHA-256: $committedApiHash"
    Write-Host "Byte-for-byte deterministic across probe runs: $byteForByteDeterministic"
}
finally {
    $resolvedStagingParent = [IO.Path]::GetFullPath($stagingParent)
    $resolvedStagingRoot = [IO.Path]::GetFullPath($stagingRoot)
    if ($resolvedStagingRoot.StartsWith($resolvedStagingParent, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedStagingRoot)) {
        Remove-Item -LiteralPath $resolvedStagingRoot -Recurse -Force
    }
}
