[CmdletBinding()]
param(
    [string]$BufPath = 'buf'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$protocol = Join-Path $PSScriptRoot 'Test-CrossTargetProtocol.ps1'
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryBase "briosa-protocol-policy-$([Guid]::NewGuid().ToString('N'))"
$summaryPath = Join-Path $fixture 'summary.md'
$first = '2026.1.0529.7'
$second = '2024.1.0508.5'
$savedSummary = $env:GITHUB_STEP_SUMMARY
$savedActions = $env:GITHUB_ACTIONS

function Set-FixtureFile([string]$Path, [string]$Content) {
    $fullPath = Join-Path $fixture $Path
    [IO.Directory]::CreateDirectory((Split-Path -Parent $fullPath)) | Out-Null
    [IO.File]::WriteAllText($fullPath, $Content.Replace("`r`n", "`n"))
}
function Set-Allowlist([string]$Name, [object[]]$Entries) {
    Set-FixtureFile $Name (ConvertTo-Json -Depth 5 -InputObject @{ typeConflicts = @($Entries) })
    Join-Path $fixture $Name
}
function New-Entry([int]$Number, [string]$FirstSignature, [string]$SecondSignature, [string]$Rationale = 'Fixture rationale.') {
    [ordered]@{
        message = 'briosa.Shared'
        number = $Number
        signatures = [ordered]@{ $first = $FirstSignature; $second = $SecondSignature }
        rationale = $Rationale
        decision = 'Fixture decision'
    }
}
function Invoke-Protocol([string]$Allowlist, [switch]$Actions) {
    if (Test-Path -LiteralPath $summaryPath) { Remove-Item -LiteralPath $summaryPath }
    $env:GITHUB_STEP_SUMMARY = $summaryPath
    $env:GITHUB_ACTIONS = if ($Actions) { 'true' } else { 'false' }
    $output = @(& $protocol -BufPath $BufPath -RepositoryRoot $fixture -AllowlistPath $Allowlist 6>&1 | ForEach-Object { "$_" })
    $exitCode = $LASTEXITCODE
    $sections = @{}
    $section = $null
    foreach ($line in [IO.File]::ReadAllLines($summaryPath)) {
        if ($line -match '^<details><summary>(?<title>.+) \(\d+\)</summary>$') {
            $section = $Matches.title
            $sections[$section] = [Collections.Generic.List[string]]::new()
        }
        elseif ($line -ceq '### Wire hazards: same-number type or cardinality conflicts') {
            $section = 'Conflicts'
            $sections[$section] = [Collections.Generic.List[string]]::new()
        }
        elseif ($line -ceq '### Informational differences' -or $line -ceq '</details>') {
            $section = $null
        }
        elseif ($null -ne $section -and $line -match '^\| ' -and $line -notmatch '^\| (Message|RPC|Enum) \|' -and $line -notmatch '^\|( -+:? \|)+$') {
            $sections[$section].Add($line)
        }
    }
    [pscustomobject]@{
        ExitCode = $exitCode
        Output = $output
        Summary = [IO.File]::ReadAllText($summaryPath)
        Sections = $sections
    }
}
function Assert-Section($Result, [string]$Section, [string[]]$Rows) {
    $actual = @(if ($Result.Sections.ContainsKey($Section)) { $Result.Sections[$Section] }) -join "`n"
    $expected = $Rows -join "`n"
    if ($actual -cne $expected) {
        throw "Section '$Section' reported:`n$actual`nexpected:`n$expected"
    }
}
function Assert-Throws([string]$Name, [string]$Allowlist, [string]$Fragment) {
    try {
        Invoke-Protocol $Allowlist | Out-Null
    }
    catch {
        if ($_.Exception.Message -notlike "*$Fragment*") {
            throw "$Name threw '$($_.Exception.Message)', expected '$Fragment'."
        }
        return
    }
    throw "$Name did not reject the allowlist."
}

$bufYaml = "version: v2`nmodules:`n  - path: proto`n"
$firstProto = @'
syntax = "proto3";

package briosa;

enum Choice {
  CHOICE_UNSPECIFIED = 0;
}

enum FirstOnlyChoice {
  FIRST_ONLY_CHOICE_UNSPECIFIED = 0;
}

message Empty {}

message Shared {
  message Inner {}
  string name = 1;
  int32 count = 2;
  optional double offset = 3;
  string first_only_field = 4;
  string first_name = 5;
  repeated int32 values = 6;
  Choice choice = 7;
}

message FirstOnlyMessage {
  string value = 1;
}

service Fixture {
  rpc Run(Shared) returns (Shared);
  rpc Changed(Shared) returns (Empty);
  rpc FirstOnly(Empty) returns (Empty);
}
'@
$secondProto = @'
syntax = "proto3";

package briosa;

enum Choice {
  CHOICE_UNSPECIFIED = 0;
}

enum SecondOnlyChoice {
  SECOND_ONLY_CHOICE_UNSPECIFIED = 0;
}

message Empty {}

message Shared {
  string name = 1;
  int64 count = 2;
  optional bool offset = 3;
  string second_name = 5;
  int32 values = 6;
  Choice choice = 7;
  string second_only_field = 8;
}

message SecondOnlyMessage {
  string value = 1;
}

service Fixture {
  rpc Run(Shared) returns (Shared);
  rpc Changed(Empty) returns (Empty);
  rpc SecondOnly(Empty) returns (Empty);
}
'@

try {
    foreach ($target in @($first, $second)) {
        Set-FixtureFile "targets/$target/buf.yaml" $bufYaml
    }
    Set-FixtureFile "targets/$first/proto/briosa/fixture.proto" $firstProto
    Set-FixtureFile "targets/$second/proto/briosa/fixture.proto" $secondProto

    $offsetEntry = New-Entry 3 'optional double' 'optional bool'
    $countEntry = New-Entry 2 'int32' 'int64'
    $valuesEntry = New-Entry 6 'repeated int32' 'int32'

    # Detection of every category, with one allowlisted conflict and two new ones.
    $partial = Invoke-Protocol (Set-Allowlist 'partial.json' @($offsetEntry)) -Actions
    if ($partial.ExitCode -ne 1) { throw "Partially allowlisted run exited $($partial.ExitCode), expected 1." }
    Assert-Section $partial 'Conflicts' @(
        '| `briosa.Shared` | 2 | `int32 count` | `int64 count` | **no** |  |'
        '| `briosa.Shared` | 3 | `optional double offset` | `optional bool offset` | yes | Fixture decision |'
        '| `briosa.Shared` | 6 | `repeated int32 values` | `int32 values` | **no** |  |')
    Assert-Section $partial 'Renamed fields' @('| briosa.Shared | 5 | first_name | second_name |')
    Assert-Section $partial 'RPC signature differences' @(
        '| briosa.Fixture/Changed | (briosa.Shared) returns (briosa.Empty) | (briosa.Empty) returns (briosa.Empty) |')
    Assert-Section $partial "Messages only in $first" @('| briosa.FirstOnlyMessage |', '| briosa.Shared.Inner |')
    Assert-Section $partial "Messages only in $second" @('| briosa.SecondOnlyMessage |')
    Assert-Section $partial "Enums only in $first" @('| briosa.FirstOnlyChoice |')
    Assert-Section $partial "Enums only in $second" @('| briosa.SecondOnlyChoice |')
    Assert-Section $partial "RPCs only in $first" @('| briosa.Fixture/FirstOnly |')
    Assert-Section $partial "RPCs only in $second" @('| briosa.Fixture/SecondOnly |')
    Assert-Section $partial "Fields only in $first (shared messages)" @('| briosa.Shared | 4 | first_only_field | string |')
    Assert-Section $partial "Fields only in $second (shared messages)" @('| briosa.Shared | 8 | second_only_field | string |')
    if ($partial.Summary -notmatch '\| Field numbers in shared messages \| 6 \| 1 \| 1 \|') {
        throw 'Partially allowlisted run did not count shared and one-sided field numbers.'
    }
    $annotations = @($partial.Output | Where-Object { $_ -like '::error title=Cross-target protocol wire hazard::*' })
    if ($annotations.Count -ne 2) { throw "Expected 2 wire-hazard annotations, found $($annotations.Count)." }

    # Every conflict reviewed: informational differences alone do not fail.
    $full = Invoke-Protocol (Set-Allowlist 'full.json' @($offsetEntry, $countEntry, $valuesEntry))
    if ($full.ExitCode -ne 0) { throw "Fully allowlisted run exited $($full.ExitCode), expected 0." }
    if ($full.Summary -match 'Stale allowlist') { throw 'Fully allowlisted run reported a stale entry.' }

    # An entry must match both exact signatures; a changed conflict is new and the old entry is stale.
    $changed = Invoke-Protocol (Set-Allowlist 'changed.json' @(
            (New-Entry 3 'optional double' 'optional float'), $countEntry, $valuesEntry))
    if ($changed.ExitCode -ne 1) { throw "Changed-signature run exited $($changed.ExitCode), expected 1." }
    if ($changed.Summary -notmatch 'Stale allowlist entries \(no matching conflict\): `briosa.Shared` field 3') {
        throw 'Changed-signature run did not report the stale allowlist entry.'
    }

    Assert-Throws 'Missing rationale' (Set-Allowlist 'no-rationale.json' @(
            (New-Entry 3 'optional double' 'optional bool' ' '))) 'reviewed rationale'
    Assert-Throws 'Duplicate entry' (Set-Allowlist 'duplicate.json' @($offsetEntry, $offsetEntry)) 'more than once'
    $missingTarget = New-Entry 3 'optional double' 'optional bool'
    $missingTarget.signatures.Remove($second)
    Assert-Throws 'Missing target signature' (Set-Allowlist 'missing-target.json' @($missingTarget)) "no $second signature"

    # Identical schemas report no differences.
    Set-FixtureFile "targets/$second/proto/briosa/fixture.proto" $firstProto
    $identical = Invoke-Protocol (Set-Allowlist 'empty.json' @())
    if ($identical.ExitCode -ne 0 -or $identical.Summary -notmatch 'No type or cardinality conflicts\.') {
        throw 'Identical schemas did not produce an empty conflict report.'
    }

    Write-Host 'Cross-target protocol policy passed: one-sided messages, nested messages, enums, RPCs and fields; renames; RPC signatures; type and cardinality conflicts; allowlist matching, staleness and validation.'
}
finally {
    $env:GITHUB_STEP_SUMMARY = $savedSummary
    $env:GITHUB_ACTIONS = $savedActions
    $resolved = [IO.Path]::GetFullPath($fixture)
    if (-not $resolved.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -or $resolved -eq $temporaryBase) {
        throw 'Refusing cleanup outside the temporary fixture directory.'
    }
    if (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
