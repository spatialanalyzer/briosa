[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("Preflight", "Postflight")]
    [string]$Phase,

    [switch]$RequireGitHubRunner
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$issues = [Collections.Generic.List[string]]::new()

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    $issues.Add("WINDOWS_X64_REQUIRED")
}

if ($RequireGitHubRunner -and (
        $env:GITHUB_ACTIONS -ne "true" -or
        $env:RUNNER_OS -ne "Windows" -or
        $env:RUNNER_ARCH -ne "X64")) {
    $issues.Add("GITHUB_RUNNER_IDENTITY_INVALID")
}

$spatialAnalyzerProcesses = @(
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -eq "Spatial Analyzer64.exe" -and
            $_.ExecutablePath -like
                "*\SpatialAnalyzer 2026.1.0529.7\x64\Spatial Analyzer64.exe"
        })
$briosaProcesses = @(
    Get-Process -Name "Briosa.Server", "Briosa.Worker" `
        -ErrorAction SilentlyContinue)
$sdkProcesses = @(
    Get-Process -Name "SpatialAnalyzerSDK" -ErrorAction SilentlyContinue)

if ($spatialAnalyzerProcesses.Count -ne 1) {
    $issues.Add("EXACT_SA_INSTANCE_COUNT_INVALID")
}

if ($briosaProcesses.Count -ne 0) {
    $issues.Add("RESIDUAL_BRIOSA_PROCESS")
}

if ($sdkProcesses.Count -ne 0) {
    $issues.Add("RESIDUAL_SDK_PROCESS")
}

$report = [ordered]@{
    phase = $Phase.ToLowerInvariant()
    success = $issues.Count -eq 0
    exact_sa_instance_count = $spatialAnalyzerProcesses.Count
    briosa_process_count = $briosaProcesses.Count
    sdk_process_count = $sdkProcesses.Count
    issue_codes = @($issues)
}

$report | ConvertTo-Json -Compress | Write-Host

if ($issues.Count -ne 0) {
    throw "Licensed runner state check failed ($Phase): $($issues -join ', ')."
}
