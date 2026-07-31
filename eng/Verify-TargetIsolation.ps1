[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$resolvedRepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$targetsRoot = Join-Path $resolvedRepositoryRoot "targets"
$targetDirectories = @(
    Get-ChildItem -LiteralPath $targetsRoot -Directory |
        Sort-Object Name
)
if ($targetDirectories.Count -eq 0) {
    throw "The repository does not contain an exact-SA target product."
}

$retiredRootEntries = @(
    "Briosa.slnx",
    "Directory.Packages.props",
    "buf.yaml",
    "bindings",
    "interop",
    "inventory",
    "proto",
    "src",
    "tests",
    "tools",
    "values"
)
foreach ($entry in $retiredRootEntries) {
    $tracked = @(git -C $resolvedRepositoryRoot ls-files -- $entry)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect tracked root entry '$entry'."
    }
    if ($tracked.Count -gt 0) {
        throw "Target-owned entry '$entry' must not exist at the repository root."
    }
}

$ciWorkflow = Get-Content -LiteralPath (
    Join-Path $resolvedRepositoryRoot ".github\workflows\ci.yml") -Raw
$releaseWorkflow = Get-Content -LiteralPath (
    Join-Path $resolvedRepositoryRoot ".github\workflows\release.yml") -Raw

function Assert-OwnedPath {
    param(
        [Parameter(Mandatory)][string]$ProjectDirectory,
        [Parameter(Mandatory)][string]$TargetPrefix,
        [Parameter(Mandatory)][string]$Include,
        [Parameter(Mandatory)][string]$Kind
    )

    foreach ($candidate in $Include.Split(
        ';',
        [StringSplitOptions]::RemoveEmptyEntries -bor
            [StringSplitOptions]::TrimEntries)) {
        if ($candidate.Contains('$(', [StringComparison]::Ordinal)) {
            throw "$Kind include '$candidate' cannot be statically proven target-local."
        }

        $wildcard = $candidate.IndexOfAny([char[]]@('*', '?'))
        $staticCandidate = if ($wildcard -ge 0) {
            $candidate.Substring(0, $wildcard).TrimEnd('\', '/')
        }
        else {
            $candidate
        }
        if ([string]::IsNullOrWhiteSpace($staticCandidate)) {
            throw "$Kind include '$candidate' has no target-local static prefix."
        }

        $resolved = [IO.Path]::GetFullPath($staticCandidate, $ProjectDirectory)
        if (-not $resolved.StartsWith(
                $TargetPrefix,
                [StringComparison]::OrdinalIgnoreCase)) {
            throw "$Kind include '$candidate' resolves outside its target."
        }
    }
}

foreach ($target in $targetDirectories) {
    $targetPrefix = [IO.Path]::GetFullPath($target.FullName).TrimEnd('\') + '\'
    foreach ($required in @(
            "Briosa.slnx",
            "Directory.Packages.props",
            "buf.yaml",
            "proto",
            "src",
            "tests",
            "tools",
            "eng",
            "interop",
            "docs")) {
        if (-not (Test-Path -LiteralPath (Join-Path $target.FullName $required))) {
            throw "Target '$($target.Name)' does not own required entry '$required'."
        }
    }

    $targetPath = "targets/$($target.Name)"
    if (-not $ciWorkflow.Contains($targetPath, [StringComparison]::Ordinal) -or
        -not $releaseWorkflow.Contains($targetPath, [StringComparison]::Ordinal)) {
        throw "Target '$($target.Name)' is missing from CI or release orchestration."
    }

    foreach ($proto in Get-ChildItem -LiteralPath (
        Join-Path $target.FullName "proto") -Filter "*.proto" -File -Recurse) {
        $source = Get-Content -LiteralPath $proto.FullName -Raw
        if ($source -notmatch '(?m)^package briosa;\s*$' -or
            $source -notmatch '(?m)^option csharp_namespace = "Briosa";\s*$') {
            throw "Protocol '$($proto.FullName)' does not use the stable public identities."
        }
    }

    $projects = Get-ChildItem -LiteralPath $target.FullName -Filter "*.csproj" -File -Recurse |
        Where-Object FullName -NotMatch '[\\/](bin|obj)[\\/]'
    foreach ($project in $projects) {
        [xml]$xml = Get-Content -LiteralPath $project.FullName -Raw
        $projectDirectory = Split-Path -Parent $project.FullName
        foreach ($reference in @($xml.SelectNodes('//ProjectReference[@Include]'))) {
            Assert-OwnedPath $projectDirectory $targetPrefix ([string]$reference.Include) "ProjectReference"
        }
        foreach ($compile in @($xml.SelectNodes('//Compile[@Include]'))) {
            Assert-OwnedPath $projectDirectory $targetPrefix ([string]$compile.Include) "Compile"
        }
        foreach ($protobuf in @($xml.SelectNodes('//Protobuf[@Include]'))) {
            Assert-OwnedPath $projectDirectory $targetPrefix ([string]$protobuf.Include) "Protobuf"
        }
    }
}

Write-Host "Verified $($targetDirectories.Count) isolated exact-SA target product(s)."
