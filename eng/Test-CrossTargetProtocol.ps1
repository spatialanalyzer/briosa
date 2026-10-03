[CmdletBinding()]
param(
    [string]$BufPath = 'buf',
    [string]$AllowlistPath = (Join-Path $PSScriptRoot 'cross-target-protocol-allowlist.json'),
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

# Compares the committed-module protobuf schemas of the two exact-SA targets.
# Each target's module is compiled with Buf into a FileDescriptorSet (as JSON)
# and the descriptors are compared by fully qualified name and field number.
# Messages, enums, RPCs and fields present in only one target, renamed fields
# and RPC signature differences are informational. A same-number field whose
# type or cardinality differs is a wire hazard and fails unless the reviewed
# allowlist records that exact conflict with a rationale.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$targetNames = @('2026.1.0529.7', '2024.1.0508.5')
$first, $second = $targetNames
$resolvedRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$bufCommand = Get-Command -Name $BufPath -CommandType Application -ErrorAction Stop

function Get-ScalarName {
    param([Parameter(Mandatory)][string]$Type)

    $Type.Substring('TYPE_'.Length).ToLowerInvariant()
}

function Get-FieldSignature {
    param([Parameter(Mandatory)]$Field)

    $type = Get-ScalarName $Field.type
    if ($type -ceq 'message' -or $type -ceq 'enum' -or $type -ceq 'group') {
        $type = "$type $($Field.typeName.TrimStart('.'))"
    }
    $cardinality = switch ($Field.label) {
        'LABEL_REPEATED' { 'repeated ' }
        'LABEL_REQUIRED' { 'required ' }
        default {
            if ($Field.PSObject.Properties.Name -contains 'proto3Optional' -and $Field.proto3Optional) {
                'optional '
            }
            else {
                ''
            }
        }
    }
    "$cardinality$type"
}

function Get-OptionalArray {
    param($Object, [Parameter(Mandatory)][string]$Name)

    if ($null -ne $Object -and $Object.PSObject.Properties.Name -contains $Name) {
        @($Object.$Name)
    }
    else {
        @()
    }
}

function Add-Message {
    param($Schema, [string]$Prefix, $Message)

    $fullName = "$Prefix.$($Message.name)"
    $fields = [Collections.Generic.SortedDictionary[int, object]]::new()
    foreach ($field in Get-OptionalArray $Message 'field') {
        $fields[[int]$field.number] = [pscustomobject]@{
            Name = [string]$field.name
            Signature = Get-FieldSignature $field
        }
    }
    $Schema.Messages[$fullName] = $fields
    foreach ($nested in Get-OptionalArray $Message 'nestedType') {
        Add-Message $Schema $fullName $nested
    }
    foreach ($enum in Get-OptionalArray $Message 'enumType') {
        $Schema.Enums["$fullName.$($enum.name)"] = $true
    }
}

function Get-TargetSchema {
    param([Parameter(Mandatory)][string]$Target, [Parameter(Mandatory)][string]$OutputDirectory)

    $targetDirectory = Join-Path $resolvedRoot "targets/$Target"
    if (-not (Test-Path -LiteralPath (Join-Path $targetDirectory 'buf.yaml') -PathType Leaf)) {
        throw "Target '$Target' has no buf.yaml at $targetDirectory."
    }

    $imagePath = Join-Path $OutputDirectory "$Target.json"
    & $bufCommand.Source build $targetDirectory --exclude-imports --exclude-source-info -o $imagePath
    if ($LASTEXITCODE -ne 0) {
        throw "buf build for target '$Target' failed with exit code $LASTEXITCODE."
    }

    $image = [IO.File]::ReadAllText($imagePath) | ConvertFrom-Json
    $schema = [pscustomobject]@{
        Messages = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
        Enums = [Collections.Generic.Dictionary[string, bool]]::new([StringComparer]::Ordinal)
        Rpcs = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    }
    foreach ($file in Get-OptionalArray $image 'file') {
        $package = if ($file.PSObject.Properties.Name -contains 'package') { [string]$file.package } else { '' }
        foreach ($message in Get-OptionalArray $file 'messageType') {
            Add-Message $schema $package $message
        }
        foreach ($enum in Get-OptionalArray $file 'enumType') {
            $schema.Enums["$package.$($enum.name)"] = $true
        }
        foreach ($service in Get-OptionalArray $file 'service') {
            foreach ($method in Get-OptionalArray $service 'method') {
                $clientStreaming = $method.PSObject.Properties.Name -contains 'clientStreaming' -and $method.clientStreaming
                $serverStreaming = $method.PSObject.Properties.Name -contains 'serverStreaming' -and $method.serverStreaming
                $request = $(if ($clientStreaming) { 'stream ' } else { '' }) + $method.inputType.TrimStart('.')
                $response = $(if ($serverStreaming) { 'stream ' } else { '' }) + $method.outputType.TrimStart('.')
                $schema.Rpcs["$package.$($service.name)/$($method.name)"] = "($request) returns ($response)"
            }
        }
    }
    $schema
}

function Get-Allowlist {
    param([Parameter(Mandatory)][string]$Path)

    $entries = [Collections.Generic.List[object]]::new()
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Cross-target protocol allowlist '$Path' does not exist."
    }
    $document = [IO.File]::ReadAllText($Path) | ConvertFrom-Json
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entry in Get-OptionalArray $document 'typeConflicts') {
        $names = @($entry.PSObject.Properties.Name)
        foreach ($required in @('message', 'number', 'signatures', 'rationale')) {
            if ($names -notcontains $required) {
                throw "Cross-target protocol allowlist entry is missing '$required'."
            }
        }
        $message = [string]$entry.message
        $number = [int]$entry.number
        $rationale = [string]$entry.rationale
        if ([string]::IsNullOrWhiteSpace($message) -or [string]::IsNullOrWhiteSpace($rationale)) {
            throw 'Cross-target protocol allowlist entries require a message and a reviewed rationale.'
        }
        $signatures = @{}
        foreach ($target in $targetNames) {
            if ($entry.signatures.PSObject.Properties.Name -notcontains $target -or
                [string]::IsNullOrWhiteSpace([string]$entry.signatures.$target)) {
                throw "Cross-target protocol allowlist entry $message field $number has no $target signature."
            }
            $signatures[$target] = [string]$entry.signatures.$target
        }
        if (-not $seen.Add("$message#$number")) {
            throw "Cross-target protocol allowlist lists $message field $number more than once."
        }
        $entries.Add([pscustomobject]@{
                Message = $message
                Number = $number
                Signatures = $signatures
                Decision = if ($names -contains 'decision') { [string]$entry.decision } else { '' }
                Used = $false
            })
    }
    , $entries
}

function Format-Cell {
    param([string]$Value)

    $Value.Replace('|', '\|')
}

function Format-WorkflowValue {
    param([string]$Value)

    $Value.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
}

$allowlist = Get-Allowlist $AllowlistPath
$workDirectory = Join-Path ([IO.Path]::GetTempPath()) "briosa-cross-target-protocol-$([Guid]::NewGuid().ToString('N'))"
[IO.Directory]::CreateDirectory($workDirectory) | Out-Null
try {
    $firstSchema = Get-TargetSchema $first $workDirectory
    $secondSchema = Get-TargetSchema $second $workDirectory
}
finally {
    Remove-Item -LiteralPath $workDirectory -Recurse -Force -ErrorAction SilentlyContinue
}

function Compare-Names {
    param($FirstNames, $SecondNames)

    [pscustomobject]@{
        FirstOnly = @($FirstNames | Where-Object { -not $SecondNames.Contains($_) } | Sort-Object -CaseSensitive)
        SecondOnly = @($SecondNames | Where-Object { -not $FirstNames.Contains($_) } | Sort-Object -CaseSensitive)
        Shared = @($FirstNames | Where-Object { $SecondNames.Contains($_) } | Sort-Object -CaseSensitive)
    }
}

$messages = Compare-Names $firstSchema.Messages.Keys $secondSchema.Messages.Keys
$enums = Compare-Names $firstSchema.Enums.Keys $secondSchema.Enums.Keys
$rpcs = Compare-Names $firstSchema.Rpcs.Keys $secondSchema.Rpcs.Keys

$rpcSignatureDifferences = [Collections.Generic.List[object]]::new()
foreach ($rpc in $rpcs.Shared) {
    if ($firstSchema.Rpcs[$rpc] -cne $secondSchema.Rpcs[$rpc]) {
        $rpcSignatureDifferences.Add([pscustomobject]@{
                Rpc = $rpc; First = $firstSchema.Rpcs[$rpc]; Second = $secondSchema.Rpcs[$rpc]
            })
    }
}

$sharedFieldCount = 0
$sharedMessagesWithFields = 0
$oneSidedFields = [Collections.Generic.List[object]]::new()
$renames = [Collections.Generic.List[object]]::new()
$conflicts = [Collections.Generic.List[object]]::new()
foreach ($message in $messages.Shared) {
    $firstFields = $firstSchema.Messages[$message]
    $secondFields = $secondSchema.Messages[$message]
    if ($firstFields.Count -gt 0 -and $secondFields.Count -gt 0) {
        $sharedMessagesWithFields++
    }
    foreach ($number in $firstFields.Keys) {
        if (-not $secondFields.ContainsKey($number)) {
            $oneSidedFields.Add([pscustomobject]@{
                    Message = $message; Number = $number; Target = $first
                    Name = $firstFields[$number].Name; Signature = $firstFields[$number].Signature
                })
            continue
        }
        $sharedFieldCount++
        $firstField = $firstFields[$number]
        $secondField = $secondFields[$number]
        if ($firstField.Signature -ceq $secondField.Signature) {
            # A rename of a field whose type also differs is reported once, as a conflict.
            if ($firstField.Name -cne $secondField.Name) {
                $renames.Add([pscustomobject]@{
                        Message = $message; Number = $number; First = $firstField.Name; Second = $secondField.Name
                    })
            }
        }
        else {
            $entry = $allowlist | Where-Object {
                $_.Message -ceq $message -and $_.Number -eq $number -and
                $_.Signatures[$first] -ceq $firstField.Signature -and
                $_.Signatures[$second] -ceq $secondField.Signature
            } | Select-Object -First 1
            if ($null -ne $entry) {
                $entry.Used = $true
            }
            $conflicts.Add([pscustomobject]@{
                    Message = $message; Number = $number
                    FirstName = $firstField.Name; FirstSignature = $firstField.Signature
                    SecondName = $secondField.Name; SecondSignature = $secondField.Signature
                    Allowed = $null -ne $entry
                    Decision = if ($null -ne $entry) { $entry.Decision } else { '' }
                })
        }
    }
    foreach ($number in $secondFields.Keys) {
        if (-not $firstFields.ContainsKey($number)) {
            $oneSidedFields.Add([pscustomobject]@{
                    Message = $message; Number = $number; Target = $second
                    Name = $secondFields[$number].Name; Signature = $secondFields[$number].Signature
                })
        }
    }
}

$unallowed = @($conflicts | Where-Object { -not $_.Allowed })
$stale = @($allowlist | Where-Object { -not $_.Used })
$firstOnlyFields = @($oneSidedFields | Where-Object { $_.Target -ceq $first })
$secondOnlyFields = @($oneSidedFields | Where-Object { $_.Target -ceq $second })

$countRows = @(
    [pscustomobject]@{ Kind = 'Messages'; Shared = $messages.Shared.Count; First = $messages.FirstOnly.Count; Second = $messages.SecondOnly.Count }
    [pscustomobject]@{ Kind = 'Shared messages with fields in both'; Shared = $sharedMessagesWithFields; First = ''; Second = '' }
    [pscustomobject]@{ Kind = 'Enums'; Shared = $enums.Shared.Count; First = $enums.FirstOnly.Count; Second = $enums.SecondOnly.Count }
    [pscustomobject]@{ Kind = 'RPCs'; Shared = $rpcs.Shared.Count; First = $rpcs.FirstOnly.Count; Second = $rpcs.SecondOnly.Count }
    [pscustomobject]@{ Kind = 'Field numbers in shared messages'; Shared = $sharedFieldCount; First = $firstOnlyFields.Count; Second = $secondOnlyFields.Count }
)

Write-Host "Cross-target protocol comparison of targets/$first and targets/$second"
foreach ($row in $countRows) {
    if ($row.First -is [string]) {
        Write-Host "  $($row.Kind): $($row.Shared)"
    }
    else {
        Write-Host ("  {0}: {1} shared, {2} only in {3}, {4} only in {5}" -f $row.Kind, $row.Shared, $row.First, $first, $row.Second, $second)
    }
}
Write-Host "  Renamed fields: $($renames.Count); RPC signature differences: $($rpcSignatureDifferences.Count)"
Write-Host "  Same-number type or cardinality conflicts: $($conflicts.Count) ($($unallowed.Count) not allowlisted)"
foreach ($conflict in $conflicts) {
    $suffix = if ($conflict.Allowed) { ' [allowlisted]' } else { ' [NOT ALLOWLISTED]' }
    Write-Host ("    {0} field {1}: {2} '{3} {4}', {5} '{6} {7}'{8}" -f
        $conflict.Message, $conflict.Number,
        $first, $conflict.FirstSignature, $conflict.FirstName,
        $second, $conflict.SecondSignature, $conflict.SecondName, $suffix)
}
foreach ($rename in $renames) {
    Write-Host "    Renamed: $($rename.Message) field $($rename.Number): $first '$($rename.First)', $second '$($rename.Second)'"
}
foreach ($entry in $stale) {
    Write-Host "  Stale allowlist entry (no matching conflict): $($entry.Message) field $($entry.Number)"
}

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
    $summary = [Collections.Generic.List[string]]::new()
    function Add-Details {
        param([string]$Title, [string[]]$Header, [object[]]$Rows)

        $summary.Add("<details><summary>$Title ($($Rows.Count))</summary>")
        $summary.Add('')
        if ($Rows.Count -eq 0) {
            $summary.Add('None.')
        }
        else {
            $summary.Add('| ' + ($Header -join ' | ') + ' |')
            $summary.Add('|' + (' --- |' * $Header.Count))
            foreach ($row in $Rows) {
                $summary.Add('| ' + (@($row | ForEach-Object { Format-Cell ([string]$_) }) -join ' | ') + ' |')
            }
        }
        $summary.Add('')
        $summary.Add('</details>')
        $summary.Add('')
    }

    $summary.Add('## Cross-target protocol differences')
    $summary.Add('')
    $summary.Add("Compared the Buf-compiled schemas of ``targets/$first`` and ``targets/$second``.")
    $summary.Add('')
    $summary.Add("| Element | Shared | Only in $first | Only in $second |")
    $summary.Add('| --- | ---: | ---: | ---: |')
    foreach ($row in $countRows) {
        $summary.Add("| $($row.Kind) | $($row.Shared) | $($row.First) | $($row.Second) |")
    }
    $summary.Add("| Renamed fields (same number) | $($renames.Count) | | |")
    $summary.Add("| RPC signature differences | $($rpcSignatureDifferences.Count) | | |")
    $summary.Add("| Type or cardinality conflicts | $($conflicts.Count) | | |")
    $summary.Add('')
    $summary.Add('### Wire hazards: same-number type or cardinality conflicts')
    $summary.Add('')
    if ($conflicts.Count -eq 0) {
        $summary.Add('No type or cardinality conflicts.')
    }
    else {
        $summary.Add("| Message | Field | $first | $second | Allowlisted | Decision |")
        $summary.Add('| --- | ---: | --- | --- | --- | --- |')
        foreach ($conflict in $conflicts) {
            $allowedText = if ($conflict.Allowed) { 'yes' } else { '**no**' }
            $summary.Add(("| ``{0}`` | {1} | ``{2} {3}`` | ``{4} {5}`` | {6} | {7} |" -f
                    $conflict.Message, $conflict.Number,
                    (Format-Cell $conflict.FirstSignature), $conflict.FirstName,
                    (Format-Cell $conflict.SecondSignature), $conflict.SecondName,
                    $allowedText, (Format-Cell $conflict.Decision)))
        }
    }
    $summary.Add('')
    if ($stale.Count -gt 0) {
        $summary.Add('Stale allowlist entries (no matching conflict): ' + (@($stale | ForEach-Object { "``$($_.Message)`` field $($_.Number)" }) -join ', '))
        $summary.Add('')
    }
    $summary.Add('### Informational differences')
    $summary.Add('')
    Add-Details 'Renamed fields' @('Message', 'Field', $first, $second) @(
        $renames | ForEach-Object { , @($_.Message, $_.Number, $_.First, $_.Second) })
    Add-Details 'RPC signature differences' @('RPC', $first, $second) @(
        $rpcSignatureDifferences | ForEach-Object { , @($_.Rpc, $_.First, $_.Second) })
    Add-Details "Messages only in $first" @('Message') @($messages.FirstOnly | ForEach-Object { , @($_) })
    Add-Details "Messages only in $second" @('Message') @($messages.SecondOnly | ForEach-Object { , @($_) })
    Add-Details "Enums only in $first" @('Enum') @($enums.FirstOnly | ForEach-Object { , @($_) })
    Add-Details "Enums only in $second" @('Enum') @($enums.SecondOnly | ForEach-Object { , @($_) })
    Add-Details "RPCs only in $first" @('RPC') @($rpcs.FirstOnly | ForEach-Object { , @($_) })
    Add-Details "RPCs only in $second" @('RPC') @($rpcs.SecondOnly | ForEach-Object { , @($_) })
    Add-Details "Fields only in $first (shared messages)" @('Message', 'Field', 'Name', 'Type') @(
        $firstOnlyFields | ForEach-Object { , @($_.Message, $_.Number, $_.Name, $_.Signature) })
    Add-Details "Fields only in $second (shared messages)" @('Message', 'Field', 'Name', 'Type') @(
        $secondOnlyFields | ForEach-Object { , @($_.Message, $_.Number, $_.Name, $_.Signature) })
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary -Encoding utf8
}

if ($env:GITHUB_ACTIONS -ceq 'true') {
    foreach ($conflict in $unallowed) {
        $message = Format-WorkflowValue (
            "$($conflict.Message) field $($conflict.Number) is '$($conflict.FirstSignature)' in $first " +
            "but '$($conflict.SecondSignature)' in $second. Add a reviewed allowlist entry or align the targets.")
        Write-Host "::error title=Cross-target protocol wire hazard::$message"
    }
}

if ($unallowed.Count -gt 0) {
    Write-Host "$($unallowed.Count) same-number type or cardinality conflict(s) are not on the reviewed allowlist."
    exit 1
}

exit 0
