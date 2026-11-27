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

function Assert-ReaderRejected([hashtable]$Mutation, [string]$Rule) {
    # The YAML reader rejects the construct on the line holding Mark, by file and line,
    # before any policy rule runs: the message is the reader's, not a rule's finding.
    $directory = New-WorkflowCopy $Mutation.Name
    Set-WorkflowText $directory $Mutation.File $Mutation.Old $Mutation.New
    $lines = [IO.File]::ReadAllLines((Join-Path $directory $Mutation.File))
    $line = @(for ($index = 0; $index -lt $lines.Count; $index++) { if ($lines[$index].Contains($Mutation.Mark)) { $index + 1 } })
    if ($line.Count -ne 1) { throw "$($Mutation.Name): the mark '$($Mutation.Mark)' is not on exactly one line." }
    $pattern = "$Rule[^:]*: it uses a YAML form the policy does not read \($([regex]::Escape($Mutation.File)) line $($line[0]): $($Mutation.Construct)"
    $failure = $null
    try { & $checker -WorkflowDirectory $directory 6>$null | Out-Null } catch { $failure = $_.Exception.Message }
    if ($null -eq $failure) { throw "$($Mutation.Name) was accepted; expected the reader to reject it." }
    if ($failure -notmatch $pattern) { throw "$($Mutation.Name) failed with '$failure', expected '$pattern'." }
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
    # The same rules on the parsed YAML: scalar grants, job-level grants, a
    # repository-qualified reusable call, and a secret named in an expression in any case,
    # after a brace in a string literal, or in an if condition without braces.
    $soakFile = 'conformance-soak.yml'
    $gateFile = 'retained-client-compatibility.yml'
    $gateRule = 'retained-client-compatibility\.yml must run only when called'
    $pinned = '0123456789abcdef0123456789abcdef01234567'
    $summary = "  summary:`n    name: Summarise the soak`n"
    $soakJob = "    uses: ./.github/workflows/retained-client-compatibility.yml`n"
    $gate = "  gate:`n    needs:`n      - policy`n      - conformance`n"
    $iterations = "          ITERATIONS: `${{ inputs.iterations }}`n"
    $iterationsCall = "      soak-iterations: `${{ fromJSON(inputs.iterations) }}`n"
    $firstStep = "    steps:`n      - name: Download the iteration results`n"
    # A backslash, written so that no tool decodes the escapes below.
    $escape = [char]92
    foreach ($mutation in @(
            @{ Name = 'soak with a workflow-wide scalar write grant'; File = $soakFile; Old = "permissions:`n  contents: read"; New = 'permissions: write-all'; Rule = $soakRule },
            @{ Name = 'soak with a job-level scalar write grant'; File = $soakFile; Old = $summary; New = "$($summary)    permissions: write-all`n"; Rule = $soakRule },
            @{ Name = 'soak with a job-level OIDC token'; File = $soakFile; Old = $summary; New = "$($summary)    permissions:`n      id-token: write`n"; Rule = "conformance-soak\.yml(?::summary requests an OIDC token| must run only from workflow_dispatch)" },
            @{ Name = 'soak with a repository-qualified reusable call'; File = $soakFile; Old = "  soak:`n    needs: package`n"; New = "  extra:`n    uses: spatialanalyzer/briosa/.github/workflows/release.yml@$pinned`n  soak:`n    needs: package`n"; Rule = $soakRule },
            @{ Name = 'soak with a secret in an expression'; File = $soakFile; Old = $iterations; New = "$($iterations)          TOKEN: `${{ secrets.GITHUB_TOKEN }}`n"; Rule = $soakRule },
            @{ Name = 'soak with an upper-case secrets context'; File = $soakFile; Old = $iterations; New = "$($iterations)          TOKEN: `${{ SECRETS.GITHUB_TOKEN }}`n"; Rule = "$soakRule.*an expression refers to secrets" },
            @{ Name = 'soak with a secret after a brace in a string literal'; File = $soakFile; Old = $iterations; New = "$($iterations)          TOKEN: `${{ format('{0}', secrets.GITHUB_TOKEN) }}`n"; Rule = "$soakRule.*an expression refers to secrets" },
            @{ Name = 'soak with a secret in an if condition'; File = $soakFile; Old = $firstStep; New = "$($firstStep)        if: Secrets.GITHUB_TOKEN != ''`n"; Rule = "$soakRule.*an expression refers to secrets" },
            # The values the soak passes to the gate, against the types the gate declares
            # (#450): the dispatch's number input passed directly was the first dispatch's failure.
            @{ Name = 'soak passing the dispatch number input directly'; File = $soakFile; Old = $iterationsCall; New = "      soak-iterations: `${{ inputs.iterations }}`n"; Rule = "$soakRule.*job 'soak' passes a string to the number input 'soak-iterations' of retained-client-compatibility\.yml at conformance-soak\.yml line [0-9]+; a workflow_dispatch number input reaches the inputs context as a string" },
            @{ Name = 'soak passing a quoted boolean'; File = $soakFile; Old = "      allow-source-candidates: true`n"; New = "      allow-source-candidates: 'true'`n"; Rule = "$soakRule.*passes a string to the boolean input 'allow-source-candidates'" },
            @{ Name = 'soak passing a quoted count'; File = $soakFile; Old = $iterationsCall; New = "      soak-iterations: '20'`n"; Rule = "$soakRule.*passes a string to the number input 'soak-iterations'" },
            @{ Name = 'soak passing a number as the server version'; File = $soakFile; Old = "      server-version: 0.2.0-soak`n"; New = "      server-version: 2`n"; Rule = "$soakRule.*passes a number to the string input 'server-version'" },
            @{ Name = 'soak parsing a string input'; File = $soakFile; Old = $iterationsCall; New = "      soak-iterations: `${{ fromJSON(inputs.client-version) }}`n"; Rule = "$soakRule.*passes 'soak-iterations' at conformance-soak\.yml line [0-9]+ a value whose type the policy cannot tell" },
            @{ Name = 'soak passing an undeclared input'; File = $soakFile; Old = "      server-version: 0.2.0-soak`n"; New = "      server-version: 0.2.0-soak`n      soak-extra: x`n"; Rule = "$soakRule.*passes 'soak-extra', which retained-client-compatibility\.yml does not declare" },
            @{ Name = 'soak leaving out a required input'; File = $soakFile; Old = "      server-version: 0.2.0-soak`n"; New = ''; Rule = "$soakRule.*does not pass the input 'server-version' that retained-client-compatibility\.yml requires" },
            @{ Name = 'soak parsing a secret'; File = $soakFile; Old = $iterationsCall; New = "      soak-iterations: `${{ fromJSON(secrets.ITERATIONS) }}`n"; Rule = "$soakRule.*an expression refers to secrets" },
            @{ Name = 'gate with a workflow-wide scalar write grant'; File = $gateFile; Old = "permissions:`n  contents: read"; New = 'permissions: write-all'; Rule = $gateRule },
            @{ Name = 'gate declaring secrets'; File = $gateFile; Old = "permissions:`n  contents: read`njobs:"; New = "    secrets:`n      token:`n        required: false`npermissions:`n  contents: read`njobs:"; Rule = $gateRule },
            @{ Name = 'gate calling a workflow'; File = $gateFile; Old = $gate; New = "  extra:`n    uses: ./.github/workflows/release.yml`n$gate"; Rule = $gateRule },
            @{ Name = 'gate with a secret in an expression'; File = $gateFile; Old = "          SERVER_VERSION: `${{ inputs.server-version }}`n"; New = "          SERVER_VERSION: `${{ inputs.server-version }}`n          TOKEN: `${{ Secrets.GITHUB_TOKEN }}`n"; Rule = $gateRule })) {
        $current = $mutation
        Assert-Rejected $current.Name { param($d) Set-WorkflowText $d $current.File $current.Old $current.New } $current.Rule
    }
    # Every construct outside the reader's plain block subset, each beside a violation
    # it could hide from a reader that decoded or guessed at it. The reader rejects
    # each by file and line before any policy rule runs. The first two are the review's
    # own: an escaped double-quoted key and an implicit mapping in a flow sequence.
    foreach ($mutation in @(
            @{ Name = 'escaped double-quoted key'; File = $soakFile; Old = $summary; New = "$($summary)    `"permi$($escape)u0073sions`": write-all`n"; Mark = "permi$($escape)u0073sions"; Construct = 'a quoted key' },
            @{ Name = 'implicit mapping in a flow sequence'; File = $soakFile; Old = $summary; New = "  extra:`n    runs-on: ubuntu-latest`n    steps: [uses: ./.github/workflows/release.yml]`n$summary"; Mark = 'steps: [uses'; Construct = 'a flow sequence or flow mapping in value position' },
            @{ Name = 'single-quoted key'; File = $soakFile; Old = $soakJob; New = "$($soakJob)    'secrets': inherit`n"; Mark = "'secrets'"; Construct = 'a quoted key' },
            @{ Name = 'quoted trigger key'; File = $soakFile; Old = "on:`n  workflow_dispatch:"; New = "'on':`n  push:`n    branches:`n      - main`n  workflow_dispatch:"; Mark = "'on':"; Construct = 'a quoted key' },
            @{ Name = 'quoted key in a sequence item'; File = $soakFile; Old = "      - name: Download the iteration results`n"; New = "      - 'uses': ./.github/workflows/release.yml`n      - name: Download the iteration results`n"; Mark = "'uses'"; Construct = 'a quoted key' },
            @{ Name = 'flow mapping grant'; File = $soakFile; Old = "permissions:`n  contents: read"; New = 'permissions: { contents: write }'; Mark = 'permissions: {'; Construct = 'a flow sequence or flow mapping in value position' },
            @{ Name = 'flow mapping step'; File = $soakFile; Old = "      - name: Download the iteration results`n"; New = "      - {uses: ./.github/workflows/release.yml}`n      - name: Download the iteration results`n"; Mark = '- {uses'; Construct = 'a flow sequence or flow mapping in key position' },
            @{ Name = 'flow sequence of needs'; File = $gateFile; Old = $gate; New = "  gate:`n    needs: [policy, conformance]`n    permissions: write-all`n"; Mark = 'needs: ['; Construct = 'a flow sequence or flow mapping in value position' },
            @{ Name = 'anchor'; File = $soakFile; Old = $summary; New = "  summary:`n    name: &call ./.github/workflows/release.yml`n"; Mark = '&call'; Construct = 'an anchor in value position' },
            @{ Name = 'alias'; File = $soakFile; Old = "      - name: Download the iteration results`n"; New = "      - uses: *call`n      - name: Download the iteration results`n"; Mark = '*call'; Construct = 'an alias in value position' },
            @{ Name = 'tag'; File = $soakFile; Old = "      - name: Download the iteration results`n"; New = "      - !!str uses: ./.github/workflows/release.yml`n      - name: Download the iteration results`n"; Mark = '!!str'; Construct = 'a tag in value position' },
            @{ Name = 'tag on a key'; File = $soakFile; Old = $summary; New = "$($summary)    !!str permissions: write-all`n"; Mark = '!!str'; Construct = 'a tag in key position' },
            @{ Name = 'merge key'; File = $soakFile; Old = $summary; New = "$($summary)    <<:`n      permissions: write-all`n"; Mark = '<<:'; Construct = 'a merge key' },
            @{ Name = 'second document'; File = $soakFile; Old = "permissions:`n  contents: read`n"; New = "permissions:`n  contents: read`n--- # a second document`npermissions: write-all`n"; Mark = '--- # a second document'; Construct = 'a document marker' },
            @{ Name = 'directive'; File = $soakFile; Old = "name: Client conformance soak`n"; New = "%YAML 1.2`nname: Client conformance soak`n"; Mark = '%YAML'; Construct = 'a directive' },
            @{ Name = 'block scalar in key position'; File = $soakFile; Old = $summary; New = "$($summary)    ? |-`n      permissions`n    : write-all`n"; Mark = '? |-'; Construct = 'an explicit key' },
            @{ Name = 'tab'; File = $soakFile; Old = $summary; New = "$($summary)    permissions:`twrite-all`n"; Mark = "permissions:`t"; Construct = 'a tab' },
            @{ Name = 'repeated key'; File = $soakFile; Old = $summary; New = "$($summary)    permissions:`n      contents: read`n    permissions: write-all`n"; Mark = 'permissions: write-all'; Construct = "a repeated key \('permissions'\)" },
            @{ Name = 'repeated key in another case'; File = $soakFile; Old = $summary; New = "$($summary)    permissions:`n      contents: read`n    Permissions: write-all`n"; Mark = 'Permissions: write-all'; Construct = "a repeated key \('Permissions'\)" },
            @{ Name = 'boolean job key'; File = $soakFile; Old = $summary; New = "  no:`n    runs-on: ubuntu-latest`n    permissions: write-all`n    steps:`n      - run: exit 0`n$summary"; Mark = '  no:'; Construct = "a key YAML may read as a boolean or a null \('no'\)" },
            @{ Name = 'boolean trigger key'; File = $soakFile; Old = "on:`n  workflow_dispatch:"; New = "On:`n  push:`n    branches:`n      - main`n  workflow_dispatch:"; Mark = 'On:'; Construct = "a key YAML may read as a boolean or a null \('On'\)" },
            @{ Name = 'null trigger key'; File = $gateFile; Old = "on:`n  workflow_call:`n"; New = "on:`n  null:`n  workflow_call:`n"; Mark = '  null:'; Construct = "a key YAML may read as a boolean or a null \('null'\)" },
            @{ Name = 'numeric permission key'; File = $soakFile; Old = "permissions:`n  contents: read"; New = "permissions:`n  contents: read`n  1: write"; Mark = '  1: write'; Construct = "a key YAML may read as a number or a null \('1'\)" },
            @{ Name = 'escaped double-quoted value'; File = $soakFile; Old = "      - name: Download the iteration results`n"; New = "      - uses: `"./.github/workflows/rel$($escape)u0065ase.yml`"`n      - name: Download the iteration results`n"; Mark = "rel$($escape)u0065ase"; Construct = 'a double-quoted scalar with an escape' })) {
        Assert-ReaderRejected $mutation $(if ($mutation.File -eq $gateFile) { $gateRule } else { $soakRule })
    }

    Write-Host 'Release workflow policy rejects tag triggers, unreviewed environments, wider signing refs, OIDC without an environment, misplaced R2 credentials, executable signing in the R2 environment, non-draft releases, a soak or retained-client gate that could reach credentials or release jobs, a soak that passes the gate an input of another type than the gate declares, and either written in YAML outside the plain block subset, by file and line.'
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($root)
    if ($resolvedRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedRoot)) {
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
}
