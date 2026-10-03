<#
.SYNOPSIS
Curates the SA 2024.1.0508.5 binding facts for the implemented operation subset.

.DESCRIPTION
Reads the registered operations from this target's handwritten registry, the
published briosa-docs MP command catalog at an exact commit, and, only when
-EvidenceRepository is supplied, the private briosa-evidence command-step exports
at revision 07acae44. It writes evidence/sa/2024.1.0508.5/bindings.json.

The private exports are consulted for one purpose: restoring embedded double
quotes that the published catalog strips from argument labels. No other fact is
taken from them, and no export text is written except those corrected labels.
Without -EvidenceRepository, previously committed quote corrections are reused.

This is a local maintainer procedure. CI never runs it and never reads private
evidence; the portable test reads only the committed fact file. The file is
reference evidence and must not be used to generate operations.

.EXAMPLE
./eng/Update-Sa2024BindingFacts.ps1 -DocsRepository ../../../briosa-docs `
    -EvidenceRepository ../../../briosa-evidence

.EXAMPLE
./eng/Update-Sa2024BindingFacts.ps1 -DocsRepository ../../../briosa-docs `
    -DocsRevision e8eccbe8687434409fdc29895ac7410fd9f11be0 -Check
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$DocsRepository,
    [string]$DocsRevision = "origin/main",
    [string]$EvidenceRepository,
    [string]$OutputPath,
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$EvidenceRevision = "07acae44ad6cbed11391b38391317c4974910af8"
$EvidenceProvenance = "briosa-evidence@07acae4"
$EvidenceExportRoot = "2024.1.0508.5/mp-command-step-exports"
$DocsProvenance = "briosa-docs"
$CatalogPath = "mp-command-catalog/commands"
$Target = "2024.1.0508.5"

$targetRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputPath) {
    $OutputPath = Join-Path $targetRoot "evidence/sa/$Target/bindings.json"
}
$OutputPath = [IO.Path]::GetFullPath($OutputPath)

function Invoke-GitText {
    param([Parameter(Mandatory)][string]$Repository, [Parameter(Mandatory)][string[]]$Arguments)
    $info = [Diagnostics.ProcessStartInfo]::new("git")
    $info.ArgumentList.Add("-C")
    $info.ArgumentList.Add($Repository)
    foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.UseShellExecute = $false
    $info.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $process = [Diagnostics.Process]::Start($info)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "git $($Arguments -join ' ') failed in '$Repository': $($stderr.Result)"
    }
    return $stdout.Result.Replace("`r`n", "`n").TrimStart([char]0xFEFF)
}

# --- Registered operations (handwritten registry; never generated from this file) ---

function Get-RegisteredOperations {
    $operationsRoot = Join-Path $targetRoot "src/Briosa.Server/Operations"
    $registry = Get-Content -LiteralPath (Join-Path $operationsRoot "SpatialAnalyzerApi.cs") -Raw
    $classes = [Text.RegularExpressions.Regex]::Matches(
        $registry, '(?m)^\s+([A-Za-z0-9_]+)\.Descriptor,?\s*$') | ForEach-Object { $_.Groups[1].Value }

    $string = '"(?:[^"\\]|\\.)*"'
    $token = "(?:$string|[A-Za-z_][A-Za-z0-9_.]*)"
    $descriptor = [regex]::new(
        'OperationDescriptor\s+Descriptor\s*\{\s*get;\s*\}\s*=\s*(?:new|[A-Za-z_][A-Za-z0-9_.]*)\(\s*(' +
        $token + ')\s*,\s*(' + $token + ')\s*,')
    $constant = [regex]::new('const\s+string\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(' + $string + ')\s*;')
    $unescape = { param($literal) [Text.Json.JsonSerializer]::Deserialize[string]($literal) }

    $byClass = @{}
    foreach ($file in Get-ChildItem -LiteralPath $operationsRoot -Filter "*.cs" -File -Recurse) {
        $source = Get-Content -LiteralPath $file.FullName -Raw
        $match = $descriptor.Match($source)
        if (-not $match.Success) { continue }
        $classMatch = [regex]::Match($source, 'static\s+class\s+([A-Za-z0-9_]+)')
        $constants = @{}
        foreach ($c in $constant.Matches($source)) { $constants[$c.Groups[1].Value] = & $unescape $c.Groups[2].Value }
        $resolve = {
            param($value)
            if ($value.StartsWith('"')) { return & $unescape $value }
            if ($constants.ContainsKey($value)) { return $constants[$value] }
            throw "Cannot resolve descriptor value '$value' in '$($file.FullName)'."
        }
        $byClass[$classMatch.Groups[1].Value] = [pscustomobject]@{
            OperationId = & $resolve $match.Groups[1].Value
            MpStep = & $resolve $match.Groups[2].Value
        }
    }

    $operations = foreach ($class in $classes) {
        if (-not $byClass.ContainsKey($class)) { throw "Registered class '$class' has no parseable descriptor." }
        $byClass[$class]
    }
    $ids = @($operations.OperationId)
    if (($ids | Sort-Object -Unique).Count -ne $ids.Count) { throw "Registered operation ids are not unique." }
    return @($operations)
}

# --- Published catalog parsing ---

# Docusaurus/github-slugger anchor for a heading, including duplicate suffixes.
function Get-Slug {
    param([string]$Heading, [hashtable]$Seen)
    $text = $Heading.Trim().ToLowerInvariant()
    $text = [regex]::Replace($text, '[^\p{L}\p{M}\p{N}\p{Pc} -]', '')
    $slug = $text.Replace(' ', '-')
    if ($Seen.ContainsKey($slug)) {
        $Seen[$slug]++
        return "$slug-$($Seen[$slug])"
    }
    $Seen[$slug] = 0
    return $slug
}

function Split-TableRow {
    param([string]$Line)
    $cells = [Collections.Generic.List[string]]::new()
    $current = [Text.StringBuilder]::new()
    $inCode = $false
    foreach ($character in $Line.Trim().Trim('|').ToCharArray()) {
        if ($character -eq '`') { $inCode = -not $inCode }
        if ($character -eq '|' -and -not $inCode) {
            $cells.Add($current.ToString().Trim()); [void]$current.Clear(); continue
        }
        [void]$current.Append($character)
    }
    $cells.Add($current.ToString().Trim())
    return , $cells.ToArray()
}

# Returns exact labels when a cell holds only `label` spans joined by " / "; otherwise $null.
function Get-CellLabels {
    param([string]$Cell)
    if ($Cell.Contains('``')) { throw "Double-backtick code spans are not supported: $Cell" }
    $match = [regex]::Match($Cell, '^`([^`]*)`(?: / `([^`]*)`)*$')
    if (-not $match.Success) { return $null }
    $labels = @($match.Groups[1].Value) + @($match.Groups[2].Captures | ForEach-Object Value)
    return , $labels
}

function ConvertTo-Direction {
    param([string]$Cell)
    switch -CaseSensitive ($Cell) {
        "Input" { return "input" }
        "Output" { return "output" }
        "Input/Output" { return "input_output" }
        default { throw "Unknown argument direction '$Cell'." }
    }
}

function Get-FirstTable {
    param([string]$Text)
    $lines = $Text.Split("`n")
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\| Direction \|') {
            $header = Split-TableRow $lines[$i]
            $rows = [Collections.Generic.List[object]]::new()
            for ($j = $i + 2; $j -lt $lines.Count -and $lines[$j].StartsWith('|'); $j++) {
                $cells = Split-TableRow $lines[$j]
                # The catalog's explicit no-argument row.
                if ($cells[0] -ceq [string][char]0x2014 -and $cells[1] -ceq "None") { continue }
                $rows.Add($cells)
            }
            return [pscustomobject]@{ Header = $header; Rows = $rows.ToArray() }
        }
    }
    return $null
}

function Get-CatalogCommands {
    param([string]$Revision)
    $pages = (Invoke-GitText $DocsRepository @("ls-tree", "--name-only", $Revision, "$CatalogPath/")).Split(
        "`n", [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { $_.EndsWith(".md") } | Sort-Object
    $commands = [Collections.Generic.List[object]]::new()
    foreach ($page in $pages) {
        $text = Invoke-GitText $DocsRepository @("show", "${Revision}:$page")
        $headings = [regex]::Matches($text, '(?m)^(#{1,6}) (.+)$')
        $seen = @{}
        $marks = foreach ($heading in $headings) {
            $after = $text.Substring($heading.Index + $heading.Length).TrimStart("`n")
            [pscustomobject]@{
                Level = $heading.Groups[1].Value.Length
                Title = $heading.Groups[2].Value.TrimEnd()
                Index = $heading.Index
                Anchor = Get-Slug $heading.Groups[2].Value $seen
                IsCommand = $after.StartsWith('<div className="catalog-target-contexts">')
            }
        }
        $marks = @($marks)
        for ($k = 0; $k -lt $marks.Count; $k++) {
            if (-not $marks[$k].IsCommand) { continue }
            $end = $text.Length
            for ($n = $k + 1; $n -lt $marks.Count; $n++) {
                if ($marks[$n].IsCommand -or $marks[$n].Level -le 2) { $end = $marks[$n].Index; break }
            }
            $body = $text.Substring($marks[$k].Index, $end - $marks[$k].Index)
            $context = [regex]::Match($body, 'data-target="2024\.1\.0508\.5" data-status="([a-z-]+)"')
            $commands.Add([pscustomobject]@{
                Page = $page
                Title = $marks[$k].Title
                Anchor = $marks[$k].Anchor
                Status2024 = if ($context.Success) { $context.Groups[1].Value } else { $null }
                Body = $body
            })
        }
    }
    return $commands.ToArray()
}

# Argument kinds published in "Captured 2024 Argument Signature" blocks name the
# exporter call family. Each maps to the exact SDK method for its direction.
$KindMethods = @{
    "Angular Units" = @("SetAngularUnitsArg", $null)
    "Auto Filter Proximity Settings" = @("SetAutoFilterProximitySettingsArg", "GetAutoFilterProximitySettingsArg")
    "Axis Name" = @("SetAxisNameArg", $null)
    "B-Spline Fit Options" = @("SetBSplineFitOptionsArg", $null)
    "Boolean" = @("SetBoolArg", "GetBoolArg")
    "Cloud Name" = @("SetCloudNameArg", $null)
    "Cloud Thinning Mode Type" = @("SetCloudThinningModeTypeArg", $null)
    "Cloud Thinning Options" = @("SetCloudThinningOptionsArg", $null)
    "Collection Instrument ID" = @("SetColInstIdArg", "GetColInstIdArg")
    "Collection Instrument ID Ref List" = @("SetColInstIdRefListArg", "GetColInstIdRefListArg")
    "Collection Machine ID" = @("SetColMachineIdArg", "GetColMachineIdArg")
    "Collection Name" = @("SetCollectionNameArg", "GetCollectionNameArg")
    "Collection Object Name" = @("SetCollectionObjectNameArg", "GetCollectionObjectNameArg")
    "Collection Object Name Ref List" = @("SetCollectionObjectNameRefListArg", "GetCollectionObjectNameRefListArg")
    "Collection Vector Group Name" = @("SetColVectorGroupNameArg", $null)
    "Collection Vector Group Name Ref List" = @("SetCollectionVectorGroupNameRefListArg", "GetCollectionVectorGroupNameRefListArg")
    "Collimation Baseline Type" = @("SetCollimationBaselineTypeArg", $null)
    "Collimation Type" = @("SetCollimationTypeArg", $null)
    "Color" = @("SetColorArg", $null)
    "Comp Technique" = @("SetCompTechniqueArg", $null)
    "Coordinate System Type" = @("SetCoordinateSystemTypeArg", $null)
    "Double" = @("SetDoubleArg", "GetDoubleArg")
    "Double List" = @("SetDoubleArrayArg", "GetDoubleArrayArg")
    "Dynamic Circle Mode" = @("SetDynamicCircleModeArg", $null)
    "Dynamic Ellipse Mode" = @("SetDynamicEllipseModeArg", $null)
    "Dynamic Line Mode" = @("SetDynamicLineModeArg", $null)
    "Dynamic Plane Mode" = @("SetDynamicPlaneModeArg", $null)
    "Dynamic Point Mode" = @("SetDynamicPointModeArg", $null)
    "Edge Mode" = @("SetEdgeModeArg", $null)
    "Edit Text" = @("SetEditTextArg", "GetEditTextArg")
    "Export Data Delimeter Type" = @("SetExportDataDelimeterTypeArg", $null)
    "File Path or Embedded File" = @("SetFilePathArg", $null)
    "Fit Dof Options" = @("SetFitDofOptionsArg", $null)
    "Fit Method" = @("SetFitMethodArg", $null)
    "Font Type" = @("SetFontTypeArg", $null)
    "GD&T Distance Between Mode" = @("SetMPGDTOptionsDistanceBetweenModeArg", $null)
    "GD&T Evaluation Method" = @("SetMPGDTOptionsCheckValidatorTypeArg", $null)
    "Geometry Type" = @("SetGeometryTypeArg", $null)
    "Instrument Type" = @("SetInstTypeNameArg", $null)
    "Integer" = @("SetIntegerArg", "GetIntegerArg")
    "Item Type" = @("SetItemTypeArg", $null)
    "Measured Side For Radial Offset" = @("SetMeasuredSideForRadialOffsetArg", $null)
    "Mesh Orientation Type" = @("SetMeshOrientationTypeArg", $null)
    "Object Type" = @("SetObjectTypeArg", $null)
    "Offset Direction Type" = @("SetOffsetDirectionTypeArg", $null)
    "Point Filter Input Type" = @("SetPointFilterInputTypeArg", $null)
    "Point Name" = @("SetPointNameArg", "GetPointNameArg")
    "Point Name Ref List" = @("SetPointNameRefListArg", "GetPointNameRefListArg")
    "Projection Options" = @("SetProjectionOptionsArg", $null)
    "Show Usmn Dialog Type" = @("SetShowUsmnDialogTypeArg", $null)
    "String" = @("SetStringArg", "GetStringArg")
    "String Ref List" = @("SetStringRefListArg", "GetStringRefListArg")
    "Surf Dissect Mode Type" = @("SetSurfDissectModeTypeArg", $null)
    "Target Computation Method" = @("SetTargetComputationMethodArg", $null)
    "Tolerance Scalar Options" = @("SetToleranceScalarOptionsArg", $null)
    "Tolerance Vector Options" = @("SetToleranceVectorOptionsArg", $null)
    "Transform" = @("SetTransformArg", "GetTransformArg")
    "UDP Transmit Settings" = @("SetUdpTransmitSettingsArg", $null)
    "Vector" = @("SetVectorArg", "GetVectorArg")
    "Vector Group Name" = @("SetVectorGroupNameArg", $null)
    "Vector Name Ref List" = @("SetVectorNameRefListArg", "GetVectorNameRefListArg")
    "World Transform" = @("SetWorldTransformArg", "GetWorldTransformArg")
}
$UnavailableCapturedKind = "Binding not emitted by the SDK exporter"
$UnavailableReferenceType = "SDK-unavailable MP argument"
# A per-command catalog statement that its collection-object inputs use the 2024
# three-argument collection/name setter (review notes, "Collection-Object Bindings").
$CollectionObjectBindingStatement = "Collection-object inputs use the [2024 collection/name binding](/mp-command-catalog/2024.1.0508.5/review-notes#collection-object-bindings)."
$CollectionObjectReferenceType = "Collection Object Name"
$CollectionObjectSetter = "SetCollectionObjectNameArg"

function New-Binding {
    param([string]$Status, [string]$Method)
    return [ordered]@{ status = $Status; method = if ($Method) { $Method } else { $null } }
}

function New-Argument {
    param([int]$Ordinal, [string]$Name, [string]$Direction, [string]$Kind, $Setter, $Getter, [string]$MethodBasis)
    return [ordered]@{
        ordinal = $Ordinal
        sdk_name = $Name
        direction = $Direction
        argument_kind = $Kind
        setter = $Setter
        getter = $Getter
        method_basis = if ($MethodBasis) { $MethodBasis } else { $null }
        name_provenance = $DocsProvenance
        findings = [string[]]@()
    }
}

function Get-CapturedArguments {
    param([string]$Block, [string]$Context)
    $table = Get-FirstTable $Block
    if ($null -eq $table) {
        if ($Block -match 'no SDK argument calls') { return , @() }
        throw "Captured 2024 block for '$Context' has neither a table nor a no-argument statement."
    }
    if ($table.Header[1] -cne "Exact MP Argument" -or $table.Header[2] -cne "Argument Kind") {
        throw "Unexpected captured table header for '$Context'."
    }
    $arguments = [Collections.Generic.List[object]]::new()
    foreach ($row in $table.Rows) {
        $labels = Get-CellLabels $row[1]
        if ($null -eq $labels -or $labels.Count -ne 1) { throw "Captured row for '$Context' is not one exact label: $($row[1])" }
        $direction = ConvertTo-Direction $row[0]
        $kind = $row[2]
        $basis = $null
        if ($kind -ceq $UnavailableCapturedKind) {
            $setter = New-Binding "unavailable" $null
            $getter = New-Binding "not_observed" $null
            if ($direction -ne "input") { throw "Unavailable captured binding for '$Context' is not an input." }
        }
        else {
            if (-not $KindMethods.ContainsKey($kind)) { throw "Unmapped captured argument kind '$kind' in '$Context'." }
            $pair = $KindMethods[$kind]
            $setterMethod = if ($direction -ne "output") { $pair[0] } else { $null }
            $getterMethod = if ($direction -ne "input") { $pair[1] } else { $null }
            if ($direction -ne "output" -and -not $setterMethod) { throw "No setter mapping for '$kind' in '$Context'." }
            if ($direction -ne "input" -and -not $getterMethod) { throw "No getter mapping for '$kind' in '$Context'." }
            $setter = if ($setterMethod) { New-Binding "available" $setterMethod } else { New-Binding "not_observed" $null }
            $getter = if ($getterMethod) { New-Binding "available" $getterMethod } else { New-Binding "not_observed" $null }
            $basis = "captured_argument_kind"
        }
        $arguments.Add((New-Argument $arguments.Count $labels[0] $direction $kind $setter $getter $basis))
    }
    return , $arguments.ToArray()
}

function Get-ReferenceArguments {
    param([string]$Reference, [string]$Context, [Collections.Generic.List[string]]$Findings, [bool]$CollectionObjectBinding)
    $table = Get-FirstTable $Reference
    if ($null -eq $table) {
        $Findings.Add("reference_signature_table_absent")
        return , @()
    }
    if ($table.Header[2] -cne "MP Type") { throw "Unexpected reference table header for '$Context'." }
    $arguments = [Collections.Generic.List[object]]::new()
    foreach ($row in $table.Rows) {
        $direction = ConvertTo-Direction $row[0]
        $labels = Get-CellLabels $row[1]
        if ($null -eq $labels) {
            if (-not $Findings.Contains("reference_row_without_exact_label")) { $Findings.Add("reference_row_without_exact_label") }
            continue
        }
        $types = @($row[2])
        $split = $row[2] -split ' / '
        if ($labels.Count -gt 1 -and $split.Count -eq $labels.Count) { $types = $split }
        for ($i = 0; $i -lt $labels.Count; $i++) {
            $type = if ($types.Count -eq 1) { $types[0] } else { $types[$i] }
            $setter = New-Binding "not_observed" $null
            $getter = New-Binding "not_observed" $null
            $basis = $null
            if ($type -ceq $UnavailableReferenceType) {
                if ($direction -ne "input") { throw "Unavailable reference binding for '$Context' is not an input." }
                $setter = New-Binding "unavailable" $null
            }
            else {
                if ($direction -ne "output") { $setter = New-Binding "unpublished" $null }
                if ($direction -ne "input") { $getter = New-Binding "unpublished" $null }
                if ($CollectionObjectBinding -and $direction -ne "output" -and $type -ceq $CollectionObjectReferenceType) {
                    $setter = New-Binding "available" $CollectionObjectSetter
                    $basis = "collection_object_binding_statement"
                }
            }
            $arguments.Add((New-Argument $arguments.Count $labels[$i] $direction $type $setter $getter $basis))
        }
    }
    return , $arguments.ToArray()
}

# --- Private quote corrections (local only) ---

function Get-QuotedExportLabels {
    $files = (Invoke-GitText $EvidenceRepository @("ls-tree", "-r", "--name-only", $EvidenceRevision, "--", $EvidenceExportRoot)).Split(
        "`n", [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { $_.EndsWith("/commands.vb") } | Sort-Object
    $byStep = @{}
    $vbString = '"((?:[^"]|"")*)"'
    foreach ($file in $files) {
        $step = $null
        foreach ($line in (Invoke-GitText $EvidenceRepository @("show", "${EvidenceRevision}:$file")).Split("`n")) {
            $trimmed = $line.Trim()
            $stepMatch = [regex]::Match($trimmed, '^NrkSdk\.SetStep\(' + $vbString + '\)')
            if ($stepMatch.Success) { $step = $stepMatch.Groups[1].Value.Replace('""', '"'); continue }
            if ($null -eq $step) { continue }
            $call = [regex]::Match($trimmed, '^NrkSdk\.(?:Set|Get)[A-Za-z0-9]+Arg[0-9]*\(' + $vbString)
            if (-not $call.Success) { continue }
            $label = $call.Groups[1].Value.Replace('""', '"')
            if (-not $label.Contains('"')) { continue }
            if (-not $byStep.ContainsKey($step)) { $byStep[$step] = [Collections.Generic.List[string]]::new() }
            $byStep[$step].Add($label)
        }
    }
    return $byStep
}

function Get-CommittedQuoteCorrections {
    $corrections = @{}
    if (-not (Test-Path -LiteralPath $OutputPath)) { return $corrections }
    $existing = Get-Content -LiteralPath $OutputPath -Raw | ConvertFrom-Json
    foreach ($operation in $existing.operations) {
        foreach ($argument in $operation.arguments) {
            if ($argument.name_provenance -ceq $EvidenceProvenance) {
                $corrections["$($operation.operation_id)`n$($argument.sdk_name.Replace('"', ''))"] = $argument.sdk_name
            }
        }
    }
    return $corrections
}

# --- Assemble ---

$docsCommit = (Invoke-GitText $DocsRepository @("rev-parse", "--verify", "$DocsRevision^{commit}")).Trim()
$interopApi = Get-Content -LiteralPath (Join-Path $targetRoot "interop/SpatialAnalyzer/$Target/Briosa.SpatialAnalyzer.Interop.PublicApi.txt") -Raw
$interopMethods = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($m in [regex]::Matches($interopApi, '(?m)^\s+method ([A-Za-z0-9_]+) ')) { [void]$interopMethods.Add($m.Groups[1].Value) }

$registered = Get-RegisteredOperations
$catalog = Get-CatalogCommands $docsCommit
$quotedExportLabels = if ($EvidenceRepository) { Get-QuotedExportLabels } else { $null }
$committedCorrections = if ($EvidenceRepository) { @{} } else { Get-CommittedQuoteCorrections }

$operations = [Collections.Generic.List[object]]::new()
foreach ($operation in ($registered | Sort-Object OperationId -CaseSensitive)) {
    $candidates = @($catalog | Where-Object { $_.Status2024 -ceq "current" -and $_.Title -ceq $operation.MpStep })
    if ($candidates.Count -eq 0) {
        $candidates = @($catalog | Where-Object {
                $_.Status2024 -ceq "current" -and [string]::Equals($_.Title, $operation.MpStep, [StringComparison]::OrdinalIgnoreCase) })
    }
    $findings = [Collections.Generic.List[string]]::new()
    $source = [ordered]@{ kind = "none"; document = $null; anchor = $null }
    $arguments = @()
    $mpStep = $operation.MpStep
    if ($candidates.Count -gt 1) { throw "Ambiguous catalog entries for '$($operation.MpStep)'." }
    if ($candidates.Count -eq 0) {
        $findings.Add("no_public_2024_signature")
    }
    else {
        $entry = $candidates[0]
        $mpStep = $entry.Title
        $source.document = $entry.Page
        $source.anchor = $entry.Anchor
        $details = [regex]::Match($entry.Body, '(?s)<summary>SA 2024\.1\.0508\.5: Signature and Disposition</summary>(.*?)</details>')
        if (-not $details.Success) { throw "No 2024 signature details for '$($entry.Title)'." }
        $review = $details.Groups[1].Value
        $marker = "**Captured 2024 Argument Signature**"
        if ($review.Contains($marker)) {
            $source.kind = "captured_2024_signature"
            $arguments = Get-CapturedArguments $review.Substring($review.IndexOf($marker)) $entry.Title
        }
        elseif ($review.Contains("The reference signature below applies with these 2024 adjustments.")) {
            $source.kind = "reference_signature"
            if (-not $review.Contains("The MP inputs, outputs, and choices match the 2026 counterpart.")) {
                # The catalog describes 2024 choice or sample adjustments in prose.
                $findings.Add("reference_signature_with_2024_adjustments")
            }
            $scope = $entry.Body.IndexOf('<p className="catalog-reference-scope">')
            if ($scope -lt 0) { throw "No reference signature scope for '$($entry.Title)'." }
            $arguments = Get-ReferenceArguments $entry.Body.Substring($scope) $entry.Title $findings `
                $review.Contains($CollectionObjectBindingStatement)
        }
        else {
            $source.kind = "none"
            $findings.Add("no_public_2024_signature")
        }
    }

    foreach ($argument in $arguments) {
        $replacement = $null
        if ($null -ne $quotedExportLabels) {
            if ($quotedExportLabels.ContainsKey($mpStep)) {
                $hits = @($quotedExportLabels[$mpStep] | Where-Object { $_.Replace('"', '') -ceq $argument.sdk_name } | Sort-Object -Unique)
                if ($hits.Count -gt 1) { throw "Ambiguous quote correction for '$mpStep' / '$($argument.sdk_name)'." }
                if ($hits.Count -eq 1) { $replacement = $hits[0] }
            }
        }
        else {
            $key = "$($operation.OperationId)`n$($argument.sdk_name)"
            if ($committedCorrections.ContainsKey($key)) { $replacement = $committedCorrections[$key] }
        }
        if ($replacement) {
            $argument.sdk_name = $replacement
            $argument.name_provenance = $EvidenceProvenance
        }
        $argumentFindings = [Collections.Generic.List[string]]::new()
        foreach ($side in @("setter", "getter")) {
            $method = $argument[$side].method
            if ($method -and -not $interopMethods.Contains($method)) { $argumentFindings.Add("${side}_method_absent_from_interop") }
        }
        $argument.findings = [string[]]$argumentFindings.ToArray()
    }

    $operations.Add([ordered]@{
        operation_id = $operation.OperationId
        mp_step = $mpStep
        source = $source
        arguments = [object[]]$arguments
        findings = [string[]]$findings.ToArray()
    })
}

$allArguments = @($operations | ForEach-Object { $_.arguments })
$document = [ordered]@{
    '$schema' = "bindings.schema.json"
    schema_version = 1
    spatial_analyzer_target = $Target
    provenance = [ordered]@{
        public_catalog = [ordered]@{
            repository = "spatialanalyzer/briosa-docs"
            revision = $docsCommit
            path = $CatalogPath
        }
        private_quote_corrections = [ordered]@{
            repository = "spatialanalyzer/briosa-evidence"
            revision = $EvidenceRevision
            path = $EvidenceExportRoot
            use = "embedded_double_quotes_in_argument_labels_only"
            source_material_committed = $false
        }
        operation_registry = "src/Briosa.Server/Operations/SpatialAnalyzerApi.cs"
        interop_surface = "interop/SpatialAnalyzer/$Target/Briosa.SpatialAnalyzer.Interop.PublicApi.txt"
        curation_script = "eng/Update-Sa2024BindingFacts.ps1"
    }
    summary = [ordered]@{
        operation_count = $operations.Count
        captured_2024_signature_operation_count = @($operations | Where-Object { $_.source.kind -eq "captured_2024_signature" }).Count
        reference_signature_operation_count = @($operations | Where-Object { $_.source.kind -eq "reference_signature" }).Count
        unsourced_operation_count = @($operations | Where-Object { $_.source.kind -eq "none" }).Count
        argument_count = $allArguments.Count
        quote_corrected_argument_count = @($allArguments | Where-Object { $_.name_provenance -ceq $EvidenceProvenance }).Count
        unavailable_binding_count = @($allArguments | Where-Object { $_.setter.status -eq "unavailable" -or $_.getter.status -eq "unavailable" }).Count
        unpublished_method_argument_count = @($allArguments | Where-Object { $_.setter.status -eq "unpublished" -or $_.getter.status -eq "unpublished" }).Count
        collection_object_statement_method_count = @($allArguments | Where-Object { $_.method_basis -ceq "collection_object_binding_statement" }).Count
    }
}

# Remove PowerShell wrappers so System.Text.Json sees only plain values.
function ConvertTo-Plain {
    param($Value)
    if ($null -eq $Value) { return $null }
    $Value = $Value.psobject.BaseObject
    if ($Value -is [string]) { return [string]$Value }
    if ($Value -is [Collections.IDictionary]) {
        $ordered = [Collections.Generic.List[string]]::new()
        foreach ($key in $Value.Keys) { $ordered.Add([string]$key) }
        $result = [Collections.Specialized.OrderedDictionary]::new()
        foreach ($key in $ordered) { $result[$key] = ConvertTo-Plain $Value[$key] }
        return , $result
    }
    if ($Value -is [Collections.IEnumerable]) {
        $items = [Collections.Generic.List[object]]::new()
        foreach ($item in $Value) { $items.Add((ConvertTo-Plain $item)) }
        return , $items.ToArray()
    }
    return $Value
}

$document = ConvertTo-Plain $document
$operations = ConvertTo-Plain $operations.ToArray()

$options = [Text.Json.JsonSerializerOptions]::new()
$options.Encoder = [Text.Encodings.Web.JavaScriptEncoder]::UnsafeRelaxedJsonEscaping
$indented = [Text.Json.JsonSerializerOptions]::new($options)
$indented.WriteIndented = $true
$indented.IndentSize = 2
$builder = [Text.StringBuilder]::new()
[void]$builder.Append("{`n")
foreach ($key in $document.Keys) {
    $value = [Text.Json.JsonSerializer]::Serialize($document[$key], $indented).Replace("`r`n", "`n").Replace("`n", "`n  ")
    [void]$builder.Append("  ").Append([Text.Json.JsonSerializer]::Serialize($key, $options)).Append(": ").Append($value).Append(",`n")
}
[void]$builder.Append("  `"operations`": [`n")
for ($i = 0; $i -lt $operations.Count; $i++) {
    [void]$builder.Append("    ").Append([Text.Json.JsonSerializer]::Serialize($operations[$i], $options))
    [void]$builder.Append($(if ($i -lt $operations.Count - 1) { ",`n" } else { "`n" }))
}
[void]$builder.Append("  ]`n}`n")
$json = $builder.ToString()

if ($Check) {
    $committed = if (Test-Path -LiteralPath $OutputPath) { [IO.File]::ReadAllText($OutputPath).Replace("`r`n", "`n") } else { "" }
    if ($committed -cne $json) { throw "The committed binding facts differ from a fresh curation at docs $docsCommit." }
    Write-Host "Committed binding facts match a fresh curation at docs $docsCommit."
    return
}

[void][IO.Directory]::CreateDirectory((Split-Path -Parent $OutputPath))
[IO.File]::WriteAllText($OutputPath, $json, [Text.UTF8Encoding]::new($false))
$summary = $document.summary
Write-Host ("Wrote {0} operations ({1} captured 2024, {2} reference, {3} unsourced), {4} arguments, {5} quote-corrected." -f `
        $summary.operation_count, $summary.captured_2024_signature_operation_count, $summary.reference_signature_operation_count,
    $summary.unsourced_operation_count, $summary.argument_count, $summary.quote_corrected_argument_count)
