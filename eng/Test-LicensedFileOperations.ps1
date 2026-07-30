[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet(
        "save",
        "save_as",
        "save_as_read_only_template",
        "export_ascii_points",
        "export_ascii_point_set_wrong_type",
        "export_event_ref_list",
        "import_nominals_xml",
        "merge_measurements_xml",
        "output_report_pdf",
        "import_vstars_cameras",
        "import_polyworks")]
    [string]$Scenario,

    [string]$FixtureDescriptor,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ObjectiveSARoot,

    [Parameter(Mandatory)]
    [switch]$ConfirmLicensedSpatialAnalyzerTest,

    [ValidateRange(10, 60)]
    [int]$WatchdogSeconds = 45,

    [string]$SpatialAnalyzerPath =
        "C:\Program Files (x86)\New River Kinematics\SpatialAnalyzer 2026.1.0529.7\x64\Spatial Analyzer64.exe",

    [string]$SpatialAnalyzerSdkPath =
        "C:\Program Files (x86)\New River Kinematics\SpatialAnalyzer 2026.1.0529.7\SpatialAnalyzerSDK.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $ConfirmLicensedSpatialAnalyzerTest) {
    throw "Pass -ConfirmLicensedSpatialAnalyzerTest to acknowledge the licensed local test boundary."
}

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw "The licensed file-operation tests require 64-bit Windows."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$matrixPath = Join-Path $repositoryRoot `
    "tests\Briosa.SpatialAnalyzer.IntegrationTests\file-operation-matrix.json"
$matrixSchemaPath = Join-Path $repositoryRoot `
    "tests\Briosa.SpatialAnalyzer.IntegrationTests\file-operation-matrix.schema.json"
$fixtureSchemaPath = Join-Path $repositoryRoot `
    "tests\Briosa.SpatialAnalyzer.IntegrationTests\fixture-descriptor.schema.json"
$matrixJson = Get-Content -LiteralPath $matrixPath -Raw
if (-not (Test-Json -Json $matrixJson -SchemaFile $matrixSchemaPath)) {
    throw "The licensed file-operation matrix is invalid."
}
$matrix = $matrixJson | ConvertFrom-Json
$expectedVersion = [string]$matrix.spatial_analyzer_target
$expectedObjectiveSACommit = [string]$matrix.objectivesa_commit
if ($matrix.scenarios.scenario -cnotcontains $Scenario) {
    throw "The selected scenario is not present in the licensed-test matrix."
}
$resolvedObjectiveSA = [IO.Path]::GetFullPath($ObjectiveSARoot)
$objectiveProject = Join-Path $resolvedObjectiveSA "ObjectiveSA\ObjectiveSA.csproj"
$testProject = Join-Path $repositoryRoot `
    "tests\Briosa.SpatialAnalyzer.IntegrationTests\Briosa.SpatialAnalyzer.IntegrationTests.csproj"
$testExecutable = Join-Path $repositoryRoot `
    "tests\Briosa.SpatialAnalyzer.IntegrationTests\bin\Release\net8.0-windows\Briosa.SpatialAnalyzer.IntegrationTests.exe"
$resolvedSa = [IO.Path]::GetFullPath($SpatialAnalyzerPath)
$resolvedSdk = [IO.Path]::GetFullPath($SpatialAnalyzerSdkPath)
$resolvedFixture = if ([string]::IsNullOrWhiteSpace($FixtureDescriptor)) {
    $null
}
else {
    [IO.Path]::GetFullPath($FixtureDescriptor)
}

foreach ($requiredFile in @($objectiveProject, $resolvedSa, $resolvedSdk)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "A required licensed-test input is unavailable."
    }
}

$objectiveSafeDirectory = $resolvedObjectiveSA.Replace('\', '/')
$objectiveCommit = (& git -c "safe.directory=$objectiveSafeDirectory" `
        -C $resolvedObjectiveSA rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $objectiveCommit -cne $expectedObjectiveSACommit) {
    throw "The ObjectiveSA checkout does not match the pinned commit."
}
$objectiveTrackedChanges = @(& git -c "safe.directory=$objectiveSafeDirectory" `
        -C $resolvedObjectiveSA status --porcelain)
if ($LASTEXITCODE -ne 0 -or $objectiveTrackedChanges.Count -ne 0) {
    throw "The pinned ObjectiveSA working tree is not clean."
}

if ($null -ne $resolvedFixture -and
    -not (Test-Path -LiteralPath $resolvedFixture -PathType Leaf)) {
    throw "The fixture descriptor is unavailable."
}

$requiresFixture = $Scenario -in @(
    "export_event_ref_list",
    "import_nominals_xml",
    "merge_measurements_xml",
    "import_vstars_cameras",
    "import_polyworks")
if ($requiresFixture -and $null -eq $resolvedFixture) {
    throw "The selected scenario requires a local fixture descriptor."
}
if ($null -ne $resolvedFixture) {
    $fixtureJson = Get-Content -LiteralPath $resolvedFixture -Raw
    if (-not (Test-Json -Json $fixtureJson -SchemaFile $fixtureSchemaPath)) {
        throw "The local fixture descriptor is invalid."
    }
    $fixture = $fixtureJson | ConvertFrom-Json
    $requiresJob = $Scenario -in @(
        "export_event_ref_list",
        "merge_measurements_xml",
        "import_polyworks")
    $requiresInput = $Scenario -in @(
        "import_nominals_xml",
        "merge_measurements_xml",
        "import_vstars_cameras",
        "import_polyworks")
    $requiresObject = $Scenario -in @(
        "merge_measurements_xml",
        "import_polyworks")
    if ($requiresJob -and [string]::IsNullOrWhiteSpace($fixture.job_path)) {
        throw "The selected scenario requires a disposable SA job fixture."
    }
    if ($requiresInput -and [string]::IsNullOrWhiteSpace($fixture.input_path)) {
        throw "The selected scenario requires an input-file fixture."
    }
    if ($requiresObject -and $null -eq $fixture.object) {
        throw "The selected scenario requires an SA object fixture."
    }
    if ($Scenario -ceq "export_event_ref_list" -and
        @($fixture.items).Count -eq 0) {
        throw "The selected scenario requires an SA event-list fixture."
    }
    if ($Scenario -ceq "export_event_ref_list" -and
        @($fixture.items | Where-Object type -cne "Event").Count -ne 0) {
        throw "The event-list scenario requires Event item-type literals."
    }
    if ($Scenario -ceq "merge_measurements_xml" -and
        $fixture.object.type -cne "Point_Group") {
        throw "The XML-merge scenario requires a Point_Group object literal."
    }
    if ($Scenario -ceq "import_polyworks" -and
        $fixture.object.type -cne "Cloud") {
        throw "The Polyworks scenario requires a Cloud object literal."
    }
    foreach ($pathValue in @($fixture.job_path, $fixture.input_path)) {
        if (-not [string]::IsNullOrWhiteSpace($pathValue) -and
            (-not [IO.Path]::IsPathFullyQualified($pathValue) -or
            -not (Test-Path -LiteralPath $pathValue -PathType Leaf))) {
            throw "Fixture file paths must be absolute existing files."
        }
    }
}

$expectedVersionParts = @($expectedVersion.Split('.') |
        ForEach-Object { [int]$_ })
foreach ($binary in @($resolvedSa, $resolvedSdk)) {
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($binary).FileVersion
    $actualVersionParts = @($version -split '[,.]' |
            ForEach-Object { [int]$_.Trim() })
    if ($actualVersionParts.Count -ne $expectedVersionParts.Count -or
        ($actualVersionParts -join '.') -cne
            ($expectedVersionParts -join '.')) {
        throw "The licensed-test binary does not match the exact target."
    }
}

$trackedProcessNames = @(
    "Spatial Analyzer64",
    "SpatialAnalyzerSDK",
    "Briosa.Server",
    "Briosa.Worker")
foreach ($name in $trackedProcessNames) {
    if (@(Get-Process -Name $name -ErrorAction SilentlyContinue).Count -ne 0) {
        throw "Licensed file-operation preflight requires zero relevant processes."
    }
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} `
    "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
    throw "Visual Studio discovery is unavailable."
}

$msbuildCandidates = @(
    & $vswhere -latest -requires Microsoft.Component.MSBuild `
        -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1)
if ($msbuildCandidates.Count -ne 1 -or
    -not (Test-Path -LiteralPath $msbuildCandidates[0] -PathType Leaf)) {
    throw "MSBuild is unavailable."
}
$msbuild = [string]$msbuildCandidates[0]

& $msbuild $objectiveProject /restore /t:Rebuild /p:Configuration=Release `
    /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) {
    throw "ObjectiveSA failed to rebuild."
}

& $msbuild $testProject /restore /t:Rebuild /p:Configuration=Release `
    "/p:ObjectiveSARoot=$resolvedObjectiveSA" /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) {
    throw "The licensed file-operation runner failed to rebuild."
}

if (-not (Test-Path -LiteralPath $testExecutable -PathType Leaf)) {
    throw "The licensed file-operation runner is unavailable."
}

$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase `
    "briosa-file-operation-$([Guid]::NewGuid().ToString('N'))"
$saProcess = $null
$runnerProcess = $null
$runnerExited = $false
$runnerExitCode = $null
$runnerResult = $null
$executionError = $null
$sdkCleanupForced = $false
$saCleanupForced = $false
$cleanupErrors = [Collections.Generic.List[string]]::new()

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    $shell = New-Object -ComObject Shell.Application
    $shell.ShellExecute(
        $resolvedSa,
        "",
        (Split-Path -Parent $resolvedSa),
        "open",
        1)

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(45)
    do {
        Start-Sleep -Milliseconds 500
        $saProcesses = @(Get-Process -Name "Spatial Analyzer64" `
                -ErrorAction SilentlyContinue)
    } while ($saProcesses.Count -eq 0 -and
        [DateTimeOffset]::UtcNow -lt $deadline)

    if ($saProcesses.Count -ne 1 -or
        -not [string]::Equals(
            $saProcesses[0].MainModule.FileName,
            $resolvedSa,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "The exact disposable SpatialAnalyzer process did not start."
    }
    $saProcess = $saProcesses[0]

    $listenerDeadline = [DateTimeOffset]::UtcNow.AddSeconds(45)
    do {
        Start-Sleep -Milliseconds 500
        $listeners = @(Get-NetTCPConnection -State Listen `
                -OwningProcess $saProcess.Id -ErrorAction SilentlyContinue |
                Where-Object LocalPort -eq 902)
    } while ($listeners.Count -ne 1 -and
        [DateTimeOffset]::UtcNow -lt $listenerDeadline)
    if ($listeners.Count -ne 1) {
        throw "The disposable exact-target process did not expose the observed SDK channel."
    }

    $runnerStart = [Diagnostics.ProcessStartInfo]::new()
    $runnerStart.FileName = $testExecutable
    $runnerStart.UseShellExecute = $false
    $runnerStart.CreateNoWindow = $true
    $runnerStart.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $runnerStart.RedirectStandardOutput = $true
    $runnerStart.RedirectStandardError = $true
    $runnerStart.ArgumentList.Add($temporaryRoot)
    $runnerStart.ArgumentList.Add($Scenario)
    if ($null -ne $resolvedFixture) {
        $runnerStart.ArgumentList.Add($resolvedFixture)
    }
    $runnerProcess = [Diagnostics.Process]::new()
    $runnerProcess.StartInfo = $runnerStart
    if (-not $runnerProcess.Start()) {
        throw "The licensed file-operation runner did not start."
    }
    $runnerOutputTask = $runnerProcess.StandardOutput.ReadToEndAsync()
    $runnerErrorTask = $runnerProcess.StandardError.ReadToEndAsync()
    $runnerExited = $runnerProcess.WaitForExit($WatchdogSeconds * 1000)
    if (-not $runnerExited) {
        Stop-Process -Id $runnerProcess.Id -Force
        $runnerProcess.WaitForExit()
    }
    else {
        $runnerExitCode = $runnerProcess.ExitCode
    }
    $runnerOutput = $runnerOutputTask.GetAwaiter().GetResult()
    $null = $runnerErrorTask.GetAwaiter().GetResult()

    if ($runnerExited) {
        $resultLines = @($runnerOutput -split "`r?`n" |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($resultLines.Count -eq 1) {
            $runnerResult = $resultLines[0] | ConvertFrom-Json
        }
    }
}
catch {
    $executionError = "test_execution_failed"
}
finally {
    try {
        if ($null -ne $runnerProcess -and -not $runnerProcess.HasExited) {
            Stop-Process -Id $runnerProcess.Id -Force
            $runnerProcess.WaitForExit()
        }
    }
    catch {
        $cleanupErrors.Add("runner_cleanup_failed")
    }

    try {
        $sdkProcesses = @(Get-Process -Name "SpatialAnalyzerSDK" `
                -ErrorAction SilentlyContinue)
        foreach ($sdkProcess in $sdkProcesses) {
            if (-not [string]::Equals(
                    $sdkProcess.MainModule.FileName,
                    $resolvedSdk,
                    [StringComparison]::OrdinalIgnoreCase)) {
                $cleanupErrors.Add("unexpected_sdk_process")
                continue
            }
            $null = $sdkProcess.CloseMainWindow()
            if (-not $sdkProcess.WaitForExit(10000)) {
                Stop-Process -Id $sdkProcess.Id -Force
                $sdkProcess.WaitForExit()
                $sdkCleanupForced = $true
            }
        }
    }
    catch {
        $cleanupErrors.Add("sdk_cleanup_failed")
    }

    try {
        if ($null -ne $saProcess -and -not $saProcess.HasExited) {
            Add-Type -AssemblyName System.Windows.Forms
            Add-Type -AssemblyName Microsoft.VisualBasic
            $saActivated = [Microsoft.VisualBasic.Interaction]::AppActivate(
                $saProcess.Id)
            if ($saActivated) {
                [System.Windows.Forms.SendKeys]::SendWait("{ESC}")
            }
            Start-Sleep -Milliseconds 500
            $null = $saProcess.CloseMainWindow()
            if (-not $saProcess.WaitForExit(15000)) {
                Add-Type -AssemblyName UIAutomationClient
                $processCondition = [System.Windows.Automation.PropertyCondition]::new(
                    [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
                    $saProcess.Id)
                $nameCondition = [System.Windows.Automation.PropertyCondition]::new(
                    [System.Windows.Automation.AutomationElement]::NameProperty,
                    "No")
                $condition = [System.Windows.Automation.AndCondition]::new(
                    $processCondition,
                    $nameCondition)
                $button = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
                    [System.Windows.Automation.TreeScope]::Descendants,
                    $condition)
                if ($null -ne $button) {
                    $pattern = $button.GetCurrentPattern(
                        [System.Windows.Automation.InvokePattern]::Pattern)
                    $pattern.Invoke()
                }
            }
            if (-not $saProcess.WaitForExit(15000)) {
                Stop-Process -Id $saProcess.Id -Force
                $saProcess.WaitForExit()
                $saCleanupForced = $true
            }
        }
    }
    catch {
        $cleanupErrors.Add("sa_cleanup_failed")
        try {
            if ($null -ne $saProcess -and -not $saProcess.HasExited -and
                [string]::Equals(
                    $saProcess.MainModule.FileName,
                    $resolvedSa,
                    [StringComparison]::OrdinalIgnoreCase)) {
                Stop-Process -Id $saProcess.Id -Force
                $saProcess.WaitForExit()
                $saCleanupForced = $true
            }
        }
        catch {
            $cleanupErrors.Add("sa_force_cleanup_failed")
        }
    }

    try {
        $lateSdkDeadline = [DateTimeOffset]::UtcNow.AddSeconds(5)
        do {
            $lateSdkProcesses = @(Get-Process -Name "SpatialAnalyzerSDK" `
                    -ErrorAction SilentlyContinue)
            foreach ($sdkProcess in $lateSdkProcesses) {
                if (-not [string]::Equals(
                        $sdkProcess.MainModule.FileName,
                        $resolvedSdk,
                        [StringComparison]::OrdinalIgnoreCase)) {
                    $cleanupErrors.Add("unexpected_late_sdk_process")
                    continue
                }
                $null = $sdkProcess.CloseMainWindow()
                if (-not $sdkProcess.WaitForExit(2000)) {
                    Stop-Process -Id $sdkProcess.Id -Force
                    $sdkProcess.WaitForExit()
                    $sdkCleanupForced = $true
                }
            }
            if ($lateSdkProcesses.Count -eq 0) {
                Start-Sleep -Milliseconds 250
            }
        } while ([DateTimeOffset]::UtcNow -lt $lateSdkDeadline)
    }
    catch {
        $cleanupErrors.Add("late_sdk_cleanup_failed")
    }
}

$postCounts = @{}
foreach ($name in $trackedProcessNames) {
    $postCounts[$name] = @(Get-Process -Name $name `
            -ErrorAction SilentlyContinue).Count
}

$report = [ordered]@{
    scenario = $Scenario
    exact_target = $expectedVersion
    exited_within_watchdog = $runnerExited
    runner_completed = $null -ne $runnerResult -and
        $runnerResult.Scenario -ceq $Scenario -and
        $runnerResult.RunnerCompleted
    execution_disposition = if ($null -ne $runnerResult -and
        $runnerResult.Scenario -ceq $Scenario) {
        $runnerResult.ExecutionDisposition
    }
    elseif (-not $runnerExited) {
        "completion_unknown"
    }
    else {
        "definitely_not_started"
    }
    mp_succeeded = $null -ne $runnerResult -and
        $runnerResult.Scenario -ceq $Scenario -and
        $runnerResult.MpSucceeded
    expectation_met = $null -ne $runnerResult -and
        $runnerResult.Scenario -ceq $Scenario -and
        $runnerResult.ExpectationMet
    postcondition_status = if ($null -ne $runnerResult -and
        $runnerResult.Scenario -ceq $Scenario -and
        $runnerResult.PostconditionStatus -cin @("passed", "failed", "not_checked")) {
        $runnerResult.PostconditionStatus
    }
    else {
        "not_checked"
    }
    diagnostic = if ($null -ne $executionError) {
        $executionError
    }
    elseif ($null -ne $runnerResult -and
        $runnerResult.Scenario -cne $Scenario) {
        "scenario_result_mismatch"
    }
    elseif ($null -ne $runnerResult -and
        (($runnerResult.ExpectationMet -and
        $runnerResult.PostconditionStatus -cne "failed" -and
        $runnerResult.ExecutionDisposition -ceq "completed" -and
        $runnerExitCode -ne 0) -or
        ((-not $runnerResult.ExpectationMet -or
        $runnerResult.PostconditionStatus -ceq "failed" -or
        $runnerResult.ExecutionDisposition -cne "completed") -and
        $runnerExitCode -eq 0))) {
        "runner_exit_result_mismatch"
    }
    elseif ($null -ne $runnerResult) {
        $runnerResult.Diagnostic
    }
    elseif (-not $runnerExited) {
        "completion_unknown_after_watchdog"
    }
    else {
        "missing_structural_result"
    }
    cleanup_diagnostic = if ($cleanupErrors.Count -eq 0) {
        $null
    }
    else {
        @($cleanupErrors | Sort-Object -Unique) -join ","
    }
    sdk_cleanup_forced = $sdkCleanupForced
    sa_cleanup_forced = $saCleanupForced
    residual_sa = $postCounts["Spatial Analyzer64"]
    residual_sdk = $postCounts["SpatialAnalyzerSDK"]
    residual_server = $postCounts["Briosa.Server"]
    residual_worker = $postCounts["Briosa.Worker"]
}

$resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
$temporaryPrefix = $temporaryBase.TrimEnd(
    [IO.Path]::DirectorySeparatorChar,
    [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
try {
    if ($resolvedTemporaryRoot.StartsWith(
            $temporaryPrefix,
            [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
    else {
        $cleanupErrors.Add("temporary_cleanup_target_rejected")
    }
}
catch {
    $cleanupErrors.Add("temporary_cleanup_failed")
}

$report.cleanup_diagnostic = if ($cleanupErrors.Count -eq 0) {
    $null
}
else {
    @($cleanupErrors | Sort-Object -Unique) -join ","
}

$report | ConvertTo-Json

if (-not $report.exited_within_watchdog -or
    -not $report.runner_completed -or
    $report.execution_disposition -cne "completed" -or
    -not $report.expectation_met -or
    $report.postcondition_status -ceq "failed" -or
    $null -ne $report.cleanup_diagnostic -or
    $report.residual_sa -ne 0 -or
    $report.residual_sdk -ne 0 -or
    $report.residual_server -ne 0 -or
    $report.residual_worker -ne 0) {
    exit 1
}
