Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# A strict reader for the YAML subset this repository's workflows use (#450): block
# mappings and sequences, plain and quoted scalars, block scalars (| and >), and flow
# sequences of scalars. Anything else, such as a flow mapping, an anchor, an alias, a
# tag, a second document or a multi-line plain scalar, is rejected rather than guessed
# at, so a policy check on the result can never be bypassed by a spelling it does not
# understand. Every scalar is returned as a string, mappings as ordered dictionaries
# and sequences as lists.

function Get-Indent([string]$Line) {
    $Line.Length - $Line.TrimStart(' ').Length
}

function Remove-Comment([string]$Text) {
    # A # starts a comment at the start of the text or after whitespace, outside quotes.
    $quote = [char]0
    for ($index = 0; $index -lt $Text.Length; $index++) {
        $character = $Text[$index]
        if ($quote -ne [char]0) {
            if ($character -eq $quote) {
                if ($quote -eq "'" -and $index + 1 -lt $Text.Length -and $Text[$index + 1] -eq "'") { $index++; continue }
                $quote = [char]0
            }
            elseif ($quote -eq '"' -and $character -eq '\') { $index++ }
            continue
        }
        if (($character -eq "'" -or $character -eq '"') -and ($index -eq 0 -or $Text[$index - 1] -in @(' ', '[', ','))) {
            $quote = $character
            continue
        }
        if ($character -eq '#' -and ($index -eq 0 -or [char]::IsWhiteSpace($Text[$index - 1]))) {
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

function ConvertFrom-Scalar([object]$Reader, [string]$Text) {
    if ($Text.Length -eq 0) { return $null }
    $first = $Text[0]
    if ($first -eq "'") {
        if ($Text.Length -lt 2 -or -not $Text.EndsWith("'")) { Stop-Unsupported $Reader 'an unterminated single-quoted scalar.' }
        return $Text.Substring(1, $Text.Length - 2).Replace("''", "'")
    }
    if ($first -eq '"') {
        if ($Text.Length -lt 2 -or -not $Text.EndsWith('"')) { Stop-Unsupported $Reader 'an unterminated double-quoted scalar.' }
        $inner = $Text.Substring(1, $Text.Length - 2)
        if ($inner -match '\\[^"\\/nt]') { Stop-Unsupported $Reader 'an unsupported escape in a double-quoted scalar.' }
        return $inner.Replace('\"', '"').Replace('\n', "`n").Replace('\t', "`t").Replace('\/', '/').Replace('\\', '\')
    }
    if ($first -eq '[') {
        if (-not $Text.EndsWith(']')) { Stop-Unsupported $Reader 'a flow sequence that does not end on its line.' }
        $items = [Collections.Generic.List[object]]::new()
        $inner = $Text.Substring(1, $Text.Length - 2).Trim()
        if ($inner.Length -eq 0) { return , $items }
        foreach ($part in $inner.Split(',')) {
            $item = $part.Trim()
            if ($item.Length -eq 0 -or $item[0] -in @('[', '{', '&', '*', '!', '?', '|', '>', '%', '@', '`')) {
                Stop-Unsupported $Reader 'a flow sequence item that is not a plain or quoted scalar.'
            }
            $items.Add((ConvertFrom-Scalar $Reader $item))
        }
        return , $items
    }
    if ($first -in @('{', '&', '*', '!', '?', '%', '@', '`')) {
        Stop-Unsupported $Reader "a value that starts with '$first' (flow mappings, anchors, aliases, tags and directives are not supported)."
    }
    if ($Text -match '(^|\s)[&*!][A-Za-z0-9_-]' -and -not $Text.Contains('${{')) {
        Stop-Unsupported $Reader 'an anchor, alias or tag inside a plain scalar.'
    }
    $Text
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
        $match = [regex]::Match($text, '\A(?<key>[A-Za-z0-9_][A-Za-z0-9_./-]*|''[^'']*''|"[^"]*")[ ]*:(?:[ ]+(?<rest>.*))?\z')
        if (-not $match.Success) { Stop-Unsupported $Reader 'a line that is not a supported mapping entry.' }
        $key = $match.Groups['key'].Value
        if ($key.StartsWith("'") -or $key.StartsWith('"')) { $key = $key.Substring(1, $key.Length - 2) }
        if ($mapping.Contains($key)) { Stop-Unsupported $Reader "a repeated key '$key'." }
        $mapping[$key] = Read-Value $Reader $Indent $match.Groups['rest'].Value.Trim()
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
        if ($item -match '\A([A-Za-z0-9_][A-Za-z0-9_./-]*|''[^'']*''|"[^"]*")[ ]*:(?:[ ]+|\z)') {
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
    $lines = [Collections.Generic.List[string]]::new()
    foreach ($line in [IO.File]::ReadAllLines($Path)) {
        if ($line.Contains("`t") -and $line.TrimStart(' ').StartsWith("`t")) { throw "$([IO.Path]::GetFileName($Path)): a tab in indentation." }
        $lines.Add($line)
    }
    foreach ($line in $lines) {
        if ($line -match '\A(---|\.\.\.)(\s|\z)' -or $line -match '\A%') {
            throw "$([IO.Path]::GetFileName($Path)): document markers and directives are not supported."
        }
    }
    $reader = [pscustomobject]@{ Lines = $lines; Index = 0; Name = [IO.Path]::GetFileName($Path) }
    Skip-Blank $reader
    if ($reader.Index -ge $lines.Count) { throw "$($reader.Name): the workflow is empty." }
    $indent = Get-Indent $lines[$reader.Index]
    if ($indent -ne 0) { Stop-Unsupported $reader 'a workflow whose first line is indented.' }
    $root = Read-Mapping $reader 0
    Skip-Blank $reader
    if ($reader.Index -lt $lines.Count) { Stop-Unsupported $reader 'content after the workflow mapping.' }
    , $root
}

Export-ModuleMember -Function ConvertFrom-WorkflowYaml
