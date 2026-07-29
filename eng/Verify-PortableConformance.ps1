[CmdletBinding()]
param(
    [string]$SpatialAnalyzerTarget = "2026.1.0529.7",
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$generatorProject = Join-Path $repositoryRoot "tools\Briosa.Generator\Briosa.Generator.csproj"
$schemaPath = Join-Path $repositoryRoot "conformance\schemas\v1\manifest.schema.json"
$committedPath = Join-Path $repositoryRoot "generated\conformance\sa\$SpatialAnalyzerTarget\manifest.json"
$catalogPath = Join-Path $repositoryRoot "catalog\sa\$SpatialAnalyzerTarget\catalog.json"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-portable-conformance-$([Guid]::NewGuid().ToString('N'))"

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

function Assert-UniqueIds {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Values,
        [Parameter(Mandatory)][string]$Description
    )

    if ($Values.Count -ne @($Values | Sort-Object -Unique).Count) {
        throw "$Description contains duplicate identities."
    }
}

function Invoke-Generation {
    param([Parameter(Mandatory)][string]$OutputRoot)

    & dotnet run `
        --project $generatorProject `
        -c $Configuration `
        --no-build `
        --no-restore `
        -- `
        portable-conformance-generate `
        $repositoryRoot `
        $OutputRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Portable conformance generation failed with exit code $LASTEXITCODE."
    }
}

if (-not $NoBuild) {
    & dotnet build $generatorProject -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Portable conformance generator build failed with exit code $LASTEXITCODE."
    }
}

try {
    if (-not (Test-Json -Json (Get-Content -Raw -LiteralPath $committedPath) -SchemaFile $schemaPath)) {
        throw "Portable conformance manifest schema validation failed: $committedPath"
    }

    $manifest = Get-Content -Raw -LiteralPath $committedPath | ConvertFrom-Json -Depth 100
    if ($manifest.schema_version -ne 1 -or
        $manifest.generated_by -cne "Briosa.Generator portable conformance" -or
        $manifest.spatial_analyzer_target -cne $SpatialAnalyzerTarget) {
        throw "Portable conformance manifest identity is invalid."
    }

    $catalog = Get-Content -Raw -LiteralPath $catalogPath | ConvertFrom-Json -Depth 100
    $catalogOperationIds = @($catalog.operation_files | ForEach-Object {
            $operationPath = Join-Path (Split-Path -Parent $catalogPath) $_
            (Get-Content -Raw -LiteralPath $operationPath | ConvertFrom-Json -Depth 100).operation_id
        } | Sort-Object)
    $manifestOperationIds = @($manifest.operations.operation_id | Sort-Object)
    if (Compare-Object $catalogOperationIds $manifestOperationIds) {
        throw "Portable conformance operation coverage differs from the supported catalog."
    }

    Assert-UniqueIds -Values @($manifest.operations.operation_id) -Description "Operation coverage"
    foreach ($operation in $manifest.operations) {
        Assert-UniqueIds `
            -Values @($operation.scenarios.scenario_id) `
            -Description "Operation '$($operation.operation_id)' scenarios"
    }
    foreach ($property in @(
            "binding_cases",
            "value_family_cases",
            "enum_cases",
            "structured_cases",
            "assignment_cases")) {
        Assert-UniqueIds -Values @($manifest.$property.case_id) -Description $property
    }

    $countChecks = [ordered]@{
        operation_count = @($manifest.operations).Count
        binding_case_count = @($manifest.binding_cases).Count
        value_family_case_count = @($manifest.value_family_cases).Count
        enum_case_count = @($manifest.enum_cases).Count
        structured_case_count = @($manifest.structured_cases).Count
        assignment_case_count = @($manifest.assignment_cases).Count
    }
    foreach ($entry in $countChecks.GetEnumerator()) {
        if ($manifest.counts.($entry.Key) -ne $entry.Value) {
            throw "Portable conformance count '$($entry.Key)' does not match its emitted cases."
        }
    }

    foreach ($evidence in $manifest.evidence_inputs) {
        $evidencePath = [IO.Path]::GetFullPath($evidence.path, $repositoryRoot)
        if (-not $evidencePath.StartsWith(
                [IO.Path]::GetFullPath($repositoryRoot) + [IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
            throw "Portable conformance evidence path is missing or escapes the repository: $($evidence.path)"
        }
        if ((Get-Sha256 $evidencePath) -cne $evidence.sha256) {
            throw "Portable conformance evidence fingerprint is stale: $($evidence.path)"
        }
    }

    $firstRoot = Join-Path $temporaryRoot "first"
    $secondRoot = Join-Path $temporaryRoot "second"
    Invoke-Generation -OutputRoot $firstRoot
    Invoke-Generation -OutputRoot $secondRoot
    $relativeManifest = "generated\conformance\sa\$SpatialAnalyzerTarget\manifest.json"
    $firstPath = Join-Path $firstRoot $relativeManifest
    $secondPath = Join-Path $secondRoot $relativeManifest
    if ((Get-Sha256 $firstPath) -cne (Get-Sha256 $secondPath)) {
        throw "Two clean portable conformance generations differed."
    }
    if ((Get-Sha256 $firstPath) -cne (Get-Sha256 $committedPath)) {
        throw "Committed portable conformance manifest is stale: $committedPath"
    }

    $generatedFiles = @(
        Get-ChildItem -LiteralPath $firstRoot -File -Recurse |
            ForEach-Object { [IO.Path]::GetRelativePath($firstRoot, $_.FullName) })
    if ($generatedFiles.Count -ne 1 -or $generatedFiles[0] -cne $relativeManifest) {
        throw "Portable conformance generation emitted an unexpected file set: $($generatedFiles -join ', ')"
    }

    Write-Host (
        "Verified portable conformance for ${SpatialAnalyzerTarget}: " +
        "$($countChecks.operation_count) operations, " +
        "$($countChecks.binding_case_count) binding cases, " +
        "$($countChecks.value_family_case_count) value-family cases, " +
        "$($countChecks.enum_case_count) enum cases, " +
        "$($countChecks.structured_case_count) structured cases, and " +
        "$($countChecks.assignment_case_count) assignment cases.")
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
