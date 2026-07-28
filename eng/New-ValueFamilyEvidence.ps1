[CmdletBinding()]
param(
    [string]$SpatialAnalyzerTarget = "2026.1.0529.7",
    [Parameter(Mandatory)][string]$ObjectiveSARoot,
    [Parameter(Mandatory)][string]$InstrumentListPath,
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot "values\sa\$SpatialAnalyzerTarget\catalog.json"
}

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

function Get-TextSha256 {
    param([Parameter(Mandatory)][string]$Text)
    $hex = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text)))
    return $hex.ToLowerInvariant()
}

function Get-ProtoDefinitions {
    param([Parameter(Mandatory)][string[]]$Paths)

    $enums = @{}
    $messages = @{}
    foreach ($path in $Paths) {
        $text = Get-Content -Raw -LiteralPath $path
        foreach ($match in [regex]::Matches($text, '(?ms)^enum\s+(?<name>[A-Za-z0-9_]+)\s*\{(?<body>.*?)^\}')) {
            $members = [Collections.Generic.List[object]]::new()
            foreach ($member in [regex]::Matches(
                    $match.Groups['body'].Value,
                    '(?m)^\s*(?<name>[A-Z][A-Z0-9_]*)\s*=\s*(?<number>[0-9]+)\s*;')) {
                $members.Add([ordered]@{
                    symbol = $member.Groups['name'].Value
                    number = [int]$member.Groups['number'].Value
                })
            }
            $enums[$match.Groups['name'].Value] = $members.ToArray()
        }

        foreach ($match in [regex]::Matches($text, '(?ms)^message\s+(?<name>[A-Za-z0-9_]+)\s*\{(?<body>.*?)^\}')) {
            $fields = [Collections.Generic.List[object]]::new()
            foreach ($field in [regex]::Matches(
                    $match.Groups['body'].Value,
                    '(?m)^\s*(?<presence>optional|repeated)?\s*(?<type>[A-Za-z0-9_.]+)\s+' +
                    '(?<name>[a-z][a-z0-9_]*)\s*=\s*(?<number>[0-9]+)\s*;')) {
                $fields.Add([ordered]@{
                    name = $field.Groups['name'].Value
                    number = [int]$field.Groups['number'].Value
                    type = $field.Groups['type'].Value
                    cardinality = if ($field.Groups['presence'].Success) {
                        $field.Groups['presence'].Value
                    }
                    else {
                        "singular"
                    }
                })
            }
            $messages[$match.Groups['name'].Value] = $fields.ToArray()
        }
    }

    return [pscustomobject]@{ Enums = $enums; Messages = $messages }
}

function Split-TopLevel {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)
    $parts = [Collections.Generic.List[string]]::new()
    $start = 0
    $depth = 0
    for ($index = 0; $index -lt $Value.Length; $index++) {
        switch ($Value[$index]) {
            '(' { $depth++ }
            ')' { $depth-- }
            '<' { $depth++ }
            '>' { $depth-- }
            '[' { $depth++ }
            ']' { $depth-- }
            ',' {
                if ($depth -eq 0) {
                    $parts.Add($Value.Substring($start, $index - $start).Trim())
                    $start = $index + 1
                }
            }
        }
    }
    $parts.Add($Value.Substring($start).Trim())
    return $parts.ToArray()
}

function Get-WorkerDefinitions {
    param([Parameter(Mandatory)][string[]]$Paths)
    $enums = @{}
    $records = @{}
    foreach ($path in $Paths) {
        $text = Get-Content -Raw -LiteralPath $path
        foreach ($match in [regex]::Matches(
                $text,
                '(?ms)^internal\s+enum\s+(?<name>Sdk[A-Za-z0-9_]+Value)\s*\{(?<body>.*?)\}')) {
            $body = [regex]::Replace($match.Groups['body'].Value, '//.*$', '', 'Multiline')
            $members = @(
                (Split-TopLevel $body) |
                    ForEach-Object { ($_ -replace '=.*$', '').Trim() } |
                    Where-Object { $_ -match '^[A-Za-z_][A-Za-z0-9_]*$' })
            $enums[$match.Groups['name'].Value] = $members
        }
        foreach ($match in [regex]::Matches(
                $text,
                '(?ms)^internal\s+sealed\s+record\s+(?<name>Sdk[A-Za-z0-9_]+)\s*\((?<parameters>.*?)\)\s*;')) {
            $fields = [Collections.Generic.List[string]]::new()
            foreach ($parameter in Split-TopLevel $match.Groups['parameters'].Value) {
                if ($parameter -match '(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:=.*)?$') {
                    $fields.Add($Matches['name'])
                }
            }
            $records[$match.Groups['name'].Value] = $fields.ToArray()
        }
    }
    return [pscustomobject]@{ Enums = $enums; Records = $records }
}

function Get-SdkLiterals {
    param([Parameter(Mandatory)][string[]]$Paths)
    $result = @{}
    foreach ($path in $Paths) {
        $text = Get-Content -Raw -LiteralPath $path
        foreach ($match in [regex]::Matches(
                $text,
                '(?<type>Sdk[A-Za-z0-9_]+Value)\.(?<member>[A-Za-z_][A-Za-z0-9_]*)' +
                '\s*=>\s*"(?<literal>(?:\\.|[^"])*)"')) {
            $result["$($match.Groups['type'].Value).$($match.Groups['member'].Value)"] =
                [regex]::Unescape($match.Groups['literal'].Value)
        }
    }
    return $result
}

function Get-InstrumentSnapshot {
    param([Parameter(Mandatory)][string]$Path)
    $lines = Get-Content -LiteralPath $Path
    $records = [Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $lines.Count - 2; $index++) {
        if ($lines[$index] -notmatch '^\s*(?<category>[0-9]+)\s*$') {
            continue
        }
        $category = [int]$Matches['category']
        if ($lines[$index + 1] -notmatch '^\s*[0-9]+\s*(?:\(|$)') {
            continue
        }
        $records.Add([pscustomobject]@{ Category = $category; Name = $lines[$index + 2] })
    }
    $selected = @($records | Where-Object Category -ne 10 | ForEach-Object Name)
    return [pscustomobject]@{
        Records = $records.ToArray()
        Selected = $selected
        SelectedSha256 = Get-TextSha256 (($selected -join "`n") + "`n")
    }
}

function Test-ItemDomain {
    param([Parameter(Mandatory)][string]$DocumentedType, [Parameter(Mandatory)][string]$Name)
    $semanticText = "$DocumentedType $Name"
    return $semanticText -match '(?i)\b(item|picture|callout|chart|dimension|event|feature check|' +
        'relationship|report|annotation|calibration appliance|tcp fixture|table|alignment|sa doc|scale bar)\b'
}

function Find-InventoryArgument {
    param($Inventory, [string]$InventoryKey, [string]$Method, [int]$SdkOrder)
    $command = @($Inventory.commands | Where-Object inventory_key -eq $InventoryKey)
    if ($command.Count -ne 1) { throw "Inventory key '$InventoryKey' is not unique." }
    $matches = @($command[0].arguments | Where-Object {
            $_.sdk_order -eq $SdkOrder -and
            ($_.sdk_binding.setter.method -eq $Method -or $_.sdk_binding.getter.method -eq $Method)
        })
    if ($matches.Count -eq 0) {
        throw "No exact inventory observation for '$Method', '$InventoryKey', SDK order $SdkOrder."
    }
    return [pscustomobject]@{ Command = $command[0]; Arguments = $matches }
}

$targetToken = "v" + ($SpatialAnalyzerTarget -replace '\.', '_')
$targetProtoRoot = Join-Path $repositoryRoot "proto\briosa\sa\$targetToken\v1alpha1"
$protoPaths = @(
    Join-Path $targetProtoRoot "values.proto"
    Join-Path $targetProtoRoot "specialized_values.proto"
)
$workerPaths = @(
    Join-Path $repositoryRoot "src\Briosa.Worker\Sdk\SdkContracts.cs"
    Join-Path $repositoryRoot "src\Briosa.Worker\Sdk\SdkSpecializedValueContracts.cs"
)
$codecPaths = @(
    Join-Path $repositoryRoot "src\Briosa.Worker\Sdk\SdkSpecializedValueCodec.cs"
    Join-Path $repositoryRoot "src\Briosa.Worker\Sdk\SpatialAnalyzerSdkAdapter.cs"
)
$inventoryPath = Join-Path $repositoryRoot "inventory\sa\$SpatialAnalyzerTarget\inventory.json"
$registryPath = Join-Path $repositoryRoot "bindings\sa\$SpatialAnalyzerTarget\registry.json"
$reviewPath = Join-Path $repositoryRoot "bindings\sa\$SpatialAnalyzerTarget\review.json"
$interopApiPath = Join-Path $repositoryRoot "interop\SpatialAnalyzer\$SpatialAnalyzerTarget\Briosa.SpatialAnalyzer.Interop.PublicApi.txt"
$inventory = Get-Content -Raw -LiteralPath $inventoryPath | ConvertFrom-Json -Depth 100
$registry = Get-Content -Raw -LiteralPath $registryPath | ConvertFrom-Json -Depth 100
$review = Get-Content -Raw -LiteralPath $reviewPath | ConvertFrom-Json -Depth 100
$proto = Get-ProtoDefinitions $protoPaths
$worker = Get-WorkerDefinitions $workerPaths
$sdkLiterals = Get-SdkLiterals $codecPaths
$instrument = Get-InstrumentSnapshot $InstrumentListPath

$objectiveSafeDirectory = $ObjectiveSARoot.Replace('\', '/')
$objectiveCommit = (& git -c "safe.directory=$objectiveSafeDirectory" -C $ObjectiveSARoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw "Could not read the ObjectiveSA commit." }
$expectedObjectiveCommit = "324c73b8e172868b4ccb4a0121e3bd1cbc520c5c"
if ($objectiveCommit -ne $expectedObjectiveCommit) {
    throw "ObjectiveSA commit '$objectiveCommit' does not match '$expectedObjectiveCommit'."
}
$objectiveProjectPath = Join-Path $ObjectiveSARoot "ObjectiveSA\ObjectiveSA.csproj"
$objectiveProject = [xml](Get-Content -Raw -LiteralPath $objectiveProjectPath)
$objectiveVersion = [string]$objectiveProject.Project.PropertyGroup.Version
if ($objectiveVersion -ne "2024.1.5.1") {
    throw "ObjectiveSA declared version '$objectiveVersion' is not the reviewed 2024.1.5.1 baseline."
}
$objectiveFiles = @(Get-ChildItem (Join-Path $ObjectiveSARoot "ObjectiveSA\Methods") -Filter "*.cs" -Recurse -File |
        Sort-Object { [IO.Path]::GetRelativePath($ObjectiveSARoot, $_.FullName) })
$objectiveSnapshotLines = foreach ($file in $objectiveFiles) {
    $relative = [IO.Path]::GetRelativePath($ObjectiveSARoot, $file.FullName).Replace('\', '/')
    "$relative|$(Get-Sha256 $file.FullName)"
}
$objectiveSourceSha = Get-TextSha256 (($objectiveSnapshotLines -join "`n") + "`n")

if ($instrument.Records.Count -ne 195 -or $instrument.Selected.Count -ne 190 -or
    @($instrument.Records | Where-Object Category -eq 10).Count -ne 5) {
    throw "Instrument.lst selection did not reproduce 195 total, 190 selected, and 5 category-10 records."
}

$families = [Collections.Generic.List[object]]::new()
foreach ($family in $registry.value_families | Sort-Object family_id) {
    $families.Add([ordered]@{
        family_id = $family.family_id
        shape = $family.shape
        public_type_target = $family.public_type_target
        worker_type_target = $family.worker_type_target
        implementation_status = $family.implementation_status
        binding_methods = @($family.binding_methods)
        evidence_source_ids = @("exact_target_interop", "installed_command_documentation", "view_sdk_code")
    })
}
if (-not @($families | Where-Object family_id -eq "collection_item_name")) {
    $families.Add([ordered]@{
        family_id = "collection_item_name"
        shape = "identifier"
        public_type_target = "CollectionItemName"
        worker_type_target = "SdkCollectionItemNameValue"
        implementation_status = "implemented"
        binding_methods = @("GetCollectionObjectNameArg", "SetCollectionObjectNameArg2")
        evidence_source_ids = @("installed_command_documentation", "view_sdk_code")
    })
}
if (-not @($families | Where-Object family_id -eq "collection_item_name_list")) {
    $families.Add([ordered]@{
        family_id = "collection_item_name_list"
        shape = "reference_list"
        public_type_target = "CollectionItemNameList"
        worker_type_target = "SdkCollectionItemNameListValue"
        implementation_status = "implemented"
        binding_methods = @("GetCollectionObjectNameRefListArg", "SetCollectionObjectNameRefListArg")
        evidence_source_ids = @("installed_command_documentation", "view_sdk_code")
    })
}
$families = @($families | Sort-Object family_id)

$enumTypes = [Collections.Generic.List[object]]::new()
foreach ($enumName in @($proto.Enums.Keys | Sort-Object)) {
    $workerType = "Sdk${enumName}Value"
    if (-not $worker.Enums.ContainsKey($workerType)) {
        throw "Public enum '$enumName' has no exact worker enum '$workerType'."
    }
    $publicMembers = @($proto.Enums[$enumName] | Where-Object number -ne 0)
    $workerMembers = @($worker.Enums[$workerType] | Where-Object { $_ -ne "Unspecified" })
    if ($publicMembers.Count -ne $workerMembers.Count) {
        throw "Enum '$enumName' has $($publicMembers.Count) public and $($workerMembers.Count) worker members."
    }
    $members = [Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $publicMembers.Count; $index++) {
        $workerSymbol = $workerMembers[$index]
        $literalKey = "$workerType.$workerSymbol"
        if (-not $sdkLiterals.ContainsKey($literalKey)) {
            throw "Enum member '$literalKey' has no exact SDK literal mapping."
        }
        $sourceIds = if ($enumName -eq "InstrumentType") {
            @("instrument_list", "objectivesa_secondary")
        }
        else {
            @("installed_command_documentation", "view_sdk_code")
        }
        $members.Add([ordered]@{
            public_symbol = $publicMembers[$index].symbol
            public_number = $publicMembers[$index].number
            worker_symbol = $workerSymbol
            sdk_literal = $sdkLiterals[$literalKey]
            evidence_source_ids = $sourceIds
        })
    }
    $enumTypes.Add([ordered]@{
        public_type = $enumName
        worker_type = $workerType
        members = $members.ToArray()
    })
}

$instrumentEnum = @($enumTypes | Where-Object public_type -eq "InstrumentType")
$instrumentLiterals = @($instrumentEnum[0].members | ForEach-Object sdk_literal)
if (@($instrument.Selected | Where-Object { $_ -cnotin $instrumentLiterals }).Count -ne 0 -or
    @($instrumentLiterals | Where-Object { $_ -cnotin $instrument.Selected }).Count -ne 0) {
    throw "InstrumentType literals do not exactly match the category-10-excluded Instrument.lst snapshot."
}

$workerMessageOverrides = @{
    ScalarToleranceLimit = "SdkToleranceLimit"
    ToleranceLimit = "SdkToleranceLimit"
    Vector3 = "SdkVectorValue"
}
$structuredTypes = [Collections.Generic.List[object]]::new()
foreach ($messageName in @($proto.Messages.Keys | Sort-Object)) {
    $workerType = if ($workerMessageOverrides.ContainsKey($messageName)) {
        $workerMessageOverrides[$messageName]
    }
    else {
        "Sdk${messageName}Value"
    }
    if (-not $worker.Records.ContainsKey($workerType)) {
        throw "Public value message '$messageName' has no exact worker record '$workerType'."
    }
    $structuredTypes.Add([ordered]@{
        public_type = $messageName
        worker_type = $workerType
        public_fields = @($proto.Messages[$messageName])
        worker_fields = @($worker.Records[$workerType])
        evidence_source_ids = @("exact_target_interop", "installed_command_documentation", "view_sdk_code")
    })
}

$assignmentMethods = @(
    "GetCollectionObjectNameArg",
    "GetCollectionObjectNameRefListArg",
    "SetAsciiFileFormatArg",
    "SetAxisNameArg",
    "SetCollectionObjectNameArg2",
    "SetCollectionObjectNameRefListArg"
)
$collectionMethods = $assignmentMethods | Where-Object { $_ -match "CollectionObjectName" }
$assignments = [Collections.Generic.List[object]]::new()

foreach ($existing in $review.argument_family_assignments) {
    if ($existing.method -in $collectionMethods) { continue }
    $observation = Find-InventoryArgument $inventory $existing.inventory_key $existing.method $existing.sdk_order
    $argument = $observation.Arguments[0]
    $assignments.Add([ordered]@{
        method = $existing.method
        inventory_key = $existing.inventory_key
        sdk_order = $existing.sdk_order
        documented_ordinals = @($existing.documented_ordinals)
        family_id = $existing.family_id
        mp_name = $argument.mp_name
        documented_type = $argument.documented_type
        evidence_source_ids = @(
            if ($null -ne $observation.Command.documentation) { "installed_command_documentation" }
            "view_sdk_code"
        )
    })
}

foreach ($command in $inventory.commands) {
    $groups = @{}
    foreach ($argument in $command.arguments) {
        foreach ($side in @("setter", "getter")) {
            $method = $argument.sdk_binding.$side.method
            if ($method -notin $collectionMethods) { continue }
            $key = "$method|$($argument.sdk_order)"
            if (-not $groups.ContainsKey($key)) { $groups[$key] = [Collections.Generic.List[object]]::new() }
            $groups[$key].Add($argument)
        }
    }
    foreach ($key in @($groups.Keys | Sort-Object)) {
        $parts = $key.Split('|')
        $method = $parts[0]
        $sdkOrder = [int]$parts[1]
        $arguments = @($groups[$key])
        $itemFlags = @($arguments | ForEach-Object { Test-ItemDomain $_.documented_type $_.mp_name } | Sort-Object -Unique)
        if ($itemFlags.Count -ne 1) {
            throw "Conflicting object/item evidence for '$method', '$($command.inventory_key)', SDK order $sdkOrder."
        }
        $isList = $method -match "RefListArg$"
        $familyId = if ($itemFlags[0]) {
            if ($isList) { "collection_item_name_list" } else { "collection_item_name" }
        }
        else {
            if ($isList) { "collection_object_name_list" } else { "collection_object_name" }
        }
        $assignments.Add([ordered]@{
            method = $method
            inventory_key = $command.inventory_key
            sdk_order = $sdkOrder
            documented_ordinals = @($arguments.ordinal | Where-Object { $null -ne $_ } | Sort-Object -Unique)
            family_id = $familyId
            mp_name = $arguments[0].mp_name
            documented_type = $arguments[0].documented_type
            evidence_source_ids = @(
                if ($null -ne $command.documentation) { "installed_command_documentation" }
                "view_sdk_code"
            )
        })
    }
}
$assignments = @($assignments | Sort-Object method, inventory_key, sdk_order)

$catalog = [ordered]@{
    '$schema' = "../../schemas/v1/catalog.schema.json"
    schema_version = 1
    spatial_analyzer_target = $SpatialAnalyzerTarget
    conflict_policy = "Exact SA $SpatialAnalyzerTarget evidence wins. ObjectiveSA is pinned secondary evidence; conflicts remain needs_review."
    sources = @(
        [ordered]@{ source_id = "exact_target_interop"; kind = "exact_target_interop_public_api"; sha256 = Get-Sha256 $interopApiPath; source_material_committed = $true },
        [ordered]@{ source_id = "installed_command_documentation"; kind = "installed_mp_documentation"; sha256 = $inventory.provenance.documentation.aggregate_sha256; record_count = $inventory.provenance.documentation.record_count; source_material_committed = $false },
        [ordered]@{ source_id = "instrument_list"; kind = "installed_instrument_model_list"; sha256 = Get-Sha256 $InstrumentListPath; source_material_committed = $false; selection_rule = "Parse record headers; exclude category 10 stand/mount graphics; preserve source order and exact model-name text."; record_count = $instrument.Records.Count; selected_count = $instrument.Selected.Count; excluded_count = @($instrument.Records | Where-Object Category -eq 10).Count; selected_sha256 = $instrument.SelectedSha256 },
        [ordered]@{ source_id = "objectivesa_secondary"; kind = "prior_release_secondary_evidence"; repository_url = "https://github.com/spatialanalyzer/ObjectiveSA"; commit = $objectiveCommit; declared_version = $objectiveVersion; source_layout = "ObjectiveSA/Methods (colocated I*.cs interfaces and implementations)"; file_count = $objectiveFiles.Count; sha256 = $objectiveSourceSha; source_material_committed = $false },
        [ordered]@{ source_id = "view_sdk_code"; kind = "generated_sdk_sample"; sha256 = $inventory.provenance.sdk_code.aggregate_sha256; record_count = $inventory.provenance.sdk_code.record_count; source_material_committed = $false }
    )
    tracked_inputs = @(
        [ordered]@{ path = "../../../inventory/sa/$SpatialAnalyzerTarget/inventory.json"; sha256 = Get-Sha256 $inventoryPath },
        [ordered]@{ path = "../../../interop/SpatialAnalyzer/$SpatialAnalyzerTarget/Briosa.SpatialAnalyzer.Interop.PublicApi.txt"; sha256 = Get-Sha256 $interopApiPath }
    )
    shared_methods = @(
        [ordered]@{ method = "GetCollectionObjectNameArg"; families = @("collection_item_name", "collection_object_name") },
        [ordered]@{ method = "GetCollectionObjectNameRefListArg"; families = @("collection_item_name_list", "collection_object_name_list") },
        [ordered]@{ method = "SetAsciiFileFormatArg"; families = @("ascii_frame_set_format", "ascii_import_file_format") },
        [ordered]@{ method = "SetAxisNameArg"; families = @("axis_identifier", "wcf_axis_identifier") },
        [ordered]@{ method = "SetCollectionObjectNameArg2"; families = @("collection_item_name", "collection_object_name") },
        [ordered]@{ method = "SetCollectionObjectNameRefListArg"; families = @("collection_item_name_list", "collection_object_name_list") }
    )
    families = $families
    enum_types = @($enumTypes)
    structured_types = @($structuredTypes)
    command_assignments = $assignments
}

$json = ($catalog | ConvertTo-Json -Depth 100).Replace("`r`n", "`n").Replace("`r", "`n") + "`n"
$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
[IO.File]::WriteAllText($OutputPath, $json, [Text.UTF8Encoding]::new($false))
Write-Host "Wrote value-family evidence catalog: $OutputPath"
Write-Host "Families: $($families.Count)"
Write-Host "Enum types: $($enumTypes.Count)"
Write-Host "Structured types: $($structuredTypes.Count)"
Write-Host "Shared-method assignments: $($assignments.Count)"
