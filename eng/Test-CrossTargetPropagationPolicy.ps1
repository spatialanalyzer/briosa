Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$propagation = Join-Path $PSScriptRoot 'Test-CrossTargetPropagation.ps1'
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryBase "briosa-propagation-policy-$([Guid]::NewGuid().ToString('N'))"
$repository = Join-Path $fixture 'repository'
$summaryPath = Join-Path $fixture 'summary.md'
$first = 'targets/2026.1.0529.7'
$second = 'targets/2024.1.0508.5'
$savedSummary = $env:GITHUB_STEP_SUMMARY
$savedActions = $env:GITHUB_ACTIONS

function Invoke-FixtureGit {
    & git -c core.autocrlf=false -c commit.gpgsign=false -c user.name=Fixture -c user.email=fixture@example.invalid -C $repository @args
    if ($LASTEXITCODE -ne 0) { throw "Fixture git $($args[0]) failed." }
}
function Set-FixtureFile([string]$Target, [string]$Path, [string]$Content) {
    $fullPath = Join-Path (Join-Path $repository $Target) $Path
    [IO.Directory]::CreateDirectory((Split-Path -Parent $fullPath)) | Out-Null
    [IO.File]::WriteAllText($fullPath, $Content)
}
function Set-Twin([string]$Path, [string]$Content) {
    Set-FixtureFile $first $Path $Content
    Set-FixtureFile $second $Path $Content
}
function Set-VersionTwin([string]$Path, [string]$Suffix) {
    Set-FixtureFile $first $Path "package sa-2026.1.0529.7`nassembly 2026.1.529.7`nnew Version(2026, 1, 529, 7)`n$Suffix"
    Set-FixtureFile $second $Path "package sa-2024.1.0508.5`nassembly 2024.1.508.5`nnew Version(2024, 1, 508, 5)`n$Suffix"
}
function Invoke-Propagation([hashtable]$Parameters) {
    if (Test-Path -LiteralPath $summaryPath) { Remove-Item -LiteralPath $summaryPath }
    $env:GITHUB_STEP_SUMMARY = $summaryPath
    $env:GITHUB_ACTIONS = 'false'
    & $propagation -RepositoryRoot $repository @Parameters 6>$null
    $exitCode = $LASTEXITCODE
    $rows = @{}
    foreach ($line in [IO.File]::ReadAllLines($summaryPath)) {
        if ($line -match '^\| `(?<path>[^`]+)` \| (?<kind>[^|]+) \| [a-z]+ \| [a-z]+ \| (?<allowed>yes|no) \|$') {
            $rows[$Matches.path] = "$($Matches.kind.Trim())/$($Matches.allowed)"
        }
    }
    [pscustomobject]@{ ExitCode = $exitCode; Rows = $rows }
}
function Assert-Result([string]$Name, $Result, [int]$ExitCode, [hashtable]$Rows) {
    if ($Result.ExitCode -ne $ExitCode) { throw "$Name exited $($Result.ExitCode), expected $ExitCode." }
    $actual = @($Result.Rows.Keys | Sort-Object | ForEach-Object { "$_=$($Result.Rows[$_])" }) -join '; '
    $expected = @($Rows.Keys | Sort-Object | ForEach-Object { "$_=$($Rows[$_])" }) -join '; '
    if ($actual -cne $expected) { throw "$Name reported '$actual', expected '$expected'." }
}

try {
    [IO.Directory]::CreateDirectory($repository) | Out-Null
    Invoke-FixtureGit init --quiet
    foreach ($path in @('one-sided.txt', 'both.txt', 'diverged.txt', 'deleted.txt', 'untouched.txt', 'nested/dir/File.cs')) {
        Set-Twin $path "shared`n"
    }
    Set-VersionTwin 'version-both.txt' ''
    Set-VersionTwin 'version-one-sided.txt' ''
    Set-FixtureFile $first 'independent.txt' "alpha`n"
    Set-FixtureFile $second 'independent.txt' "beta`n"
    # A padded release string and an assembly-form string are not version-only twins.
    Set-FixtureFile $first 'version-form.txt' "sa-2026.1.0529.7`n"
    Set-FixtureFile $second 'version-form.txt' "sa-2024.1.508.5`n"
    Invoke-FixtureGit add --all
    Invoke-FixtureGit commit --quiet -m base
    $base = (& git -C $repository rev-parse HEAD).Trim()

    Set-FixtureFile $first 'one-sided.txt' "shared`nfix`n"
    Set-Twin 'both.txt' "shared`nfix`n"
    Set-FixtureFile $first 'diverged.txt' "first`n"
    Set-FixtureFile $second 'diverged.txt' "second`n"
    Remove-Item -LiteralPath (Join-Path $repository "$second/deleted.txt")
    Set-VersionTwin 'version-both.txt' "fix`n"
    Set-FixtureFile $first 'version-one-sided.txt' "package sa-2026.1.0529.7`nassembly 2026.1.529.7`nnew Version(2026, 1, 529, 7)`nfix`n"
    Set-FixtureFile $second 'nested/dir/File.cs' "shared`nfix`n"
    Set-FixtureFile $first 'independent.txt' "alpha`nfix`n"
    Set-FixtureFile $first 'version-form.txt' "sa-2026.1.0529.7`nfix`n"
    Invoke-FixtureGit add --all
    Invoke-FixtureGit commit --quiet -m head
    # Uncommitted bytes must not hide a committed one-sided change.
    Set-FixtureFile $second 'one-sided.txt' "shared`nfix`n"

    $expected = @{
        'one-sided.txt' = 'One-sided change/no'
        'diverged.txt' = 'Diverged change/no'
        'deleted.txt' = 'One-sided change/no'
        'version-one-sided.txt' = 'One-sided change/no'
        'nested/dir/File.cs' = 'One-sided change/no'
    }
    Assert-Result 'Report-only run' (Invoke-Propagation @{ BaseRef = $base }) 0 $expected
    Assert-Result 'Strict run' (Invoke-Propagation @{ BaseRef = $base; Strict = $true }) 1 $expected

    $allowed = @{}
    foreach ($key in $expected.Keys) { $allowed[$key] = $expected[$key].Replace('/no', '/yes') }
    Assert-Result 'Fully allowed strict run' (Invoke-Propagation @{
            BaseRef = $base; Strict = $true
            AllowPath = @('one-sided.txt', 'diverged.txt', 'deleted.txt', 'version-*', 'nested/*')
        }) 0 $allowed

    $partial = $expected.Clone()
    $partial['nested/dir/File.cs'] = 'One-sided change/yes'
    Assert-Result 'Partially allowed strict run' (Invoke-Propagation @{
            BaseRef = $base; Strict = $true; AllowPath = @('nested\*')
        }) 1 $partial

    Invoke-FixtureGit update-ref refs/remotes/origin/main $base
    Assert-Result 'Default merge-base run' (Invoke-Propagation @{}) 0 $expected
    Assert-Result 'Unchanged strict run' (Invoke-Propagation @{ BaseRef = 'HEAD'; Strict = $true }) 0 @{}
    if (-not (Select-String -LiteralPath $summaryPath -SimpleMatch 'No propagation findings.' -Quiet)) {
        throw 'Unchanged run did not summarize an empty result.'
    }

    Write-Host 'Cross-target propagation policy passed: one-sided, diverged, deleted, version-only, allowed, strict, and committed-blob cases.'
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
