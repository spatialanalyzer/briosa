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
    Assert-Rejected 'draft refusal without a recovery' {
        param($d) Set-WorkflowText $d 'release.yml' 'Re-run failed jobs' 'Retry'
    } 'must write the maintainer''s recovery'
    # The conformance soak (#450) never reaches a release, signing or registry job.
    $soakRule = 'conformance-soak\.yml must run only from workflow_dispatch'
    Assert-Rejected 'soak calling the release workflow' {
        param($d) Set-WorkflowText $d 'conformance-soak.yml' '    uses: ./.github/workflows/retained-client-compatibility.yml' '    uses: ./.github/workflows/release.yml'
    } $soakRule
    Assert-Rejected 'soak inheriting secrets' {
        param($d) Set-WorkflowText $d 'conformance-soak.yml' "    uses: ./.github/workflows/retained-client-compatibility.yml`n" "    uses: ./.github/workflows/retained-client-compatibility.yml`n    secrets: inherit`n"
    } $soakRule
    Assert-Rejected 'soak with a write permission' {
        param($d) Set-WorkflowText $d 'conformance-soak.yml' "permissions:`n  contents: read" "permissions:`n  contents: write"
    } $soakRule
    Assert-Rejected 'soak on push' {
        param($d) Set-WorkflowText $d 'conformance-soak.yml' "on:`n  workflow_dispatch:" "on:`n  push:`n    branches:`n      - main`n  workflow_dispatch:"
    } $soakRule
    Assert-Rejected 'retained-client gate with an OIDC token' {
        param($d) Set-WorkflowText $d 'retained-client-compatibility.yml' "permissions:`n  contents: read" "permissions:`n  contents: read`n  id-token: write"
    } 'retained-client-compatibility\.yml must run only when called'
    # The same rules on the parsed YAML, whatever the spelling: scalar and flow-style
    # grants, job-level grants, a repository-qualified reusable call, a secret in an
    # expression, a quoted trigger key and forms the policy does not read.
    $soakFile = 'conformance-soak.yml'
    $gateFile = 'retained-client-compatibility.yml'
    $gateRule = 'retained-client-compatibility\.yml must run only when called'
    $pinned = '0123456789abcdef0123456789abcdef01234567'
    foreach ($mutation in @(
            @{ Name = 'soak with a workflow-wide scalar write grant'; File = $soakFile; Old = "permissions:`n  contents: read"; New = 'permissions: write-all'; Rule = $soakRule },
            @{ Name = 'soak with a flow-style write grant'; File = $soakFile; Old = "permissions:`n  contents: read"; New = 'permissions: { contents: write }'; Rule = $soakRule },
            @{ Name = 'soak with a flow-style read grant'; File = $soakFile; Old = "permissions:`n  contents: read"; New = 'permissions: {contents: read}'; Rule = $soakRule },
            @{ Name = 'soak with a job-level scalar write grant'; File = $soakFile; Old = "  summary:`n    name: Summarise the soak`n"; New = "  summary:`n    name: Summarise the soak`n    permissions: write-all`n"; Rule = $soakRule },
            @{ Name = 'soak with a job-level OIDC token'; File = $soakFile; Old = "  summary:`n    name: Summarise the soak`n"; New = "  summary:`n    name: Summarise the soak`n    permissions:`n      id-token: write`n"; Rule = "conformance-soak\.yml(?::summary requests an OIDC token| must run only from workflow_dispatch)" },
            @{ Name = 'soak with a repository-qualified reusable call'; File = $soakFile; Old = "  soak:`n    needs: package`n"; New = "  extra:`n    uses: spatialanalyzer/briosa/.github/workflows/release.yml@$pinned`n  soak:`n    needs: package`n"; Rule = $soakRule },
            @{ Name = 'soak with a secret in an expression'; File = $soakFile; Old = "          ITERATIONS: `${{ inputs.iterations }}`n"; New = "          ITERATIONS: `${{ inputs.iterations }}`n          TOKEN: `${{ secrets.GITHUB_TOKEN }}`n"; Rule = $soakRule },
            @{ Name = 'soak with a quoted trigger key'; File = $soakFile; Old = "on:`n  workflow_dispatch:"; New = "'on':`n  push:`n    branches:`n      - main`n  workflow_dispatch:"; Rule = $soakRule },
            @{ Name = 'soak with an anchor'; File = $soakFile; Old = "permissions:`n  contents: read"; New = "permissions: &read`n  contents: read"; Rule = $soakRule },
            @{ Name = 'gate with a workflow-wide scalar write grant'; File = $gateFile; Old = "permissions:`n  contents: read"; New = 'permissions: write-all'; Rule = $gateRule },
            @{ Name = 'gate with a flow-style job grant'; File = $gateFile; Old = "  gate:`n    needs: [policy, conformance]`n"; New = "  gate:`n    needs: [policy, conformance]`n    permissions: {contents: write}`n"; Rule = $gateRule },
            @{ Name = 'gate declaring secrets'; File = $gateFile; Old = "permissions:`n  contents: read`njobs:"; New = "    secrets:`n      token:`n        required: false`npermissions:`n  contents: read`njobs:"; Rule = $gateRule },
            @{ Name = 'gate calling a workflow'; File = $gateFile; Old = "  gate:`n    needs: [policy, conformance]`n"; New = "  extra:`n    uses: ./.github/workflows/release.yml`n  gate:`n    needs: [policy, conformance]`n"; Rule = $gateRule },
            @{ Name = 'gate with a secret in an expression'; File = $gateFile; Old = "          SERVER_VERSION: `${{ inputs.server-version }}`n"; New = "          SERVER_VERSION: `${{ inputs.server-version }}`n          TOKEN: `${{ secrets.GITHUB_TOKEN }}`n"; Rule = $gateRule })) {
        $current = $mutation
        Assert-Rejected $current.Name { param($d) Set-WorkflowText $d $current.File $current.Old $current.New } $current.Rule
    }

    Write-Host 'Release workflow policy rejects tag triggers, unreviewed environments, wider signing refs, OIDC without an environment, misplaced R2 credentials, executable signing in the R2 environment, non-draft releases, and a soak or retained-client gate that could reach credentials or release jobs.'
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($root)
    if ($resolvedRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedRoot)) {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
}
