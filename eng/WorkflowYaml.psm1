Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# A strict reader for the plain block-style YAML subset the conformance soak and the
# retained-client gate are written in (#450). It accepts block mappings whose keys are
# plain names, block sequences, plain scalars, single-quoted scalars without escapes,
# double-quoted scalars without escapes, and literal or folded block scalars. Before any
# policy rule sees the result, it rejects every other construct with the file and line,
# and never decodes or normalises one: quoted keys, explicit keys (and so block scalars
# in key position), flow mappings and flow sequences, anchors, aliases, tags, merge keys,
# document markers and directives, tabs, repeated keys, and keys YAML would read as a
# boolean, a null or a number, other than those listed below. Every scalar is returned
# as a string, mappings as ordered dictionaries and sequences as lists.

# Keys that YAML 1.1 reads as booleans but GitHub Actions reads as the string itself.
$script:StringKeys = @('on')
$script:NonStringKeys = @('y', 'yes', 'n', 'no', 'true', 'false', 'on', 'off', 'null')

function Get-Indent([string]$Line) {
    $Line.Length - $Line.TrimStart(' ').Length
}

function Remove-Comment([string]$Text) {
    # A # starts a comment at the start of the text or after a space, outside a quoted
    # scalar. A quote starts a quoted scalar only where a value or a key starts: at the
    # start of the text, or after "key:" or "-" and a space. Elsewhere it is part of a
    # plain scalar.
    $quote = [char]0
    for ($index = 0; $index -lt $Text.Length; $index++) {
        $character = $Text[$index]
        if ($quote -ne [char]0) {
            if ($character -eq $quote) { $quote = [char]0 }
            continue
        }
        if (($character -eq "'" -or $character -eq '"') -and ($index -eq 0 -or ($Text[$index - 1] -eq ' ' -and
                    $Text.Substring(0, $index).TrimEnd(' ') -match '(?:\A|:|\A-)\z'))) {
            $quote = $character
            continue
        }
        if ($character -eq '#' -and ($index -eq 0 -or $Text[$index - 1] -eq ' ')) {
            return $Text.Substring(0, $index).TrimEnd()
        }
    }
    $Text.TrimEnd()
}

function Stop-Unsupported([object]$Reader, [string]$Message) {
    throw "$($Reader.Name) line $($Reader.Index + 1): $Message"
}

function Test-Content([string]$Line) {
    $trimmed = $Line.Trim()
    $trimmed.Length -gt 0 -and -not $trimmed.StartsWith('#')
}

function Skip-Blank([object]$Reader) {
    while ($Reader.Index -lt $Reader.Lines.Count -and -not (Test-Content $Reader.Lines[$Reader.Index])) { $Reader.Index++ }
}

function Assert-NodeStart([object]$Reader, [string]$Text, [string]$Position) {
    # The constructs that start a node or a key and lie outside the subset.
    if ($Text.Length -eq 0) { return }
    $first = $Text[0]
    if ($first -eq '[' -or $first -eq '{') { Stop-Unsupported $Reader "a flow sequence or flow mapping in $Position position." }
    if ($first -eq '&') { Stop-Unsupported $Reader "an anchor in $Position position." }
    if ($first -eq '*') { Stop-Unsupported $Reader "an alias in $Position position." }
    if ($first -eq '!') { Stop-Unsupported $Reader "a tag in $Position position." }
    if ($Text -match '\A\?(?: |\z)') { Stop-Unsupported $Reader "an explicit key (?), such as a block scalar in key position." }
    if ($first -eq '|' -or $first -eq '>') { Stop-Unsupported $Reader "a block scalar in $Position position that the reader does not support." }
    if ($first -in @('%', '@', '`', ',', ']', '}')) { Stop-Unsupported $Reader "a reserved indicator ('$first') in $Position position." }
}

function ConvertFrom-Scalar([object]$Reader, [string]$Text) {
    if ($Text.Length -eq 0) { return $null }
    Assert-NodeStart $Reader $Text 'value'
    $first = $Text[0]
    if ($first -eq "'") {
        if ($Text -notmatch "\A'[^']*'\z") { Stop-Unsupported $Reader 'a single-quoted scalar with an escape or that does not end on its line.' }
        return $Text.Substring(1, $Text.Length - 2)
    }
    if ($first -eq '"') {
        if ($Text -notmatch '\A"[^"\\]*"\z') { Stop-Unsupported $Reader 'a double-quoted scalar with an escape or that does not end on its line.' }
        return $Text.Substring(1, $Text.Length - 2)
    }
    if ($Text -match '\A[-:](?: |\z)') { Stop-Unsupported $Reader "a block indicator ('$first') where a value belongs." }
    if ($Text -match ': |:\z') { Stop-Unsupported $Reader "a plain scalar that contains ': ', which YAML reads as a mapping." }
    $Text
}

function Get-MappingEntry([object]$Reader, [string]$Text) {
    # The key of a mapping entry and the rest of its line, or a rejection naming the
    # construct. A key is a plain name; YAML reads it as a string.
    if ($Text.StartsWith("'") -or $Text.StartsWith('"')) { Stop-Unsupported $Reader 'a quoted key.' }
    if ($Text.StartsWith('<<')) { Stop-Unsupported $Reader 'a merge key (<<).' }
    Assert-NodeStart $Reader $Text 'key'
    $match = [regex]::Match($Text, '\A(?<key>[^\s:]+)[ ]*:(?:[ ]+(?<rest>.*))?\z')
    if (-not $match.Success) { Stop-Unsupported $Reader 'a line that is not a supported mapping entry.' }
    $key = $match.Groups['key'].Value
    if ($key -match '\A[-+.0-9~]') { Stop-Unsupported $Reader "a key YAML may read as a number or a null ('$key')." }
    if ($key -cnotmatch '\A[A-Za-z_][A-Za-z0-9_-]*\z') { Stop-Unsupported $Reader "a key with characters other than letters, digits, '_' and '-' ('$key')." }
    if ($key -in $script:NonStringKeys -and $key -cnotin $script:StringKeys) {
        Stop-Unsupported $Reader "a key YAML may read as a boolean or a null ('$key')."
    }
    [pscustomobject]@{ Key = $key; Rest = $match.Groups['rest'].Value.Trim() }
}

function Read-BlockScalar([object]$Reader, [int]$ParentIndent, [string]$Indicator) {
    $content = [Collections.Generic.List[string]]::new()
    $blockIndent = -1
    while ($Reader.Index -lt $Reader.Lines.Count) {
        $line = $Reader.Lines[$Reader.Index]
        if ($line.Trim().Length -eq 0) { $content.Add(''); $Reader.Index++; continue }
        $indent = Get-Indent $line
        if ($indent -le $ParentIndent) { break }
        if ($blockIndent -lt 0) { $blockIndent = $indent }
        if ($indent -lt $blockIndent) { Stop-Unsupported $Reader 'a block scalar line indented less than its first line.' }
        $content.Add($line.Substring($blockIndent))
        $Reader.Index++
    }
    $separator = if ($Indicator.StartsWith('>')) { ' ' } else { "`n" }
    ($content -join $separator).TrimEnd()
}

function Read-Value([object]$Reader, [int]$Indent, [string]$Rest) {
    # The value after "key:" or "-": a block scalar, an inline scalar, or a nested block.
    if ($Rest -match '\A[|>][+-]?\z') {
        $Reader.Index++
        return Read-BlockScalar $Reader $Indent $Rest
    }
    if ($Rest.Length -gt 0) {
        $value = ConvertFrom-Scalar $Reader $Rest
        $Reader.Index++
        Skip-Blank $Reader
        if ($Reader.Index -lt $Reader.Lines.Count -and (Get-Indent $Reader.Lines[$Reader.Index]) -gt $Indent) {
            Stop-Unsupported $Reader 'a multi-line plain scalar or a value nested under a scalar.'
        }
        return , $value
    }
    $Reader.Index++
    Skip-Blank $Reader
    if ($Reader.Index -ge $Reader.Lines.Count) { return $null }
    $next = $Reader.Lines[$Reader.Index]
    $nextIndent = Get-Indent $next
    if ($nextIndent -gt $Indent) { return , (Read-Block $Reader $nextIndent) }
    if ($nextIndent -eq $Indent -and ($next.TrimStart().StartsWith('- ') -or $next.Trim() -eq '-')) {
        return , (Read-Sequence $Reader $Indent)
    }
    $null
}

function Read-Block([object]$Reader, [int]$Indent) {
    $line = $Reader.Lines[$Reader.Index].TrimStart()
    if ($line.StartsWith('- ') -or $line -eq '-') { return , (Read-Sequence $Reader $Indent) }
    , (Read-Mapping $Reader $Indent)
}

function Read-Mapping([object]$Reader, [int]$Indent) {
    $mapping = [ordered]@{}
    while ($true) {
        Skip-Blank $Reader
        if ($Reader.Index -ge $Reader.Lines.Count) { break }
        $raw = $Reader.Lines[$Reader.Index]
        $lineIndent = Get-Indent $raw
        if ($lineIndent -lt $Indent) { break }
        if ($lineIndent -gt $Indent) { Stop-Unsupported $Reader 'a line indented deeper than its mapping.' }
        $text = Remove-Comment $raw.Substring($lineIndent)
        if ($text.StartsWith('- ') -or $text -eq '-') { break }
        $entry = Get-MappingEntry $Reader $text
        # Keys are compared without regard to case, so no two can differ only in case.
        if ($mapping.Contains($entry.Key)) { Stop-Unsupported $Reader "a repeated key ('$($entry.Key)')." }
        $mapping[$entry.Key] = Read-Value $Reader $Indent $entry.Rest
    }
    , $mapping
}

function Read-Sequence([object]$Reader, [int]$Indent) {
    $sequence = [Collections.Generic.List[object]]::new()
    while ($true) {
        Skip-Blank $Reader
        if ($Reader.Index -ge $Reader.Lines.Count) { break }
        $raw = $Reader.Lines[$Reader.Index]
        $lineIndent = Get-Indent $raw
        if ($lineIndent -lt $Indent) { break }
        $text = Remove-Comment $raw.Substring($lineIndent)
        if ($lineIndent -gt $Indent -or -not ($text.StartsWith('- ') -or $text -eq '-')) {
            if ($lineIndent -eq $Indent) { break }
            Stop-Unsupported $Reader 'a line indented deeper than its sequence.'
        }
        $item = if ($text -eq '-') { '' } else { $text.Substring(2).TrimStart() }
        $itemIndent = $lineIndent + ($text.Length - $text.Substring(1).TrimStart().Length)
        if ($item.StartsWith('- ') -or $item -eq '-') { Stop-Unsupported $Reader 'a sequence nested on one line (- -).' }
        if ($item -match '\A(?:''[^'']*''|"[^"]*")[ ]*:(?: |\z)') { Stop-Unsupported $Reader 'a quoted key.' }
        if ($item -match '\A[^\s:''"][^\s:]*[ ]*:(?: |\z)') {
            # A mapping item: read it as a mapping whose first key is on this line.
            $Reader.Lines[$Reader.Index] = (' ' * $itemIndent) + $raw.Substring($lineIndent).Substring(1).TrimStart()
            $sequence.Add((Read-Mapping $Reader $itemIndent))
            continue
        }
        $sequence.Add((Read-Value $Reader $Indent $item))
    }
    , $sequence
}

function ConvertFrom-WorkflowYaml {
    param([Parameter(Mandatory)][string]$Path)
    $name = [IO.Path]::GetFileName($Path)
    $lines = [Collections.Generic.List[string]]::new()
    foreach ($line in [IO.File]::ReadAllLines($Path)) { $lines.Add($line) }
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        if ($line.Contains("`t")) { throw "$name line $($index + 1): a tab." }
        if ($line -match '\A(---|\.\.\.)(\s|\z)') { throw "$name line $($index + 1): a document marker." }
        if ($line -match '\A%') { throw "$name line $($index + 1): a directive." }
    }
    $reader = [pscustomobject]@{ Lines = $lines; Index = 0; Name = $name }
    Skip-Blank $reader
    if ($reader.Index -ge $lines.Count) { throw "${name}: the workflow is empty." }
    if ((Get-Indent $lines[$reader.Index]) -ne 0) { Stop-Unsupported $reader 'a workflow whose first line is indented.' }
    $root = Read-Mapping $reader 0
    Skip-Blank $reader
    if ($reader.Index -lt $lines.Count) { Stop-Unsupported $reader 'content after the workflow mapping.' }
    , $root
}

Export-ModuleMember -Function ConvertFrom-WorkflowYaml
