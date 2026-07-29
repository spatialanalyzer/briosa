[CmdletBinding()]
param(
    [string]$SpatialAnalyzerTarget = "2026.1.0529.7",
    [string]$ObjectiveSARoot,
    [string]$InstrumentListPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$catalogPath = Join-Path $repositoryRoot "values\sa\$SpatialAnalyzerTarget\catalog.json"
$reviewPath = Join-Path $repositoryRoot "bindings\sa\$SpatialAnalyzerTarget\review.json"
$generatedDirectory = Join-Path $repositoryRoot "generated\values\sa\$SpatialAnalyzerTarget"
$documentationPath = Join-Path $repositoryRoot "docs\reference\generated\sa\$SpatialAnalyzerTarget\value-families.md"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-values-$([Guid]::NewGuid().ToString('N'))"

function Test-JsonDocument {
    param([Parameter(Mandatory)][string]$DocumentPath, [Parameter(Mandatory)][string]$SchemaPath)
    if (-not (Test-Json -Json (Get-Content -Raw -LiteralPath $DocumentPath) -SchemaFile $SchemaPath)) {
        throw "JSON Schema validation failed for '$DocumentPath'."
    }
}

function Get-Sha256 {
    param([Parameter(Mandatory)][string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

function Assert-JsonEqual {
    param([Parameter(Mandatory)]$Expected, [Parameter(Mandatory)]$Actual, [Parameter(Mandatory)][string]$Description)
    $expectedJson = $Expected | ConvertTo-Json -Depth 100 -Compress
    $actualJson = $Actual | ConvertTo-Json -Depth 100 -Compress
    if ($expectedJson -cne $actualJson) { throw "$Description differs from the committed evidence catalog." }
}

function Assert-FileEqual {
    param([Parameter(Mandatory)][string]$ExpectedPath, [Parameter(Mandatory)][string]$ActualPath)
    if (-not (Test-Path -LiteralPath $ActualPath -PathType Leaf) -or
        (Get-Sha256 $ExpectedPath) -cne (Get-Sha256 $ActualPath)) {
        throw "Generated value-family artifact is stale: $ExpectedPath"
    }
}

function Get-EvidenceSummary {
    param([string[]]$SourceIds = @())

    $relativeCatalogPath = [IO.Path]::GetRelativePath(
        $repositoryRoot,
        $catalogPath).Replace('\', '/')
    $parts = [Collections.Generic.List[string]]::new()
    $parts.Add("$relativeCatalogPath=$(Get-Sha256 $catalogPath)")
    foreach ($sourceId in @($SourceIds | Sort-Object -Unique)) {
        $source = @($catalog.sources | Where-Object source_id -CEQ $sourceId)
        if ($source.Count -eq 1) {
            $parts.Add("$sourceId=$($source[0].sha256)")
        }
    }
    return $parts -join "; "
}

function Assert-MemberRecordsEqual {
    param(
        [Parameter(Mandatory)][object[]]$Expected,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Actual,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][string]$AffectedSurface
    )

    $expectedByKey = @{}
    foreach ($record in $Expected) { $expectedByKey[$record.key] = $record }
    $actualByKey = @{}
    foreach ($record in $Actual) { $actualByKey[$record.key] = $record }
    $differences = [Collections.Generic.List[string]]::new()
    foreach ($key in @($expectedByKey.Keys + $actualByKey.Keys | Sort-Object -Unique)) {
        $expectedRecord = $expectedByKey[$key]
        $actualRecord = $actualByKey[$key]
        $expectedValue = if ($null -eq $expectedRecord) { "missing" } else { $expectedRecord.value }
        $actualValue = if ($null -eq $actualRecord) { "missing" } else { $actualRecord.value }
        if ($expectedValue -cne $actualValue) {
            $sourceIds = if ($null -eq $expectedRecord) {
                @()
            }
            else {
                @($expectedRecord.evidence_source_ids)
            }
            $differences.Add(
                "member '$key' expected '$expectedValue' but found '$actualValue'. " +
                "Evidence: $(Get-EvidenceSummary -SourceIds $sourceIds). " +
                "Affected generated surface: $AffectedSurface")
        }
    }
    if ($differences.Count -ne 0) {
        throw "$Description member-level drift:`n$($differences -join "`n")"
    }
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

function Get-ProtoDefinitions {
    param([Parameter(Mandatory)][string[]]$Paths)
    $enums = @{}
    $messages = @{}
    foreach ($path in $Paths) {
        $text = Get-Content -Raw -LiteralPath $path
        foreach ($match in [regex]::Matches($text, '(?ms)^enum\s+(?<name>[A-Za-z0-9_]+)\s*\{(?<body>.*?)^\}')) {
            $members = [Collections.Generic.List[object]]::new()
            foreach ($member in [regex]::Matches($match.Groups['body'].Value, '(?m)^\s*(?<name>[A-Z][A-Z0-9_]*)\s*=\s*(?<number>[0-9]+)\s*;')) {
                if ([int]$member.Groups['number'].Value -eq 0) { continue }
                $members.Add([ordered]@{ public_symbol = $member.Groups['name'].Value; public_number = [int]$member.Groups['number'].Value })
            }
            $enums[$match.Groups['name'].Value] = $members.ToArray()
        }
        foreach ($match in [regex]::Matches($text, '(?ms)^message\s+(?<name>[A-Za-z0-9_]+)\s*\{(?<body>.*?)^\}')) {
            $fields = [Collections.Generic.List[object]]::new()
            foreach ($field in [regex]::Matches($match.Groups['body'].Value, '(?m)^\s*(?<presence>optional|repeated)?\s*(?<type>[A-Za-z0-9_.]+)\s+(?<name>[a-z][a-z0-9_]*)\s*=\s*(?<number>[0-9]+)\s*;')) {
                $fields.Add([ordered]@{
                    name = $field.Groups['name'].Value
                    number = [int]$field.Groups['number'].Value
                    type = $field.Groups['type'].Value
                    cardinality = if ($field.Groups['presence'].Success) { $field.Groups['presence'].Value } else { "singular" }
                })
            }
            $messages[$match.Groups['name'].Value] = $fields.ToArray()
        }
    }
    return [pscustomobject]@{ Enums = $enums; Messages = $messages }
}

function Get-WorkerDefinitions {
    param([Parameter(Mandatory)][string[]]$Paths)
    $enums = @{}
    $records = @{}
    foreach ($path in $Paths) {
        $text = Get-Content -Raw -LiteralPath $path
        foreach ($match in [regex]::Matches($text, '(?ms)^internal\s+enum\s+(?<name>Sdk[A-Za-z0-9_]+Value)\s*\{(?<body>.*?)\}')) {
            $body = [regex]::Replace($match.Groups['body'].Value, '//.*$', '', 'Multiline')
            $enums[$match.Groups['name'].Value] = @((Split-TopLevel $body) |
                ForEach-Object { ($_ -replace '=.*$', '').Trim() } |
                Where-Object { $_ -match '^[A-Za-z_][A-Za-z0-9_]*$' -and $_ -ne 'Unspecified' })
        }
        foreach ($match in [regex]::Matches($text, '(?ms)^internal\s+sealed\s+record\s+(?<name>Sdk[A-Za-z0-9_]+)\s*\((?<parameters>.*?)\)\s*;')) {
            $fields = [Collections.Generic.List[string]]::new()
            foreach ($parameter in Split-TopLevel $match.Groups['parameters'].Value) {
                if ($parameter -match '(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:=.*)?$') { $fields.Add($Matches['name']) }
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
        foreach ($match in [regex]::Matches($text, '(?<type>Sdk[A-Za-z0-9_]+Value)\.(?<member>[A-Za-z_][A-Za-z0-9_]*)\s*=>\s*"(?<literal>(?:\\.|[^"])*)"')) {
            $result["$($match.Groups['type'].Value).$($match.Groups['member'].Value)"] = [regex]::Unescape($match.Groups['literal'].Value)
        }
    }
    return $result
}

function Invoke-Synchronization {
    param([Parameter(Mandatory)][string]$Root)
    $reviewOutput = Join-Path $Root "review.json"
    $generatedOutput = Join-Path $Root "generated"
    $documentationOutput = Join-Path $Root "value-families.md"
    & (Join-Path $PSScriptRoot "Sync-ValueFamilyEvidence.ps1") `
        -SpatialAnalyzerTarget $SpatialAnalyzerTarget `
        -CatalogPath $catalogPath `
        -BindingReviewInputPath $reviewPath `
        -BindingReviewOutputPath $reviewOutput `
        -GeneratedOutputDirectory $generatedOutput `
        -DocumentationOutputPath $documentationOutput
    return [pscustomobject]@{ Review = $reviewOutput; Generated = $generatedOutput; Documentation = $documentationOutput }
}

try {
    Test-JsonDocument $catalogPath (Join-Path $repositoryRoot "values\schemas\v1\catalog.schema.json")
    Test-JsonDocument (Join-Path $generatedDirectory "manifest.json") (Join-Path $repositoryRoot "values\schemas\v1\manifest.schema.json")
    Test-JsonDocument (Join-Path $generatedDirectory "default-review-queue.json") (Join-Path $repositoryRoot "values\schemas\v1\default-review-queue.schema.json")
    $catalog = Get-Content -Raw -LiteralPath $catalogPath | ConvertFrom-Json -Depth 100
    $defaultReviewQueue = Get-Content -Raw -LiteralPath (Join-Path $generatedDirectory "default-review-queue.json") | ConvertFrom-Json -Depth 100
    if ($defaultReviewQueue.summary.needs_review_count -ne @($defaultReviewQueue.entries).Count) {
        throw "Default-review queue summary does not match its entries."
    }
    if ($SpatialAnalyzerTarget -eq "2026.1.0529.7" -and
        ($defaultReviewQueue.summary.reviewed_no_default_count -ne 314 -or
            $defaultReviewQueue.summary.needs_review_count -ne 0 -or
            @($defaultReviewQueue.entries).Count -ne 0)) {
        throw "The accepted issue #82 decisions require 314 reviewed-no-default inputs and an empty pending queue."
    }
    $registry = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot "bindings\sa\$SpatialAnalyzerTarget\registry.json") | ConvertFrom-Json -Depth 100
    $bindingReview = Get-Content -Raw -LiteralPath $reviewPath | ConvertFrom-Json -Depth 100

    $catalogFamilies = @($catalog.families | Sort-Object family_id | ForEach-Object {
            [ordered]@{
                family_id = $_.family_id
                shape = $_.shape
                public_type_target = $_.public_type_target
                worker_type_target = $_.worker_type_target
                implementation_status = $_.implementation_status
                binding_methods = @($_.binding_methods)
            }
        })
    $registryFamilies = @($registry.value_families | Sort-Object family_id | ForEach-Object {
            [ordered]@{
                family_id = $_.family_id
                shape = $_.shape
                public_type_target = $_.public_type_target
                worker_type_target = $_.worker_type_target
                implementation_status = $_.implementation_status
                binding_methods = @($_.binding_methods)
            }
        })
    Assert-JsonEqual $catalogFamilies $registryFamilies "Binding-registry value-family inventory"

    $targetToken = "v" + ($SpatialAnalyzerTarget -replace '\.', '_')
    $targetProtoRoot = Join-Path $repositoryRoot "proto\briosa\sa\$targetToken\v1alpha1"
    $proto = Get-ProtoDefinitions @((Join-Path $targetProtoRoot "values.proto"), (Join-Path $targetProtoRoot "specialized_values.proto"))
    $worker = Get-WorkerDefinitions @((Join-Path $repositoryRoot "src\Briosa.Worker\Sdk\SdkContracts.cs"), (Join-Path $repositoryRoot "src\Briosa.Worker\Sdk\SdkSpecializedValueContracts.cs"))
    $sdkLiterals = Get-SdkLiterals @((Join-Path $repositoryRoot "src\Briosa.Worker\Sdk\SdkSpecializedValueCodec.cs"), (Join-Path $repositoryRoot "src\Briosa.Worker\Sdk\SpatialAnalyzerSdkAdapter.cs"))

    Assert-JsonEqual @($catalog.enum_types.public_type | Sort-Object) @($proto.Enums.Keys | Sort-Object) "Public enum type inventory"
    foreach ($enumType in $catalog.enum_types) {
        $expectedPublicMembers = @($enumType.members | ForEach-Object {
                [pscustomobject]@{
                    key = $_.public_symbol
                    value = "$($_.public_number)"
                    evidence_source_ids = @($_.evidence_source_ids)
                }
            })
        $actualPublicMembers = @($proto.Enums[$enumType.public_type] | ForEach-Object {
                [pscustomobject]@{
                    key = $_.public_symbol
                    value = "$($_.public_number)"
                    evidence_source_ids = @()
                }
            })
        Assert-MemberRecordsEqual `
            -Expected $expectedPublicMembers `
            -Actual $actualPublicMembers `
            -Description "Public enum '$($enumType.public_type)'" `
            -AffectedSurface "proto/briosa/sa/$targetToken/v1alpha1"

        $expectedWorkerMembers = @($enumType.members | ForEach-Object {
                [pscustomobject]@{
                    key = $_.worker_symbol
                    value = $_.worker_symbol
                    evidence_source_ids = @($_.evidence_source_ids)
                }
            })
        $actualWorkerMembers = @($worker.Enums[$enumType.worker_type] | ForEach-Object {
                [pscustomobject]@{
                    key = $_
                    value = $_
                    evidence_source_ids = @()
                }
            })
        Assert-MemberRecordsEqual `
            -Expected $expectedWorkerMembers `
            -Actual $actualWorkerMembers `
            -Description "Worker enum '$($enumType.worker_type)'" `
            -AffectedSurface "src/Briosa.Worker/Sdk"
    }
    $expectedSdkLiterals = @($catalog.enum_types | ForEach-Object {
            $enumType = $_
            $enumType.members | ForEach-Object {
                [pscustomobject]@{
                    key = "$($enumType.worker_type).$($_.worker_symbol)"
                    value = $_.sdk_literal
                    evidence_source_ids = @($_.evidence_source_ids)
                }
            }
        })
    $actualSdkLiterals = @($sdkLiterals.Keys | ForEach-Object {
            [pscustomobject]@{
                key = $_
                value = $sdkLiterals[$_]
                evidence_source_ids = @()
            }
        })
    Assert-MemberRecordsEqual `
        -Expected $expectedSdkLiterals `
        -Actual $actualSdkLiterals `
        -Description "SDK enum literals" `
        -AffectedSurface "src/Briosa.Worker/Sdk"

    Assert-JsonEqual @($catalog.structured_types.public_type | Sort-Object) @($proto.Messages.Keys | Sort-Object) "Public structured type inventory"
    foreach ($structuredType in $catalog.structured_types) {
        $expectedPublicFields = @($structuredType.public_fields | ForEach-Object {
                [pscustomobject]@{
                    key = $_.name
                    value = "$($_.number)|$($_.type)|$($_.cardinality)"
                    evidence_source_ids = @($structuredType.evidence_source_ids)
                }
            })
        $actualPublicFields = @($proto.Messages[$structuredType.public_type] | ForEach-Object {
                [pscustomobject]@{
                    key = $_.name
                    value = "$($_.number)|$($_.type)|$($_.cardinality)"
                    evidence_source_ids = @()
                }
            })
        Assert-MemberRecordsEqual `
            -Expected $expectedPublicFields `
            -Actual $actualPublicFields `
            -Description "Public fields for '$($structuredType.public_type)'" `
            -AffectedSurface "proto/briosa/sa/$targetToken/v1alpha1"

        $expectedWorkerFields = @($structuredType.worker_fields | ForEach-Object {
                [pscustomobject]@{
                    key = $_
                    value = $_
                    evidence_source_ids = @($structuredType.evidence_source_ids)
                }
            })
        $actualWorkerFields = @($worker.Records[$structuredType.worker_type] | ForEach-Object {
                [pscustomobject]@{
                    key = $_
                    value = $_
                    evidence_source_ids = @()
                }
            })
        Assert-MemberRecordsEqual `
            -Expected $expectedWorkerFields `
            -Actual $actualWorkerFields `
            -Description "Worker fields for '$($structuredType.worker_type)'" `
            -AffectedSurface "src/Briosa.Worker/Sdk"
    }

    $expectedAssignments = @($catalog.command_assignments | ForEach-Object {
            [pscustomobject]@{
                key = "$($_.method)|$($_.inventory_key)|$($_.sdk_order)"
                value = "$($_.family_id)|$(@($_.documented_ordinals) -join ',')"
                evidence_source_ids = @($_.evidence_source_ids)
            }
        })
    $actualAssignments = @($bindingReview.argument_family_assignments | ForEach-Object {
            [pscustomobject]@{
                key = "$($_.method)|$($_.inventory_key)|$($_.sdk_order)"
                value = "$($_.family_id)|$(@($_.documented_ordinals) -join ',')"
                evidence_source_ids = @()
            }
        })
    Assert-MemberRecordsEqual `
        -Expected $expectedAssignments `
        -Actual $actualAssignments `
        -Description "Command-argument family assignments" `
        -AffectedSurface "bindings/sa/$SpatialAnalyzerTarget/review.json"

    $first = Invoke-Synchronization (Join-Path $temporaryRoot "first")
    $second = Invoke-Synchronization (Join-Path $temporaryRoot "second")
    Assert-FileEqual $first.Review $second.Review
    Assert-FileEqual (Join-Path $first.Generated "default-review-queue.json") (Join-Path $second.Generated "default-review-queue.json")
    Assert-FileEqual (Join-Path $first.Generated "manifest.json") (Join-Path $second.Generated "manifest.json")
    Assert-FileEqual $first.Documentation $second.Documentation
    Assert-FileEqual $reviewPath $first.Review
    Assert-FileEqual (Join-Path $generatedDirectory "default-review-queue.json") (Join-Path $first.Generated "default-review-queue.json")
    Assert-FileEqual (Join-Path $generatedDirectory "manifest.json") (Join-Path $first.Generated "manifest.json")
    Assert-FileEqual $documentationPath $first.Documentation

    if ([string]::IsNullOrWhiteSpace($ObjectiveSARoot) -xor [string]::IsNullOrWhiteSpace($InstrumentListPath)) {
        throw "ObjectiveSARoot and InstrumentListPath must be supplied together."
    }
    if (-not [string]::IsNullOrWhiteSpace($ObjectiveSARoot)) {
        $externalCatalog = Join-Path $temporaryRoot "external-catalog.json"
        & (Join-Path $PSScriptRoot "New-ValueFamilyEvidence.ps1") `
            -SpatialAnalyzerTarget $SpatialAnalyzerTarget `
            -ObjectiveSARoot $ObjectiveSARoot `
            -InstrumentListPath $InstrumentListPath `
            -OutputPath $externalCatalog
        Assert-FileEqual $catalogPath $externalCatalog
    }

    Write-Host "Verified exact-target value-family evidence: $($catalog.families.Count) families, $(@($catalog.enum_types.members).Count) enum literals, $($catalog.command_assignments.Count) command assignments."
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
