[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [string]$Configuration = "Release",

    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw "Client host scenarios require 64-bit Windows."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedPackage = [IO.Path]::GetFullPath($PackagePath, $repositoryRoot)
$smokeClientProject = Join-Path $repositoryRoot "tools\Briosa.SmokeClient\Briosa.SmokeClient.csproj"
$smokeWorkerProject = Join-Path $repositoryRoot "tests\Briosa.SmokeWorker\Briosa.SmokeWorker.csproj"
$smokeClientDll = Join-Path $repositoryRoot "tools\Briosa.SmokeClient\bin\$Configuration\net10.0\Briosa.SmokeClient.dll"
$smokeWorkerExe = Join-Path $repositoryRoot "tests\Briosa.SmokeWorker\bin\$Configuration\net10.0-windows\Briosa.SmokeWorker.exe"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-client-scenarios-$([Guid]::NewGuid().ToString('N'))"
$extractRoot = Join-Path $temporaryRoot "package"

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

function Start-ScenarioServer {
    param(
        [Parameter(Mandatory)][string]$ServerExecutable,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [Parameter(Mandatory)][string]$WorkerScenario,
        [Parameter(Mandatory)][string]$StatePath,
        [Parameter(Mandatory)][int]$Port,
        [Parameter(Mandatory)][string]$StandardOutput,
        [Parameter(Mandatory)][string]$StandardError,
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
        "BRIOSA_TEST_WORKER_SCENARIO" = $WorkerScenario
        "BRIOSA_TEST_WORKER_STATE_PATH" = $StatePath
        "Briosa__Worker__ExecutionWatchdogTimeout" = $effectiveWatchdogTimeout
        "Briosa__Security__Operations__Deny__0" = $(if ($DenyOperation) { "file_operations.get_working_directory" } else { $null })
        "Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Version" = "2026.1.0529.7"
        "Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Reference" = "portable-fake-worker"
        "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Version" = "2026.1.0529.7"
        "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Reference" = "portable-fake-worker"
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
            ArgumentList = @("--Briosa:Endpoint:Port=$Port")
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
        Watchdog = "00:00:00.250"
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
        Invoke-DotNet @("restore", $smokeWorkerProject, "--locked-mode")
        $clientBuild = @(
            "build", $smokeClientProject,
            "-c", $Configuration,
            "--no-restore")
        Invoke-DotNet $clientBuild
        $workerBuild = @(
            "build", $smokeWorkerProject,
            "-c", $Configuration,
            "--no-restore")
        Invoke-DotNet $workerBuild
    }

    if (-not (Test-Path -LiteralPath $smokeClientDll -PathType Leaf) -or
        -not (Test-Path -LiteralPath $smokeWorkerExe -PathType Leaf)) {
        throw "The smoke client and worker must be built before running scenarios."
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
        $beforeWorkers = @(
            Get-Process -Name "Briosa.SmokeWorker" -ErrorAction SilentlyContinue |
                Select-Object -ExpandProperty Id)
        $scenarioRoot = Join-Path $temporaryRoot $scenario.Client
        [IO.Directory]::CreateDirectory($scenarioRoot) | Out-Null
        $statePath = Join-Path $scenarioRoot "worker-state"
        $standardOutput = Join-Path $scenarioRoot "server.stdout.log"
        $standardError = Join-Path $scenarioRoot "server.stderr.log"
        $port = Get-AvailablePort
        try {
            $serverArguments = @{
                ServerExecutable = $serverExecutable
                WorkingDirectory = $packageRoot
                WorkerScenario = $scenario.Worker
                StatePath = $statePath
                Port = $port
                StandardOutput = $standardOutput
                StandardError = $standardError
                WatchdogTimeout = $scenario.Watchdog
                DenyOperation = $scenario.DenyOperation
            }
            $serverProcess = Start-ScenarioServer @serverArguments
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

            $clientArguments = @(
                $smokeClientDll,
                "--address", "http://127.0.0.1:$port",
                "--scenario", $scenario.Client,
                "--timeout-seconds", "15")
            $clientOutput = @(
                & dotnet @clientArguments 2>&1 |
                    ForEach-Object { [string]$_ })
            if ($LASTEXITCODE -ne 0) {
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

            Write-Host "Passed client scenario: $($scenario.Client)"
        }
        finally {
            if ($null -ne $serverProcess -and -not $serverProcess.HasExited) {
                Stop-Process -Id $serverProcess.Id -Force
                $serverProcess.WaitForExit()
            }

            Start-Sleep -Milliseconds 500
            $newWorkers = @(
                Get-Process -Name "Briosa.SmokeWorker" -ErrorAction SilentlyContinue |
                    Where-Object { $_.Id -notin $beforeWorkers })
            foreach ($worker in $newWorkers) {
                Stop-Process -Id $worker.Id -Force
            }
        }
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
