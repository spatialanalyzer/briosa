[CmdletBinding()]
param(
    [string]$WorkflowDirectory = (Join-Path $PSScriptRoot '../.github/workflows')
)

# Reviewed release-trust policy for the repository workflows (#249).
# GitHub runs a dispatched workflow from the dispatched commit, so the trust boundary
# is each environment's branch rules and required reviewers, not this script. The
# script keeps reviewed workflows from drifting: no tag-push trigger, environments
# only in the listed jobs, and every signing job limited to main or a release line.
# Scheduled R2 renewal has its own environment (D11): it holds the R2 credentials and
# a catalog-only Azure identity, needs no reviewer, and never signs executables.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedDirectory = [IO.Path]::GetFullPath($WorkflowDirectory)
if (-not (Test-Path -LiteralPath $resolvedDirectory -PathType Container)) {
    throw "Workflow directory '$resolvedDirectory' does not exist."
}

$mainOnly = "github.ref == 'refs/heads/main'"
$releaseLine = "startsWith(github.ref, 'refs/heads/release/')"
# Every job that names an environment, keyed by workflow file and job.
$environmentJobs = @{
    'licensed-sa.yml:licensed-sa'      = @{ Environment = 'licensed-sa-2026-1-0529-7'; Events = @('workflow_dispatch'); Signing = $false }
    'licensed-sa-2024.yml:licensed-sa' = @{ Environment = 'licensed-sa-2024-1-0508-5'; Events = @('workflow_dispatch'); Signing = $false }
    'publish-r2.yml:publish'           = @{ Environment = 'r2-publication'; Events = @('workflow_dispatch', 'schedule'); Signing = $true }
    'release.yml:sign'                 = @{ Environment = 'release-signing'; Events = @('workflow_dispatch'); Signing = $true; ReleaseLines = $true }
    'sign-catalog.yml:sign'            = @{ Environment = 'release-signing'; Events = @('workflow_dispatch'); Signing = $true }
    'signing-smoke.yml:signing'        = @{ Environment = 'release-signing'; Events = @('workflow_dispatch'); Signing = $true }
}

function Find-LineIndex([string[]]$Lines, [string]$Pattern) {
    for ($index = 0; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index] -match $Pattern) { return $index }
    }
    -1
}

function Get-IndentedBlock([string[]]$Lines, [int]$Start, [int]$Indent) {
    # Lines after $Start that are blank or indented deeper than $Indent.
    $block = [Collections.Generic.List[string]]::new()
    for ($index = $Start + 1; $index -lt $Lines.Count; $index++) {
        $line = $Lines[$index]
        if ($line.Trim().Length -gt 0 -and ($line.Length - $line.TrimStart().Length) -le $Indent) { break }
        $block.Add($line)
    }
    $block
}

function Get-ScalarOrBlock([string[]]$Lines, [int]$Index, [string]$Value, [int]$Indent) {
    if ($Value -match '^[>|][+-]?$') {
        return ((Get-IndentedBlock $Lines $Index $Indent | ForEach-Object Trim) -join ' ').Trim()
    }
    $Value.Trim()
}

function Read-Workflow([IO.FileInfo]$File) {
    $lines = @(Get-Content -LiteralPath $File.FullName)
    $triggerIndex = Find-LineIndex $lines '^(on|"on"|''on''):\s*$'
    if ($triggerIndex -lt 0) { throw "$($File.Name) must declare its triggers as an 'on:' block." }
    $triggerBlock = @(Get-IndentedBlock $lines $triggerIndex 0)
    $events = @($triggerBlock | Where-Object { $_ -match '^  ([a-z_]+):' } | ForEach-Object { $Matches[1] })

    $jobs = [ordered]@{}
    $jobsIndex = Find-LineIndex $lines '^jobs:\s*$'
    if ($jobsIndex -ge 0) {
        for ($index = $jobsIndex + 1; $index -lt $lines.Count -and $lines[$index] -notmatch '^\S'; $index++) {
            if ($lines[$index] -notmatch '^  (?<name>[A-Za-z0-9_-]+):\s*(#.*)?$') { continue }
            $name = $Matches.name
            $body = @(Get-IndentedBlock $lines $index 2)
            $job = @{ Environment = $null; If = $null; Permissions = @(); Text = ($body -join "`n") }
            for ($line = 0; $line -lt $body.Count; $line++) {
                if ($body[$line] -match '^    environment:\s*(?<value>.*?)\s*$') {
                    if ($Matches.value -eq '' -or $Matches.value.StartsWith('{')) {
                        throw "$($File.Name) job '$name' must name its environment with the scalar 'environment: <name>' form."
                    }
                    $job.Environment = $Matches.value
                }
                elseif ($body[$line] -match '^    if:\s*(?<value>.*?)\s*$') {
                    $job.If = Get-ScalarOrBlock $body $line $Matches.value 4
                }
                elseif ($body[$line] -match '^    permissions:\s*$') {
                    $job.Permissions = @(Get-IndentedBlock $body $line 4 | ForEach-Object Trim | Where-Object { $_ })
                }
            }
            $jobs[$name] = $job
        }
    }
    [pscustomobject]@{ Name = $File.Name; Lines = $lines; Events = $events; TriggerBlock = $triggerBlock; Jobs = $jobs }
}

$workflows = @(Get-ChildItem -LiteralPath $resolvedDirectory -File -Include '*.yml', '*.yaml' -Recurse | Sort-Object Name | ForEach-Object { Read-Workflow $_ })
$found = @{}
foreach ($workflow in $workflows) {
    # Tags are created by publishing a reviewed draft release; they never start a run.
    # A push trigger without a branch filter also runs for tags.
    if ($workflow.Events -contains 'push') {
        $pushIndex = Find-LineIndex $workflow.TriggerBlock '^  push:'
        $push = @(Get-IndentedBlock $workflow.TriggerBlock $pushIndex 2)
        if (@($push | Where-Object { $_ -match '^    (tags|tags-ignore):' }).Count -gt 0 -or
            @($push | Where-Object { $_ -match '^    (branches|branches-ignore):' }).Count -eq 0) {
            throw "$($workflow.Name) must not run on tag pushes; filter its push trigger to branches only."
        }
    }

    foreach ($name in $workflow.Jobs.Keys) {
        $job = $workflow.Jobs[$name]
        $key = "$($workflow.Name):$name"
        $hasOidc = $job.Permissions -contains 'id-token: write'
        if ($null -eq $job.Environment) {
            if ($hasOidc) { throw "$key requests an OIDC token without a reviewed environment." }
            continue
        }
        $expected = $environmentJobs[$key]
        if ($null -eq $expected -or $job.Environment -cne $expected.Environment) {
            throw "$key uses environment '$($job.Environment)', which is not in the reviewed release-trust policy."
        }
        $found[$key] = $true
        foreach ($event in $workflow.Events) {
            if ($expected.Events -notcontains $event) {
                throw "$($workflow.Name) uses environment '$($job.Environment)' and must not be triggered by '$event'."
            }
        }
        if (-not $expected.Signing) { continue }
        $condition = [string]$job.If
        if (-not $condition.Contains($mainOnly, [StringComparison]::Ordinal) -or $condition.Contains('refs/tags', [StringComparison]::Ordinal)) {
            throw "$key must run only on main ($mainOnly) and never on tag refs."
        }
        $allowsOtherRefs = $condition.Replace($mainOnly, '').Contains('github.ref', [StringComparison]::Ordinal)
        if ($allowsOtherRefs -and -not ($expected.ContainsKey('ReleaseLines') -and $condition.Contains($releaseLine, [StringComparison]::Ordinal))) {
            throw "$key may admit only main$(if ($expected.ContainsKey('ReleaseLines')) { ' and release lines' })."
        }
    }
}
foreach ($key in $environmentJobs.Keys) {
    if (-not $found.ContainsKey($key)) { throw "Reviewed environment job '$key' is missing or names a different environment." }
}

# R2 credentials live only in the R2 environment and its one job, which signs
# catalogs with the catalog-only identity and never runs Authenticode signing.
foreach ($workflow in $workflows) {
    $references = [regex]::Matches(($workflow.Lines -join "`n"), 'secrets\.R2_').Count
    foreach ($name in $workflow.Jobs.Keys) {
        $job = $workflow.Jobs[$name]
        if ($job.Environment -ceq 'r2-publication') {
            if ($job.Text -match 'artifact-signing-action|actions/sign-package') {
                throw "$($workflow.Name):$name must not sign executables in the r2-publication environment."
            }
            $references -= [regex]::Matches($job.Text, 'secrets\.R2_').Count
        }
    }
    if ($references -ne 0) {
        throw "$($workflow.Name) references R2 credentials outside the r2-publication job."
    }
}

# The release workflow decides eligibility from the dispatched ref and leaves the tag
# to the maintainer, who creates it by publishing the draft release.
$release = $workflows | Where-Object Name -ceq 'release.yml'
if ($null -eq $release) { throw 'release.yml is missing.' }
$triggers = $release.TriggerBlock -join "`n"
if (($release.Events -join ',') -cne 'workflow_dispatch' -or
    $triggers -notmatch '(?m)^      version:\s*$' -or $triggers -notmatch '(?m)^      publish:\s*$') {
    throw 'release.yml must run only from workflow_dispatch with version and publish inputs.'
}
$versionJob = $release.Jobs['version']
if ($null -eq $versionJob -or $versionJob.Text -notmatch '\./eng/Resolve-ReleaseVersion\.ps1\s+-Ref \$env:GITHUB_REF\s') {
    throw 'release.yml must resolve eligibility with Resolve-ReleaseVersion.ps1 -Ref $env:GITHUB_REF.'
}
$writers = @($release.Jobs.Keys | Where-Object { $release.Jobs[$_].Permissions -contains 'contents: write' })
if (($writers -join ',') -cne 'publish') { throw 'Only the release.yml publish job may write repository contents.' }
$publish = $release.Jobs['publish']
if ($publish.If -notmatch '^inputs\.publish && needs\.version\.outputs\.eligible == ''true''$' -or
    $publish.Text -notmatch '''--draft''' -or $publish.Text -notmatch '''--target'',\s*\$env:GITHUB_SHA') {
    throw 'The release.yml publish job must create a draft release targeting the signed commit, only when publish is requested.'
}
# GitHub refuses release creation by the workflow token when the signed commit has
# workflow files that differ from main; the summary must give the maintainer's
# recovery for the same commit and signed files.
if ($publish.Text -notmatch ':refs/tags/\$tag' -or $publish.Text -notmatch 'Re-run failed jobs' -or $publish.Text -notmatch 'gh run download') {
    throw "The release.yml publish job must write the maintainer's recovery for the signed commit when the draft release is refused."
}

# The conformance soak (#450) is dispatched by hand and repeats the retained-client gate's
# own jobs. It may call only that reusable workflow, which runs only when called, and
# neither may use secrets, an environment, a write permission or an OIDC token, so a soak
# can never reach a release, signing or registry job. Both files are read as parsed YAML
# in the plain block subset WorkflowYaml.psm1 accepts. A construct outside it fails the
# check, with its file and line, before any rule below runs, so no spelling of a
# permission, a call or a secret escapes them.
Import-Module (Join-Path $PSScriptRoot 'WorkflowYaml.psm1') -Force

function Test-ReadOnlyPermissions([object]$Permissions, [switch]$Required) {
    # Only a mapping of scopes to read or none; a scalar such as write-all, read-all or
    # an empty value is not accepted.
    if ($null -eq $Permissions) { return -not $Required }
    if ($Permissions -isnot [Collections.Specialized.OrderedDictionary] -or $Permissions.Count -eq 0) { return $false }
    foreach ($entry in $Permissions.GetEnumerator()) {
        if ($entry.Value -isnot [string] -or $entry.Value -cnotin @('read', 'none') -or
            ($entry.Key -ceq 'id-token' -and $entry.Value -cne 'none')) {
            return $false
        }
    }
    $true
}

function Get-ExpressionTexts([object]$Node, [switch]$Condition) {
    # The text of every expression in the parsed workflow: each ${{ }} in a scalar, up to
    # its closing braces outside a string literal or to the end of the scalar, and the
    # whole value of an if condition, which Actions evaluates without the braces.
    if ($Node -is [string]) {
        if ($Condition) { return , $Node }
        $index = 0
        while (($start = $Node.IndexOf('${{', $index, [StringComparison]::Ordinal)) -ge 0) {
            $position = $start + 3
            $quoted = $false
            while ($position -lt $Node.Length) {
                if ($Node[$position] -eq "'") { $quoted = -not $quoted }
                elseif (-not $quoted -and $Node[$position] -eq '}' -and $position + 1 -lt $Node.Length -and $Node[$position + 1] -eq '}') { break }
                $position++
            }
            $Node.Substring($start + 3, $position - $start - 3)
            $index = $position + 2
        }
        return
    }
    if ($Node -is [Collections.Specialized.OrderedDictionary]) {
        foreach ($entry in $Node.GetEnumerator()) { Get-ExpressionTexts $entry.Value -Condition:($entry.Key -eq 'if') }
        return
    }
    if ($Node -is [Collections.IEnumerable]) { foreach ($item in $Node) { Get-ExpressionTexts $item } }
}

function Get-ReusableWorkflowFindings([string]$Name, [string[]]$Triggers, [string[]]$AllowedCalls) {
    # The ways a workflow could reach credentials or another workflow, as messages.
    $findings = [Collections.Generic.List[string]]::new()
    try {
        $yaml = ConvertFrom-WorkflowYaml -Path (Join-Path $resolvedDirectory $Name)
    }
    catch {
        $findings.Add("it uses a YAML form the policy does not read ($($_.Exception.Message))")
        return , $findings
    }
    foreach ($key in $yaml.Keys) {
        if ($key -cnotin @('name', 'run-name', 'on', 'permissions', 'concurrency', 'env', 'jobs')) { $findings.Add("it has a top-level '$key'") }
    }
    $on = $yaml['on']
    if ($on -isnot [Collections.Specialized.OrderedDictionary] -or (@($on.Keys) -join ',') -cne ($Triggers -join ',')) {
        $findings.Add("its triggers are not exactly $($Triggers -join ', ')")
    }
    elseif ($on[$Triggers[0]] -is [Collections.Specialized.OrderedDictionary] -and $on[$Triggers[0]].Contains('secrets')) {
        $findings.Add('it declares secrets')
    }
    if (-not (Test-ReadOnlyPermissions $yaml['permissions'] -Required)) { $findings.Add('its workflow permissions are not a read-only mapping') }
    $calls = [Collections.Generic.List[string]]::new()
    $jobs = $yaml['jobs']
    if ($jobs -isnot [Collections.Specialized.OrderedDictionary] -or $jobs.Count -eq 0) {
        $findings.Add('it has no jobs mapping')
        $jobs = [ordered]@{}
    }
    foreach ($entry in $jobs.GetEnumerator()) {
        $job = $entry.Value
        if ($job -isnot [Collections.Specialized.OrderedDictionary]) { $findings.Add("job '$($entry.Key)' is not a mapping"); continue }
        if ($job.Contains('permissions') -and -not (Test-ReadOnlyPermissions $job['permissions'] -Required)) {
            $findings.Add("job '$($entry.Key)' asks for more than read permissions")
        }
        foreach ($forbidden in @('environment', 'secrets')) {
            if ($job.Contains($forbidden)) { $findings.Add("job '$($entry.Key)' has $forbidden") }
        }
        if ($job.Contains('uses')) { $calls.Add([string]$job['uses']) }
        foreach ($step in @(if ($job['steps'] -is [Collections.IEnumerable] -and $job['steps'] -isnot [string]) { $job['steps'] })) {
            if ($step -is [Collections.Specialized.OrderedDictionary] -and $step['uses'] -is [string] -and
                $step['uses'].Contains('.github/workflows/', [StringComparison]::OrdinalIgnoreCase)) {
                $findings.Add("a step of job '$($entry.Key)' calls a workflow")
            }
        }
    }
    if ((@($calls) -join ',') -cne ($AllowedCalls -join ',')) {
        $findings.Add("its workflow calls are not exactly $(if ($AllowedCalls.Count) { $AllowedCalls -join ', ' } else { 'none' })")
    }
    # Actions resolves context names without regard to case, so the name is matched the
    # same way, anywhere in an expression, string literals included.
    if (@(Get-ExpressionTexts $yaml | Where-Object { $_ -match '(?i)\bsecrets\b' }).Count -gt 0) {
        $findings.Add('an expression refers to secrets')
    }
    , $findings
}

function Get-PassedValueType([object]$Scalar, [object]$DispatchInputs) {
    # The type a value in a job's with: mapping reaches the called workflow as, from its
    # YAML form and, for an expression, from what it names; $null when it cannot be
    # told. A workflow_dispatch number input reaches the inputs context as a string,
    # so only fromJSON() of one is a number.
    if ($null -eq $Scalar -or $Scalar -isnot [pscustomobject]) { return $null }
    if ($Scalar.Style -cne 'plain') { return 'string' }
    $text = [string]$Scalar.Text
    $expression = [regex]::Match($text, '\A\$\{\{\s*(?<body>(?:(?!\}\}).)*?)\s*\}\}\z')
    if ($expression.Success) {
        $body = $expression.Groups['body'].Value
        $direct = [regex]::Match($body, '\Ainputs\.(?<name>[A-Za-z_][A-Za-z0-9_-]*)\z')
        $parsed = [regex]::Match($body, '\A(?i:fromJSON)\(\s*inputs\.(?<name>[A-Za-z_][A-Za-z0-9_-]*)\s*\)\z')
        $name = $(if ($direct.Success) { $direct.Groups['name'].Value } elseif ($parsed.Success) { $parsed.Groups['name'].Value })
        if ($null -eq $name -or $DispatchInputs -isnot [Collections.Specialized.OrderedDictionary] -or -not $DispatchInputs.Contains($name)) {
            return $null
        }
        $declared = $DispatchInputs[$name]
        $type = $(if ($declared -is [Collections.Specialized.OrderedDictionary] -and $declared.Contains('type')) { [string]$declared['type'].Text } else { 'string' })
        if ($direct.Success) {
            return @{ string = 'string'; choice = 'string'; number = 'string'; boolean = 'boolean' }[$type]
        }
        return @{ number = 'number'; boolean = 'boolean' }[$type]
    }
    # Text around an expression makes a string.
    if ($text.Contains('${{')) { return 'string' }
    if ($text -match '\A(?:true|false)\z') { return 'boolean' }
    if ($text -cmatch '\A(?:[-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][-+]?[0-9]+)?|0x[0-9A-Fa-f]+|0o[0-7]+|[-+]?\.(?:inf|Inf|INF)|\.(?:nan|NaN|NAN))\z') {
        return 'number'
    }
    if ($text -cmatch '\A(?:null|Null|NULL|~)\z') { return $null }
    'string'
}

function Get-ReusableCallTypeFindings([string]$Name) {
    # Each value a job of this dispatch-only workflow passes to a reusable workflow in
    # this repository must reach it with the type the called workflow declares, and
    # every input the called workflow requires must be passed. A workflow the reader
    # does not accept is reported by the other checks.
    $findings = [Collections.Generic.List[string]]::new()
    try {
        $yaml = ConvertFrom-WorkflowYaml -Path (Join-Path $resolvedDirectory $Name) -ScalarInfo
    }
    catch {
        return , $findings
    }
    $on = $yaml['on']
    $dispatch = $(if ($on -is [Collections.Specialized.OrderedDictionary] -and $on['workflow_dispatch'] -is [Collections.Specialized.OrderedDictionary]) {
            $on['workflow_dispatch']['inputs'] })
    $jobs = $(if ($yaml['jobs'] -is [Collections.Specialized.OrderedDictionary]) { $yaml['jobs'] } else { [ordered]@{} })
    foreach ($job in $jobs.GetEnumerator()) {
        if ($job.Value -isnot [Collections.Specialized.OrderedDictionary] -or $job.Value['uses'] -isnot [pscustomobject]) { continue }
        $local = [regex]::Match([string]$job.Value['uses'].Text, '\A\./\.github/workflows/(?<file>[A-Za-z0-9_.-]+\.ya?ml)\z')
        if (-not $local.Success) { continue }
        $file = $local.Groups['file'].Value
        try {
            $called = ConvertFrom-WorkflowYaml -Path (Join-Path $resolvedDirectory $file)
            $declared = $called['on']['workflow_call']['inputs']
        }
        catch {
            # The called workflow's own check reports the form it was written in; only
            # the retained-client gate may be called at all.
            continue
        }
        if ($declared -isnot [Collections.Specialized.OrderedDictionary]) { $declared = [ordered]@{} }
        $with = $(if ($job.Value['with'] -is [Collections.Specialized.OrderedDictionary]) { $job.Value['with'] } else { [ordered]@{} })
        foreach ($entry in $with.GetEnumerator()) {
            if (-not $declared.Contains($entry.Key)) {
                $findings.Add("job '$($job.Key)' passes '$($entry.Key)', which $file does not declare")
                continue
            }
            $want = [string]$declared[$entry.Key]['type']
            $have = Get-PassedValueType $entry.Value $dispatch
            $line = $(if ($entry.Value -is [pscustomobject]) { " at $Name line $($entry.Value.Line)" })
            if ($null -eq $have) {
                $findings.Add("job '$($job.Key)' passes '$($entry.Key)'$line a value whose type the policy cannot tell")
            }
            elseif ($have -cne $want) {
                $hint = $(if ($have -ceq 'string' -and $want -cne 'string' -and [string]$entry.Value.Text -match '\A\$\{\{\s*inputs\.') {
                        '; a workflow_dispatch number input reaches the inputs context as a string, so pass fromJSON() of it' })
                $findings.Add("job '$($job.Key)' passes a $have to the $want input '$($entry.Key)' of $file$line$hint")
            }
        }
        foreach ($input in $declared.GetEnumerator()) {
            if ($input.Value -is [Collections.Specialized.OrderedDictionary] -and [string]$input.Value['required'] -ceq 'true' -and
                -not $with.Contains($input.Key)) {
                $findings.Add("job '$($job.Key)' does not pass the input '$($input.Key)' that $file requires")
            }
        }
    }
    , $findings
}

$soakFindings = Get-ReusableWorkflowFindings 'conformance-soak.yml' @('workflow_dispatch') @('./.github/workflows/retained-client-compatibility.yml')
foreach ($finding in (Get-ReusableCallTypeFindings 'conformance-soak.yml')) { $soakFindings.Add($finding) }
if ($soakFindings.Count -gt 0) {
    throw "conformance-soak.yml must run only from workflow_dispatch, call only retained-client-compatibility.yml, pass it each input with the type it declares, and use no secrets, environment, write permissions or OIDC token: $($soakFindings -join '; ')."
}
$gateFindings = Get-ReusableWorkflowFindings 'retained-client-compatibility.yml' @('workflow_call') @()
if ($gateFindings.Count -gt 0) {
    throw "retained-client-compatibility.yml must run only when called, call no other workflow, and use no secrets, environment, write permissions or OIDC token: $($gateFindings -join '; ')."
}

Write-Host (
    'Release workflows never run on tag pushes, name environments only in reviewed jobs, ' +
    'limit signing to main or a release line, keep R2 credentials in the R2 environment, ' +
    'and leave tag creation to the published draft release. The conformance soak runs only ' +
    'by dispatch and only the retained-client gate''s jobs, passing each of its inputs with the declared type, without secrets or write permissions.')
