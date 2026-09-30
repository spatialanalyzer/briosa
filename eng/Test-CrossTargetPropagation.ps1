[CmdletBinding()]
param(
    [string]$BaseRef,
    [string]$HeadRef = 'HEAD',
    [switch]$Strict,
    [string[]]$AllowPath = @(),
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

# Reports twin files that were identical in both exact-SA targets at the base
# revision but changed on only one side, or changed on both sides and no longer
# match. Twins whose committed blobs differ only by their target version strings
# or 'sa<year>' target labels count as identical. Only committed Git blobs are compared, never working-tree
# bytes. Findings are advisory unless -Strict is set.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$targetNames = @('2026.1.0529.7', '2024.1.0508.5')
$resolvedRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$safeRoot = $resolvedRoot.Replace('\', '/')

function Invoke-Git {
    param([Parameter(Mandatory)][string[]]$Arguments, [switch]$AllowFailure)

    $startInfo = [Diagnostics.ProcessStartInfo]::new('git')
    foreach ($argument in @('-c', "safe.directory=$safeRoot", '-C', $resolvedRoot) + $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($startInfo)
    try {
        $errorTask = $process.StandardError.ReadToEndAsync()
        $output = [IO.MemoryStream]::new()
        $process.StandardOutput.BaseStream.CopyTo($output)
        $process.WaitForExit()
        $null = $errorTask.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0 -and -not $AllowFailure) {
            throw "git $($Arguments[0]) failed with exit code $($process.ExitCode)."
        }
        [pscustomobject]@{
            ExitCode = $process.ExitCode
            Text = [Text.Encoding]::UTF8.GetString($output.ToArray())
        }
    }
    finally {
        $process.Dispose()
    }
}

function Resolve-Commit {
    param([Parameter(Mandatory)][string]$Reference)

    $result = Invoke-Git -AllowFailure -Arguments @(
        'rev-parse', '--verify', '--quiet', '--end-of-options', "$Reference^{commit}")
    if ($result.ExitCode -ne 0) {
        throw "Git revision '$Reference' does not resolve to a commit."
    }
    $result.Text.Trim()
}

function Get-TargetBlobs {
    param([Parameter(Mandatory)][string]$Commit, [Parameter(Mandatory)][string]$Target)

    $prefix = "targets/$Target/"
    $blobs = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
    $listing = (Invoke-Git -Arguments @(
            'ls-tree', '-r', '-z', '--full-tree', $Commit, '--', "targets/$Target")).Text
    foreach ($entry in $listing.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)) {
        $tab = $entry.IndexOf("`t", [StringComparison]::Ordinal)
        $fields = $entry.Substring(0, $tab).Split(' ')
        $path = $entry.Substring($tab + 1)
        if ($fields[1] -ceq 'blob' -and $path.StartsWith($prefix, [StringComparison]::Ordinal)) {
            $blobs[$path.Substring($prefix.Length)] = $fields[2]
        }
    }
    , $blobs
}

$versionForms = @(
    foreach ($target in $targetNames) {
        $parts = $target.Split('.')
        $assemblyParts = @($parts | ForEach-Object { ([int]$_).ToString([Globalization.CultureInfo]::InvariantCulture) })
        foreach ($separator in @('.', '-', '_', ', ')) {
            $padded = $parts -join $separator
            $assembly = $assemblyParts -join $separator
            [pscustomobject]@{
                Pattern = [regex]::new('(?<![0-9])' + [regex]::Escape($padded) + '(?![0-9])')
                Token = "<sa-target|$separator|release>"
            }
            if ($assembly -cne $padded) {
                [pscustomobject]@{
                    Pattern = [regex]::new('(?<![0-9])' + [regex]::Escape($assembly) + '(?![0-9])')
                    Token = "<sa-target|$separator|assembly>"
                }
            }
        }
    }
    # Short target labels such as '--confirm-licensed-sa2026'. They are applied
    # after the full version forms, so 'sa-2026.1.0529.7' stays a release form.
    foreach ($target in $targetNames) {
        [pscustomobject]@{
            Pattern = [regex]::new(
                '(?<=\bsa[-_]?)' + [regex]::Escape($target.Split('.')[0]) + '(?![0-9])',
                [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::CultureInvariant)
            Token = '<sa-target|year>'
        }
    }
)

$blobCache = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
$catFile = $null

function Get-NormalizedBlob {
    param([Parameter(Mandatory)][string]$Blob)

    if ($blobCache.ContainsKey($Blob)) {
        return $blobCache[$Blob]
    }

    if ($null -eq $script:catFile) {
        $startInfo = [Diagnostics.ProcessStartInfo]::new('git')
        foreach ($argument in @('-c', "safe.directory=$safeRoot", '-C', $resolvedRoot, 'cat-file', '--batch')) {
            $startInfo.ArgumentList.Add($argument)
        }
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardInput = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
        $script:catFile = [Diagnostics.Process]::Start($startInfo)
    }

    $script:catFile.StandardInput.Write("$Blob`n")
    $script:catFile.StandardInput.Flush()
    $stream = $script:catFile.StandardOutput.BaseStream
    $header = [Text.StringBuilder]::new()
    while (($next = $stream.ReadByte()) -ne 10) {
        if ($next -lt 0) {
            throw 'git cat-file ended unexpectedly.'
        }
        $null = $header.Append([char]$next)
    }
    $fields = $header.ToString().Split(' ')
    if ($fields.Count -ne 3 -or $fields[1] -cne 'blob') {
        throw "Git object '$Blob' is not a readable blob."
    }

    $bytes = [byte[]]::new([long]$fields[2])
    $offset = 0
    while ($offset -lt $bytes.Length) {
        $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
        if ($read -le 0) {
            throw 'git cat-file ended unexpectedly.'
        }
        $offset += $read
    }
    $null = $stream.ReadByte()

    # Binary blobs are compared only by object identity.
    $normalized = $null
    if ([Array]::IndexOf($bytes, [byte]0) -lt 0) {
        # Latin-1 maps each byte to one character, so ASCII version text is
        # replaced without decoding assumptions.
        $normalized = [Text.Encoding]::Latin1.GetString($bytes)
        foreach ($form in $versionForms) {
            $normalized = $form.Pattern.Replace($normalized, $form.Token)
        }
    }
    $blobCache[$Blob] = $normalized
    $normalized
}

function Test-Twin {
    param($First, $Second)

    if ($null -eq $First -or $null -eq $Second) {
        return $null -eq $First -and $null -eq $Second
    }
    if ($First -ceq $Second) {
        return $true
    }
    $normalizedFirst = Get-NormalizedBlob $First
    $normalizedSecond = Get-NormalizedBlob $Second
    $null -ne $normalizedFirst -and $null -ne $normalizedSecond -and $normalizedFirst -ceq $normalizedSecond
}

function Get-Change {
    param([string]$Base, $Head)

    if ($null -eq $Head) { 'deleted' } elseif ($Head -ceq $Base) { 'unchanged' } else { 'modified' }
}

function Format-WorkflowValue {
    param([string]$Value, [switch]$Property)

    $escaped = $Value.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
    if ($Property) {
        $escaped = $escaped.Replace(':', '%3A').Replace(',', '%2C')
    }
    $escaped
}

$headCommit = Resolve-Commit $HeadRef
if ([string]::IsNullOrWhiteSpace($BaseRef)) {
    $mergeBase = Invoke-Git -AllowFailure -Arguments @('merge-base', 'origin/main', $headCommit)
    if ($mergeBase.ExitCode -ne 0) {
        throw 'Could not compute the merge base with origin/main. Fetch origin/main or pass -BaseRef.'
    }
    $baseCommit = $mergeBase.Text.Trim()
}
else {
    $baseCommit = Resolve-Commit $BaseRef
}

$allowPatterns = @(
    foreach ($pattern in $AllowPath) {
        if (-not [string]::IsNullOrWhiteSpace($pattern)) {
            [Management.Automation.WildcardPattern]::new(
                $pattern.Trim().Replace('\', '/').TrimStart('/'),
                [Management.Automation.WildcardOptions]::None)
        }
    }
)

$first, $second = $targetNames
$findings = [Collections.Generic.List[object]]::new()
$twinCount = 0
$changedTwinCount = 0
try {
    $baseFirst = Get-TargetBlobs $baseCommit $first
    $baseSecond = Get-TargetBlobs $baseCommit $second
    $headFirst = Get-TargetBlobs $headCommit $first
    $headSecond = Get-TargetBlobs $headCommit $second

    foreach ($path in ($baseFirst.Keys | Sort-Object -CaseSensitive)) {
        if (-not $baseSecond.ContainsKey($path)) {
            continue
        }
        $twinCount++

        $headFirstBlob = if ($headFirst.ContainsKey($path)) { $headFirst[$path] } else { $null }
        $headSecondBlob = if ($headSecond.ContainsKey($path)) { $headSecond[$path] } else { $null }
        $firstChange = Get-Change $baseFirst[$path] $headFirstBlob
        $secondChange = Get-Change $baseSecond[$path] $headSecondBlob
        if ($firstChange -ceq 'unchanged' -and $secondChange -ceq 'unchanged') {
            continue
        }
        $changedTwinCount++

        if (-not (Test-Twin $baseFirst[$path] $baseSecond[$path])) {
            continue
        }

        if ($firstChange -ceq 'unchanged' -or $secondChange -ceq 'unchanged') {
            $kind = 'One-sided change'
        }
        elseif (Test-Twin $headFirstBlob $headSecondBlob) {
            continue
        }
        else {
            $kind = 'Diverged change'
        }

        $allowed = $false
        foreach ($pattern in $allowPatterns) {
            if ($pattern.IsMatch($path)) {
                $allowed = $true
                break
            }
        }
        $findings.Add([pscustomobject]@{
                Path = $path
                Kind = $kind
                FirstChange = $firstChange
                SecondChange = $secondChange
                Allowed = $allowed
            })
    }
}
finally {
    if ($null -ne $catFile) {
        $catFile.StandardInput.Close()
        $catFile.WaitForExit()
        $catFile.Dispose()
    }
}

$unallowed = @($findings | Where-Object { -not $_.Allowed })
$range = "$($baseCommit.Substring(0, 12))..$($headCommit.Substring(0, 12))"

Write-Host "Cross-target propagation $range across targets/$first and targets/$second"
Write-Host "Twin paths at base: $twinCount; changed twins: $changedTwinCount; findings: $($findings.Count) ($($unallowed.Count) not allowed)."
foreach ($finding in $findings) {
    $suffix = if ($finding.Allowed) { ' [allowed]' } else { '' }
    Write-Host "  $($finding.Kind): $($finding.Path) ($first $($finding.FirstChange), $second $($finding.SecondChange))$suffix"
}

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
    $summary = [Collections.Generic.List[string]]::new()
    $summary.Add('## Cross-target propagation')
    $summary.Add('')
    $summary.Add("Compared ``$range``: $twinCount twin paths, $changedTwinCount changed.")
    $summary.Add('')
    if ($findings.Count -eq 0) {
        $summary.Add('No propagation findings.')
    }
    else {
        $summary.Add("| Path | Finding | $first | $second | Allowed |")
        $summary.Add('| --- | --- | --- | --- | --- |')
        foreach ($finding in $findings) {
            $allowedText = if ($finding.Allowed) { 'yes' } else { 'no' }
            $summary.Add("| ``$($finding.Path.Replace('|', '\|'))`` | $($finding.Kind) | $($finding.FirstChange) | $($finding.SecondChange) | $allowedText |")
        }
    }
    $summary.Add('')
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary -Encoding utf8
}

if ($env:GITHUB_ACTIONS -ceq 'true') {
    foreach ($finding in $unallowed) {
        $changedTarget = if ($finding.FirstChange -cne 'unchanged') { $first } else { $second }
        $file = Format-WorkflowValue "targets/$changedTarget/$($finding.Path)" -Property
        $message = Format-WorkflowValue (
            "$($finding.Kind) to a twin that matched at base: " +
            "$first $($finding.FirstChange), $second $($finding.SecondChange).")
        Write-Host "::warning file=$file,title=Cross-target propagation::$message"
    }
}

if ($Strict -and $unallowed.Count -gt 0) {
    Write-Host "Strict mode: $($unallowed.Count) unallowed cross-target propagation finding(s)."
    exit 1
}

exit 0
