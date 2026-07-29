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
$policySchema = Join-Path $repositoryRoot "release\schemas\v1\audit-policy.schema.json"
$matrixSchema = Join-Path $repositoryRoot "release\schemas\v1\support-matrix.schema.json"
$auditSchema = Join-Path $repositoryRoot "release\schemas\v1\release-audit.schema.json"
$policyPath = Join-Path $repositoryRoot "release\sa\$SpatialAnalyzerTarget\audit-policy.json"
$matrixPath = Join-Path $repositoryRoot "generated\release\sa\$SpatialAnalyzerTarget\support-matrix.json"
$auditPath = Join-Path $repositoryRoot "generated\release\sa\$SpatialAnalyzerTarget\release-audit.json"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-release-evidence-$([Guid]::NewGuid().ToString('N'))"

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

function Invoke-Generation {
    param([Parameter(Mandatory)][string]$OutputRoot)

    & dotnet run `
        --project $generatorProject `
        -c $Configuration `
        --no-build `
        --no-restore `
        -- `
        release-evidence-generate `
        $repositoryRoot `
        $OutputRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Release-evidence generation failed with exit code $LASTEXITCODE."
    }
}

function Assert-Evidence {
    param(
        [Parameter(Mandatory)][object[]]$Evidence,
        [Parameter(Mandatory)][string]$Description
    )

    $paths = @($Evidence.path)
    if ($paths.Count -ne @($paths | Sort-Object -Unique).Count) {
        throw "$Description contains duplicate evidence paths."
    }
    if (Compare-Object $paths @($paths | Sort-Object -CaseSensitive)) {
        throw "$Description evidence paths are not in ordinal order."
    }

    foreach ($entry in $Evidence) {
        $path = [IO.Path]::GetFullPath($entry.path, $repositoryRoot)
        if (-not $path.StartsWith(
                [IO.Path]::GetFullPath($repositoryRoot) + [IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "$Description evidence path is missing or escapes the repository: $($entry.path)"
        }
        if ((Get-Sha256 $path) -cne $entry.sha256) {
            throw "$Description evidence fingerprint is stale: $($entry.path)"
        }
    }
}

if (-not $NoBuild) {
    & dotnet build $generatorProject -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Release-evidence generator build failed with exit code $LASTEXITCODE."
    }
}

try {
    if (-not (Test-Json -Json (Get-Content -Raw -LiteralPath $policyPath) -SchemaFile $policySchema)) {
        throw "Release audit policy schema validation failed: $policyPath"
    }
    if (-not (Test-Json -Json (Get-Content -Raw -LiteralPath $matrixPath) -SchemaFile $matrixSchema)) {
        throw "Support matrix schema validation failed: $matrixPath"
    }
    if (-not (Test-Json -Json (Get-Content -Raw -LiteralPath $auditPath) -SchemaFile $auditSchema)) {
        throw "Release audit schema validation failed: $auditPath"
    }

    $matrix = Get-Content -Raw -LiteralPath $matrixPath | ConvertFrom-Json -Depth 100
    $audit = Get-Content -Raw -LiteralPath $auditPath | ConvertFrom-Json -Depth 100
    if ($matrix.generated_by -cne "Briosa.Generator release evidence" -or
        $audit.generated_by -cne "Briosa.Generator release evidence" -or
        $matrix.spatial_analyzer_target -cne $SpatialAnalyzerTarget -or
        $audit.spatial_analyzer_target -cne $SpatialAnalyzerTarget) {
        throw "Generated release-evidence identity is invalid."
    }

    $inventoryKeys = @($matrix.commands.inventory_key)
    if ($inventoryKeys.Count -ne @($inventoryKeys | Sort-Object -Unique).Count) {
        throw "Support matrix contains duplicate inventory keys."
    }
    if (Compare-Object $inventoryKeys @($inventoryKeys | Sort-Object -CaseSensitive)) {
        throw "Support matrix commands are not in ordinal inventory-key order."
    }
    if ($matrix.counts.inventory_commands -ne @($matrix.commands).Count -or
        $matrix.counts.approved_candidates -ne
            ($matrix.counts.cataloged_operations + $matrix.counts.approved_not_cataloged) -or
        $matrix.counts.inventory_commands -ne
            ($matrix.counts.approved_candidates +
             $matrix.counts.blocked +
             $matrix.counts.intentional_exclusions +
             $matrix.counts.sdk_unavailable) -or
        $matrix.counts.cataloged_operations -ne
            $matrix.counts.portable_validated_cataloged_operations) {
        throw "Support matrix counts do not reconcile."
    }

    $cataloged = @($matrix.commands | Where-Object operation_id)
    if (@($cataloged.operation_id).Count -ne
        @($cataloged.operation_id | Sort-Object -Unique).Count) {
        throw "Support matrix contains duplicate catalog operation IDs."
    }
    foreach ($command in $cataloged) {
        if ($command.release_classification -cne "cataloged_portable_only" -or
            $command.validation.portable -cne "portable_briosa_contract" -or
            $command.validation.protected_spatial_analyzer -cne "not_performed") {
            throw "Cataloged operation '$($command.operation_id)' overstates its validation tier."
        }
    }

    $criterionIds = @($audit.criteria.criterion_id)
    if ($criterionIds.Count -ne @($criterionIds | Sort-Object -Unique).Count) {
        throw "Release audit contains duplicate criterion IDs."
    }
    $requiredEpicCriteria = @(
        "epic-47-performance-and-reproducibility",
        "epic-47-portable-conformance",
        "epic-47-protected-runner",
        "epic-47-risk-fixtures",
        "epic-47-support-matrix")
    if (Compare-Object $requiredEpicCriteria @(
            $audit.criteria |
                Where-Object source -CEQ "https://github.com/spatialanalyzer/briosa/issues/47" |
                ForEach-Object criterion_id |
                Sort-Object -CaseSensitive)) {
        throw "Release audit does not account for every issue #47 exit criterion."
    }

    $passed = @($audit.criteria | Where-Object status -CEQ "passed").Count
    $blocked = @($audit.criteria | Where-Object status -CEQ "blocked").Count
    $notApplicable = @($audit.criteria | Where-Object status -CEQ "not_applicable").Count
    if ($audit.summary.passed -ne $passed -or
        $audit.summary.blocked -ne $blocked -or
        $audit.summary.not_applicable -ne $notApplicable -or
        $audit.release_ready -ne ($blocked -eq 0)) {
        throw "Release audit summary or readiness state is inconsistent."
    }

    Assert-Evidence -Evidence @($matrix.evidence_inputs) -Description "Support matrix"
    Assert-Evidence -Evidence @($audit.evidence_inputs) -Description "Release audit"

    $firstRoot = Join-Path $temporaryRoot "first"
    $secondRoot = Join-Path $temporaryRoot "second"
    Invoke-Generation -OutputRoot $firstRoot
    Invoke-Generation -OutputRoot $secondRoot
    $expectedFiles = @(
        "docs/reference/generated/sa/$SpatialAnalyzerTarget/release-audit.md",
        "docs/reference/generated/sa/$SpatialAnalyzerTarget/support-matrix.md",
        "generated/release/sa/$SpatialAnalyzerTarget/release-audit.json",
        "generated/release/sa/$SpatialAnalyzerTarget/support-matrix.json")
    $actualFiles = @(
        Get-ChildItem -LiteralPath $firstRoot -File -Recurse |
            ForEach-Object { [IO.Path]::GetRelativePath($firstRoot, $_.FullName).Replace('\', '/') } |
            Sort-Object -CaseSensitive)
    if (Compare-Object $expectedFiles $actualFiles) {
        throw "Release-evidence generation emitted an unexpected file set."
    }

    foreach ($relativePath in $expectedFiles) {
        $firstPath = Join-Path $firstRoot $relativePath
        $secondPath = Join-Path $secondRoot $relativePath
        $committedPath = Join-Path $repositoryRoot $relativePath
        if ((Get-Sha256 $firstPath) -cne (Get-Sha256 $secondPath)) {
            throw "Two clean release-evidence generations differed at '$relativePath'."
        }
        if ((Get-Sha256 $firstPath) -cne (Get-Sha256 $committedPath)) {
            throw "Committed release-evidence artifact is stale: $relativePath"
        }
    }

    Write-Host (
        "Verified release evidence for ${SpatialAnalyzerTarget}: " +
        "$($matrix.counts.inventory_commands) reconciled commands, " +
        "$($matrix.counts.cataloged_operations) cataloged operation(s), " +
        "$passed passing, $blocked blocked, and $notApplicable not-applicable audit criteria.")
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
