[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [string]$Configuration = "Release",

    [switch]$NoBuild,

    # A failing scenario copies its server logs and client output here.
    [string]$DiagnosticsDirectory = "artifacts\client-scenarios-diagnostics"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw "Client host scenarios require 64-bit Windows."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedPackage = [IO.Path]::GetFullPath($PackagePath, $repositoryRoot)
$smokeClientProject = Join-Path $repositoryRoot "tools\Briosa.SmokeClient\Briosa.SmokeClient.csproj"
$lifecycleClientProject = Join-Path $repositoryRoot "tools\Briosa.LifecycleClient\Briosa.LifecycleClient.csproj"
$smokeWorkerProject = Join-Path $repositoryRoot "tests\Briosa.SmokeWorker\Briosa.SmokeWorker.csproj"
$smokeClientDll = Join-Path $repositoryRoot "tools\Briosa.SmokeClient\bin\$Configuration\net10.0\Briosa.SmokeClient.dll"
$lifecycleClientDll = Join-Path $repositoryRoot "tools\Briosa.LifecycleClient\bin\$Configuration\net10.0\Briosa.LifecycleClient.dll"
$smokeWorkerExe = Join-Path $repositoryRoot "tests\Briosa.SmokeWorker\bin\$Configuration\net10.0-windows\Briosa.SmokeWorker.exe"
$smokeWorkerOutput = Split-Path -Parent $smokeWorkerExe
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-client-scenarios-$([Guid]::NewGuid().ToString('N'))"
$extractRoot = Join-Path $temporaryRoot "package"
$diagnosticsRoot = Join-Path ([IO.Path]::GetFullPath($DiagnosticsDirectory, $repositoryRoot)) `
    ("{0:yyyyMMddTHHmmss}Z-{1}" -f [DateTime]::UtcNow, [Guid]::NewGuid().ToString('N').Substring(0, 8))
# Each client bounds its whole run, not one call. watchdog-recovery waits out
# its watchdog budget before recovering, and a busy machine slows every cold
# server and worker start: under parallel build load that scenario took up to
# 13 s of the former 15 s budget (#302).
$clientTimeoutSeconds = "30"

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-AvailablePort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Wait-ForListener {
    param(
        [Parameter(Mandatory)][Diagnostics.Process]$Process,
        [Parameter(Mandatory)][int]$Port
    )

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            return $false
        }

        $client = [Net.Sockets.TcpClient]::new()
        try {
            $task = $client.ConnectAsync([Net.IPAddress]::Loopback, $Port)
            if ($task.Wait(250) -and $client.Connected) {
                return $true
            }
        }
        catch {
        }
        finally {
            $client.Dispose()
        }

        Start-Sleep -Milliseconds 100
    }

    return $false
}

function Get-ChildProcessRecord {
    param(
        [Parameter(Mandatory)][int]$ParentId,
        [Parameter(Mandatory)][DateTime]$ParentStartTime
    )

    # Identify children by parent process ID and creation time, never by image
    # name. Concurrent runs on one machine (another worktree, the other target,
    # or a test suite) start workers with the same name, and killing them makes
    # those runs fail with Unavailable or not-ready lifecycle errors (#302).
    return @(Get-CimInstance -ClassName Win32_Process `
            -Filter "ParentProcessId = $ParentId" -ErrorAction SilentlyContinue |
        Where-Object { $_.CreationDate -ge $ParentStartTime.AddSeconds(-1) })
}

function Stop-ScenarioProcess {
    param(
        [Diagnostics.Process]$Process,
        [Nullable[DateTime]]$StartTime
    )

    # Stops one process this harness started, then the children it launched
    # (the server's SDK worker). Returns the IDs of processes that did not exit.
    if ($null -eq $Process) {
        return @()
    }

    $children = @()
    if ($null -ne $StartTime) {
        try {
            $children = @(Get-ChildProcessRecord -ParentId $Process.Id -ParentStartTime $StartTime)
        }
        catch {
            # Still stop the process itself; the tree kill covers live children.
            Write-Warning "Could not list child processes of $($Process.Id): $_"
        }
    }
    try {
        if (-not $Process.HasExited) {
            $Process.Kill($true)
        }
    }
    catch [InvalidOperationException] {
        # The process exited after HasExited was read.
    }
    $survivors = [Collections.Generic.List[int]]::new()
    if (-not $Process.WaitForExit(30000)) {
        $survivors.Add($Process.Id)
    }

    foreach ($child in $children) {
        $childProcess = Get-Process -Id $child.ProcessId -ErrorAction SilentlyContinue
        if ($null -eq $childProcess) {
            continue
        }
        try {
            # A recycled process ID belongs to someone else.
            if ([Math]::Abs(($childProcess.StartTime - $child.CreationDate).TotalSeconds) -gt 1) {
                continue
            }
            if (-not $childProcess.HasExited) {
                $childProcess.Kill($true)
            }
        }
        catch [InvalidOperationException] {
            # The child exited after it was found.
        }
        if (-not $childProcess.WaitForExit(10000)) {
            $survivors.Add($child.ProcessId)
        }
        $childProcess.Dispose()
    }

    return $survivors.ToArray()
}

function Save-ScenarioDiagnostics {
    param(
        [Parameter(Mandatory)][object]$Scenario,
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [Parameter(Mandatory)][string]$Failure,
        [Parameter(Mandatory)][System.Collections.IDictionary]$Timing
    )

    $destination = Join-Path $diagnosticsRoot $Scenario.Client
    [IO.Directory]::CreateDirectory($destination) | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $ScenarioRoot -Force)) {
        # The fake application directory holds only copied test binaries.
        if ($item.Name -ne "fake-spatial-analyzer") {
            Copy-Item -LiteralPath $item.FullName -Destination $destination -Recurse -Force
        }
    }
    [ordered]@{
        scenario = $Scenario.Client
        worker_scenario = $Scenario.Worker
        watchdog_timeout = $Scenario.Watchdog
        failure = $Failure
        timing_milliseconds = $Timing
    } | ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath (Join-Path $destination "failure.json") -Encoding utf8
    Write-Host "Saved diagnostics for client scenario '$($Scenario.Client)' to '$destination'."
}

function Start-ScenarioServer {
    param(
        [Parameter(Mandatory)][string]$ServerExecutable,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [Parameter(Mandatory)][string]$WorkerScenario,
        [Parameter(Mandatory)][string]$StatePath,
        [Parameter(Mandatory)][int]$Port,
        [Parameter(Mandatory)][string]$StandardOutput,
        [Parameter(Mandatory)][string]$StandardError,
        [Parameter(Mandatory)][string]$SpatialAnalyzerExecutable,
        [Parameter(Mandatory)][string]$LogDirectory,
        [string]$WatchdogTimeout,
        [switch]$DenyOperation
    )

    # A [string] parameter coerces an explicit $null to an empty string.
    # Normalize it before setting the environment so the packaged default is
    # not replaced by an invalid empty configuration value on newer runtimes.
    $effectiveWatchdogTimeout = if (
        [string]::IsNullOrWhiteSpace($WatchdogTimeout)) {
        $null
    } else {
        $WatchdogTimeout
    }
    $environmentValues = [ordered]@{
        "Briosa__Worker__ExecutablePath" = $smokeWorkerExe
        "Briosa__SpatialAnalyzer__ExecutablePath" = $SpatialAnalyzerExecutable
        "BRIOSA_TEST_WORKER_SCENARIO" = $WorkerScenario
        "BRIOSA_TEST_WORKER_STATE_PATH" = $StatePath
        "Briosa__Worker__ExecutionWatchdogTimeout" = $effectiveWatchdogTimeout
        "Briosa__Security__Operations__Overrides__file_operations__get_working_directory" = $(if ($DenyOperation) { "deny" } else { $null })
        "Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Version" = "2026.1.0529.7"
        "Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Reference" = "portable-fake-worker"
        "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Version" = "2026.1.0529.7"
        "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Reference" = "portable-fake-worker"
        # Keep each scenario's server log with its diagnostics instead of the
        # user's shared Briosa log directory.
        "Briosa__Logging__File__Directory" = $LogDirectory
    }
    $previousValues = [ordered]@{}
    foreach ($entry in $environmentValues.GetEnumerator()) {
        $previousValues[$entry.Key] =
            [Environment]::GetEnvironmentVariable($entry.Key)
        if ($null -eq $entry.Value) {
            Remove-Item -LiteralPath "Env:$($entry.Key)" -ErrorAction SilentlyContinue
        } else {
            [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value)
        }
    }

    try {
        $processArguments = @{
            FilePath = $ServerExecutable
            # This harness force-stops its API hosts. A tray monitor would outlive
            # them and retain redirected log handles. Desktop lifecycle coverage
            # runs separately in Test-WindowsPackage.ps1.
            ArgumentList = @("--Briosa:Endpoint:Port=$Port", "--Briosa:Desktop:Mode=Disabled")
            WorkingDirectory = $WorkingDirectory
            WindowStyle = "Hidden"
            RedirectStandardOutput = $StandardOutput
            RedirectStandardError = $StandardError
            PassThru = $true
        }
        return Start-Process @processArguments
    }
    finally {
        foreach ($entry in $previousValues.GetEnumerator()) {
            if ($null -eq $entry.Value) {
                Remove-Item -LiteralPath "Env:$($entry.Key)" -ErrorAction SilentlyContinue
            } else {
                [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value)
            }
        }
    }
}

if (-not (Test-Path -LiteralPath $resolvedPackage -PathType Leaf)) {
    throw "The package archive does not exist."
}
$scenarios = @(
    [pscustomobject]@{
        Worker = "ready"
        Client = "ready"
        Watchdog = $null
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "OK"
            OperationSucceeded = $true
            TypedErrorObserved = $false
            RecoverySucceeded = $false
            FailureKinds = @()
        }
    },
    [pscustomobject]@{
        Worker = "disconnected"
        Client = "unavailable"
        Watchdog = $null
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $false
            GrpcStatus = "UNAVAILABLE"
            OperationSucceeded = $false
            TypedErrorObserved = $true
            RecoverySucceeded = $false
            FailureKinds = @("SpatialAnalyzerUnavailable", "WorkerUnavailable")
        }
    },
    [pscustomobject]@{
        Worker = "ready"
        Client = "policy-denied"
        Watchdog = $null
        DenyOperation = $true
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "PERMISSION_DENIED"
            OperationSucceeded = $false
            TypedErrorObserved = $true
            RecoverySucceeded = $false
            FailureKinds = @("PolicyDenied")
        }
    },
    [pscustomobject]@{
        Worker = "mp-failure"
        Client = "mp-failure"
        Watchdog = $null
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "FAILED_PRECONDITION"
            OperationSucceeded = $false
            TypedErrorObserved = $true
            RecoverySucceeded = $false
            FailureKinds = @("MpFailure")
        }
    },
    [pscustomobject]@{
        Worker = "output-failure"
        Client = "output-failure"
        Watchdog = $null
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "DATA_LOSS"
            OperationSucceeded = $false
            TypedErrorObserved = $true
            RecoverySucceeded = $false
            FailureKinds = @("OutputRetrievalFailure")
        }
    },
    [pscustomobject]@{
        Worker = "sdk-call-faulted"
        Client = "sdk-call-faulted"
        Watchdog = $null
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "INTERNAL"
            OperationSucceeded = $false
            TypedErrorObserved = $true
            RecoverySucceeded = $false
            FailureKinds = @("SdkCallFaulted")
        }
    },
    [pscustomobject]@{
        Worker = "delay-first-execute"
        Client = "deadline"
        Watchdog = $null
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "DEADLINE_EXCEEDED"
            OperationSucceeded = $false
            TypedErrorObserved = $false
            RecoverySucceeded = $true
            FailureKinds = @()
        }
    },
    [pscustomobject]@{
        Worker = "delay-first-execute"
        Client = "cancellation"
        Watchdog = $null
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "CANCELLED"
            OperationSucceeded = $false
            TypedErrorObserved = $false
            RecoverySucceeded = $true
            FailureKinds = @()
        }
    },
    [pscustomobject]@{
        Worker = "hang-first-execute"
        Client = "watchdog-recovery"
        # The scripted hang never returns, so the budget only has to exceed a
        # cold worker's first execution with headroom. 250 ms did not: a slow
        # runner could expire it before the first worker claimed the hang, or
        # on the replacement worker's first call (#302).
        Watchdog = "00:00:05.000"
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "UNAVAILABLE"
            OperationSucceeded = $false
            TypedErrorObserved = $true
            RecoverySucceeded = $true
            FailureKinds = @("WorkerWatchdogTimeout")
        }
    },
    [pscustomobject]@{
        Worker = "ready"
        Client = "unsupported-service"
        Watchdog = $null
        DenyOperation = $false
        Expected = [pscustomobject]@{
            ReadyForMp = $true
            GrpcStatus = "UNIMPLEMENTED"
            OperationSucceeded = $false
            TypedErrorObserved = $false
            RecoverySucceeded = $false
            FailureKinds = @("Unsupported")
        }
    }
)

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    if (-not $NoBuild) {
        Invoke-DotNet @("restore", $smokeClientProject, "--locked-mode")
        Invoke-DotNet @("restore", $lifecycleClientProject, "--locked-mode")
        Invoke-DotNet @("restore", $smokeWorkerProject, "--locked-mode")
        $clientBuild = @(
            "build", $smokeClientProject,
            "-c", $Configuration,
            "--no-restore")
        Invoke-DotNet $clientBuild
        $lifecycleClientBuild = @(
            "build", $lifecycleClientProject,
            "-c", $Configuration,
            "--no-restore")
        Invoke-DotNet $lifecycleClientBuild
        $workerBuild = @(
            "build", $smokeWorkerProject,
            "-c", $Configuration,
            "--no-restore")
        Invoke-DotNet $workerBuild
    }

    if (-not (Test-Path -LiteralPath $smokeClientDll -PathType Leaf) -or
        -not (Test-Path -LiteralPath $lifecycleClientDll -PathType Leaf) -or
        -not (Test-Path -LiteralPath $smokeWorkerExe -PathType Leaf)) {
        throw "The smoke client, lifecycle client, and worker must be built before running scenarios."
    }

    Expand-Archive -LiteralPath $resolvedPackage -DestinationPath $extractRoot
    $packageDirectories = @(Get-ChildItem -LiteralPath $extractRoot -Directory)
    if ($packageDirectories.Count -ne 1) {
        throw "The package archive must contain exactly one top-level directory."
    }

    $packageRoot = $packageDirectories[0].FullName
    $serverExecutable = Join-Path $packageRoot "Briosa.Server.exe"
    if (-not (Test-Path -LiteralPath $serverExecutable -PathType Leaf)) {
        throw "The package does not contain Briosa.Server.exe."
    }

    foreach ($scenario in $scenarios) {
        $serverProcess = $null
        $serverStartTime = $null
        $fakeApplication = $null
        $fakeApplicationStartTime = $null
        $scenarioFailure = $null
        $timing = [ordered]@{}
        $stopwatch = [Diagnostics.Stopwatch]::StartNew()
        $scenarioRoot = Join-Path $temporaryRoot $scenario.Client
        [IO.Directory]::CreateDirectory($scenarioRoot) | Out-Null
        $statePath = Join-Path $scenarioRoot "worker-state"
        $standardOutput = Join-Path $scenarioRoot "server.stdout.log"
        $standardError = Join-Path $scenarioRoot "server.stderr.log"
        $serverLogDirectory = Join-Path $scenarioRoot "server-logs"
        $lifecycleLog = Join-Path $scenarioRoot "lifecycle-client.log"
        $clientLog = Join-Path $scenarioRoot "smoke-client.log"
        $fakeApplicationRoot = Join-Path $scenarioRoot "fake-spatial-analyzer"
        [IO.Directory]::CreateDirectory($fakeApplicationRoot) | Out-Null
        Copy-Item -LiteralPath (Join-Path $smokeWorkerOutput "Briosa.SmokeWorker.dll") -Destination $fakeApplicationRoot
        Copy-Item -LiteralPath (Join-Path $smokeWorkerOutput "Briosa.SmokeWorker.deps.json") -Destination $fakeApplicationRoot
        Copy-Item -LiteralPath (Join-Path $smokeWorkerOutput "Briosa.SmokeWorker.runtimeconfig.json") -Destination $fakeApplicationRoot
        Copy-Item -LiteralPath (Join-Path $smokeWorkerOutput "Briosa.Worker.Control.dll") -Destination $fakeApplicationRoot
        $fakeApplicationExecutable = Join-Path $fakeApplicationRoot "Spatial Analyzer64.exe"
        Copy-Item -LiteralPath $smokeWorkerExe -Destination $fakeApplicationExecutable
        $port = Get-AvailablePort
        try {
            $fakeApplication = Start-Process `
                -FilePath $fakeApplicationExecutable `
                -ArgumentList @("--hold") `
                -WorkingDirectory $fakeApplicationRoot `
                -WindowStyle Hidden `
                -PassThru
            $fakeApplicationStartTime = $fakeApplication.StartTime
            $serverArguments = @{
                ServerExecutable = $serverExecutable
                WorkingDirectory = $packageRoot
                WorkerScenario = $scenario.Worker
                StatePath = $statePath
                Port = $port
                StandardOutput = $standardOutput
                StandardError = $standardError
                SpatialAnalyzerExecutable = $fakeApplicationExecutable
                LogDirectory = $serverLogDirectory
                WatchdogTimeout = $scenario.Watchdog
                DenyOperation = $scenario.DenyOperation
            }
            $serverProcess = Start-ScenarioServer @serverArguments
            $serverStartTime = $serverProcess.StartTime
            if (-not (Wait-ForListener -Process $serverProcess -Port $port)) {
                foreach ($log in @(
                    [pscustomobject]@{ Name = "stdout"; Path = $standardOutput },
                    [pscustomobject]@{ Name = "stderr"; Path = $standardError })) {
                    if (Test-Path -LiteralPath $log.Path -PathType Leaf) {
                        Write-Host "--- server $($log.Name) for $($scenario.Client) ---"
                        foreach ($line in Get-Content -LiteralPath $log.Path) {
                            $redactedLine = $line.Replace($temporaryRoot, "<temporary-root>", [StringComparison]::OrdinalIgnoreCase)
                            $redactedLine = $redactedLine.Replace($repositoryRoot, "<repository-root>", [StringComparison]::OrdinalIgnoreCase)
                            $redactedLine = $redactedLine.Replace($packageRoot, "<package-root>", [StringComparison]::OrdinalIgnoreCase)
                            Write-Host $redactedLine
                        }
                    }
                }

                throw "The packaged server did not listen for scenario '$($scenario.Client)'."
            }
            $timing.listen = $stopwatch.ElapsedMilliseconds

            $lifecycleScenario = if ($scenario.Worker -eq "disconnected") {
                "start-sdk"
            }
            else {
                "external-connect"
            }
            $lifecycleArguments = @(
                $lifecycleClientDll,
                "--address", "http://127.0.0.1:$port",
                "--scenario", $lifecycleScenario,
                "--timeout-seconds", $clientTimeoutSeconds)
            $phaseStarted = $stopwatch.ElapsedMilliseconds
            $lifecycleOutput = @(
                & dotnet @lifecycleArguments 2>&1 |
                    ForEach-Object { [string]$_ })
            $lifecycleExitCode = $LASTEXITCODE
            $timing.lifecycle = $stopwatch.ElapsedMilliseconds - $phaseStarted
            Set-Content -LiteralPath $lifecycleLog -Value $lifecycleOutput -Encoding utf8
            if ($lifecycleExitCode -ne 0) {
                throw "Lifecycle setup failed scenario '$($scenario.Client)': $($lifecycleOutput -join ' ')"
            }

            $clientArguments = @(
                $smokeClientDll,
                "--address", "http://127.0.0.1:$port",
                "--scenario", $scenario.Client,
                "--timeout-seconds", $clientTimeoutSeconds)
            $phaseStarted = $stopwatch.ElapsedMilliseconds
            $clientOutput = @(
                & dotnet @clientArguments 2>&1 |
                    ForEach-Object { [string]$_ })
            $clientExitCode = $LASTEXITCODE
            $timing.client = $stopwatch.ElapsedMilliseconds - $phaseStarted
            Set-Content -LiteralPath $clientLog -Value $clientOutput -Encoding utf8
            if ($clientExitCode -ne 0) {
                throw "The client failed scenario '$($scenario.Client)': $($clientOutput -join ' ')"
            }

            $report = ($clientOutput -join [Environment]::NewLine) |
                ConvertFrom-Json
            if (-not $report.success) {
                throw "The client did not report success."
            }

            if ($report.ready_for_mp -ne $scenario.Expected.ReadyForMp -or
                $report.grpc_status -ne $scenario.Expected.GrpcStatus -or
                $report.operation_succeeded -ne $scenario.Expected.OperationSucceeded -or
                $report.typed_error_observed -ne $scenario.Expected.TypedErrorObserved -or
                $report.recovery_succeeded -ne $scenario.Expected.RecoverySucceeded) {
                throw "The client report does not match scenario '$($scenario.Client)'."
            }

            $expectedFailureKinds = @($scenario.Expected.FailureKinds)
            if (($expectedFailureKinds.Count -eq 0 -and
                    $null -ne $report.failure_kind) -or
                ($expectedFailureKinds.Count -gt 0 -and
                    $report.failure_kind -notin $expectedFailureKinds)) {
                throw "The client failure kind does not match scenario '$($scenario.Client)'."
            }
        }
        catch {
            $scenarioFailure = $_
        }
        finally {
            # Stop only the processes this scenario started, with their children.
            $survivors = [Collections.Generic.List[string]]::new()
            foreach ($owned in @(
                    @{ Process = $serverProcess; StartTime = $serverStartTime },
                    @{ Process = $fakeApplication; StartTime = $fakeApplicationStartTime })) {
                try {
                    foreach ($survivor in @(Stop-ScenarioProcess @owned)) {
                        $survivors.Add([string]$survivor)
                    }
                }
                catch {
                    $survivors.Add("cleanup of process $($owned.Process.Id) failed: $_")
                }
            }
        }

        if ($null -eq $scenarioFailure -and $survivors.Count -gt 0) {
            $scenarioFailure = "Processes started for scenario '$($scenario.Client)' did not stop: $($survivors -join '; ')."
        }
        if ($null -ne $scenarioFailure) {
            $timing.total = $stopwatch.ElapsedMilliseconds
            Save-ScenarioDiagnostics `
                -Scenario $scenario `
                -ScenarioRoot $scenarioRoot `
                -Failure "$scenarioFailure" `
                -Timing $timing
            throw $scenarioFailure
        }

        Write-Host ("Passed client scenario: {0} (lifecycle {1} ms, client {2} ms)" -f
            $scenario.Client, $timing.lifecycle, $timing.client)
    }

    Write-Host "All packaged client scenarios passed without SpatialAnalyzer."
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith(
            $temporaryBase,
            [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
