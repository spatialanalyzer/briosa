[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$catalogRoot = Join-Path $repositoryRoot "catalog"
$manifestSchema = Join-Path $catalogRoot "schemas\v1\catalog.schema.json"
$operationSchema = Join-Path $catalogRoot "schemas\v1\operation.schema.json"
$generatorProject = Join-Path $repositoryRoot "tools\Briosa.Generator\Briosa.Generator.csproj"

function Test-CatalogJson {
    param(
        [Parameter(Mandatory)][string]$DocumentPath,
        [Parameter(Mandatory)][string]$SchemaPath
    )

    $json = Get-Content -LiteralPath $DocumentPath -Raw
    $valid = Test-Json -Json $json -SchemaFile $SchemaPath -ErrorAction Stop
    if (-not $valid) {
        throw "JSON Schema validation failed for '$DocumentPath'."
    }
}

$manifests = @(Get-ChildItem -Path (Join-Path $catalogRoot "sa") -Filter "catalog.json" -Recurse -File)
if ($manifests.Count -eq 0) {
    throw "No exact-target catalog manifests were found."
}

foreach ($manifestPath in $manifests.FullName) {
    Test-CatalogJson -DocumentPath $manifestPath -SchemaPath $manifestSchema

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $targetDirectory = Split-Path -Parent $manifestPath
    foreach ($relativeOperationPath in $manifest.operation_files) {
        $operationPath = Join-Path $targetDirectory $relativeOperationPath
        Test-CatalogJson -DocumentPath $operationPath -SchemaPath $operationSchema
    }
}

if (-not $NoBuild) {
    & dotnet build $generatorProject -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Catalog validator build failed with exit code $LASTEXITCODE."
    }
}

& dotnet run `
    --project $generatorProject `
    -c $Configuration `
    --no-build `
    --no-restore `
    -- `
    catalog-validate `
    $catalogRoot
if ($LASTEXITCODE -ne 0) {
    throw "Catalog semantic validation failed with exit code $LASTEXITCODE."
}
