[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

# Requires each exact-SA target's runtime conditional option table
# (src/Briosa.Server/Security/OperationConditionalOptions.cs) to agree exactly
# with the reviewed seed in eng/classification/conditional-ui.json and
# conditional-background.json (#293), in both directions: every caller_option
# and caller_option_default_flips entry that names the target has exactly one
# matching row, and every row has exactly one matching entry. It compares the
# operation, request field, MP argument, effect, trigger, enabling condition,
# enabling values and what Briosa sends when the field is absent. It also
# checks that each entry's briosa_default_enables_ui or
# briosa_default_leaves_work_running agrees with its guard. Any finding fails.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$targetNames = @('2026.1.0529.7', '2024.1.0508.5')
$resolvedRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$lists = @(
    [pscustomobject]@{ File = 'conditional-ui.json'; Effect = 'InteractiveUi'; DefaultKey = 'briosa_default_enables_ui' },
    [pscustomobject]@{ File = 'conditional-background.json'; Effect = 'BackgroundWork'; DefaultKey = 'briosa_default_leaves_work_running' }
)
$triggers = @{ caller_option = 'CallerOption'; caller_option_default_flips = 'DefaultFlip' }
$conditions = @{ when_true = 'WhenTrue'; when_false = 'WhenFalse'; when_present = 'WhenPresent'; when_non_empty = 'WhenNonEmpty'; when_one_of = 'WhenOneOf' }
$absences = @{ sends_true = 'SendsTrue'; sends_false = 'SendsFalse'; sends_nothing = 'SendsNothing'; rejected = 'Rejected' }

$rowPattern = [regex]::new(
    '^\s*new\("(?<id>[a-z0-9_]+\.[a-z0-9_]+)",\s*"(?<field>[a-z0-9_]+)",\s*"(?<mp>[^"]+)",\s*' +
    '(?<effect>[A-Za-z]+),\s*(?<trigger>[A-Za-z]+),\s*(?<condition>[A-Za-z]+),\s*(?<absence>[A-Za-z]+)' +
    '(?<values>(?:,\s*"[A-Z0-9_]+")*)\),?\s*$')

function Format-Row {
    param($Id, $Field, $Mp, $Effect, $Trigger, $Condition, $Absence, [string[]]$Values)
    '{0}.{1} | {2} | {3} | {4} | {5} | {6} | [{7}]' -f $Id, $Field, $Mp, $Effect, $Trigger, $Condition, $Absence, ($Values -join ',')
}

function Test-DefaultEnables {
    param([string]$Condition, [string]$Absence)
    switch ($Absence) {
        'sends_true' { return $Condition -ceq 'when_true' }
        'sends_false' { return $Condition -ceq 'when_false' }
        'sends_nothing' { return $false }
        'rejected' { return $null }
    }
    throw "Unknown absence '$Absence'."
}

$findings = [Collections.Generic.List[string]]::new()
$expected = @{}
foreach ($target in $targetNames) { $expected[$target] = [Collections.Generic.List[string]]::new() }

foreach ($list in $lists) {
    $path = Join-Path $resolvedRoot "eng/classification/$($list.File)"
    foreach ($entry in @((Get-Content -LiteralPath $path -Raw | ConvertFrom-Json).entries)) {
        if ($entry.trigger -ceq 'job_or_file_state') {
            if ($entry.PSObject.Properties.Name -contains 'guard') {
                $findings.Add("$($list.File): job_or_file_state entry $($entry.operation_id) must not have a guard.")
            }
            continue
        }
        $label = "$($list.File): $($entry.operation_id).$($entry.field)"
        if (-not $triggers.ContainsKey($entry.trigger) -or $entry.PSObject.Properties.Name -notcontains 'guard') {
            $findings.Add("${label}: needs a known trigger and a guard.")
            continue
        }
        $guard = $entry.guard
        if (-not $conditions.ContainsKey($guard.condition) -or -not $absences.ContainsKey($guard.absent)) {
            $findings.Add("${label}: unknown guard condition or absence.")
            continue
        }
        $values = @()
        if ($guard.PSObject.Properties.Name -contains 'values') { $values = @($guard.values) }
        if (($guard.condition -ceq 'when_one_of') -ne ($values.Count -gt 0)) {
            $findings.Add("${label}: enabling values are listed exactly for when_one_of.")
        }
        $defaultEnables = Test-DefaultEnables $guard.condition $guard.absent
        $recorded = $entry.($list.DefaultKey)
        if ($recorded -ne $defaultEnables -or ($null -eq $recorded) -ne ($null -eq $defaultEnables)) {
            $findings.Add("${label}: $($list.DefaultKey) is '$recorded' but the guard gives '$defaultEnables'.")
        }
        $row = Format-Row $entry.operation_id $entry.field $entry.mp_argument $list.Effect `
            $triggers[$entry.trigger] $conditions[$guard.condition] $absences[$guard.absent] $values
        foreach ($target in @($entry.targets)) {
            if (-not $expected.ContainsKey($target)) {
                $findings.Add("${label}: unknown target '$target'.")
                continue
            }
            $expected[$target].Add($row)
        }
    }
}

$counts = [ordered]@{}
foreach ($target in $targetNames) {
    $path = Join-Path $resolvedRoot "targets/$target/src/Briosa.Server/Security/OperationConditionalOptions.cs"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Target '$target' has no conditional option table at '$path'."
    }
    $actual = [Collections.Generic.List[string]]::new()
    $lineNumber = 0
    foreach ($line in [IO.File]::ReadAllLines($path)) {
        $lineNumber++
        if ($line -notmatch '^\s*new\("') { continue }
        $match = $rowPattern.Match($line)
        if (-not $match.Success) {
            throw "Cannot read the conditional option row at ${path}:$lineNumber. Keep one entry per line."
        }
        $values = @([regex]::Matches($match.Groups['values'].Value, '"([A-Z0-9_]+)"') | ForEach-Object { $_.Groups[1].Value })
        $actual.Add((Format-Row $match.Groups['id'].Value $match.Groups['field'].Value $match.Groups['mp'].Value `
                    $match.Groups['effect'].Value $match.Groups['trigger'].Value $match.Groups['condition'].Value `
                    $match.Groups['absence'].Value $values))
    }
    $counts[$target] = $actual.Count
    foreach ($row in ($actual | Group-Object -CaseSensitive | Where-Object Count -gt 1)) {
        $findings.Add("${target}: the C# table repeats $($row.Name).")
    }
    foreach ($row in $expected[$target]) {
        if (-not $actual.Contains($row)) { $findings.Add("${target}: JSON entry has no matching C# row: $row") }
    }
    foreach ($row in $actual) {
        if (-not $expected[$target].Contains($row)) { $findings.Add("${target}: C# row has no matching JSON entry: $row") }
    }
}

Write-Host ("Operation conditional options: {0} finding(s); {1}." -f $findings.Count,
    (($counts.GetEnumerator() | ForEach-Object { "$($_.Value) rows in $($_.Key)" }) -join ', '))
foreach ($finding in $findings) {
    Write-Host "  $finding"
}
if ($findings.Count -gt 0) {
    exit 1
}
exit 0
