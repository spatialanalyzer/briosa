[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$workflowPath = Join-Path $repositoryRoot ".github\workflows\licensed-sa.yml"
if (-not (Test-Path -LiteralPath $workflowPath -PathType Leaf)) {
    throw "The licensed SpatialAnalyzer workflow is missing."
}

$workflow = Get-Content -LiteralPath $workflowPath -Raw

function Assert-WorkflowPattern {
    param(
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Message
    )

    if ($workflow -notmatch $Pattern) {
        throw $Message
    }
}

Assert-WorkflowPattern '(?m)^  workflow_dispatch:\s*$' `
    "The licensed workflow must be manually dispatched."
if ($workflow -match
    '(?m)^  (pull_request|pull_request_target|push|workflow_run|repository_dispatch):') {
    throw "The licensed workflow must not accept automatic or untrusted triggers."
}

Assert-WorkflowPattern "github\.repository == 'spatialanalyzer/briosa'" `
    "The licensed workflow must pin the repository identity."
Assert-WorkflowPattern "github\.ref == 'refs/heads/main'" `
    "The licensed workflow must reject non-main refs."
Assert-WorkflowPattern "github\.event_name == 'workflow_dispatch'" `
    "The licensed workflow must reject other event types."
Assert-WorkflowPattern '(?m)^  group: licensed-sa-2026-1-0529-7\s*$' `
    "The licensed workflow must serialize exact-target runs."
Assert-WorkflowPattern '(?m)^  cancel-in-progress: false\s*$' `
    "An in-flight licensed run must never be cancelled by a later dispatch."

$protectedJobMatch = [regex]::Match(
    $workflow,
    '(?ms)^  licensed-sa:\r?\n(?<job>.*)\z')
if (-not $protectedJobMatch.Success) {
    throw "The protected licensed-sa job is missing or is not the final job."
}

$protectedJob = $protectedJobMatch.Groups['job'].Value
foreach ($requiredPattern in @(
        '(?m)^    environment: licensed-sa-2026-1-0529-7\s*$',
        '(?m)^      group: briosa-licensed-sa\s*$',
        '(?m)^      labels: \[self-hosted, windows, x64, briosa-licensed, sa-2026-1-0529-7\]\s*$',
        'actions/download-artifact@',
        'Test-LicensedRunnerState\.ps1',
        '-ConfirmLicensedSpatialAnalyzerTest',
        '(?m)^        if: always\(\)\s*$')) {
    if ($protectedJob -notmatch $requiredPattern) {
        throw "The protected licensed-sa job is missing required policy '$requiredPattern'."
    }
}

if ($protectedJob -match 'actions/checkout@') {
    throw "The licensed runner must not check out repository content."
}

Assert-WorkflowPattern '(?ms)^  prepare:\r?\n.*?^    runs-on: windows-latest\s*$' `
    "The payload must be prepared on a GitHub-hosted Windows runner."
Assert-WorkflowPattern 'persist-credentials: false' `
    "The hosted checkout must not persist Git credentials."
Assert-WorkflowPattern 'payload_sha256' `
    "The protected job must verify the hosted payload hash."

$actionReferences = [regex]::Matches(
    $workflow,
    '(?m)^\s*-?\s*uses:\s+[^@\s]+@(?<reference>[^\s#]+)')
if ($actionReferences.Count -eq 0) {
    throw "The licensed workflow did not declare any action references."
}

foreach ($actionReference in $actionReferences) {
    if ($actionReference.Groups['reference'].Value -notmatch '^[0-9a-f]{40}$') {
        throw "Every action in the licensed workflow must use an immutable commit SHA."
    }
}

Write-Host "Licensed SpatialAnalyzer workflow policy verification passed."
