[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('push', 'workflow_dispatch')][string]$EventName,
    [Parameter(Mandatory)][string]$Commit,
    [string]$TagName,
    [string]$DispatchVersion,
    [string]$MainRef = 'refs/remotes/origin/main',
    [switch]$SkipMainFetch,
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

# Resolves the release version, signing eligibility, and protocol baseline for
# release.yml.
# - A tag push releases the pushed tag; a manual dispatch releases DispatchVersion.
# - Only commits on main are eligible to sign. A tag on any other commit fails; a
#   dispatch from another commit is an unsigned dry run.
# - The version must be greater than every stable v* tag in the repository except
#   the tag being released, wherever that tag points.
# - The protocol baseline is the highest stable v* tag reachable from the commit,
#   again excluding only the tag being released.
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

if ($EventName -eq 'push') {
    if ($TagName -notmatch '^v') {
        throw "Release push ref '$TagName' is not a v-prefixed tag."
    }
    $version = $TagName.Substring(1)
    $releaseTag = $TagName
}
else {
    $version = $DispatchVersion
    $releaseTag = $null
}

if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') {
    throw "Version '$version' is not a supported semantic version."
}
$semanticVersion = $null
if (-not [System.Management.Automation.SemanticVersion]::TryParse($version, [ref]$semanticVersion)) {
    throw "Version '$version' is not a supported semantic version."
}

# Only commits already on main may be signed or published.
if (-not $SkipMainFetch) {
    Invoke-Git @('fetch', '--no-tags', 'origin', "+refs/heads/main:$MainRef") | Out-Null
}
$ancestry = Invoke-Git @('merge-base', '--is-ancestor', $Commit, $MainRef) -AllowFailure
$eligible = switch ($ancestry.ExitCode) {
    0 { $true }
    1 { $false }
    default { throw "Could not determine whether $Commit is on main." }
}
if (-not $eligible) {
    if ($EventName -eq 'push') {
        throw "Release tag $TagName does not point to a commit on main."
    }
    Write-Host "::warning::$Commit is not on main; this dispatch is an unsigned dry run."
}

# Version order is global: a lower version cannot be released on the same commit as,
# or on an older commit than, an existing stable release.
$highest = Get-HighestStableTag (Invoke-Git @('tag', '--list', 'v[0-9]*')).Output $releaseTag
if ($null -ne $highest -and $semanticVersion -le $highest.Version) {
    throw "Version $version is not greater than the highest stable release $($highest.Tag)."
}

$baseline = Get-HighestStableTag (Invoke-Git @('tag', '--merged', $Commit, '--list', 'v[0-9]*')).Output $releaseTag
$previous = if ($null -ne $baseline) { $baseline.Tag } else { '' }

Write-Host "Version $version; protocol baseline: $(if ($previous) { $previous } else { 'none' }); eligible to sign: $eligible."

if ($env:GITHUB_OUTPUT) {
    "value=$version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "previous=$previous" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "eligible=$($eligible.ToString().ToLowerInvariant())" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
}

[pscustomobject]@{ Version = $version; Previous = $previous; Eligible = $eligible }
