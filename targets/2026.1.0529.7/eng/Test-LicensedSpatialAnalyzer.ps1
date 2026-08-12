[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [string]$SmokeClientPath,

    [string]$LifecycleClientPath,

    [Parameter(Mandatory)]
    [switch]$ConfirmLicensedSpatialAnalyzerTest,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ActivatedSdkAttestedVersion,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ActivatedSdkAttestationReference,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ConnectedSpatialAnalyzerAttestedVersion,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ConnectedSpatialAnalyzerAttestationReference,

    [ValidateRange(1024, 65535)]
    [int]$Port = 50051,

    [string]$Configuration = "Release",

    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $ConfirmLicensedSpatialAnalyzerTest) {
    throw "Pass -ConfirmLicensedSpatialAnalyzerTest to acknowledge the licensed-machine prerequisites."
}

foreach ($version in @(
        $ActivatedSdkAttestedVersion,
        $ConnectedSpatialAnalyzerAttestedVersion)) {
    if ([string]::IsNullOrWhiteSpace($version) -or
        $version.Length -gt 128 -or
        $version.Contains("`r") -or
        $version.Contains("`n")) {
        throw "Identity attestation versions must be non-empty, single-line values of at most 128 characters."
    }
}

foreach ($reference in @(
        $ActivatedSdkAttestationReference,
        $ConnectedSpatialAnalyzerAttestationReference)) {
    if ([string]::IsNullOrWhiteSpace($reference) -or
        $reference.Length -gt 256 -or
        $reference.Contains("`r") -or
        $reference.Contains("`n")) {
        throw "Identity attestation references must be non-empty, single-line values of at most 256 characters."
    }
}

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw "The licensed SpatialAnalyzer smoke test requires 64-bit Windows."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedPackage = [IO.Path]::GetFullPath($PackagePath, $repositoryRoot)
$smokeClientProject = Join-Path $repositoryRoot "tools\Briosa.SmokeClient\Briosa.SmokeClient.csproj"
$lifecycleClientProject = Join-Path $repositoryRoot "tools\Briosa.LifecycleClient\Briosa.LifecycleClient.csproj"
$smokeClientDll = Join-Path $repositoryRoot "tools\Briosa.SmokeClient\bin\$Configuration\net10.0\Briosa.SmokeClient.dll"
$lifecycleClientDll = Join-Path $repositoryRoot "tools\Briosa.LifecycleClient\bin\$Configuration\net10.0\Briosa.LifecycleClient.dll"
$resolvedSmokeClient = if ([string]::IsNullOrWhiteSpace($SmokeClientPath)) {
    $smokeClientDll
}
else {
    [IO.Path]::GetFullPath($SmokeClientPath, $repositoryRoot)
}
$usesPrebuiltSmokeClient = -not [string]::IsNullOrWhiteSpace($SmokeClientPath)
$resolvedLifecycleClient = if ([string]::IsNullOrWhiteSpace($LifecycleClientPath)) {
    $lifecycleClientDll
}
else {
    [IO.Path]::GetFullPath($LifecycleClientPath, $repositoryRoot)
}
$usesPrebuiltLifecycleClient = -not [string]::IsNullOrWhiteSpace($LifecycleClientPath)
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-licensed-smoke-$([Guid]::NewGuid().ToString('N'))"
$extractRoot = Join-Path $temporaryRoot "package"
$standardOutput = Join-Path $temporaryRoot "server.stdout.log"
$standardError = Join-Path $temporaryRoot "server.stderr.log"
$serverProcess = $null
$beforeWorkers = @(
    Get-Process -Name "Briosa.Worker" -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Id)
$beforeSdkProcesses = @(
    Get-Process -Name "SpatialAnalyzerSDK" -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Id)

function Wait-ForListener {
    param(
        [Parameter(Mandatory)][Diagnostics.Process]$Process,
        [Parameter(Mandatory)][int]$ListenerPort
    )

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            return $false
        }

        $client = [Net.Sockets.TcpClient]::new()
        try {
            $task = $client.ConnectAsync(
                [Net.IPAddress]::Loopback,
                $ListenerPort)
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

if (-not (Test-Path -LiteralPath $resolvedPackage -PathType Leaf)) {
    throw "The package archive does not exist."
}

$saProcesses = @(
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -eq "Spatial Analyzer64.exe" -and
            $_.ExecutablePath -like
                "*\SpatialAnalyzer 2026.1.0529.7\x64\Spatial Analyzer64.exe"
        })
if ($saProcesses.Count -ne 1) {
    throw "Exactly one SpatialAnalyzer 2026.1.0529.7 x64 process must already be running."
}

if ($beforeWorkers.Count -ne 0 -or $beforeSdkProcesses.Count -ne 0) {
    throw "Close existing Briosa workers and standalone SpatialAnalyzer SDK clients before this test."
}

$listenerArguments = @{
    LocalPort = $Port
    State = "Listen"
    ErrorAction = "SilentlyContinue"
}
$existingListener = Get-NetTCPConnection @listenerArguments
if ($null -ne $existingListener) {
    throw "The requested loopback port is already in use."
}

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    if (-not $NoBuild -and -not $usesPrebuiltSmokeClient) {
        & dotnet restore $smokeClientProject --locked-mode
        if ($LASTEXITCODE -ne 0) {
            throw "The standard gRPC smoke client restore failed."
        }

        & dotnet build $smokeClientProject -c $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "The standard gRPC smoke client build failed."
        }
    }

    if (-not $NoBuild -and -not $usesPrebuiltLifecycleClient) {
        & dotnet restore $lifecycleClientProject --locked-mode
        if ($LASTEXITCODE -ne 0) {
            throw "The lifecycle gRPC client restore failed."
        }

        & dotnet build $lifecycleClientProject -c $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "The lifecycle gRPC client build failed."
        }
    }

    if (-not (Test-Path -LiteralPath $resolvedSmokeClient -PathType Leaf)) {
        throw "The standard gRPC smoke client must be built before the licensed test."
    }
    if (-not (Test-Path -LiteralPath $resolvedLifecycleClient -PathType Leaf)) {
        throw "The lifecycle gRPC client must be built before the licensed test."
    }

    Expand-Archive -LiteralPath $resolvedPackage -DestinationPath $extractRoot
    $packageDirectories = @(Get-ChildItem -LiteralPath $extractRoot -Directory)
    if ($packageDirectories.Count -ne 1) {
        throw "The package archive must contain exactly one top-level directory."
    }

    $packageRoot = $packageDirectories[0].FullName
    $serverExecutable = Join-Path $packageRoot "Briosa.Server.exe"
    $diagnosticsOutput = @(& $serverExecutable diagnostics)
    if ($LASTEXITCODE -ne 0) {
        throw "Packaged offline diagnostics failed."
    }

    $diagnostics = ($diagnosticsOutput -join [Environment]::NewLine) |
        ConvertFrom-Json
    if (-not $diagnostics.ready_to_launch -or
        $diagnostics.spatial_analyzer_target -ne "2026.1.0529.7") {
        throw "The package diagnostics do not match the licensed test target."
    }

    $processArguments = @{
        FilePath = $serverExecutable
        ArgumentList = @(
            "--Briosa:Endpoint:Port=$Port"
            "--Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Version=$ActivatedSdkAttestedVersion"
            "--Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Reference=$ActivatedSdkAttestationReference"
            "--Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Version=$ConnectedSpatialAnalyzerAttestedVersion"
            "--Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Reference=$ConnectedSpatialAnalyzerAttestationReference"
        )
        WorkingDirectory = $packageRoot
        WindowStyle = "Hidden"
        RedirectStandardOutput = $standardOutput
        RedirectStandardError = $standardError
        PassThru = $true
    }
    $serverProcess = Start-Process @processArguments
    if (-not (Wait-ForListener -Process $serverProcess -ListenerPort $Port)) {
        throw "The packaged Briosa server did not open its loopback endpoint."
    }

    $lifecycleArguments = @(
        "--address", "http://127.0.0.1:$Port",
        "--scenario", "external-connect",
        "--timeout-seconds", "90")
    $lifecycleOutput = @(
        & dotnet $resolvedLifecycleClient @lifecycleArguments 2>&1 |
            ForEach-Object { [string]$_ })
    if ($LASTEXITCODE -ne 0) {
        throw "The public lifecycle RPCs did not establish SpatialAnalyzer readiness."
    }
    $lifecycleReport = ($lifecycleOutput -join [Environment]::NewLine) |
        ConvertFrom-Json
    if (-not $lifecycleReport.success -or
        -not $lifecycleReport.ready_for_mp -or
        $lifecycleReport.outcome -ne "external-connected") {
        throw "The public lifecycle client did not report a ready SDK generation."
    }

    $clientArguments = @(
        "--address", "http://127.0.0.1:$Port",
        "--scenario", "ready",
        "--timeout-seconds", "30")
    $clientOutput = if (
        [IO.Path]::GetExtension($resolvedSmokeClient) -eq ".dll") {
        @(
            & dotnet $resolvedSmokeClient @clientArguments 2>&1 |
                ForEach-Object { [string]$_ })
    }
    else {
        @(
            & $resolvedSmokeClient @clientArguments 2>&1 |
                ForEach-Object { [string]$_ })
    }
    if ($LASTEXITCODE -ne 0) {
        $failureReport = $null
        try {
            $failureReport = ($clientOutput -join [Environment]::NewLine) |
                ConvertFrom-Json
        }
        catch {
        }

        $diagnosticCode = [string]$failureReport.diagnostic_code
        if ($diagnosticCode -notmatch '^[a-z0-9-]{1,128}$') {
            $diagnosticCode = "licensed-client-failure-unclassified"
        }

        throw "The gRPC client did not complete the licensed smoke test ($diagnosticCode)."
    }

    $report = ($clientOutput -join [Environment]::NewLine) | ConvertFrom-Json
    if (-not $report.success -or
        -not $report.ready_for_mp -or
        -not $report.operation_succeeded) {
        throw "The gRPC client reported an unsuccessful licensed smoke test."
    }

    $stopArguments = @(
        "--address", "http://127.0.0.1:$Port",
        "--scenario", "stop-sdk",
        "--timeout-seconds", "90")
    $stopOutput = @(
        & dotnet $resolvedLifecycleClient @stopArguments 2>&1 |
            ForEach-Object { [string]$_ })
    if ($LASTEXITCODE -ne 0) {
        throw "The public lifecycle RPCs did not stop the SDK after the smoke test."
    }

    $errorText = [string](
        Get-Content -LiteralPath $standardError -Raw -ErrorAction SilentlyContinue)
    if (-not [string]::IsNullOrWhiteSpace($errorText)) {
        throw "The packaged server wrote to standard error during the licensed test."
    }

    Write-Host "Licensed SpatialAnalyzer read-only operation smoke tests passed."
    Write-Host "Returned SpatialAnalyzer values were intentionally not logged."
}
finally {
    if ($null -ne $serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
        $serverProcess.WaitForExit()
    }

    Start-Sleep -Seconds 2
    $newWorkers = @(
        Get-Process -Name "Briosa.Worker" -ErrorAction SilentlyContinue |
            Where-Object { $_.Id -notin $beforeWorkers })
    foreach ($worker in $newWorkers) {
        Stop-Process -Id $worker.Id -Force
    }

    $residualSdkProcesses = @(
        Get-Process -Name "SpatialAnalyzerSDK" -ErrorAction SilentlyContinue |
            Where-Object { $_.Id -notin $beforeSdkProcesses })
    if ($residualSdkProcesses.Count -ne 0) {
        Write-Warning "A SpatialAnalyzerSDK process created during the test remains."
    }

    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith(
            $temporaryBase,
            [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
