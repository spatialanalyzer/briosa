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
# Releases run only from workflow_dispatch; the dispatched ref decides eligibility.
function Dispatch([string]$Ref, [string]$Version, [string]$Commit) { @{ Ref = $Ref; Version = $Version; Commit = $Commit } }
function Main([string]$Version, [string]$Commit) { Dispatch 'refs/heads/main' $Version $Commit }

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
    Assert-Rejected 'lower version on a released commit' (Main '0.9.2' $merge) 'not greater than the highest stable release v0\.10\.0'
    Invoke-FixtureGit tag -d v0.9.2 | Out-Null

    # A lower version on an older main commit than an existing release.
    Invoke-FixtureGit tag v0.9.2 $middle | Out-Null
    Assert-Rejected 'lower version on an older commit' (Main '0.9.2' $middle) 'not greater than the highest stable release v0\.10\.0'
    Invoke-FixtureGit tag -d v0.9.2 | Out-Null

    # An existing tag for the version must point at the dispatched commit: a rerun, or
    # a tag pushed before the dispatch. It is excluded from the version order.
    Assert-Accepted 'rerun of a released version' (Main '0.10.0' $merge) 'v0.9.1' 'true'
    Invoke-FixtureGit tag v0.10.1 $merge | Out-Null
    Assert-Accepted 'higher tag on a released commit' (Main '0.10.1' $merge) 'v0.10.0' 'true'
    Invoke-FixtureGit tag -d v0.10.1 | Out-Null
    Assert-Rejected 'existing version on another commit' (Main '0.10.0' $head) 'v0\.10\.0 already points to'
    Invoke-FixtureGit tag v0.12.0 $topic | Out-Null
    Assert-Rejected 'tag pushed on another commit' (Main '0.12.0' $head) 'v0\.12\.0 already points to'
    Invoke-FixtureGit tag -d v0.12.0 | Out-Null

    # Tags reachable only through a merge's second parent count; prereleases never do.
    Assert-Accepted 'next release on main' (Main '0.11.0' $head) 'v0.10.0' 'true'
    Assert-Accepted 'no reachable baseline' (Main '0.12.0' $root) '' 'true'
    Assert-Rejected 'main ref with a commit outside main' (Main '0.12.0' $topic) 'is not on main'

    # Any other branch is an unsigned dry run, ordered like main.
    Assert-Accepted 'prerelease dry run outside main' (Dispatch 'refs/heads/topic' '0.11.0-rc.2' $topic) 'v0.10.0' 'false'
    Assert-Accepted 'unrecognized release branch name' (Dispatch 'refs/heads/release/next' '0.11.0-rc.2' $topic) 'v0.10.0' 'false'
    Assert-Rejected 'dry run of an old version' (Dispatch 'refs/heads/topic' '0.9.9' $topic) 'not greater than the highest stable release v0\.10\.0'

    # Publishing the draft release creates the tag, so tag refs never release.
    Assert-Rejected 'tag ref' (Dispatch 'refs/tags/v0.12.0' '0.12.0' $head) 'Dispatch releases from main or a release branch'
    Assert-Rejected 'pull request ref' (Dispatch 'refs/pull/1/merge' '0.12.0' $head) 'not a branch'
    Assert-Rejected 'unsupported version' (Main '1.0' $head) 'not a supported semantic version'

    # A release line is cut from a stable release of that line on main. Later releases
    # on main do not block its hotfixes; releases of the same line do.
    Invoke-FixtureGit tag v0.11.0 $head | Out-Null
    Invoke-FixtureGit switch --quiet -c release/0.10 $merge | Out-Null
    $hotfix = New-FixtureCommit 'hotfix'
    Invoke-FixtureGit switch --quiet main | Out-Null
    $line = 'refs/heads/release/0.10'
    Assert-Accepted 'hotfix behind a later main release' (Dispatch $line '0.10.1' $hotfix) 'v0.10.0' 'true'
    Assert-Accepted 'hotfix prerelease' (Dispatch $line '0.10.1-rc.1' $hotfix) 'v0.10.0' 'true'
    Assert-Rejected 'hotfix outside its line' (Dispatch $line '0.11.1' $hotfix) 'not in the 0\.10 release line'
    Assert-Rejected 'hotfix below its line' (Dispatch $line '0.10.0-rc.9' $hotfix) 'not greater than the highest stable 0\.10 release v0\.10\.0'
    Invoke-FixtureGit tag v0.10.1 $hotfix | Out-Null
    Invoke-FixtureGit switch --quiet release/0.10 | Out-Null
    $hotfix2 = New-FixtureCommit 'hotfix-two'
    Invoke-FixtureGit switch --quiet main | Out-Null
    Assert-Accepted 'second hotfix on a line' (Dispatch $line '0.10.2' $hotfix2) 'v0.10.1' 'true'
    Assert-Rejected 'repeated hotfix version' (Dispatch $line '0.10.1' $hotfix2) 'v0\.10\.1 already points to'
    Assert-Accepted 'main still orders after hotfixes' (Main '0.11.1' $head) 'v0.11.0' 'true'
    Assert-Rejected 'line without a release of its own' (Dispatch 'refs/heads/release/0.12' '0.12.1' $head) 'must descend from a stable v0\.12\.\* release on main'
    Invoke-FixtureGit tag v0.13.0 $topic | Out-Null
    Assert-Rejected 'line cut from a release outside main' (Dispatch 'refs/heads/release/0.13' '0.13.1' $topic) 'must descend from a stable v0\.13\.\* release on main'

    Write-Host 'Release version resolution policy tests passed.'
}
finally {
    $env:GITHUB_OUTPUT = $savedOutput
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if ($resolvedFixture.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedFixture)) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
exit 0
