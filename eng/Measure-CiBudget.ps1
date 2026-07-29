[CmdletBinding(DefaultParameterSetName = "Command")]
param(
    [Parameter(Mandatory)]
    [ValidateSet(
        "restore",
        "generation",
        "compile",
        "test",
        "package",
        "startup",
        "descriptor-size")]
    [string]$Metric,

    [Parameter(Mandatory, ParameterSetName = "Command")]
    [string]$Executable,

    [Parameter(ParameterSetName = "Command")]
    [string[]]$ArgumentList = @(),

    [Parameter(Mandatory, ParameterSetName = "Observed")]
    [ValidateRange(0, [double]::MaxValue)]
    [double]$ObservedValue,

    [string]$PolicyPath,

    [string]$OutputDirectory = "artifacts/ci-metrics",

    [switch]$Quiet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PolicyPath)) {
    $PolicyPath = Join-Path $PSScriptRoot "full-surface-policy.json"
}
else {
    $PolicyPath = [IO.Path]::GetFullPath($PolicyPath, $repositoryRoot)
}

$schemaPath = Join-Path $PSScriptRoot "schemas/full-surface-policy.schema.json"
$policyJson = Get-Content -Raw -LiteralPath $PolicyPath
if (-not (Test-Json -Json $policyJson -SchemaFile $schemaPath)) {
    throw "Full-surface policy schema validation failed: $PolicyPath"
}
$policy = $policyJson | ConvertFrom-Json -Depth 100
if ($policy.'$schema' -cne "schemas/full-surface-policy.schema.json" -or
    $policy.schema_version -ne 1) {
    throw "Full-surface policy identity is invalid."
}
$budget = @($policy.budgets | Where-Object metric -CEQ $Metric)
if ($budget.Count -ne 1) {
    throw "Full-surface policy must define exactly one '$Metric' budget."
}

$commandExitCode = 0
if ($PSCmdlet.ParameterSetName -eq "Command") {
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    & $Executable @ArgumentList
    $commandExitCode = $LASTEXITCODE
    $stopwatch.Stop()
    $ObservedValue = $stopwatch.Elapsed.TotalSeconds
}

$roundedValue = [Math]::Round($ObservedValue, 3, [MidpointRounding]::AwayFromZero)
$maximum = [double]$budget[0].maximum
$budgetStatus = if ($ObservedValue -le $maximum) { "within_budget" } else { "over_budget" }
$status = if ($commandExitCode -ne 0) { "command_failed" } else { $budgetStatus }
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot)
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$outputPath = Join-Path $outputRoot "$Metric.json"
$result = [ordered]@{
    schema_version = 1
    metric = $Metric
    unit = $budget[0].unit
    observed = $roundedValue
    maximum = $maximum
    status = $status
    budget_status = $budgetStatus
    command_exit_code = $commandExitCode
    policy = "eng/full-surface-policy.json"
    review_path = $budget[0].review_path
}
$json = ($result | ConvertTo-Json -Depth 10).Replace("`r`n", "`n") + "`n"
[IO.File]::WriteAllText($outputPath, $json, [Text.UTF8Encoding]::new($false))

if (-not $Quiet) {
    Write-Host (
        "CI budget metric '{0}': {1} {2} (maximum {3}; {4}). Report: {5}" -f
        $Metric,
        $roundedValue,
        $budget[0].unit,
        $maximum,
        $budgetStatus,
        $outputPath)
}

if ($commandExitCode -ne 0) {
    throw "Measured command for '$Metric' failed with exit code $commandExitCode."
}
if ($budgetStatus -eq "over_budget") {
    throw (
        "CI budget '$Metric' exceeded using the raw measurement. " +
        "Reported value (rounded): $roundedValue $($budget[0].unit); maximum: " +
        "$maximum $($budget[0].unit). Review $($budget[0].review_path) before changing the policy.")
}
