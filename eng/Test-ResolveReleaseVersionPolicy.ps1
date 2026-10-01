Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$resolver = Join-Path $PSScriptRoot 'Resolve-ReleaseVersion.ps1'
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryBase "briosa-release-version-policy-$([Guid]::NewGuid().ToString('N'))"
$savedOutput = $env:GITHUB_OUTPUT

function Invoke-FixtureGit {
    $output = & git -c core.autocrlf=false -c commit.gpgsign=false -c tag.gpgsign=false -c user.name=Fixture -c user.email=fixture@example.invalid -C $fixture @args
    if ($LASTEXITCODE -ne 0) { throw "Fixture git $($args[0]) failed." }
    $output
}
function New-FixtureCommit([string]$Name) {
    [IO.File]::WriteAllText((Join-Path $fixture "$Name.txt"), "$Name`n")
    Invoke-FixtureGit add "$Name.txt" | Out-Null
    Invoke-FixtureGit commit --quiet -m $Name | Out-Null
    (Invoke-FixtureGit rev-parse HEAD).Trim()
}
function Invoke-Resolver([hashtable]$Parameters) {
    $outputFile = Join-Path $fixture "github-output-$([Guid]::NewGuid().ToString('N')).txt"
    $env:GITHUB_OUTPUT = $outputFile
    try {
        $result = & $resolver -RepositoryRoot $fixture -MainRef refs/heads/main -SkipMainFetch @Parameters 6>$null
        $outputs = @{}
        foreach ($line in [IO.File]::ReadAllLines($outputFile)) {
            $name, $value = $line -split '=', 2
            $outputs[$name] = $value
        }
        [pscustomobject]@{ Failed = $false; Error = ''; Result = $result; Outputs = $outputs }
    }
    catch {
        [pscustomobject]@{ Failed = $true; Error = $_.Exception.Message; Result = $null; Outputs = @{} }
    }
}
function Assert-Accepted([string]$Name, [hashtable]$Parameters, [string]$Previous, [string]$Eligible) {
    $actual = Invoke-Resolver $Parameters
    if ($actual.Failed) { throw "$Name was rejected: $($actual.Error)" }
    if ($actual.Outputs['previous'] -cne $Previous) { throw "$Name baseline was '$($actual.Outputs['previous'])', expected '$Previous'." }
    if ($actual.Outputs['eligible'] -cne $Eligible) { throw "$Name eligibility was '$($actual.Outputs['eligible'])', expected '$Eligible'." }
}
function Assert-Rejected([string]$Name, [hashtable]$Parameters, [string]$Pattern) {
    $actual = Invoke-Resolver $Parameters
    if (-not $actual.Failed) { throw "$Name was accepted; expected rejection matching '$Pattern'." }
    if ($actual.Error -notmatch $Pattern) { throw "$Name failed with '$($actual.Error)', expected '$Pattern'." }
}
function Push([string]$Tag, [string]$Commit) { @{ EventName = 'push'; TagName = $Tag; Commit = $Commit } }
function Dispatch([string]$Version, [string]$Commit) { @{ EventName = 'workflow_dispatch'; DispatchVersion = $Version; Commit = $Commit } }

try {
    [IO.Directory]::CreateDirectory($fixture) | Out-Null
    Invoke-FixtureGit init --quiet --initial-branch=main | Out-Null
    $root = New-FixtureCommit 'root'
    $old = New-FixtureCommit 'old'
    Invoke-FixtureGit tag v0.9.0 $old | Out-Null
    Invoke-FixtureGit switch --quiet -c side | Out-Null
    $side = New-FixtureCommit 'side'
    Invoke-FixtureGit tag v0.9.1 $side | Out-Null
    Invoke-FixtureGit switch --quiet main | Out-Null
    $middle = New-FixtureCommit 'middle'
    Invoke-FixtureGit merge --quiet --no-ff -m merge side | Out-Null
    $merge = (Invoke-FixtureGit rev-parse HEAD).Trim()
    Invoke-FixtureGit tag v0.10.0 $merge | Out-Null
    Invoke-FixtureGit tag v0.11.0-rc.1 $merge | Out-Null
    $head = New-FixtureCommit 'head'
    Invoke-FixtureGit switch --quiet -c topic | Out-Null
    $topic = New-FixtureCommit 'topic'
    Invoke-FixtureGit switch --quiet main | Out-Null

    # A lower version on the commit that already carries a higher release.
    Invoke-FixtureGit tag v0.9.2 $merge | Out-Null
    Assert-Rejected 'lower tag on a released commit' (Push 'v0.9.2' $merge) 'not greater than the highest stable release v0\.10\.0'
    Invoke-FixtureGit tag -d v0.9.2 | Out-Null

    # A lower version on an older main commit than an existing release.
    Invoke-FixtureGit tag v0.9.2 $middle | Out-Null
    Assert-Rejected 'lower tag on an older commit' (Push 'v0.9.2' $middle) 'not greater than the highest stable release v0\.10\.0'
    Invoke-FixtureGit tag -d v0.9.2 | Out-Null

    # Re-running an existing release excludes only its own tag.
    Assert-Accepted 'rerun of the released tag' (Push 'v0.10.0' $merge) 'v0.9.1' 'true'
    # A higher release on the same commit keeps the other stable tag as its baseline.
    Invoke-FixtureGit tag v0.10.1 $merge | Out-Null
    Assert-Accepted 'higher tag on a released commit' (Push 'v0.10.1' $merge) 'v0.10.0' 'true'
    Invoke-FixtureGit tag -d v0.10.1 | Out-Null
    # Tags reachable only through a merge's second parent count; prereleases never do.
    Assert-Accepted 'next release on main' (Push 'v0.11.0' $head) 'v0.10.0' 'true'
    Assert-Accepted 'dispatch on main' (Dispatch '0.11.0' $head) 'v0.10.0' 'true'
    Assert-Accepted 'prerelease dispatch outside main' (Dispatch '0.11.0-rc.2' $topic) 'v0.10.0' 'false'
    Assert-Accepted 'no reachable baseline' (Dispatch '0.12.0' $root) '' 'true'
    Assert-Rejected 'dispatch of an existing version' (Dispatch '0.10.0' $head) 'not greater than the highest stable release v0\.10\.0'
    Assert-Rejected 'tag outside main' (Push 'v0.12.0' $topic) 'does not point to a commit on main'
    Assert-Rejected 'unsupported version' (Dispatch '1.0' $head) 'not a supported semantic version'
    Assert-Rejected 'non-version tag' (Push 'release-1' $head) 'not a v-prefixed tag'

    Write-Host 'Release version resolution policy tests passed.'
}
finally {
    $env:GITHUB_OUTPUT = $savedOutput
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if ($resolvedFixture.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedFixture)) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
