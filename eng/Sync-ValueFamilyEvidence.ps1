[CmdletBinding()]
param(
    [string]$SpatialAnalyzerTarget = "2026.1.0529.7",
    [string]$CatalogPath,
    [string]$BindingReviewInputPath,
    [string]$BindingReviewOutputPath,
    [string]$GeneratedOutputDirectory,
    [string]$DocumentationOutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CatalogPath)) {
    $CatalogPath = Join-Path $repositoryRoot "values\sa\$SpatialAnalyzerTarget\catalog.json"
}
if ([string]::IsNullOrWhiteSpace($BindingReviewInputPath)) {
    $BindingReviewInputPath = Join-Path $repositoryRoot "bindings\sa\$SpatialAnalyzerTarget\review.json"
}
if ([string]::IsNullOrWhiteSpace($BindingReviewOutputPath)) {
    $BindingReviewOutputPath = $BindingReviewInputPath
}
if ([string]::IsNullOrWhiteSpace($GeneratedOutputDirectory)) {
    $GeneratedOutputDirectory = Join-Path $repositoryRoot "generated\values\sa\$SpatialAnalyzerTarget"
}
if ([string]::IsNullOrWhiteSpace($DocumentationOutputPath)) {
    $DocumentationOutputPath = Join-Path $repositoryRoot "docs\reference\generated\sa\$SpatialAnalyzerTarget\value-families.md"
}

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

function Write-Utf8Json {
    param([Parameter(Mandatory)]$Value, [Parameter(Mandatory)][string]$Path)
    $json = ($Value | ConvertTo-Json -Depth 100).Replace("`r`n", "`n").Replace("`r", "`n") + "`n"
    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    [IO.File]::WriteAllText($Path, $json, [Text.UTF8Encoding]::new($false))
}

function Convert-PropertiesToOrderedMap {
    param([Parameter(Mandatory)]$Value)
    $result = [ordered]@{}
    foreach ($property in $Value.PSObject.Properties | Sort-Object Name) {
        $result[$property.Name] = $property.Value
    }
    return $result
}

$catalog = Get-Content -Raw -LiteralPath $CatalogPath | ConvertFrom-Json -Depth 100
if ($catalog.spatial_analyzer_target -ne $SpatialAnalyzerTarget) {
    throw "Catalog target '$($catalog.spatial_analyzer_target)' does not match '$SpatialAnalyzerTarget'."
}
$inventoryPath = Join-Path $repositoryRoot "inventory\sa\$SpatialAnalyzerTarget\inventory.json"
$inventory = Get-Content -Raw -LiteralPath $inventoryPath | ConvertFrom-Json -Depth 100
$inventoryByKey = @{}
foreach ($command in $inventory.commands) { $inventoryByKey[$command.inventory_key] = $command }

$sourceIds = @($catalog.sources.source_id)
if ($sourceIds.Count -ne @($sourceIds | Sort-Object -Unique).Count) {
    throw "Value evidence source IDs must be unique."
}
foreach ($tracked in $catalog.tracked_inputs) {
    $path = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $CatalogPath) $tracked.path))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Tracked value evidence input is missing: $path"
    }
    if ((Get-Sha256 $path) -ne $tracked.sha256) {
        throw "Tracked value evidence input fingerprint is stale: $path"
    }
}
$documentationSource = @($catalog.sources | Where-Object source_id -eq "installed_command_documentation")
$sdkCodeSource = @($catalog.sources | Where-Object source_id -eq "view_sdk_code")
if ($documentationSource.Count -ne 1 -or
    $documentationSource[0].sha256 -ne $inventory.provenance.documentation.aggregate_sha256 -or
    $documentationSource[0].record_count -ne $inventory.provenance.documentation.record_count) {
    throw "Installed command-documentation provenance differs from the tracked inventory."
}
if ($sdkCodeSource.Count -ne 1 -or
    $sdkCodeSource[0].sha256 -ne $inventory.provenance.sdk_code.aggregate_sha256 -or
    $sdkCodeSource[0].record_count -ne $inventory.provenance.sdk_code.record_count) {
    throw "View SDK Code provenance differs from the tracked inventory."
}

$familyIds = @($catalog.families.family_id)
if ($familyIds.Count -ne @($familyIds | Sort-Object -Unique).Count) {
    throw "Value family IDs must be unique."
}
foreach ($family in $catalog.families) {
    foreach ($sourceId in $family.evidence_source_ids) {
        if ($sourceId -notin $sourceIds) {
            throw "Family '$($family.family_id)' references unknown evidence '$sourceId'."
        }
    }
}
foreach ($enumType in $catalog.enum_types) {
    foreach ($member in $enumType.members) {
        foreach ($sourceId in $member.evidence_source_ids) {
            if ($sourceId -notin $sourceIds) {
                throw "Enum '$($enumType.public_type)' references unknown evidence '$sourceId'."
            }
        }
    }
}
foreach ($structuredType in $catalog.structured_types) {
    foreach ($sourceId in $structuredType.evidence_source_ids) {
        if ($sourceId -notin $sourceIds) {
            throw "Structured type '$($structuredType.public_type)' references unknown evidence '$sourceId'."
        }
    }
}

$assignmentKeys = @($catalog.command_assignments | ForEach-Object {
        "$($_.method)|$($_.inventory_key)|$($_.sdk_order)"
    })
if ($assignmentKeys.Count -ne @($assignmentKeys | Sort-Object -Unique).Count) {
    throw "Exact shared-method assignment keys must be unique."
}
foreach ($shared in $catalog.shared_methods) {
    if (@($shared.families | Where-Object { $_ -notin $familyIds }).Count -ne 0) {
        throw "Shared method '$($shared.method)' references an unknown family."
    }
    $assignments = @($catalog.command_assignments | Where-Object method -eq $shared.method)
    if ($assignments.Count -eq 0) {
        throw "Shared method '$($shared.method)' has no exact command assignments."
    }
    if (@($assignments | Where-Object { $_.family_id -notin $shared.families }).Count -ne 0) {
        throw "Shared method '$($shared.method)' has an assignment outside its reviewed domains."
    }
}
foreach ($assignment in $catalog.command_assignments) {
    foreach ($sourceId in $assignment.evidence_source_ids) {
        if ($sourceId -notin $sourceIds) {
            throw "Assignment '$($assignment.inventory_key)' references unknown evidence '$sourceId'."
        }
    }
}

$review = Get-Content -Raw -LiteralPath $BindingReviewInputPath | ConvertFrom-Json -Depth 100
$review.decision_references = @($review.decision_references +
    "https://github.com/spatialanalyzer/briosa/issues/87" | Sort-Object -Unique)

$overrides = [ordered]@{}
foreach ($shared in $catalog.shared_methods | Sort-Object method) {
    $overrides[$shared.method] = @($shared.families | Sort-Object -Unique)
}
$review.binding_family_overrides = $overrides

$review.argument_family_assignments = @($catalog.command_assignments | ForEach-Object {
        [ordered]@{
            method = $_.method
            inventory_key = $_.inventory_key
            sdk_order = $_.sdk_order
            documented_ordinals = @($_.documented_ordinals)
            family_id = $_.family_id
        }
    })

$families = Convert-PropertiesToOrderedMap $review.families
$families["CollectionItemName"] = [ordered]@{
    family_id = "collection_item_name"
    shape = "identifier"
    public_type_target = "CollectionItemName"
    worker_type_target = "SdkCollectionItemNameValue"
}
$families["CollectionItemNameRefList"] = [ordered]@{
    family_id = "collection_item_name_list"
    shape = "reference_list"
    public_type_target = "CollectionItemNameList"
    worker_type_target = "SdkCollectionItemNameListValue"
}
$sortedFamilies = [ordered]@{}
foreach ($key in @($families.Keys | Sort-Object)) { $sortedFamilies[$key] = $families[$key] }
$review.families = $sortedFamilies
Write-Utf8Json $review $BindingReviewOutputPath

$queueEntries = [Collections.Generic.List[object]]::new()
$corroborated = 0
$reviewedNoDefault = 0
$noCandidate = 0
foreach ($shardPath in Get-ChildItem (Join-Path $repositoryRoot "disposition\sa\$SpatialAnalyzerTarget\categories") -Filter "*.json" -File) {
    $shard = Get-Content -Raw -LiteralPath $shardPath.FullName | ConvertFrom-Json -Depth 100
    foreach ($entry in $shard.entries) {
        if ($entry.disposition -ne "approved_candidate" -or $entry.command_shape.status -ne "resolved") {
            continue
        }
        foreach ($argument in $entry.command_shape.arguments) {
            if ($null -eq $argument.input) { continue }
            if ($argument.input.default.status -eq "reviewed") {
                $corroborated++
                continue
            }
            if ($argument.input.default.status -eq "reviewed_no_default") {
                $reviewedNoDefault++
                continue
            }
            if ($null -ne $argument.input.default.PSObject.Properties["review_status"] -and
                $argument.input.default.review_status -eq "needs_review") {
                $inventoryCommand = $inventoryByKey[$entry.inventory_key]
                if ($argument.inventory_index -lt 0 -or
                    $argument.inventory_index -ge $inventoryCommand.arguments.Count) {
                    throw "Could not resolve one exact SDK order for default-review argument '$($entry.inventory_key)' index $($argument.inventory_index)."
                }
                $inventoryArgument = $inventoryCommand.arguments[$argument.inventory_index]
                if ($inventoryArgument.sdk_binding.setter.method -ne $argument.sdk_binding.setter) {
                    throw "Default-review setter drift for '$($entry.inventory_key)' index $($argument.inventory_index)."
                }
                $queueEntries.Add([ordered]@{
                    inventory_key = $entry.inventory_key
                    mp_step = $entry.command_shape.mp_step
                    argument_ordinal = $argument.ordinal
                    sdk_order = $inventoryArgument.sdk_order
                    mp_name = $argument.mp_name
                    setter = $argument.sdk_binding.setter
                    candidates = @($argument.input.default.candidates)
                })
            }
            else {
                $noCandidate++
            }
        }
    }
}
$queueEntries = @($queueEntries | Sort-Object inventory_key, sdk_order, argument_ordinal, mp_name)
$objectiveSource = @($catalog.sources | Where-Object source_id -eq "objectivesa_secondary")[0]
$queue = [ordered]@{
    '$schema' = "../../../../values/schemas/v1/default-review-queue.schema.json"
    schema_version = 1
    spatial_analyzer_target = $SpatialAnalyzerTarget
    conflict_policy = $catalog.conflict_policy
    objective_sa = [ordered]@{
        repository_url = $objectiveSource.repository_url
        commit = $objectiveSource.commit
        declared_version = $objectiveSource.declared_version
        source_layout = $objectiveSource.source_layout
        sha256 = $objectiveSource.sha256
    }
    summary = [ordered]@{
        corroborated_default_count = $corroborated
        reviewed_no_default_count = $reviewedNoDefault
        needs_review_count = $queueEntries.Count
        no_candidate_count = $noCandidate
    }
    entries = $queueEntries
}
$queuePath = Join-Path $GeneratedOutputDirectory "default-review-queue.json"
Write-Utf8Json $queue $queuePath

$manifest = [ordered]@{
    '$schema' = "../../../../values/schemas/v1/manifest.schema.json"
    schema_version = 1
    spatial_analyzer_target = $SpatialAnalyzerTarget
    catalog = [ordered]@{ path = "../../../../values/sa/$SpatialAnalyzerTarget/catalog.json"; sha256 = Get-Sha256 $CatalogPath }
    binding_review = [ordered]@{ path = "../../../../bindings/sa/$SpatialAnalyzerTarget/review.json"; sha256 = Get-Sha256 $BindingReviewOutputPath }
    default_review_queue = [ordered]@{ path = "default-review-queue.json"; sha256 = Get-Sha256 $queuePath }
    counts = [ordered]@{
        family_count = $catalog.families.Count
        enum_type_count = $catalog.enum_types.Count
        enum_member_count = @($catalog.enum_types.members).Count
        structured_type_count = $catalog.structured_types.Count
        public_field_count = @($catalog.structured_types.public_fields).Count
        worker_field_count = @($catalog.structured_types.worker_fields).Count
        shared_method_count = $catalog.shared_methods.Count
        command_assignment_count = $catalog.command_assignments.Count
    }
}
$manifestPath = Join-Path $GeneratedOutputDirectory "manifest.json"
Write-Utf8Json $manifest $manifestPath

$lines = [Collections.Generic.List[string]]::new()
$lines.Add("# SA $SpatialAnalyzerTarget value-family evidence")
$lines.Add("")
$lines.Add("This generated report summarizes the reviewed exact-target value-family source of truth. Inventory membership and evidence do not approve a public operation.")
$lines.Add("")
$lines.Add("- Families: $($catalog.families.Count)")
$lines.Add("- Exact enum types: $($catalog.enum_types.Count)")
$lines.Add("- Exact enum members and SDK literals: $(@($catalog.enum_types.members).Count)")
$lines.Add("- Structured value types: $($catalog.structured_types.Count)")
$lines.Add("- Public structured fields: $(@($catalog.structured_types.public_fields).Count)")
$lines.Add("- Worker structured fields: $(@($catalog.structured_types.worker_fields).Count)")
$lines.Add("- Shared SDK methods: $($catalog.shared_methods.Count)")
$lines.Add("- Exact command assignments: $($catalog.command_assignments.Count)")
$lines.Add("- ObjectiveSA corroborated defaults: $corroborated")
$lines.Add("- Reviewed candidates retaining required input: $reviewedNoDefault")
$lines.Add("- Defaults awaiting #82 review: $($queueEntries.Count)")
$lines.Add("")
$lines.Add("## Shared-method domains")
$lines.Add("")
$lines.Add("| SDK method | Reviewed families | Assignments |")
$lines.Add("| --- | --- | ---: |")
foreach ($shared in $catalog.shared_methods) {
    $count = @($catalog.command_assignments | Where-Object method -eq $shared.method).Count
    $lines.Add("| ``$($shared.method)`` | ``$($shared.families -join '`, `')`` | $count |")
}
$lines.Add("")
$lines.Add("## Evidence sources")
$lines.Add("")
$lines.Add("| Source | Kind | Fingerprint | Raw material committed |")
$lines.Add("| --- | --- | --- | --- |")
foreach ($source in $catalog.sources) {
    $lines.Add("| ``$($source.source_id)`` | ``$($source.kind)`` | ``$($source.sha256)`` | ``$($source.source_material_committed)`` |")
}
$documentation = ($lines -join "`n") + "`n"
$documentationDirectory = Split-Path -Parent $DocumentationOutputPath
New-Item -ItemType Directory -Path $documentationDirectory -Force | Out-Null
[IO.File]::WriteAllText($DocumentationOutputPath, $documentation, [Text.UTF8Encoding]::new($false))

Write-Host "Synchronized value-family evidence."
Write-Host "Binding assignments: $($catalog.command_assignments.Count)"
Write-Host "Default review queue: $($queueEntries.Count)"
