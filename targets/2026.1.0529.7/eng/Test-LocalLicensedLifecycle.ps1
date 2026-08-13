[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("OwnedApplication", "ExternalApplication", "SdkLossRecovery")]
    [string]$Scenario,

    [Parameter(Mandatory)]
    [switch]$ConfirmLicensedSpatialAnalyzerTest,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$SpatialAnalyzerExecutablePath,

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

    [string]$ServerPath,

    [string]$LifecycleClientPath,

    [ValidateRange(1024, 65535)]
    [int]$Port = 50051,

    [string]$Configuration = "Debug",

    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $ConfirmLicensedSpatialAnalyzerTest) {
    throw "Pass -ConfirmLicensedSpatialAnalyzerTest to acknowledge that this test controls a licensed local SpatialAnalyzer installation."
}

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw "The licensed lifecycle test requires a 64-bit Windows process."
}

$targetVersion = "2026.1.0529.7"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$serverProject = Join-Path $repositoryRoot "src\Briosa.Server\Briosa.Server.csproj"
$clientProject = Join-Path $repositoryRoot "tools\Briosa.LifecycleClient\Briosa.LifecycleClient.csproj"
$defaultServer = Join-Path $repositoryRoot "src\Briosa.Server\bin\$Configuration\net10.0-windows\Briosa.Server.exe"
$defaultClient = Join-Path $repositoryRoot "tools\Briosa.LifecycleClient\bin\$Configuration\net10.0\Briosa.LifecycleClient.dll"
$resolvedServer = if ([string]::IsNullOrWhiteSpace($ServerPath)) {
    $defaultServer
}
else {
    [IO.Path]::GetFullPath($ServerPath, $repositoryRoot)
}
$resolvedClient = if ([string]::IsNullOrWhiteSpace($LifecycleClientPath)) {
    $defaultClient
}
else {
    [IO.Path]::GetFullPath($LifecycleClientPath, $repositoryRoot)
}
$resolvedSaExecutable = [IO.Path]::GetFullPath($SpatialAnalyzerExecutablePath)
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-lifecycle-$([Guid]::NewGuid().ToString('N'))"
$serverOutput = Join-Path $temporaryRoot "server.stdout.log"
$serverError = Join-Path $temporaryRoot "server.stderr.log"
$serverProcess = $null

function Assert-SafeText {
    param(
        [Parameter(Mandatory)][string]$Value,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][int]$MaximumLength
    )

    if ([string]::IsNullOrWhiteSpace($Value) -or
        $Value.Length -gt $MaximumLength -or
        $Value.Contains("`r") -or
        $Value.Contains("`n")) {
        throw "$Name must be a non-empty, single-line value of at most $MaximumLength characters."
    }
}

function Get-ProcessIds {
    param([Parameter(Mandatory)][string]$Name)

    return @(
        Get-Process -Name $Name -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty Id |
            Sort-Object)
}

function Get-EligibleSpatialAnalyzerProcesses {
    return @(
        Get-CimInstance Win32_Process |
            Where-Object {
                $_.Name -eq "Spatial Analyzer64.exe" -and
                -not [string]::IsNullOrWhiteSpace($_.ExecutablePath) -and
                [string]::Equals(
                    [IO.Path]::GetFullPath($_.ExecutablePath),
                    $resolvedSaExecutable,
                    [StringComparison]::OrdinalIgnoreCase)
            })
}

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
            $connect = $client.ConnectAsync([Net.IPAddress]::Loopback, $ListenerPort)
            if ($connect.Wait(250) -and $client.Connected) {
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

function Invoke-LifecycleClient {
    param([Parameter(Mandatory)][string]$ClientScenario)

    $arguments = @(
        "--address", "http://127.0.0.1:$Port",
        "--scenario", $ClientScenario,
        "--timeout-seconds", "180")
    $output = @(
        & dotnet $resolvedClient @arguments 2>&1 |
            ForEach-Object { [string]$_ })
    if ($LASTEXITCODE -ne 0) {
        $failure = $null
        try {
            $failure = ($output -join [Environment]::NewLine) | ConvertFrom-Json
        }
        catch {
        }

        $diagnosticCode = [string]$failure.diagnostic_code
        if ($diagnosticCode -notmatch '^[a-z0-9-]{1,128}$') {
            $diagnosticCode = "licensed-lifecycle-client-failure-unclassified"
        }

        throw "The lifecycle client failed ($diagnosticCode)."
    }

    $report = ($output -join [Environment]::NewLine) | ConvertFrom-Json
    if (-not $report.success) {
        throw "The lifecycle client returned an unsuccessful report."
    }

    return $report
}

foreach ($value in @(
        $ActivatedSdkAttestedVersion,
        $ConnectedSpatialAnalyzerAttestedVersion)) {
    Assert-SafeText -Value $value -Name "Identity attestation version" -MaximumLength 128
}
foreach ($value in @(
        $ActivatedSdkAttestationReference,
        $ConnectedSpatialAnalyzerAttestationReference)) {
    Assert-SafeText -Value $value -Name "Identity attestation reference" -MaximumLength 256
}

if (-not (Test-Path -LiteralPath $resolvedSaExecutable -PathType Leaf) -or
    -not [string]::Equals(
        [IO.Path]::GetFileName($resolvedSaExecutable),
        "Spatial Analyzer64.exe",
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "SpatialAnalyzerExecutablePath must identify the exact-target Spatial Analyzer64.exe."
}

$beforeWorkers = @(Get-ProcessIds -Name "Briosa.Worker")
$beforeSdk = @(Get-ProcessIds -Name "SpatialAnalyzerSDK")
$beforeApplications = @(Get-EligibleSpatialAnalyzerProcesses)
if ($beforeWorkers.Count -ne 0 -or $beforeSdk.Count -ne 0) {
    throw "Close existing Briosa workers and standalone SpatialAnalyzer SDK clients before this test."
}
if ($Scenario -eq "ExternalApplication") {
    if ($beforeApplications.Count -ne 1) {
        throw "ExternalApplication requires exactly one exact-target SpatialAnalyzer process started outside Briosa."
    }
}
elseif ($beforeApplications.Count -ne 0) {
    throw "$Scenario requires no exact-target SpatialAnalyzer process at startup."
}

$existingListener = Get-NetTCPConnection `
    -LocalPort $Port `
    -State Listen `
    -ErrorAction SilentlyContinue
if ($null -ne $existingListener) {
    throw "The requested loopback port is already in use."
}

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    if (-not $NoBuild) {
        & dotnet restore $serverProject --locked-mode
        if ($LASTEXITCODE -ne 0) {
            throw "The Briosa server restore failed."
        }
        & dotnet restore $clientProject --locked-mode
        if ($LASTEXITCODE -ne 0) {
            throw "The lifecycle client restore failed."
        }
        & dotnet build $serverProject -c $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "The Briosa server build failed."
        }
        & dotnet build $clientProject -c $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "The lifecycle client build failed."
        }
    }

    if (-not (Test-Path -LiteralPath $resolvedServer -PathType Leaf) -or
        -not (Test-Path -LiteralPath $resolvedClient -PathType Leaf)) {
        throw "The server and lifecycle client must exist before the test runs."
    }

    $serverProcess = Start-Process `
        -FilePath $resolvedServer `
        -ArgumentList @(
            "--Briosa:Endpoint:Port=$Port",
            "--Briosa:SpatialAnalyzer:ExecutablePath=`"$resolvedSaExecutable`"",
            "--Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Version=$ActivatedSdkAttestedVersion",
            "--Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Reference=$ActivatedSdkAttestationReference",
            "--Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Version=$ConnectedSpatialAnalyzerAttestedVersion",
            "--Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Reference=$ConnectedSpatialAnalyzerAttestationReference") `
        -WorkingDirectory ([IO.Path]::GetDirectoryName($resolvedServer)) `
        -WindowStyle Hidden `
        -RedirectStandardOutput $serverOutput `
        -RedirectStandardError $serverError `
        -PassThru
    if (-not (Wait-ForListener -Process $serverProcess -ListenerPort $Port)) {
        throw "The Briosa server did not open its loopback endpoint."
    }

    $inert = Invoke-LifecycleClient -ClientScenario "inert"
    if (@(Get-ProcessIds -Name "Briosa.Worker").Count -ne 0 -or
        @(Get-ProcessIds -Name "SpatialAnalyzerSDK").Count -ne 0) {
        throw "Manual server startup caused an SDK or worker side effect."
    }

    switch ($Scenario) {
        "OwnedApplication" {
            $report = Invoke-LifecycleClient -ClientScenario "owned"
            if ($report.outcome -ne "owned-complete") {
                throw "The owned lifecycle scenario did not complete."
            }
        }
        "ExternalApplication" {
            $externalProcessId = [int]$beforeApplications[0].ProcessId
            $report = Invoke-LifecycleClient -ClientScenario "external"
            if ($report.outcome -ne "external-preserved" -or
                $null -eq (Get-Process -Id $externalProcessId -ErrorAction SilentlyContinue)) {
                throw "The external SpatialAnalyzer process was not preserved."
            }
        }
        "SdkLossRecovery" {
            $sdkBeforePrepare = @(Get-ProcessIds -Name "SpatialAnalyzerSDK")
            $prepare = Invoke-LifecycleClient -ClientScenario "sdk-loss-prepare"
            $newSdk = @(
                Get-Process -Name "SpatialAnalyzerSDK" -ErrorAction SilentlyContinue |
                    Where-Object { $_.Id -notin $sdkBeforePrepare })
            if ($newSdk.Count -ne 1) {
                throw "The SDK-loss test could not identify exactly one SDK process owned by the current generation."
            }

            Stop-Process -Id $newSdk[0].Id -Force
            $newSdk[0].WaitForExit()
            $recovered = Invoke-LifecycleClient -ClientScenario "sdk-loss-recover"
            if ($recovered.outcome -ne "sdk-loss-recovered" -or
                $recovered.incident_kind -ne "SDK_PROCESS_EXITED") {
                throw "The SDK-loss recovery scenario did not preserve the expected incident."
            }
        }
    }

    $newSdkAfter = @(
        Get-Process -Name "SpatialAnalyzerSDK" -ErrorAction SilentlyContinue |
            Where-Object { $_.Id -notin $beforeSdk })
    $newApplicationsAfter = @(
        Get-EligibleSpatialAnalyzerProcesses |
            Where-Object { $_.ProcessId -notin $beforeApplications.ProcessId })
    if ($newSdkAfter.Count -ne 0 -or $newApplicationsAfter.Count -ne 0) {
        throw "A process created by the lifecycle scenario remains; stop and inspect it manually."
    }

    Write-Host "Licensed local lifecycle scenario '$Scenario' passed for SpatialAnalyzer $targetVersion."
    Write-Host "No MP values, paths, identity references, or proprietary data were logged."
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

    $residualSdk = @(
        Get-Process -Name "SpatialAnalyzerSDK" -ErrorAction SilentlyContinue |
            Where-Object { $_.Id -notin $beforeSdk })
    if ($residualSdk.Count -ne 0) {
        Write-Warning "An SDK process created during the test remains. It was not broadly terminated."
    }

    $residualApplications = @(
        Get-EligibleSpatialAnalyzerProcesses |
            Where-Object { $_.ProcessId -notin $beforeApplications.ProcessId })
    if ($residualApplications.Count -ne 0) {
        Write-Warning "A SpatialAnalyzer process created during the test remains. It was not forcefully terminated."
    }

    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith(
            $temporaryBase,
            [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
