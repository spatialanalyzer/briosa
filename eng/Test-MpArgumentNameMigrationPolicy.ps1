[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$TargetDirectory,
    [string]$BufPath = 'buf'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$target = (Resolve-Path -LiteralPath $TargetDirectory).Path
$repository = Split-Path -Parent $PSScriptRoot
$ledger = Get-Content (Join-Path $target 'docs/development/mp-argument-name-migration.json') -Raw | ConvertFrom-Json
$gitDirectory = (& git -C $repository rev-parse --path-format=absolute --git-common-dir).Trim().Replace('\', '/')
if ($LASTEXITCODE -ne 0) { throw 'Cannot locate the protocol baseline repository.' }
$subdirectory = [IO.Path]::GetRelativePath($repository, $target).Replace('\', '/')
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryBase "briosa-name-policy-$([Guid]::NewGuid().ToString('N'))"
$null = [IO.Directory]::CreateDirectory($fixture)
try {
    Copy-Item -LiteralPath (Join-Path $target 'proto') -Destination $fixture -Recurse
    Copy-Item -LiteralPath (Join-Path $target 'buf.yaml') -Destination $fixture
    $null = [IO.Directory]::CreateDirectory((Join-Path $fixture 'eng'))
    $null = [IO.Directory]::CreateDirectory((Join-Path $fixture 'docs/development'))
    Copy-Item -LiteralPath (Join-Path $target 'eng/Test-MpArgumentNameMigration.ps1') -Destination (Join-Path $fixture 'eng')
    Copy-Item -LiteralPath (Join-Path $target 'docs/development/mp-argument-name-migration.json') -Destination (Join-Path $fixture 'docs/development')
    $baseline = Join-Path $fixture 'baseline.json'
    & $BufPath build "$gitDirectory#ref=$($ledger.baseline),subdir=$subdirectory" --exclude-source-info --as-file-descriptor-set -o $baseline
    if ($LASTEXITCODE -ne 0) { throw 'Cannot build the pinned baseline.' }
    $checker = Join-Path $fixture 'eng/Test-MpArgumentNameMigration.ps1'
    & $checker -Against $baseline -BufPath $BufPath *> (Join-Path $fixture 'allowed.log')
    $cases = @(
        @{ Name = 'unlisted name'; File = 'analysis_operations.proto'; Before = 'optional CollectionObjectName selected_line = 1;'; After = 'optional CollectionObjectName unrelated_line = 1;' },
        @{ Name = 'wire type'; File = 'analysis_operations.proto'; Before = 'optional double angle_tolerance = 4;'; After = 'optional string angle_tolerance = 4;' },
        @{ Name = 'presence'; File = 'analysis_operations.proto'; Before = 'optional double angle_tolerance = 4;'; After = 'double angle_tolerance = 4;' },
        @{ Name = 'field number'; File = 'analysis_operations.proto'; Before = 'optional double angle_tolerance = 4;'; After = 'optional double angle_tolerance = 40;' },
        @{ Name = 'collision exception'; File = 'gdt_operations.proto'; Before = 'double measured_deviation_upper = 4;'; After = 'double measured_deviation = 4;' }
    )
    foreach ($case in $cases) {
        $path = Join-Path $fixture "proto/briosa/$($case.File)"
        $original = [IO.File]::ReadAllText($path)
        if (-not $original.Contains($case.Before)) { throw "Missing mutation fixture: $($case.Name)." }
        [IO.File]::WriteAllText($path, $original.Replace($case.Before, $case.After))
        $rejected = $false
        try { & $checker -Against $baseline -BufPath $BufPath *> (Join-Path $fixture 'rejected.log') }
        catch { $rejected = $true }
        finally { [IO.File]::WriteAllText($path, $original) }
        if (-not $rejected) { throw "Migration policy accepted an unapproved change: $($case.Name)." }
    }
    # An already-migrated baseline must use ordinary strict FILE checking.
    Push-Location $fixture
    try { & $BufPath build --exclude-source-info --as-file-descriptor-set -o (Join-Path $fixture 'current.json') }
    finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw 'Cannot build the migrated baseline.' }
    & $checker -Against (Join-Path $fixture 'current.json') -BufPath $BufPath
    $path = Join-Path $fixture 'proto/briosa/analysis_operations.proto'
    [IO.File]::WriteAllText($path, ([IO.File]::ReadAllText($path).Replace('optional double angle_tolerance = 4;', 'optional double angle_tolerance_new = 4;')))
    $rejected = $false
    try { & $checker -Against (Join-Path $fixture 'current.json') -BufPath $BufPath *> (Join-Path $fixture 'fallback.log') }
    catch { $rejected = $true }
    if (-not $rejected) { throw 'Migration exception leaked into a different baseline.' }
    Write-Host "Verified migration policy for $($ledger.target): approved changes pass; six unapproved changes fail."
    $global:LASTEXITCODE = 0
}
finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    $prefix = $temporaryBase.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid fixture cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
