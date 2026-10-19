[CmdletBinding()]
param()

# Proves that Verify-ReleaseWorkflowPolicy.ps1 rejects the regressions it exists to
# catch, using mutated copies of this repository's workflows.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$checker = Join-Path $PSScriptRoot 'Verify-ReleaseWorkflowPolicy.ps1'
$source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.github/workflows'))
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$root = Join-Path $temporaryBase "briosa-release-workflow-policy-$([Guid]::NewGuid().ToString('N'))"

function New-WorkflowCopy([string]$Name) {
    $directory = Join-Path $root $Name
    $null = New-Item -ItemType Directory -Path $directory
    Copy-Item -Path (Join-Path $source '*') -Destination $directory -Recurse
    $directory
}

function Set-WorkflowText([string]$Directory, [string]$File, [string]$Old, [string]$New) {
    $path = Join-Path $Directory $File
    $text = [IO.File]::ReadAllText($path)
    if (-not $text.Contains($Old, [StringComparison]::Ordinal)) { throw "Fixture text for $File was not found: $Old" }
    [IO.File]::WriteAllText($path, $text.Replace($Old, $New))
}

function Assert-Rejected([string]$Name, [scriptblock]$Mutate, [string]$Pattern) {
    $directory = New-WorkflowCopy $Name
    & $Mutate $directory
    $failure = $null
    try { & $checker -WorkflowDirectory $directory 6>$null | Out-Null } catch { $failure = $_.Exception.Message }
    if ($null -eq $failure) { throw "$Name was accepted; expected rejection matching '$Pattern'." }
    if ($failure -notmatch $Pattern) { throw "$Name failed with '$failure', expected '$Pattern'." }
}

try {
    $null = New-Item -ItemType Directory -Path $root
    & $checker -WorkflowDirectory (New-WorkflowCopy 'unchanged') 6>$null | Out-Null

    Assert-Rejected 'tag trigger' {
        param($d) Set-WorkflowText $d 'release.yml' "on:`n  workflow_dispatch:" "on:`n  push:`n    tags: ['v*']`n  workflow_dispatch:"
    } 'release\.yml must not run on tag pushes'
    Assert-Rejected 'unfiltered push trigger' {
        param($d) Set-WorkflowText $d 'ci.yml' "  push:`n    branches:`n      - main`n" "  push:`n"
    } 'ci\.yml must not run on tag pushes'
    Assert-Rejected 'unreviewed signing workflow' {
        param($d) [IO.File]::WriteAllText((Join-Path $d 'extra.yml'), "name: Extra`non:`n  workflow_dispatch:`njobs:`n  extra:`n    if: github.ref == 'refs/heads/main'`n    runs-on: windows-2025`n    environment: release-signing`n    steps:`n      - run: exit 0`n")
    } 'extra\.yml:extra uses environment ''release-signing'', which is not in the reviewed'
    Assert-Rejected 'block environment form' {
        param($d) Set-WorkflowText $d 'release.yml' "    environment: release-signing`n" "    environment:`n      name: release-signing`n"
    } 'scalar'
    Assert-Rejected 'signing on tag refs' {
        param($d) Set-WorkflowText $d 'sign-catalog.yml' "    if: github.ref == 'refs/heads/main'`n" "    if: github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/v')`n"
    } 'sign-catalog\.yml:sign must run only on main'
    Assert-Rejected 'signing on another branch' {
        param($d) Set-WorkflowText $d 'signing-smoke.yml' "    if: github.ref == 'refs/heads/main'`n" "    if: github.ref == 'refs/heads/main' || github.ref == 'refs/heads/dev'`n"
    } 'signing-smoke\.yml:signing may admit only main'
    Assert-Rejected 'release lines outside release.yml' {
        param($d) Set-WorkflowText $d 'signing-smoke.yml' "    if: github.ref == 'refs/heads/main'`n" "    if: github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/heads/release/')`n"
    } 'signing-smoke\.yml:signing may admit only main'
    Assert-Rejected 'automatic trigger for a signing workflow' {
        param($d) Set-WorkflowText $d 'publish-r2.yml' "on:`n  workflow_dispatch:" "on:`n  push:`n    branches: [main]`n  workflow_dispatch:"
    } 'must not be triggered by ''push'''
    Assert-Rejected 'OIDC without an environment' {
        param($d) Set-WorkflowText $d 'release.yml' "    permissions:`n      contents: write`n" "    permissions:`n      contents: write`n      id-token: write`n"
    } 'release\.yml:publish requests an OIDC token without a reviewed environment'
    Assert-Rejected 'R2 renewal in the signing environment' {
        param($d) Set-WorkflowText $d 'publish-r2.yml' "    environment: r2-publication`n" "    environment: release-signing`n"
    } 'publish-r2\.yml:publish uses environment ''release-signing'', which is not in the reviewed'
    Assert-Rejected 'R2 credentials outside the R2 job' {
        param($d) Set-WorkflowText $d 'sign-catalog.yml' "          KEY_ID: `${{ vars.AZURE_CATALOG_KEY_ID }}`n" "          KEY_ID: `${{ vars.AZURE_CATALOG_KEY_ID }}`n          R2_SECRET_ACCESS_KEY: `${{ secrets.R2_SECRET_ACCESS_KEY }}`n"
    } 'sign-catalog\.yml references R2 credentials outside the r2-publication job'
    Assert-Rejected 'executable signing in the R2 environment' {
        param($d) Set-WorkflowText $d 'publish-r2.yml' "      - name: Install pinned R2 client`n" "      - uses: ./.github/actions/sign-package`n      - name: Install pinned R2 client`n"
    } 'must not sign executables in the r2-publication environment'
    Assert-Rejected 'eligibility from a fixed ref' {
        param($d) Set-WorkflowText $d 'release.yml' '-Ref $env:GITHUB_REF' "-Ref 'refs/heads/main'"
    } 'Resolve-ReleaseVersion\.ps1 -Ref'
    Assert-Rejected 'published instead of draft release' {
        param($d) Set-WorkflowText $d 'release.yml' "            '--draft',`n" ''
    } 'must create a draft release'
    Assert-Rejected 'publish without the input' {
        param($d) Set-WorkflowText $d 'release.yml' "    if: inputs.publish && needs.version.outputs.eligible == 'true'" "    if: needs.version.outputs.eligible == 'true'"
    } 'must create a draft release'

    Write-Host 'Release workflow policy rejects tag triggers, unreviewed environments, wider signing refs, OIDC without an environment, misplaced R2 credentials, executable signing in the R2 environment, and non-draft releases.'
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($root)
    if ($resolvedRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedRoot)) {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
}
