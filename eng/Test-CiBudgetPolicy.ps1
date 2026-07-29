[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$measureScript = Join-Path $PSScriptRoot "Measure-CiBudget.ps1"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-ci-budget-$([Guid]::NewGuid().ToString('N'))"

try {
    & $measureScript `
        -Metric restore `
        -ObservedValue 300 `
        -OutputDirectory (Join-Path $temporaryRoot "at-limit") `
        -Quiet

    $overLimitRejected = $false
    try {
        & $measureScript `
            -Metric restore `
            -ObservedValue 300.0004 `
            -OutputDirectory (Join-Path $temporaryRoot "over-limit") `
            -Quiet
    }
    catch {
        $overLimitRejected = $true
    }
    if (-not $overLimitRejected) {
        throw "A raw measurement above the budget was accepted after display rounding."
    }

    $reportPath = Join-Path $temporaryRoot "over-limit/restore.json"
    $report = Get-Content -Raw -LiteralPath $reportPath | ConvertFrom-Json
    if ($report.observed -ne 300 -or
        $report.maximum -ne 300 -or
        $report.status -ne "over_budget" -or
        $report.budget_status -ne "over_budget" -or
        $report.command_exit_code -ne 0) {
        throw "The over-budget boundary report did not preserve the reviewed result contract."
    }

    $newBudgetBoundaries = [ordered]@{
        "package-size" = 268435456
        "startup-working-set" = 536870912
        "dispatch-p95" = 250
        "request-mapping-p95" = 50
        "discovery-p95" = 50
        "retained-managed-memory" = 33554432
    }
    foreach ($entry in $newBudgetBoundaries.GetEnumerator()) {
        & $measureScript `
            -Metric $entry.Key `
            -ObservedValue $entry.Value `
            -OutputDirectory (Join-Path $temporaryRoot "new-at-limit") `
            -Quiet

        $justOverRejected = $false
        try {
            & $measureScript `
                -Metric $entry.Key `
                -ObservedValue ([double]$entry.Value + 1) `
                -OutputDirectory (Join-Path $temporaryRoot "new-over-limit") `
                -Quiet
        }
        catch {
            $justOverRejected = $true
        }
        if (-not $justOverRejected) {
            throw "Budget '$($entry.Key)' accepted a raw value above its reviewed maximum."
        }
    }

    $byteCommandRejected = $false
    try {
        & $measureScript `
            -Metric package-size `
            -Executable pwsh `
            -ArgumentList @("-NoLogo", "-NoProfile", "-Command", "exit 0") `
            -OutputDirectory (Join-Path $temporaryRoot "invalid-command-unit") `
            -Quiet
    }
    catch {
        $byteCommandRejected = $true
    }
    if (-not $byteCommandRejected) {
        throw "A byte budget accepted command-duration measurement."
    }

    $policyPath = Join-Path $PSScriptRoot "full-surface-policy.json"
    $schemaPath = Join-Path $PSScriptRoot "schemas/full-surface-policy.schema.json"
    $multiShardPolicy = Get-Content -Raw -LiteralPath $policyPath | ConvertFrom-Json -Depth 100
    $multiShardPolicy.sharding.shard_count = 2
    $multiShardJson = $multiShardPolicy | ConvertTo-Json -Depth 100
    if (Test-Json -Json $multiShardJson -SchemaFile $schemaPath -ErrorAction SilentlyContinue) {
        throw "The policy schema accepted multiple shards without a checked CI matrix."
    }

    Write-Host "CI budget boundaries and fail-closed sharding policy checks passed."
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedTemporaryRoot).StartsWith(
            "briosa-ci-budget-",
            [StringComparison]::Ordinal) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
