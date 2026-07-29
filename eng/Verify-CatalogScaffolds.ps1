[CmdletBinding()]
param(
    [string]$SpatialAnalyzerTarget = "2026.1.0529.7",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$inventoryPath = Join-Path $repositoryRoot "inventory/sa/$SpatialAnalyzerTarget/inventory.json"
$dispositionDirectory = Join-Path $repositoryRoot "disposition/sa/$SpatialAnalyzerTarget"
$valueFamilyCatalogPath = Join-Path $repositoryRoot "values/sa/$SpatialAnalyzerTarget/catalog.json"
$catalogRoot = Join-Path $repositoryRoot "catalog"
$manifestSchema = Join-Path $catalogRoot "schemas/v1/scaffold-manifest.schema.json"
$scaffoldSchema = Join-Path $catalogRoot "schemas/v1/scaffold.schema.json"
$temporaryBase = [IO.Path]::GetTempPath()
$temporaryRoot = Join-Path $temporaryBase "briosa-catalog-scaffold-$([Guid]::NewGuid().ToString('N'))"
$firstOutput = Join-Path $temporaryRoot "first"
$secondOutput = Join-Path $temporaryRoot "second"

function Invoke-ScaffoldGeneration {
    param([Parameter(Mandatory)][string]$OutputDirectory)

    $arguments = @(
        "run",
        "--project", (Join-Path $repositoryRoot "tools/Briosa.Generator"),
        "-c", "Release"
    )
    if ($NoBuild) {
        $arguments += "--no-build"
    }

    $arguments += @(
        "--",
        "catalog-scaffold-generate",
        $inventoryPath,
        $dispositionDirectory,
        $valueFamilyCatalogPath,
        $catalogRoot,
        $OutputDirectory
    )
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Catalog scaffold generation failed with exit code $LASTEXITCODE."
    }
}

function Get-TreeFingerprint {
    param([Parameter(Mandatory)][string]$Root)

    return @(
        Get-ChildItem -LiteralPath $Root -File -Recurse |
            ForEach-Object {
                [pscustomobject]@{
                    path = [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/')
                    sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            } |
            Sort-Object path |
            ConvertTo-Json -Compress
    )
}

try {
    Invoke-ScaffoldGeneration -OutputDirectory $firstOutput
    Invoke-ScaffoldGeneration -OutputDirectory $secondOutput

    $firstFingerprint = Get-TreeFingerprint -Root $firstOutput
    $secondFingerprint = Get-TreeFingerprint -Root $secondOutput
    if (-not [Linq.Enumerable]::SequenceEqual(
            [string[]]$firstFingerprint,
            [string[]]$secondFingerprint)) {
        throw "Catalog scaffold generation was not byte-identical across two clean runs."
    }

    $manifestPath = Join-Path $firstOutput "manifest.json"
    if (-not (Get-Content -Raw -LiteralPath $manifestPath |
            Test-Json -SchemaFile $manifestSchema)) {
        throw "Catalog scaffold manifest schema validation failed."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    if ($manifest.scaffold_count -ne $manifest.scaffold_files.Count) {
        throw "Catalog scaffold manifest count does not match its file list."
    }

    if ($manifest.approved_candidate_count -ne
        ($manifest.existing_catalog_operation_count + $manifest.scaffold_count)) {
        throw "Approved candidates are not fully accounted for by catalog entries and scaffolds."
    }

    foreach ($reference in $manifest.scaffold_files) {
        $path = Join-Path $firstOutput $reference.path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Catalog scaffold '$($reference.path)' is missing."
        }

        $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -ne $reference.sha256) {
            throw "Catalog scaffold '$($reference.path)' does not match its manifest hash."
        }

        if (-not (Get-Content -Raw -LiteralPath $path |
                Test-Json -SchemaFile $scaffoldSchema)) {
            throw "Catalog scaffold '$($reference.path)' failed schema validation."
        }
    }

    Write-Host ((
        "Verified {0} deterministic review scaffolds from {1} approved candidates; " +
        "{2} supported catalog operation(s) were preserved.") -f
        $manifest.scaffold_count,
        $manifest.approved_candidate_count,
        $manifest.existing_catalog_operation_count)
}
finally {
    $fullTemporaryBase = [IO.Path]::GetFullPath($temporaryBase)
    $fullTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($fullTemporaryRoot.StartsWith(
            $fullTemporaryBase,
            [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($fullTemporaryRoot).StartsWith(
            "briosa-catalog-scaffold-",
            [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $fullTemporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
