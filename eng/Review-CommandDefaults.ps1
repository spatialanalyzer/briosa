[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ObjectiveSARoot,
    [string]$SdkCodeRoot,
    [Parameter(Mandatory)][string]$InventoryPath,
    [Parameter(Mandatory)][string]$DispositionDirectory,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-NormalizedIdentifier {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    return [regex]::Replace($Value, "[^A-Za-z0-9]", "").ToLowerInvariant()
}

function Split-TopLevel {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Value)

    $parts = [Collections.Generic.List[string]]::new()
    $start = 0
    $roundDepth = 0
    $squareDepth = 0
    $angleDepth = 0
    $inString = $false

    for ($index = 0; $index -lt $Value.Length; $index++) {
        $character = $Value[$index]
        if ($character -eq '"') {
            if ($inString -and $index + 1 -lt $Value.Length -and $Value[$index + 1] -eq '"') {
                $index++
                continue
            }

            $inString = -not $inString
            continue
        }

        if ($inString) {
            continue
        }

        switch ($character) {
            '(' { $roundDepth++ }
            ')' { $roundDepth-- }
            '[' { $squareDepth++ }
            ']' { $squareDepth-- }
            '<' { $angleDepth++ }
            '>' { $angleDepth-- }
            ',' {
                if ($roundDepth -eq 0 -and $squareDepth -eq 0 -and $angleDepth -eq 0) {
                    $parts.Add($Value.Substring($start, $index - $start).Trim())
                    $start = $index + 1
                }
            }
        }
    }

    $parts.Add($Value.Substring($start).Trim())
    return $parts.ToArray()
}

function Convert-CSharpDefault {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Expression)

    $value = $Expression.Trim()
    if ($value -eq "null") {
        return [pscustomobject]@{ Kind = "null"; Value = $null; Comparable = "null" }
    }

    if ($value -in @("true", "false")) {
        $parsed = $value -eq "true"
        return [pscustomobject]@{
            Kind = "boolean"
            Value = $parsed
            Comparable = $value
        }
    }

    $numeric = $value.TrimEnd('d', 'D', 'f', 'F', 'm', 'M')
    $integer = 0L
    if ($numeric -match "^[+-]?[0-9]+$" -and [long]::TryParse(
            $numeric,
            [Globalization.NumberStyles]::Integer,
            [Globalization.CultureInfo]::InvariantCulture,
            [ref]$integer)) {
        return [pscustomobject]@{
            Kind = "integer"
            Value = $integer
            Comparable = $integer.ToString([Globalization.CultureInfo]::InvariantCulture)
        }
    }

    $number = 0.0
    if ([double]::TryParse(
            $numeric,
            [Globalization.NumberStyles]::Float,
            [Globalization.CultureInfo]::InvariantCulture,
            [ref]$number)) {
        return [pscustomobject]@{
            Kind = "number"
            Value = $number
            Comparable = $number.ToString("R", [Globalization.CultureInfo]::InvariantCulture)
        }
    }

    if ($value.StartsWith('"') -and $value.EndsWith('"')) {
        $unquoted = $value.Substring(1, $value.Length - 2).Replace('\"', '"')
        return [pscustomobject]@{
            Kind = "string"
            Value = $unquoted
            Comparable = Get-NormalizedIdentifier $unquoted
        }
    }

    if ($value -match "^[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*$") {
        $member = $value.Split('.')[-1].Replace('_', ' ')
        return [pscustomobject]@{
            Kind = "enum"
            Value = $member
            Comparable = Get-NormalizedIdentifier $member
        }
    }

    return [pscustomobject]@{ Kind = "unparsed"; Value = $value; Comparable = $null }
}

function Get-ObjectiveSADefaults {
    param([Parameter(Mandatory)][string]$Root)

    $implementationRoot = Join-Path $Root "ObjectiveSA\Methods"
    $legacyInterfaceRoot = Join-Path $Root "ObjectiveSA\Interfaces\Methods"
    $interfaceRoot = if (Test-Path -LiteralPath $legacyInterfaceRoot -PathType Container) {
        $legacyInterfaceRoot
    }
    else {
        $implementationRoot
    }
    if (-not (Test-Path -LiteralPath $interfaceRoot -PathType Container) -or
        -not (Test-Path -LiteralPath $implementationRoot -PathType Container)) {
        throw "ObjectiveSA source roots were not found below '$Root'."
    }

    $interfaces = [Collections.Generic.List[object]]::new()
    $declarationPattern = [regex]::new(
        "(?ms)^[ \t]*(?!///)(?:[A-Za-z_][A-Za-z0-9_<>,?.\[\] ]*)[ \t]+" +
        "(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*\((?<parameters>.*?)\)[ \t]*;")
    foreach ($file in Get-ChildItem -LiteralPath $interfaceRoot -Filter "*.cs" -Recurse -File) {
        $text = Get-Content -Raw -LiteralPath $file.FullName
        foreach ($match in $declarationPattern.Matches($text)) {
            $parameters = Split-TopLevel $match.Groups["parameters"].Value
            $defaults = [Collections.Generic.List[object]]::new()
            foreach ($parameter in $parameters) {
                if ($parameter -notmatch "^(?<type>.+?)[ \t]+(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*=[ \t]*(?<value>.+)$") {
                    continue
                }

                $parsed = Convert-CSharpDefault $Matches["value"]
                $defaults.Add([pscustomobject]@{
                    Parameter = $Matches["name"]
                    Expression = $Matches["value"].Trim()
                    Parsed = $parsed
                })
            }

            if ($defaults.Count -eq 0) {
                continue
            }

            $methodName = $match.Groups["name"].Value
            if ($methodName.EndsWith("Async", [StringComparison]::Ordinal)) {
                $methodName = $methodName.Substring(0, $methodName.Length - 5)
            }

            $interfaces.Add([pscustomobject]@{
                Method = $methodName
                Defaults = $defaults.ToArray()
            })
        }
    }

    $implementations = [Collections.Generic.List[object]]::new()
    $methodPattern = [regex]::new(
        "(?ms)^[ \t]*public[ \t]+(?:[A-Za-z_][A-Za-z0-9_<>,?.\[\] ]*)[ \t]+" +
        "(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*\((?<parameters>.*?)\)\s*\{")
    $stepPattern = [regex]::new(
        'SpatialAnalyzerSDKWrapper\.SetStep\(\s*"(?<step>(?:\\"|[^"])*)"\s*\)')
    $setterPattern = [regex]::new(
        '(?ms)SpatialAnalyzerSDKWrapper\.(?<setter>Set[A-Za-z0-9]+Arg[0-9]*)\(' +
        '\s*"(?<argument>(?:\\"|[^"])*)"\s*,(?<value>.*?)\);')

    foreach ($file in Get-ChildItem -LiteralPath $implementationRoot -Filter "*.cs" -Recurse -File) {
        $text = Get-Content -Raw -LiteralPath $file.FullName
        $methodMatches = $methodPattern.Matches($text)
        for ($methodIndex = 0; $methodIndex -lt $methodMatches.Count; $methodIndex++) {
            $match = $methodMatches[$methodIndex]
            $methodName = $match.Groups["name"].Value
            if ($methodName.EndsWith("Async", [StringComparison]::Ordinal)) {
                continue
            }

            $end = if ($methodIndex + 1 -lt $methodMatches.Count) {
                $methodMatches[$methodIndex + 1].Index
            }
            else {
                $text.Length
            }
            $body = $text.Substring($match.Index, $end - $match.Index)
            $stepMatch = $stepPattern.Match($body)
            if (-not $stepMatch.Success) {
                continue
            }

            $setters = [Collections.Generic.List[object]]::new()
            foreach ($setterMatch in $setterPattern.Matches($body)) {
                $setters.Add([pscustomobject]@{
                    Method = $setterMatch.Groups["setter"].Value
                    Argument = $setterMatch.Groups["argument"].Value
                    ValueExpression = $setterMatch.Groups["value"].Value.Trim()
                })
            }

            $implementations.Add([pscustomobject]@{
                Method = $methodName
                Step = $stepMatch.Groups["step"].Value
                Setters = $setters.ToArray()
            })
        }
    }

    $results = [Collections.Generic.List[object]]::new()
    Write-Verbose "ObjectiveSA interface declarations with defaults: $($interfaces.Count)"
    Write-Verbose "ObjectiveSA implementations with SetStep: $($implementations.Count)"
    foreach ($interface in $interfaces) {
        foreach ($implementation in $implementations.Where({ $_.Method -eq $interface.Method })) {
            foreach ($default in $interface.Defaults) {
                $parameterPattern = "(?<![A-Za-z0-9_])" +
                    [regex]::Escape($default.Parameter) +
                    "(?![A-Za-z0-9_])"
                foreach ($setter in $implementation.Setters.Where({
                            $_.ValueExpression -match $parameterPattern
                        })) {
                    $results.Add([pscustomobject]@{
                        Step = $implementation.Step
                        Argument = $setter.Argument
                        Setter = $setter.Method
                        Default = $default.Parsed
                    })
                }
            }
        }
    }

    return @(
        $results |
            Group-Object {
                "$(Get-NormalizedIdentifier $_.Step)|$(Get-NormalizedIdentifier $_.Argument)"
            } |
            ForEach-Object {
                $values = @($_.Group.Default)
                $comparables = @($values.Comparable | Sort-Object -Unique)
                $setters = @($_.Group.Setter | Sort-Object -Unique)
                [pscustomobject]@{
                    Key = $_.Name
                    Step = $_.Group[0].Step
                    Argument = $_.Group[0].Argument
                    Defaults = @($values | Sort-Object Comparable -Unique)
                    Setters = $setters
                    Conflict = $comparables.Count -ne 1 -or $setters.Count -ne 1
                }
            } |
            Sort-Object Key
    )
}

function Convert-CommittedCandidate {
    param([Parameter(Mandatory)]$Value)

    if ($null -eq $Value) {
        return [pscustomobject]@{ Kind = "null"; Value = $null; Comparable = "null" }
    }

    if ($Value -is [bool]) {
        return [pscustomobject]@{
            Kind = "boolean"
            Value = $Value
            Comparable = $Value.ToString().ToLowerInvariant()
        }
    }

    if ($Value -is [byte] -or $Value -is [short] -or $Value -is [int] -or
        $Value -is [long] -or $Value -is [decimal] -or $Value -is [double] -or
        $Value -is [single]) {
        $number = [Convert]::ToDouble($Value, [Globalization.CultureInfo]::InvariantCulture)
        return [pscustomobject]@{
            Kind = "number"
            Value = $Value
            Comparable = $number.ToString("R", [Globalization.CultureInfo]::InvariantCulture)
        }
    }

    if ($Value -is [string]) {
        return [pscustomobject]@{
            Kind = "string"
            Value = $Value
            Comparable = Get-NormalizedIdentifier $Value
        }
    }

    return [pscustomobject]@{ Kind = "unparsed"; Value = $Value; Comparable = $null }
}

function Get-CommittedExactTargetCalls {
    param([Parameter(Mandatory)][string]$Root)

    $results = [Collections.Generic.List[object]]::new()
    foreach ($shardPath in Get-ChildItem -LiteralPath (Join-Path $Root "categories") -Filter "*.json" -File) {
        $shard = Get-Content -Raw -LiteralPath $shardPath.FullName | ConvertFrom-Json -Depth 100
        foreach ($entry in $shard.entries) {
            if ($entry.disposition -ne "approved_candidate" -or
                $entry.command_shape.status -ne "resolved") {
                continue
            }

            foreach ($argument in $entry.command_shape.arguments) {
                if ($null -eq $argument.input) {
                    continue
                }

                $candidate = $null
                if ($argument.input.default.status -eq "reviewed" -and
                    @($argument.input.default.evidence) -contains "sa_2026_generated_vb") {
                    $candidate = $argument.input.default.value
                }
                elseif ($null -ne $argument.input.default.PSObject.Properties["review_status"] -and
                    $argument.input.default.review_status -eq "needs_review") {
                    $candidateEntry = @($argument.input.default.candidates | Where-Object {
                            $_.source -eq "sa_2026_generated_vb"
                        }) | Select-Object -First 1
                    if ($null -ne $candidateEntry) {
                        $candidate = $candidateEntry.value
                    }
                }

                if ($null -eq $candidate -or $argument.sdk_binding.setter -eq "unavailable") {
                    continue
                }

                $values = if ($candidate -is [Collections.IEnumerable] -and
                    $candidate -isnot [string]) {
                    @($candidate | ForEach-Object { Convert-CommittedCandidate $_ })
                }
                else {
                    @(Convert-CommittedCandidate $candidate)
                }
                $results.Add([pscustomobject]@{
                    Key = "$(Get-NormalizedIdentifier $entry.command_shape.mp_step)|" +
                        "$(Get-NormalizedIdentifier $argument.mp_name)"
                    Step = $entry.command_shape.mp_step
                    Argument = $argument.mp_name
                    Setter = $argument.sdk_binding.setter
                    Values = $values
                    Ambiguous = $false
                })
            }
        }
    }

    return @($results | Sort-Object Key)
}

function Get-VbCalls {
    param([Parameter(Mandatory)][string]$Root)

    $stepPattern = [regex]::new(
        'NrkSdk\s*\.\s*SetStep\s*\(\s*"(?<step>(?:""|[^"])*)"\s*\)',
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    $callPattern = [regex]::new(
        '(?ms)NrkSdk\s*\.\s*(?<setter>Set[A-Za-z0-9]+Arg[0-9]*)\s*\(' +
        '\s*"(?<argument>(?:""|[^"])*)"\s*,(?<value>.*?)\)\s*(?:\r?\n|$)')

    $results = [Collections.Generic.List[object]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $Root -Recurse -File |
            Where-Object { $_.Extension -in @(".txt", ".vb") }) {
        $text = Get-Content -Raw -LiteralPath $file.FullName
        $steps = $stepPattern.Matches($text)
        for ($index = 0; $index -lt $steps.Count; $index++) {
            $stepMatch = $steps[$index]
            $end = if ($index + 1 -lt $steps.Count) { $steps[$index + 1].Index } else { $text.Length }
            $block = $text.Substring($stepMatch.Index, $end - $stepMatch.Index)
            $executeIndex = $block.IndexOf("NrkSdk.ExecuteStep", [StringComparison]::Ordinal)
            if ($executeIndex -lt 0) {
                continue
            }

            $inputBlock = $block.Substring(0, $executeIndex)
            foreach ($call in $callPattern.Matches($inputBlock)) {
                $values = Split-TopLevel $call.Groups["value"].Value
                $parsed = [Collections.Generic.List[object]]::new()
                foreach ($value in $values) {
                    $parsed.Add((Convert-CSharpDefault $value))
                }

                $results.Add([pscustomobject]@{
                    Step = $stepMatch.Groups["step"].Value.Replace('""', '"')
                    Argument = $call.Groups["argument"].Value.Replace('""', '"')
                    Setter = $call.Groups["setter"].Value
                    Values = $parsed.ToArray()
                })
            }
        }
    }

    return @(
        $results |
            Group-Object {
                "$(Get-NormalizedIdentifier $_.Step)|$(Get-NormalizedIdentifier $_.Argument)"
            } |
            ForEach-Object {
                [pscustomobject]@{
                    Key = $_.Name
                    Step = $_.Group[0].Step
                    Argument = $_.Group[0].Argument
                    Setter = $_.Group[0].Setter
                    Values = $_.Group[0].Values
                    Ambiguous = $_.Count -ne 1
                }
            } |
            Sort-Object Key
    )
}

function Convert-CandidateValue {
    param([Parameter(Mandatory)][object[]]$Values)

    if ($Values.Count -eq 1) {
        return $Values[0].Value
    }

    return @($Values.Value)
}

function Test-EquivalentDefaults {
    param(
        [Parameter(Mandatory)][object]$ObjectiveSA,
        [Parameter(Mandatory)][object[]]$VbValues
    )

    if ($VbValues.Count -ne 1 -or $null -eq $ObjectiveSA.Comparable) {
        return $false
    }

    return $ObjectiveSA.Comparable -eq $VbValues[0].Comparable
}

$objectiveDefaults = @(Get-ObjectiveSADefaults -Root $ObjectiveSARoot)
$vbCalls = if ([string]::IsNullOrWhiteSpace($SdkCodeRoot)) {
    @(Get-CommittedExactTargetCalls -Root $DispositionDirectory)
}
else {
    @(Get-VbCalls -Root $SdkCodeRoot)
}
$objectiveByKey = @{}
foreach ($entry in $objectiveDefaults) {
    $objectiveByKey[$entry.Key] = $entry
}

$vbByKey = @{}
foreach ($entry in $vbCalls) {
    $vbByKey[$entry.Key] = $entry
}

$inventory = Get-Content -Raw -LiteralPath $InventoryPath | ConvertFrom-Json -Depth 100
$inventoryByKey = @{}
foreach ($command in $inventory.commands) {
    $inventoryByKey[$command.inventory_key] = $command
}

$reviewedCount = 0
$needsReviewCount = 0
$noDefaultCount = 0
$changedFiles = [Collections.Generic.List[string]]::new()
$reviewRows = [Collections.Generic.List[object]]::new()

foreach ($shardPath in Get-ChildItem -LiteralPath (Join-Path $DispositionDirectory "categories") -Filter "*.json" -File) {
    $shard = Get-Content -Raw -LiteralPath $shardPath.FullName | ConvertFrom-Json -Depth 100
    $shardChanged = $false
    foreach ($entry in $shard.entries) {
        if ($entry.disposition -ne "approved_candidate" -or
            $entry.command_shape.status -ne "resolved") {
            continue
        }

        $inventoryCommand = $inventoryByKey[$entry.inventory_key]
        foreach ($argument in $entry.command_shape.arguments) {
            if ($null -eq $argument.input) {
                continue
            }

            if ($argument.input.presence -eq "optional" -and
                $argument.input.omission_behavior -eq "omit_sdk_setter") {
                $argument.input.default = [pscustomobject]@{ status = "none" }
                $noDefaultCount++
                $shardChanged = $true
                continue
            }

            if (@($entry.data_classifications | Where-Object {
                        $_ -in @("credential", "license_data", "path")
                    }).Count -ne 0) {
                $argument.input.default = [pscustomobject]@{ status = "none" }
                $noDefaultCount++
                $shardChanged = $true
                continue
            }
            $key = "$(Get-NormalizedIdentifier $entry.command_shape.mp_step)|" +
                "$(Get-NormalizedIdentifier $argument.mp_name)"
            $objective = $objectiveByKey[$key]
            $vb = $vbByKey[$key]

            if ($null -ne $objective -and -not $objective.Conflict -and
                $objective.Defaults.Count -eq 1 -and $null -ne $vb -and
                -not $vb.Ambiguous -and $objective.Setters[0] -eq $vb.Setter -and
                $vb.Setter -eq $argument.sdk_binding.setter -and
                (Test-EquivalentDefaults $objective.Defaults[0] $vb.Values)) {
                $argument.input.presence = "optional"
                $argument.input.omission_behavior = "set_catalog_default"
                $argument.input.default = [ordered]@{
                    status = "reviewed"
                    value = Convert-CandidateValue $vb.Values
                    evidence = @(
                        "objectivesa_prior_release",
                        "sa_2026_generated_vb"
                    )
                }
                $reviewedCount++
                $shardChanged = $true
                $reviewRows.Add([pscustomobject]@{
                    Step = $entry.mp_step
                    Argument = $argument.mp_name
                    Status = "reviewed"
                    ObjectiveSA = $objective.Defaults[0].Value
                    SA2026 = Convert-CandidateValue $vb.Values
                })
                continue
            }

            $candidates = [Collections.Generic.List[object]]::new()
            if ($null -ne $objective) {
                foreach ($default in $objective.Defaults) {
                    $candidates.Add([ordered]@{
                        source = "objectivesa_prior_release"
                        value = $default.Value
                    })
                }
            }

            $allVbValuesParsed = $null -ne $vb -and -not $vb.Ambiguous -and
                @($vb.Values | Where-Object { $null -eq $_.Comparable }).Count -eq 0 -and
                @($vb.Values | Where-Object { $_.Kind -eq "string" -and $_.Value.Length -eq 0 }).Count -eq 0
            if ($allVbValuesParsed) {
                $candidates.Add([ordered]@{
                    source = "sa_2026_generated_vb"
                    value = Convert-CandidateValue $vb.Values
                })
            }

            if ($candidates.Count -eq 0) {
                $argument.input.default = [pscustomobject]@{ status = "none" }
                $noDefaultCount++
                $shardChanged = $true
                continue
            }

            $argument.input.presence = "required"
            $argument.input.omission_behavior = "reject_request"
            $argument.input.default = [ordered]@{
                status = "none"
                review_status = "needs_review"
                candidates = $candidates.ToArray()
            }
            $needsReviewCount++
            $shardChanged = $true
            $reviewRows.Add([pscustomobject]@{
                Step = $entry.mp_step
                Argument = $argument.mp_name
                Status = "needs_review"
                ObjectiveSA = if ($null -ne $objective) {
                    @($objective.Defaults.Value) -join "; "
                } else {
                    ""
                }
                SA2026 = if ($allVbValuesParsed) {
                    Convert-CandidateValue $vb.Values | ConvertTo-Json -Compress -Depth 20
                } else {
                    ""
                }
            })
        }
    }

    if ($Apply -and $shardChanged) {
        $json = $shard | ConvertTo-Json -Depth 100
        $normalizedJson = $json.Replace("`r`n", "`n").Replace("`r", "`n")
        [IO.File]::WriteAllText(
            $shardPath.FullName,
            "$normalizedJson`n",
            [Text.UTF8Encoding]::new($false))
        $changedFiles.Add($shardPath.FullName)
    }
}
Write-Host "ObjectiveSA default mappings: $($objectiveDefaults.Count)"
Write-Host "SA 2026 exact-target setter samples: $($vbCalls.Count)"
Write-Host "Reviewed defaults: $reviewedCount"
Write-Host "Defaults needing review: $needsReviewCount"
Write-Host "Inputs with no default candidate: $noDefaultCount"

if ($VerbosePreference -eq "Continue") {
    $reviewRows |
        Sort-Object Status, Step, Argument |
        Format-Table -AutoSize
}

if ($Apply) {
    Write-Host "Updated $($changedFiles.Count) disposition shard(s)."
}
