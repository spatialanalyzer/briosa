[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$DifferencesPath,
    [switch]$Strict
)

# Compares the D1 classification rows (#242, #293) of operations that both
# exact-SA targets register. Shared operations must have identical rows unless
# eng/classification/target-differences.json records both exact rows with a
# reviewed rationale. It also checks that every operation in
# eng/classification/conditional-ui.json names exactly the targets that register
# it. A table this script cannot read fails; differences, stale reviewed entries
# and conditional-UI mismatches are advisory unless -Strict is set.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$targetNames = @('2026.1.0529.7', '2024.1.0508.5')
$resolvedRoot = [IO.Path]::GetFullPath($RepositoryRoot)
if ([string]::IsNullOrWhiteSpace($DifferencesPath)) {
    $DifferencesPath = Join-Path $resolvedRoot 'eng/classification/target-differences.json'
}

$rowPattern = [regex]::new(
    '^\s*new\("(?<id>[a-z0-9_]+\.[a-z0-9_]+)",\s*(?<risks>[A-Za-z]+(?:\s*\|\s*[A-Za-z]+)*),\s*' +
    '(?<duration>[A-Za-z]+),\s*(?<validation>[A-Za-z]+),\s*(?<isolation>[A-Za-z]+)\),?\s*$')

function Read-ClassificationTable {
    param([Parameter(Mandatory)][string]$Target)

    $path = Join-Path $resolvedRoot "targets/$Target/src/Briosa.Server/Security/OperationClassification.cs"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Target '$Target' has no classification table at '$path'."
    }

    $rows = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $lineNumber = 0
    foreach ($line in [IO.File]::ReadAllLines($path)) {
        $lineNumber++
        if ($line -notmatch '^\s*new\("') {
            continue
        }
        $match = $rowPattern.Match($line)
        if (-not $match.Success) {
            throw "Cannot read the classification row at ${path}:$lineNumber. Keep one row per line."
        }
        $id = $match.Groups['id'].Value
        if ($rows.ContainsKey($id)) {
            throw "Target '$Target' classifies '$id' more than once."
        }
        $risks = @($match.Groups['risks'].Value.Split('|') | ForEach-Object { $_.Trim() } |
                Sort-Object -CaseSensitive) -join ' | '
        $rows[$id] = '{0}, {1}, {2}, {3}' -f $risks, $match.Groups['duration'].Value,
            $match.Groups['validation'].Value, $match.Groups['isolation'].Value
    }
    if ($rows.Count -eq 0) {
        throw "Target '$Target' classification table has no readable rows."
    }
    , $rows
}

$first, $second = $targetNames
$firstRows = Read-ClassificationTable $first
$secondRows = Read-ClassificationTable $second

$reviewed = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
foreach ($entry in @((Get-Content -LiteralPath $DifferencesPath -Raw | ConvertFrom-Json).differences)) {
    if ([string]::IsNullOrWhiteSpace($entry.operation_id) -or
        [string]::IsNullOrWhiteSpace($entry.rationale) -or
        $null -eq $entry.rows.$first -or $null -eq $entry.rows.$second) {
        throw "Every reviewed difference needs operation_id, rows for both targets, and a rationale."
    }
    if ($reviewed.ContainsKey($entry.operation_id)) {
        throw "Reviewed difference '$($entry.operation_id)' is listed more than once."
    }
    $reviewed[$entry.operation_id] = $entry
}

$findings = [Collections.Generic.List[object]]::new()
$shared = @($firstRows.Keys | Where-Object { $secondRows.ContainsKey($_) } | Sort-Object -CaseSensitive)
$reviewedDifferences = 0
foreach ($id in $shared) {
    $firstRow = $firstRows[$id]
    $secondRow = $secondRows[$id]
    $entry = if ($reviewed.ContainsKey($id)) { $reviewed[$id] } else { $null }
    if ($firstRow -ceq $secondRow) {
        if ($null -ne $entry) {
            $findings.Add([pscustomobject]@{ Id = $id; Kind = 'Stale reviewed difference (rows now match)'; First = $firstRow; Second = $secondRow })
        }
        continue
    }
    if ($null -ne $entry -and $entry.rows.$first -ceq $firstRow -and $entry.rows.$second -ceq $secondRow) {
        $reviewedDifferences++
        continue
    }
    $kind = if ($null -eq $entry) { 'Unreviewed difference' } else { 'Reviewed difference no longer matches the recorded rows' }
    $findings.Add([pscustomobject]@{ Id = $id; Kind = $kind; First = $firstRow; Second = $secondRow })
}
foreach ($id in ($reviewed.Keys | Sort-Object -CaseSensitive)) {
    if (-not ($firstRows.ContainsKey($id) -and $secondRows.ContainsKey($id))) {
        $findings.Add([pscustomobject]@{ Id = $id; Kind = 'Stale reviewed difference (not a shared operation)'; First = ''; Second = '' })
    }
}

# Every operation in the reviewed conditional-UI list must name exactly the targets that register it.
$conditionalPath = Join-Path $resolvedRoot 'eng/classification/conditional-ui.json'
$conditionalEntries = 0
if (Test-Path -LiteralPath $conditionalPath -PathType Leaf) {
    $tables = @{ $first = $firstRows; $second = $secondRows }
    foreach ($entry in @((Get-Content -LiteralPath $conditionalPath -Raw | ConvertFrom-Json).entries)) {
        $conditionalEntries++
        $registeredBy = @($targetNames | Where-Object { $tables[$_].ContainsKey($entry.operation_id) })
        $listed = @($entry.targets)
        if ($registeredBy.Count -eq 0 -or
            (Compare-Object -ReferenceObject $registeredBy -DifferenceObject $listed -CaseSensitive)) {
            $findings.Add([pscustomobject]@{
                    Id = "$($entry.operation_id) ($($entry.field))"
                    Kind = "Conditional UI entry targets [$($listed -join ', ')] do not match the registering targets [$($registeredBy -join ', ')]"
                    First = ''; Second = '' })
        }
    }
}

Write-Host ("Operation classification twins: {0} rows in {1}, {2} in {3}, {4} shared, {5} reviewed differences, {6} conditional UI entries, {7} finding(s)." -f
    $firstRows.Count, $first, $secondRows.Count, $second, $shared.Count, $reviewedDifferences, $conditionalEntries, $findings.Count)
foreach ($finding in $findings) {
    Write-Host "  $($finding.Kind): $($finding.Id)"
    if ($finding.First -or $finding.Second) {
        Write-Host "    ${first}: $($finding.First)"
        Write-Host "    ${second}: $($finding.Second)"
    }
    if ($env:GITHUB_ACTIONS -ceq 'true') {
        $message = "$($finding.Kind): $($finding.Id)".Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        Write-Host "::warning title=Operation classification twins::$message"
    }
}

if ($Strict -and $findings.Count -gt 0) {
    Write-Host "Strict mode: $($findings.Count) operation classification twin finding(s)."
    exit 1
}

exit 0
