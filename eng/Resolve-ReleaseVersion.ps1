[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Ref,
    [Parameter(Mandatory)][string]$Commit,
    [Parameter(Mandatory)][string]$Version,
    [string]$MainRef = 'refs/remotes/origin/main',
    [switch]$SkipMainFetch,
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

# Resolves the release version, signing eligibility, and protocol baseline for
# release.yml, which runs only from workflow_dispatch.
# - GitHub runs a dispatched workflow from the dispatched ref's commit. Signing is
#   bounded by the release-signing environment, which admits only main and opened
#   release lines; this script keeps the reviewed workflow from signing a commit
#   those refs should not release.
# - main: the commit must be on main. The version must be greater than every stable
#   v* tag in the repository except its own.
# - release/<major>.<minor>: the version must be in that line and greater than every
#   stable tag of the line, and the commit must descend from a stable release of the
#   line that is on main. Later releases on main do not block the line's hotfixes.
# - Any other branch is an unsigned dry run, ordered like main. Tags and other refs
#   are rejected: publishing the draft release creates the tag.
# - An existing v<version> tag must point at the commit (a rerun, or a tag pushed
#   before the dispatch).
# - The protocol baseline is the highest stable v* tag reachable from the commit,
#   excluding the tag being released.
# Writes value, previous, and eligible to GITHUB_OUTPUT when it is set.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedRoot = [IO.Path]::GetFullPath($RepositoryRoot)

function Invoke-Git {
    param([Parameter(Mandatory)][string[]]$Arguments, [switch]$AllowFailure)

    $output = @(& git -C $resolvedRoot @Arguments)
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "git $($Arguments -join ' ') failed with exit code $exitCode."
    }
    [pscustomobject]@{ ExitCode = $exitCode; Output = $output }
}

function Test-Ancestor([string]$Ancestor, [string]$Descendant) {
    $ancestry = Invoke-Git @('merge-base', '--is-ancestor', $Ancestor, $Descendant) -AllowFailure
    switch ($ancestry.ExitCode) {
        0 { $true }
        1 { $false }
        default { throw "Could not determine whether $Ancestor is an ancestor of $Descendant." }
    }
}

function ConvertTo-StableVersion([string]$Tag) {
    if ($Tag -notmatch '^v[0-9]') { return $null }
    $candidate = $null
    if ([System.Management.Automation.SemanticVersion]::TryParse($Tag.Substring(1), [ref]$candidate) -and
        [string]::IsNullOrEmpty($candidate.PreReleaseLabel)) {
        return $candidate
    }
    return $null
}

function Get-HighestStableTag([string[]]$Tags, [string]$ExcludedTag) {
    $best = $null
    foreach ($tag in $Tags) {
        if ($ExcludedTag -and $tag -ceq $ExcludedTag) { continue }
        $candidate = ConvertTo-StableVersion $tag
        if ($null -ne $candidate -and ($null -eq $best -or $candidate -gt $best.Version)) {
            $best = [pscustomobject]@{ Tag = $tag; Version = $candidate }
        }
    }
    $best
}

if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a supported semantic version."
}
$semanticVersion = $null
if (-not [System.Management.Automation.SemanticVersion]::TryParse($Version, [ref]$semanticVersion)) {
    throw "Version '$Version' is not a supported semantic version."
}
$releaseTag = "v$Version"

if ($Ref.StartsWith('refs/tags/', [StringComparison]::Ordinal)) {
    throw "Dispatch releases from main or a release branch, not $Ref; publishing the draft release creates the tag."
}
if ($Ref -notmatch '^refs/heads/(?<branch>.+)$') {
    throw "Release ref '$Ref' is not a branch."
}
$branch = $Matches.branch
$line = if ($branch -match '^release/(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)$') {
    "$($Matches.major).$($Matches.minor)"
}

if (-not $SkipMainFetch) {
    Invoke-Git @('fetch', '--no-tags', 'origin', "+refs/heads/main:$MainRef") | Out-Null
}
$commitId = (Invoke-Git @('rev-parse', '--verify', "$Commit^{commit}")).Output[0]

$existingTag = Invoke-Git @('rev-parse', '--verify', '--quiet', "refs/tags/$releaseTag^{commit}") -AllowFailure
if ($existingTag.ExitCode -eq 0 -and $existingTag.Output[0] -cne $commitId) {
    throw "Tag $releaseTag already points to $($existingTag.Output[0]), not $commitId."
}

$allTags = @((Invoke-Git @('tag', '--list', 'v[0-9]*')).Output)
if ($branch -ceq 'main') {
    if (-not (Test-Ancestor $commitId $MainRef)) {
        throw "Commit $commitId is not on main."
    }
    $eligible = $true
    $orderedTags = $allTags
    $orderName = 'stable release'
}
elseif ($line) {
    if ("$($semanticVersion.Major).$($semanticVersion.Minor)" -cne $line) {
        throw "Version $Version is not in the $line release line."
    }
    $lineTags = @($allTags | Where-Object {
            $candidate = ConvertTo-StableVersion $_
            $null -ne $candidate -and "$($candidate.Major).$($candidate.Minor)" -ceq $line
        })
    $bases = @((Invoke-Git @('tag', '--merged', $commitId, '--list', "v$line.*")).Output | Where-Object {
            $null -ne (ConvertTo-StableVersion $_) -and (Test-Ancestor "refs/tags/$_" $MainRef)
        })
    if ($bases.Count -eq 0) {
        throw "Branch release/$line must descend from a stable v$line.* release on main."
    }
    $eligible = $true
    $orderedTags = $lineTags
    $orderName = "stable $line release"
}
else {
    Write-Host "::warning::$Ref is neither main nor a release line; this dispatch is an unsigned dry run."
    $eligible = $false
    $orderedTags = $allTags
    $orderName = 'stable release'
}

$highest = Get-HighestStableTag $orderedTags $releaseTag
if ($null -ne $highest -and $semanticVersion -le $highest.Version) {
    throw "Version $Version is not greater than the highest $orderName $($highest.Tag)."
}

$baseline = Get-HighestStableTag (Invoke-Git @('tag', '--merged', $commitId, '--list', 'v[0-9]*')).Output $releaseTag
$previous = if ($null -ne $baseline) { $baseline.Tag } else { '' }

Write-Host "Version $Version; protocol baseline: $(if ($previous) { $previous } else { 'none' }); eligible to sign: $eligible."

if ($env:GITHUB_OUTPUT) {
    "value=$Version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "previous=$previous" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "eligible=$($eligible.ToString().ToLowerInvariant())" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
}

# git merge-base and rev-parse --quiet report "no" with exit code 1; do not leak it.
$global:LASTEXITCODE = 0
[pscustomobject]@{ Version = $Version; Previous = $previous; Eligible = $eligible }
