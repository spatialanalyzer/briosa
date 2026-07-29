[CmdletBinding()]
param(
    [string]$WorkflowPath = (Join-Path $PSScriptRoot "../.github/workflows/ci.yml")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$resolvedWorkflowPath = [IO.Path]::GetFullPath($WorkflowPath)
if (-not (Test-Path -LiteralPath $resolvedWorkflowPath -PathType Leaf)) {
    throw "Ordinary CI workflow '$resolvedWorkflowPath' does not exist."
}

$expectedPrefix = @(
    "name: CI",
    "",
    "on:",
    "  push:",
    "    branches:",
    "      - main",
    "  pull_request:",
    "    branches:",
    "      - main",
    "",
    "concurrency:",
    '  group: ci-${{ github.workflow }}-${{ github.event.pull_request.number || github.ref }}',
    "  cancel-in-progress: true",
    "",
    "permissions:",
    "  contents: read"
)

$actualLines = @(Get-Content -LiteralPath $resolvedWorkflowPath)
if ($actualLines.Count -lt $expectedPrefix.Count) {
    throw "Ordinary CI workflow is missing the reviewed trigger, concurrency, or permission policy."
}

for ($index = 0; $index -lt $expectedPrefix.Count; $index++) {
    if ($actualLines[$index] -cne $expectedPrefix[$index]) {
        throw (
            "Ordinary CI workflow policy differs at line {0}. Expected '{1}', found '{2}'." -f
            ($index + 1),
            $expectedPrefix[$index],
            $actualLines[$index])
    }
}

Write-Host (
    "Ordinary CI workflow runs pull-request validation once, limits push validation to main, " +
    "cancels superseded runs, and retains read-only contents permission.")
