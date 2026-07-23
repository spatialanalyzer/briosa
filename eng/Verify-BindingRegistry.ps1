[CmdletBinding()]
param(
    [string] $SpatialAnalyzerVersion = '2026.1.0529.7',
    [switch] $NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$generatorProject = Join-Path $repositoryRoot 'tools\Briosa.Generator\Briosa.Generator.csproj'
$inventoryPath = Join-Path $repositoryRoot "inventory\sa\$SpatialAnalyzerVersion\inventory.json"
$dispositionDirectory = Join-Path $repositoryRoot "disposition\sa\$SpatialAnalyzerVersion"
$interopDirectory = Join-Path $repositoryRoot "interop\SpatialAnalyzer\$SpatialAnalyzerVersion"
$registryDirectory = Join-Path $repositoryRoot "bindings\sa\$SpatialAnalyzerVersion"

if (-not $NoBuild) {
    dotnet build $generatorProject -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "The Briosa generator failed to build with exit code $LASTEXITCODE."
    }
}

dotnet run --project $generatorProject -c Release --no-build -- `
    binding-registry-validate `
    $inventoryPath `
    $dispositionDirectory `
    $interopDirectory `
    $registryDirectory
if ($LASTEXITCODE -ne 0) {
    throw "Binding registry verification failed with exit code $LASTEXITCODE."
}
