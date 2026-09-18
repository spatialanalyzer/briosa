[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$TargetDirectory,
    [Parameter(Mandatory)][string]$OutputPath,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string]$CommitSha,
    [Parameter(Mandatory)][string]$GitRef,
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$target = Get-Item -LiteralPath $TargetDirectory
[xml]$solution = Get-Content (Join-Path $target.FullName 'Briosa.slnx') -Raw
$manifests = @{}
foreach ($project in $solution.SelectNodes('//Project')) {
    $projectPath = [IO.Path]::GetFullPath($project.Path, $target.FullName)
    $sourceLocation = [IO.Path]::GetRelativePath($repositoryRoot, $projectPath).Replace('\', '/')
    $assetsPath = Join-Path (Split-Path -Parent $projectPath) 'obj/project.assets.json'
    $assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json -AsHashtable
    $directNames = @{}
    foreach ($framework in $assets.project.frameworks.Values) {
        if ($framework.ContainsKey('dependencies')) {
            foreach ($name in $framework.dependencies.Keys) { $directNames[$name] = $true }
        }
    }
    $resolved = @{}
    foreach ($framework in $assets.targets.Values) {
        $packages = @{}
        foreach ($entry in $framework.GetEnumerator()) {
            if ($entry.Value.type -eq 'package') {
                $separator = $entry.Key.LastIndexOf('/')
                $name = $entry.Key.Substring(0, $separator)
                $version = $entry.Key.Substring($separator + 1)
                $packages[$name] = 'pkg:nuget/{0}@{1}' -f [Uri]::EscapeDataString($name), $version
            }
        }
        foreach ($entry in $framework.GetEnumerator()) {
            if ($entry.Value.type -ne 'package') { continue }
            $name = $entry.Key.Substring(0, $entry.Key.LastIndexOf('/'))
            $purl = $packages[$name]
            if (-not $resolved.ContainsKey($purl)) {
                $resolved[$purl] = @{
                    package_url = $purl
                    relationship = $(if ($directNames.ContainsKey($name)) { 'direct' } else { 'indirect' })
                    dependencies = @()
                }
            }
            if ($entry.Value.ContainsKey('dependencies')) {
                $dependencies = @($entry.Value.dependencies.Keys |
                    Where-Object { $packages.ContainsKey($_) } | ForEach-Object { $packages[$_] })
                $resolved[$purl].dependencies = @(
                    @($resolved[$purl].dependencies) + $dependencies | Sort-Object -Unique)
            }
        }
    }
    # Framework/runtime packs are download dependencies rather than regular
    # PackageReferences. Include them so self-contained runtime advisories count.
    foreach ($framework in $assets.project.frameworks.Values) {
        if (-not $framework.ContainsKey('downloadDependencies')) { continue }
        foreach ($download in $framework.downloadDependencies) {
            $bounds = @($download.version.Trim('[', ']').Split(',') | ForEach-Object { $_.Trim() })
            if (-not $download.version.StartsWith('[') -or -not $download.version.EndsWith(']') -or
                $bounds.Count -gt 2 -or ($bounds.Count -eq 2 -and $bounds[0] -ne $bounds[1])) {
                throw "Unresolved download dependency: $($download.name)"
            }
            $version = $bounds[0]
            $purl = 'pkg:nuget/{0}@{1}' -f [Uri]::EscapeDataString($download.name), $version
            if (-not $resolved.ContainsKey($purl)) {
                $resolved[$purl] = @{ package_url = $purl; relationship = 'direct'; dependencies = @() }
            }
        }
    }
    $manifests[$sourceLocation] = @{
        name = $sourceLocation
        file = @{ source_location = $sourceLocation }
        resolved = $resolved
    }
}
$snapshot = @{
    version = 0; sha = $CommitSha; ref = $GitRef
    job = @{ id = 'local'; correlator = "nuget-$($target.Name)" }
    detector = @{ name = 'Briosa NuGet assets'; version = '1'; url = 'https://github.com/spatialanalyzer/briosa' }
    scanned = [DateTime]::UtcNow.ToString('o')
    manifests = $manifests
}
$parent = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
New-Item -ItemType Directory -Path $parent -Force | Out-Null
$snapshot | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Host "Captured $($manifests.Count) project manifests for $($target.Name)."
