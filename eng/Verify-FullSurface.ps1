[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$NoBuild,
    [string]$BufPath = "buf",
    [string]$PolicyPath,
    [ValidateRange(0, 31)]
    [int]$ShardIndex = 0,
    [string]$ManifestOutputPath = "artifacts/full-surface/manifest.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$generatorProject = Join-Path $repositoryRoot "tools/Briosa.Generator/Briosa.Generator.csproj"
if ([string]::IsNullOrWhiteSpace($PolicyPath)) {
    $PolicyPath = Join-Path $PSScriptRoot "full-surface-policy.json"
}
else {
    $PolicyPath = [IO.Path]::GetFullPath($PolicyPath, $repositoryRoot)
}
$ManifestOutputPath = [IO.Path]::GetFullPath($ManifestOutputPath, $repositoryRoot)
$schemaPath = Join-Path $PSScriptRoot "schemas/full-surface-policy.schema.json"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-full-surface-$([Guid]::NewGuid().ToString('N'))"

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-NormalizedRelativePath {
    param(
        [Parameter(Mandatory)][string]$BasePath,
        [Parameter(Mandatory)][string]$Path
    )

    return [IO.Path]::GetRelativePath($BasePath, $Path).Replace('\', '/')
}

function Get-TreeEntries {
    param([Parameter(Mandatory)][string]$Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return @()
    }

    return @(
        Get-ChildItem -LiteralPath $Root -File -Recurse |
            ForEach-Object {
                [pscustomobject]@{
                    path = Get-NormalizedRelativePath -BasePath $Root -Path $_.FullName
                    sha256 = Get-Sha256 -Path $_.FullName
                    bytes = $_.Length
                }
            } |
            Sort-Object path)
}

function Get-TreeSha256 {
    param([Parameter(Mandatory)][string]$Root)

    $entries = @(Get-TreeEntries -Root $Root)
    $canonical = (($entries | ForEach-Object { "$($_.sha256)  $($_.path)" }) -join "`n") + "`n"
    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($canonical))).ToLowerInvariant()
}

function Copy-Tree {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        throw "Required generation input directory is missing: $Source"
    }
    [IO.Directory]::CreateDirectory($Destination) | Out-Null
    foreach ($item in Get-ChildItem -LiteralPath $Source -Force) {
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse
    }
}

function Invoke-Generator {
    param([Parameter(Mandatory)][string[]]$GeneratorArguments)

    $arguments = @(
        "run",
        "--project", $generatorProject,
        "-c", $Configuration,
        "--no-build",
        "--no-restore",
        "--") + $GeneratorArguments
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Generator command '$($GeneratorArguments[0])' failed with exit code $LASTEXITCODE."
    }
}

function Expand-PolicyPath {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Target
    )

    return $Path.Replace("{target}", $Target, [StringComparison]::Ordinal)
}

function Get-Evidence {
    param(
        [Parameter(Mandatory)]$Surface,
        [Parameter(Mandatory)][string]$Target
    )

    return @(
        foreach ($configuredPath in $Surface.evidence_paths) {
            $relativePath = Expand-PolicyPath -Path $configuredPath -Target $Target
            $fullPath = [IO.Path]::GetFullPath($relativePath, $repositoryRoot)
            if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
                [ordered]@{
                    path = $relativePath.Replace('\', '/')
                    sha256 = Get-Sha256 -Path $fullPath
                    kind = "file"
                }
            }
            elseif (Test-Path -LiteralPath $fullPath -PathType Container) {
                [ordered]@{
                    path = $relativePath.Replace('\', '/')
                    sha256 = Get-TreeSha256 -Root $fullPath
                    kind = "tree"
                }
            }
            else {
                throw "Configured evidence path is missing: $relativePath"
            }
        })
}

function Format-Evidence {
    param(
        [Parameter(Mandatory)]$Surface,
        [Parameter(Mandatory)][string]$Target
    )

    return (@(Get-Evidence -Surface $Surface -Target $Target) |
        ForEach-Object { "$($_.path)=$($_.sha256)" }) -join "; "
}

function Invoke-CleanGeneration {
    param([Parameter(Mandatory)][string]$RunRoot)

    $surfaceRoot = Join-Path $RunRoot "surfaces"
    $workRoot = Join-Path $RunRoot "work"
    [IO.Directory]::CreateDirectory($surfaceRoot) | Out-Null
    [IO.Directory]::CreateDirectory($workRoot) | Out-Null

    foreach ($target in $policy.targets) {
        $inventorySource = Join-Path $repositoryRoot "inventory/sa/$target"
        $dispositionSource = Join-Path $repositoryRoot "disposition/sa/$target"
        $interopSource = Join-Path $repositoryRoot "interop/SpatialAnalyzer/$target"
        $reviewSource = Join-Path $repositoryRoot "bindings/sa/$target/review.json"
        $valueCatalog = Join-Path $repositoryRoot "values/sa/$target/catalog.json"
        $catalogTargetSource = Join-Path $repositoryRoot "catalog/sa/$target"

        $inventoryWork = Join-Path $workRoot "inventory/sa/$target"
        $dispositionWork = Join-Path $workRoot "disposition/sa/$target"
        $interopWork = Join-Path $workRoot "interop/SpatialAnalyzer/$target"
        $bindingWork = Join-Path $workRoot "bindings/sa/$target"
        Copy-Tree -Source $inventorySource -Destination $inventoryWork
        Copy-Tree -Source $dispositionSource -Destination $dispositionWork
        Copy-Tree -Source $interopSource -Destination $interopWork
        [IO.Directory]::CreateDirectory($bindingWork) | Out-Null

        Invoke-Generator -GeneratorArguments @(
            "disposition-sync",
            (Join-Path $inventoryWork "inventory.json"),
            $dispositionWork)
        $dispositionOutput = Join-Path $surfaceRoot "disposition/$target"
        Copy-Tree -Source $dispositionWork -Destination $dispositionOutput

        $valueOutput = Join-Path $surfaceRoot "value-family/$target"
        [IO.Directory]::CreateDirectory($valueOutput) | Out-Null
        & (Join-Path $PSScriptRoot "Sync-ValueFamilyEvidence.ps1") `
            -SpatialAnalyzerTarget $target `
            -CatalogPath $valueCatalog `
            -BindingReviewInputPath $reviewSource `
            -BindingReviewOutputPath (Join-Path $valueOutput "binding-review.json") `
            -GeneratedOutputDirectory (Join-Path $valueOutput "generated") `
            -DocumentationOutputPath (Join-Path $valueOutput "value-families.md")
        if ($LASTEXITCODE -ne 0) {
            throw "Value-family synchronization failed with exit code $LASTEXITCODE."
        }

        Copy-Item `
            -LiteralPath (Join-Path $valueOutput "binding-review.json") `
            -Destination (Join-Path $bindingWork "review.json")
        Invoke-Generator -GeneratorArguments @(
            "binding-registry-sync",
            (Join-Path $inventoryWork "inventory.json"),
            $dispositionWork,
            $interopWork,
            $bindingWork)
        $bindingOutput = Join-Path $surfaceRoot "binding-registry/$target"
        [IO.Directory]::CreateDirectory($bindingOutput) | Out-Null
        Copy-Item -LiteralPath (Join-Path $bindingWork "registry.json") -Destination $bindingOutput
        Copy-Item -LiteralPath (Join-Path $bindingWork "report.md") -Destination $bindingOutput

        $scaffoldOutput = Join-Path $surfaceRoot "catalog-scaffolds/$target"
        Invoke-Generator -GeneratorArguments @(
            "catalog-scaffold-generate",
            (Join-Path $repositoryRoot "inventory/sa/$target/inventory.json"),
            (Join-Path $repositoryRoot "disposition/sa/$target"),
            $valueCatalog,
            (Join-Path $repositoryRoot "catalog"),
            $scaffoldOutput)

        $catalogContext = Join-Path $workRoot "catalog-context/$target"
        $catalogInput = Join-Path $catalogContext "catalog"
        Copy-Tree -Source (Join-Path $repositoryRoot "catalog/schemas") `
            -Destination (Join-Path $catalogInput "schemas")
        Copy-Tree -Source $catalogTargetSource `
            -Destination (Join-Path $catalogInput "sa/$target")
        Copy-Tree -Source (Join-Path $repositoryRoot "proto") `
            -Destination (Join-Path $catalogContext "proto")
        Invoke-Generator -GeneratorArguments @(
            "catalog-generate",
            $catalogInput,
            (Join-Path $surfaceRoot "catalog-artifacts/$target"))

        $conformanceContext = Join-Path $workRoot "portable-conformance-context/$target"
        Copy-Tree -Source (Join-Path $repositoryRoot "catalog/schemas") `
            -Destination (Join-Path $conformanceContext "catalog/schemas")
        Copy-Tree -Source $catalogTargetSource `
            -Destination (Join-Path $conformanceContext "catalog/sa/$target")
        Copy-Tree -Source (Join-Path $repositoryRoot "proto") `
            -Destination (Join-Path $conformanceContext "proto")
        Copy-Tree -Source $bindingWork `
            -Destination (Join-Path $conformanceContext "bindings/sa/$target")
        $conformanceValueRoot = Join-Path $conformanceContext "values/sa/$target"
        [IO.Directory]::CreateDirectory($conformanceValueRoot) | Out-Null
        Copy-Item -LiteralPath $valueCatalog -Destination (Join-Path $conformanceValueRoot "catalog.json")
        Invoke-Generator -GeneratorArguments @(
            "portable-conformance-generate",
            $conformanceContext,
            (Join-Path $surfaceRoot "portable-conformance/$target"))
    }
}

function Get-SurfaceSelection {
    $units = [Collections.Generic.List[object]]::new()
    $ordinal = 0
    foreach ($target in $policy.targets) {
        foreach ($surface in $policy.surfaces) {
            if (($ordinal % $policy.sharding.shard_count) -eq $ShardIndex) {
                $units.Add([pscustomobject]@{ target = $target; surface = $surface })
            }
            $ordinal++
        }
    }
    return $units.ToArray()
}

function Compare-GeneratedRuns {
    param(
        [Parameter(Mandatory)][string]$FirstRoot,
        [Parameter(Mandatory)][string]$SecondRoot,
        [Parameter(Mandatory)][object[]]$Selection
    )

    $errors = [Collections.Generic.List[string]]::new()
    foreach ($unit in $Selection) {
        $firstSurface = Join-Path $FirstRoot "surfaces/$($unit.surface.id)/$($unit.target)"
        $secondSurface = Join-Path $SecondRoot "surfaces/$($unit.surface.id)/$($unit.target)"
        $firstEntries = @{}
        foreach ($entry in Get-TreeEntries -Root $firstSurface) { $firstEntries[$entry.path] = $entry }
        $secondEntries = @{}
        foreach ($entry in Get-TreeEntries -Root $secondSurface) { $secondEntries[$entry.path] = $entry }
        $paths = @($firstEntries.Keys + $secondEntries.Keys | Sort-Object -Unique)
        foreach ($path in $paths) {
            $first = $firstEntries[$path]
            $second = $secondEntries[$path]
            if ($null -eq $first -or $null -eq $second -or $first.sha256 -cne $second.sha256) {
                $firstHash = if ($null -eq $first) { "missing" } else { $first.sha256 }
                $secondHash = if ($null -eq $second) { "missing" } else { $second.sha256 }
                $evidence = Format-Evidence -Surface $unit.surface -Target $unit.target
                $errors.Add(
                    "Surface '$($unit.surface.id)' target '$($unit.target)' generated '$path' " +
                    "non-deterministically (first=$firstHash, second=$secondHash). " +
                    "Evidence: $evidence. Affected generated surface: $($unit.surface.id)/$($unit.target)/$path")
            }
        }
    }
    if ($errors.Count -ne 0) {
        throw "Two clean full-surface generations differed:`n$($errors -join "`n")"
    }
}

function Compare-File {
    param(
        [Parameter(Mandatory)][string]$GeneratedPath,
        [Parameter(Mandatory)][string]$CommittedPath,
        [Parameter(Mandatory)]$Unit,
        [Parameter(Mandatory)][AllowEmptyCollection()][Collections.Generic.List[string]]$Errors
    )

    $generatedHash = if (Test-Path -LiteralPath $GeneratedPath -PathType Leaf) {
        Get-Sha256 -Path $GeneratedPath
    } else { "missing" }
    $committedHash = if (Test-Path -LiteralPath $CommittedPath -PathType Leaf) {
        Get-Sha256 -Path $CommittedPath
    } else { "missing" }
    if ($generatedHash -cne $committedHash) {
        $evidence = Format-Evidence -Surface $Unit.surface -Target $Unit.target
        $relativeCommitted = Get-NormalizedRelativePath -BasePath $repositoryRoot -Path $CommittedPath
        $Errors.Add(
            "Surface '$($Unit.surface.id)' target '$($Unit.target)' has content drift at " +
            "'$relativeCommitted' (generated=$generatedHash, committed=$committedHash). " +
            "Evidence: $evidence. Affected generated surface: $relativeCommitted")
    }
}

function Compare-CommittedOutputs {
    param(
        [Parameter(Mandatory)][string]$FirstRoot,
        [Parameter(Mandatory)][object[]]$Selection
    )

    $errors = [Collections.Generic.List[string]]::new()
    foreach ($unit in $Selection) {
        $surfaceRoot = Join-Path $FirstRoot "surfaces/$($unit.surface.id)/$($unit.target)"
        foreach ($mapping in $unit.surface.committed_outputs) {
            $generatedRelative = Expand-PolicyPath -Path $mapping.generated_path -Target $unit.target
            $repositoryRelative = Expand-PolicyPath -Path $mapping.repository_path -Target $unit.target
            $generatedPath = [IO.Path]::GetFullPath($generatedRelative, $surfaceRoot)
            $committedPath = [IO.Path]::GetFullPath($repositoryRelative, $repositoryRoot)
            if ($mapping.mode -eq "file") {
                Compare-File `
                    -GeneratedPath $generatedPath `
                    -CommittedPath $committedPath `
                    -Unit $unit `
                    -Errors $errors
                continue
            }

            $generatedEntries = @(Get-TreeEntries -Root $generatedPath)
            $generatedByPath = @{}
            foreach ($entry in $generatedEntries) { $generatedByPath[$entry.path] = $entry }
            $paths = if ($mapping.mode -eq "tree") {
                $committedEntries = @(Get-TreeEntries -Root $committedPath)
                $committedPaths = @($committedEntries.path)
                @($generatedByPath.Keys + $committedPaths | Sort-Object -Unique)
            }
            else {
                @($generatedByPath.Keys | Sort-Object)
            }
            foreach ($path in $paths) {
                Compare-File `
                    -GeneratedPath (Join-Path $generatedPath $path) `
                    -CommittedPath (Join-Path $committedPath $path) `
                    -Unit $unit `
                    -Errors $errors
            }
        }
    }
    if ($errors.Count -ne 0) {
        throw "Committed generated surfaces are stale:`n$($errors -join "`n")"
    }
}

function Invoke-SurfaceVerification {
    param(
        [Parameter(Mandatory)][string]$SurfaceId,
        [Parameter(Mandatory)][string]$Target,
        [Parameter(Mandatory)][scriptblock]$Action
    )

    $surface = @($policy.surfaces | Where-Object id -CEQ $SurfaceId)[0]
    try {
        & $Action
        if ($LASTEXITCODE -ne 0) {
            throw "Verifier exited with code $LASTEXITCODE."
        }
    }
    catch {
        $evidence = Format-Evidence -Surface $surface -Target $Target
        throw (
            "Surface '$SurfaceId' target '$Target' verification failed. " +
            "Evidence: $evidence. Affected generated surface: $SurfaceId/$Target. " +
            "Details: $($_.Exception.Message)")
    }
}

$policyJson = Get-Content -Raw -LiteralPath $PolicyPath
if (-not (Test-Json -Json $policyJson -SchemaFile $schemaPath)) {
    throw "Full-surface policy schema validation failed: $PolicyPath"
}
$policy = $policyJson | ConvertFrom-Json -Depth 100
if ($policy.'$schema' -cne "schemas/full-surface-policy.schema.json" -or
    $policy.schema_version -ne 1) {
    throw "Full-surface policy identity is invalid."
}
if ($policy.sharding.algorithm -cne "target-then-surface-ordinal-modulo") {
    throw "Full-surface policy sharding algorithm is not recognized."
}

$targetIds = @($policy.targets)
if ($targetIds.Count -ne @($targetIds | Sort-Object -Unique).Count) {
    throw "Full-surface policy target IDs must be unique."
}
$surfaceIds = @($policy.surfaces.id)
if ($surfaceIds.Count -ne @($surfaceIds | Sort-Object -Unique).Count) {
    throw "Full-surface policy surface IDs must be unique."
}
$requiredSurfaceIds = @(
    "disposition",
    "value-family",
    "binding-registry",
    "catalog-scaffolds",
    "catalog-artifacts",
    "portable-conformance")
if (Compare-Object $requiredSurfaceIds $surfaceIds) {
    throw "Full-surface policy must define every repository-owned generation surface exactly once."
}
$requiredBudgetUnits = [ordered]@{
    restore = "seconds"
    generation = "seconds"
    compile = "seconds"
    test = "seconds"
    package = "seconds"
    startup = "seconds"
    "descriptor-size" = "bytes"
    "package-size" = "bytes"
    "startup-working-set" = "bytes"
    "dispatch-p95" = "milliseconds"
    "request-mapping-p95" = "milliseconds"
    "discovery-p95" = "milliseconds"
    "retained-managed-memory" = "bytes"
}
foreach ($entry in $requiredBudgetUnits.GetEnumerator()) {
    $matches = @($policy.budgets | Where-Object metric -CEQ $entry.Key)
    if ($matches.Count -ne 1 -or $matches[0].unit -cne $entry.Value) {
        throw "Full-surface policy must define one '$($entry.Key)' budget in $($entry.Value)."
    }
}
if ($ShardIndex -ge $policy.sharding.shard_count) {
    throw "ShardIndex $ShardIndex is outside configured shard count $($policy.sharding.shard_count)."
}
if ($policy.sharding.shard_count -ne 1 -or $ShardIndex -ne 0) {
    throw "Full-surface sharding is deferred until CI has a checked matrix; shard_count must be 1 and ShardIndex must be 0."
}

& (Join-Path $PSScriptRoot "Test-CiBudgetPolicy.ps1")

$baselineRefs = @($policy.released_protocol_baselines | ForEach-Object ref)
if ($baselineRefs.Count -ne @($baselineRefs | Sort-Object -Unique).Count) {
    throw "Released protocol baseline refs must be unique."
}
foreach ($baseline in $policy.released_protocol_baselines) {
    if ($baseline.ref -in @("main", "refs/heads/main") -or
        $baseline.ref.StartsWith("refs/heads/", [StringComparison]::Ordinal)) {
        throw "A mutable branch cannot be a released protocol baseline: $($baseline.ref)"
    }
    $resolved = (& git `
        -c "safe.directory=$($repositoryRoot.Replace('\', '/'))" `
        -C $repositoryRoot `
        rev-parse "$($baseline.ref)^{commit}").Trim()
    if ($LASTEXITCODE -ne 0 -or $resolved -cne $baseline.commit) {
        throw (
            "Released protocol baseline '$($baseline.ref)' must resolve to pinned commit " +
            "'$($baseline.commit)'; resolved '$resolved'.")
    }
}

if (-not $NoBuild) {
    & dotnet build $generatorProject -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Generator build failed with exit code $LASTEXITCODE."
    }
}

$selection = @(Get-SurfaceSelection)
if ($selection.Count -eq 0) {
    throw "Configured shard $ShardIndex owns no generation surfaces."
}

try {
    $firstRoot = Join-Path $temporaryRoot "first"
    $secondRoot = Join-Path $temporaryRoot "second"
    Invoke-CleanGeneration -RunRoot $firstRoot
    Invoke-CleanGeneration -RunRoot $secondRoot
    Compare-GeneratedRuns -FirstRoot $firstRoot -SecondRoot $secondRoot -Selection $selection
    Compare-CommittedOutputs -FirstRoot $firstRoot -Selection $selection

    foreach ($target in $policy.targets) {
        Invoke-SurfaceVerification -SurfaceId "disposition" -Target $target -Action {
            & (Join-Path $PSScriptRoot "Verify-Disposition.ps1") `
                -Configuration $Configuration -NoBuild
        }
        Invoke-SurfaceVerification -SurfaceId "value-family" -Target $target -Action {
            & (Join-Path $PSScriptRoot "Verify-ValueFamilyEvidence.ps1") `
                -SpatialAnalyzerTarget $target
        }
        Invoke-SurfaceVerification -SurfaceId "binding-registry" -Target $target -Action {
            & (Join-Path $PSScriptRoot "Verify-BindingRegistry.ps1") `
                -SpatialAnalyzerVersion $target -NoBuild
            & (Join-Path $PSScriptRoot "Verify-InteropArtifacts.ps1") `
                -SpatialAnalyzerVersion $target -NoBuild
        }
        Invoke-SurfaceVerification -SurfaceId "catalog-scaffolds" -Target $target -Action {
            & (Join-Path $PSScriptRoot "Verify-CatalogScaffolds.ps1") `
                -SpatialAnalyzerTarget $target -NoBuild
        }
        Invoke-SurfaceVerification -SurfaceId "catalog-artifacts" -Target $target -Action {
            & (Join-Path $PSScriptRoot "Verify-Catalog.ps1") `
                -Configuration $Configuration -NoBuild
            & (Join-Path $PSScriptRoot "Verify-CatalogArtifacts.ps1") `
                -Configuration $Configuration -NoBuild
        }
        Invoke-SurfaceVerification -SurfaceId "portable-conformance" -Target $target -Action {
            & (Join-Path $PSScriptRoot "Verify-PortableConformance.ps1") `
                -SpatialAnalyzerTarget $target -Configuration $Configuration -NoBuild
        }
    }

    $protocolEvidence = (
        "proto=$(Get-TreeSha256 -Root (Join-Path $repositoryRoot 'proto')); " +
        "buf.yaml=$(Get-Sha256 -Path (Join-Path $repositoryRoot 'buf.yaml'))")
    if (@($policy.released_protocol_baselines).Count -eq 0) {
        try {
            & (Join-Path $PSScriptRoot "Verify-Protocol.ps1") -BufPath $BufPath
            if ($LASTEXITCODE -ne 0) {
                throw "Protocol verifier exited with code $LASTEXITCODE."
            }
        }
        catch {
            throw (
                "Current protocol formatting, lint, or compilation drifted. " +
                "Evidence: $protocolEvidence. Affected generated surface: proto. " +
                "Details: $($_.Exception.Message)")
        }
    }
    else {
        foreach ($baseline in $policy.released_protocol_baselines) {
            try {
                & (Join-Path $PSScriptRoot "Verify-Protocol.ps1") `
                    -BufPath $BufPath `
                    -AgainstRef $baseline.ref
                if ($LASTEXITCODE -ne 0) {
                    throw "Protocol verifier exited with code $LASTEXITCODE."
                }
            }
            catch {
                throw (
                    "Protocol breaking verification failed for released baseline " +
                    "'$($baseline.ref)' at '$($baseline.commit)' affecting packages " +
                    "$($baseline.packages -join ', '). Evidence: $protocolEvidence. " +
                    "Affected generated surface: proto. Details: $($_.Exception.Message)")
            }
        }
    }

    $manifestUnits = @(
        foreach ($unit in $selection) {
            $root = Join-Path $firstRoot "surfaces/$($unit.surface.id)/$($unit.target)"
            [ordered]@{
                target = $unit.target
                surface = $unit.surface.id
                evidence = @(Get-Evidence -Surface $unit.surface -Target $unit.target)
                files = @(Get-TreeEntries -Root $root)
            }
        })
    $manifest = [ordered]@{
        schema_version = 1
        policy_sha256 = Get-Sha256 -Path $PolicyPath
        shard = [ordered]@{
            algorithm = $policy.sharding.algorithm
            index = $ShardIndex
            count = $policy.sharding.shard_count
        }
        released_protocol_baselines = @($policy.released_protocol_baselines)
        units = $manifestUnits
    }
    [IO.Directory]::CreateDirectory((Split-Path -Parent $ManifestOutputPath)) | Out-Null
    $manifestJson = ($manifest | ConvertTo-Json -Depth 100).Replace("`r`n", "`n") + "`n"
    [IO.File]::WriteAllText(
        $ManifestOutputPath,
        $manifestJson,
        [Text.UTF8Encoding]::new($false))

    $fileCount = @($manifestUnits.files).Count
    Write-Host (
        "Verified $($selection.Count) full-surface unit(s) and $fileCount generated file(s) " +
        "across two clean, byte-identical generations. Manifest: $ManifestOutputPath")
    if (@($policy.released_protocol_baselines).Count -eq 0) {
        Write-Host "No released protocol baseline is configured; unreleased main remains mutable."
    }
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedTemporaryRoot).StartsWith(
            "briosa-full-surface-",
            [StringComparison]::Ordinal) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
