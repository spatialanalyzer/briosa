[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

# Requires each exact-SA target's transcribed argument defaults table
# (tests/Briosa.Server.Tests/ArgumentDefaultsTable.cs) to agree exactly with the
# reviewed D2 table in eng/classification/argument-defaults-<target>.csv (#243), in
# both directions: every CSV row has exactly one matching C# row and every C# row
# has exactly one matching CSV row. It compares the operation, protobuf field, MP
# argument, SDK binding, and the major-3 outcome when the field is absent,
# explicitly empty, and set. It also checks the outcome vocabulary and that a
# removed row is removed in all three columns. Any finding fails.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$targetNames = @('2026.1.0529.7', '2024.1.0508.5')
$resolvedRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$absentPattern = '^(omitted|rejected|sends_blank|removed|rejected_unless:[a-z0-9_]+|sends_default:(true|false|-?[0-9.]+))$'
$emptyPattern = '^(rejected|sends_blank|sends_all_limits_disabled|not_applicable|removed)$'
$valuePattern = '^(sent|removed)$'
$rowPattern = [regex]::new(
    '^\s*new\("(?<id>[a-z0-9_]+\.[a-z0-9_]+)",\s*"(?<field>[A-Za-z0-9]+\.[a-z0-9_]+)",\s*"(?<mp>(?:[^"\\]|\\.)*)",\s*' +
    '"(?<binding>[A-Za-z0-9]+)",\s*"(?<absent>[^"]+)",\s*"(?<empty>[^"]+)",\s*"(?<value>[^"]+)"\),?\s*$')

function Format-Row {
    param($Id, $Field, $Mp, $Binding, $Absent, $Empty, $Value)
    '{0} | {1} | {2} | {3} | {4} | {5} | {6}' -f $Id, $Field, $Mp, $Binding, $Absent, $Empty, $Value
}

$findings = [Collections.Generic.List[string]]::new()
$counts = [ordered]@{}
foreach ($target in $targetNames) {
    $csvPath = Join-Path $resolvedRoot "eng/classification/argument-defaults-$target.csv"
    $tablePath = Join-Path $resolvedRoot "targets/$target/tests/Briosa.Server.Tests/ArgumentDefaultsTable.cs"
    foreach ($path in @($csvPath, $tablePath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Target '$target' has no argument defaults file at '$path'."
        }
    }

    $expected = [Collections.Generic.List[string]]::new()
    foreach ($entry in @(Import-Csv -LiteralPath $csvPath -Encoding utf8)) {
        $label = "${target} CSV: $($entry.operation_id) $($entry.proto_field)"
        if ($entry.major3_absent -cnotmatch $absentPattern -or
            $entry.major3_explicit_empty -cnotmatch $emptyPattern -or
            $entry.major3_value -cnotmatch $valuePattern) {
            $findings.Add("${label}: unknown major-3 outcome.")
        }
        $removed = @($entry.major3_absent, $entry.major3_explicit_empty, $entry.major3_value) |
            Where-Object { $_ -ceq 'removed' }
        if (@($removed).Count -notin @(0, 3)) {
            $findings.Add("${label}: a removed row is removed in all three columns.")
        }
        if (($entry.category -ceq 'removed_in_major_3') -ne (@($removed).Count -eq 3)) {
            $findings.Add("${label}: only the removed_in_major_3 category is removed.")
        }
        $expected.Add((Format-Row $entry.operation_id $entry.proto_field $entry.mp_argument $entry.sdk_binding `
                    $entry.major3_absent $entry.major3_explicit_empty $entry.major3_value))
    }

    $actual = [Collections.Generic.List[string]]::new()
    $lineNumber = 0
    foreach ($line in [IO.File]::ReadAllLines($tablePath)) {
        $lineNumber++
        if ($line -notmatch '^\s*new\("') { continue }
        $match = $rowPattern.Match($line)
        if (-not $match.Success) {
            throw "Cannot read the argument defaults row at ${tablePath}:$lineNumber. Keep one row per line."
        }
        $mp = $match.Groups['mp'].Value.Replace('\"', '"').Replace('\\', '\')
        $actual.Add((Format-Row $match.Groups['id'].Value $match.Groups['field'].Value $mp `
                    $match.Groups['binding'].Value $match.Groups['absent'].Value $match.Groups['empty'].Value `
                    $match.Groups['value'].Value))
    }

    $counts[$target] = $actual.Count
    foreach ($row in ($expected | Group-Object -CaseSensitive | Where-Object Count -gt 1)) {
        $findings.Add("${target}: the CSV repeats $($row.Name).")
    }
    foreach ($row in ($actual | Group-Object -CaseSensitive | Where-Object Count -gt 1)) {
        $findings.Add("${target}: the C# table repeats $($row.Name).")
    }
    foreach ($row in $expected) {
        if (-not $actual.Contains($row)) { $findings.Add("${target}: CSV row has no matching C# row: $row") }
    }
    foreach ($row in $actual) {
        if (-not $expected.Contains($row)) { $findings.Add("${target}: C# row has no matching CSV row: $row") }
    }
}

Write-Host ("Argument defaults: {0} finding(s); {1}." -f $findings.Count,
    (($counts.GetEnumerator() | ForEach-Object { "$($_.Value) rows in $($_.Key)" }) -join ', '))
foreach ($finding in $findings) {
    Write-Host "  $finding"
}
if ($findings.Count -gt 0) {
    exit 1
}
exit 0
