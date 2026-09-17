[CmdletBinding()]
param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version = "0.1.0-ci",

    [string]$OutputDirectory = "artifacts\package-smoke"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageScript = Join-Path $PSScriptRoot "New-WindowsPackage.ps1"
$workerTestHostProject = Join-Path $repositoryRoot "tests\Briosa.Worker.TestHost\Briosa.Worker.TestHost.csproj"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-package-test-$([Guid]::NewGuid().ToString('N'))"
$firstOutput = [IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot)
$secondOutput = Join-Path $temporaryRoot "second"
$extractRoot = Join-Path $temporaryRoot "extracted"
$workerTestHostOutput = Join-Path $temporaryRoot "worker-test-host"
$serverProcess = $null

function Assert-Condition {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Remove-TemporaryTree {
    param([Parameter(Mandatory)][string]$Path)

    for ($attempt = 1; $attempt -le 60; $attempt++) {
        try {
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            return
        }
        catch {
            if ($attempt -eq 60) {
                throw
            }
            Start-Sleep -Milliseconds 250
        }
    }
}

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$safeRepositoryRoot = $repositoryRoot.Replace('\', '/')
$sourceRevision = (& git -c "safe.directory=$safeRepositoryRoot" -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Could not determine a complete source revision."
}

$artifactBase = "briosa-$Version-sa-2024.1.0508.5-win-x64"
$zipName = "$artifactBase.zip"
$firstZip = Join-Path $firstOutput $zipName
$secondZip = Join-Path $secondOutput $zipName

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    Invoke-DotNet @("restore", $workerTestHostProject, "--locked-mode")
    Invoke-DotNet @(
        "build", $workerTestHostProject,
        "-c", "Release",
        "--no-restore",
        "-o", $workerTestHostOutput)

    $firstBuild = @{
        Version = $Version
        SourceRevision = $sourceRevision
        OutputDirectory = $firstOutput
    }
    & $packageScript @firstBuild
    $secondBuild = @{
        Version = $Version
        SourceRevision = $sourceRevision
        OutputDirectory = $secondOutput
        NoRestore = $true
    }
    & $packageScript @secondBuild

    $firstHash = (Get-FileHash -LiteralPath $firstZip -Algorithm SHA256).Hash
    $secondHash = (Get-FileHash -LiteralPath $secondZip -Algorithm SHA256).Hash
    Assert-Condition -Condition ($firstHash -eq $secondHash) -Message "Two clean package builds produced different SHA-256 hashes."
    $externalChecksumPath = "$firstZip.sha256"
    $externalChecksum = Get-Content -LiteralPath $externalChecksumPath -Raw
    Assert-Condition -Condition ($externalChecksum.Trim() -eq "$firstHash  $zipName") -Message "The external ZIP checksum does not match the package."

    Expand-Archive -LiteralPath $firstZip -DestinationPath $extractRoot
    $packageRoot = Join-Path $extractRoot $artifactBase
    Assert-Condition -Condition (Test-Path -LiteralPath $packageRoot -PathType Container) -Message "The archive does not contain the expected package root."

    $manifest = Get-Content -LiteralPath (Join-Path $packageRoot "manifest.json") -Raw | ConvertFrom-Json
    Assert-Condition -Condition ($manifest.briosaVersion -eq $Version) -Message "The manifest Briosa version is incorrect."
    Assert-Condition -Condition ($manifest.sourceRevision -eq $sourceRevision.ToLowerInvariant()) -Message "The manifest source revision is incorrect."
    Assert-Condition -Condition ($manifest.runtimeIdentifier -eq "win-x64") -Message "The manifest runtime identifier is incorrect."
    Assert-Condition -Condition ($manifest.selfContained -and -not $manifest.trimmed) -Message "The package must be self-contained and untrimmed."
    Assert-Condition -Condition ($manifest.schemaVersion -eq 2) -Message "The package manifest schema version is incorrect."
    Assert-Condition -Condition ($manifest.protocolPackage -eq "briosa") -Message "The package protocol identity is incorrect."
    Assert-Condition -Condition ($null -eq $manifest.PSObject.Properties["coreProtocolPackage"] -and $null -eq $manifest.PSObject.Properties["targetProtocolPackage"]) -Message "A retired versioned protocol identity leaked into the package."
    Assert-Condition -Condition ($null -eq $manifest.PSObject.Properties["catalogRevision"]) -Message "The retired catalog revision leaked into the package."
    Assert-Condition -Condition ($null -eq $manifest.PSObject.Properties["implementedOperations"]) -Message "Implemented operations belong to runtime discovery, not duplicated package metadata."
    Assert-Condition -Condition ($manifest.spatialAnalyzerTarget -eq "2024.1.0508.5") -Message "The package declares the wrong SpatialAnalyzer target."
    Assert-Condition -Condition ($null -eq $manifest.PSObject.Properties["supportedSpatialAnalyzerReleases"]) -Message "A multi-target release list leaked into the exact-target package."
    Assert-Condition -Condition (-not $manifest.spatialAnalyzerBundled) -Message "The package must not claim to bundle SpatialAnalyzer."

    $configuration = Get-Content -LiteralPath (Join-Path $packageRoot "appsettings.json") -Raw | ConvertFrom-Json
    $sourceConfiguration = Get-Content -LiteralPath (Join-Path $repositoryRoot "src\Briosa.Server\appsettings.json") -Raw | ConvertFrom-Json
    Assert-Condition -Condition ($configuration.Briosa.Endpoint.Address -eq "127.0.0.1") -Message "The packaged loopback address is incorrect."
    Assert-Condition -Condition ($configuration.Briosa.Endpoint.Port -eq 50051) -Message "The packaged endpoint port is incorrect."
    Assert-Condition -Condition ($configuration.Briosa.SpatialAnalyzer.Host -eq "localhost") -Message "The packaged SpatialAnalyzer target must default to localhost."
    Assert-Condition -Condition ($configuration.Briosa.Worker.ExecutionWatchdogTimeout -eq "00:00:30") -Message "The packaged execution watchdog default is incorrect."
    $packagedAllow = @($configuration.Briosa.Security.Operations.Allow)
    $sourceAllow = @($sourceConfiguration.Briosa.Security.Operations.Allow)
    Assert-Condition -Condition ($packagedAllow.Count -eq $sourceAllow.Count -and ($packagedAllow -join "`n") -eq ($sourceAllow -join "`n")) -Message "The packaged operation allowlist differs from the reviewed source configuration."
    $packagedDeny = @($configuration.Briosa.Security.Operations.Deny)
    $sourceDeny = @($sourceConfiguration.Briosa.Security.Operations.Deny)
    Assert-Condition -Condition ($packagedDeny.Count -eq $sourceDeny.Count -and ($packagedDeny -join "`n") -eq ($sourceDeny -join "`n")) -Message "The packaged operation denylist differs from the reviewed source configuration."

    foreach ($requiredFile in @(
        "Briosa.Server.exe",
        "Briosa.ControlCenter.exe",
        "Briosa.ControlCenter.dll",
        "Briosa.ControlCenter.runtimeconfig.json",
        "Briosa.Desktop.dll",
        "desktop/Briosa.ControlCenter.App.exe",
        "desktop/PresentationFramework.dll",
        "CONTROL-CENTER.md",
        "Briosa.Worker.exe",
        "Briosa.Worker.dll",
        "Briosa.Worker.deps.json",
        "Briosa.Worker.runtimeconfig.json",
        "Briosa.Worker.Control.dll",
        "Briosa.SpatialAnalyzer.Interop.dll")) {
        Assert-Condition `
            -Condition (Test-Path -LiteralPath (Join-Path $packageRoot $requiredFile) -PathType Leaf) `
            -Message "The package is missing required runtime file '$requiredFile'."
    }
    Assert-Condition `
        -Condition (-not (Test-Path -LiteralPath (Join-Path $packageRoot "Properties\launchSettings.json"))) `
        -Message "The development launch profile must not appear in the package."
    Assert-Condition `
        -Condition (-not (Test-Path -LiteralPath (Join-Path $packageRoot "appsettings.Development.json"))) `
        -Message "Development settings must not appear in the package."
    $serverAssemblyText = [Text.Encoding]::UTF8.GetString(
        [IO.File]::ReadAllBytes((Join-Path $packageRoot "Briosa.Server.dll")))
    Assert-Condition `
        -Condition (-not $serverAssemblyText.Contains(
            "spatialanalyzer-briosa-development",
            [StringComparison]::Ordinal)) `
        -Message "The Debug user-secrets identity must not appear in the Release server assembly."

    foreach ($developmentOnlyAssembly in @(
        "Grpc.AspNetCore.Server.Reflection.dll",
        "Grpc.Reflection.dll")) {
        Assert-Condition `
            -Condition (-not (Test-Path -LiteralPath (Join-Path $packageRoot $developmentOnlyAssembly))) `
            -Message "The Production package contains development-only gRPC reflection assembly '$developmentOnlyAssembly'."
    }
    $serverDependencies = Get-Content -LiteralPath (Join-Path $packageRoot "Briosa.Server.deps.json") -Raw
    Assert-Condition `
        -Condition (-not $serverDependencies.Contains("Grpc.AspNetCore.Server.Reflection", [StringComparison]::Ordinal)) `
        -Message "The Production dependency manifest includes the development reflection host."
    Assert-Condition `
        -Condition (-not $serverDependencies.Contains('Grpc.Reflection', [StringComparison]::Ordinal)) `
        -Message "The Production dependency manifest includes the reflection protocol runtime."

    $checksumRoot = Join-Path $packageRoot "files.sha256"
    foreach ($line in Get-Content -LiteralPath $checksumRoot) {
        $match = [regex]::Match($line, '^([0-9A-Fa-f]{64})  (.+)$')
        Assert-Condition -Condition $match.Success -Message "Malformed entry in files.sha256."
        $filePath = Join-Path $packageRoot $match.Groups[2].Value
        Assert-Condition -Condition (Test-Path -LiteralPath $filePath -PathType Leaf) -Message "A file listed in files.sha256 is missing."
        $actualHash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
        Assert-Condition -Condition ($actualHash -eq $match.Groups[1].Value) -Message "An internal package checksum does not match."
    }

    $serverExecutable = Join-Path $packageRoot "Briosa.Server.exe"
    $diagnosticsOutput = @(& $serverExecutable diagnostics)
    $diagnosticsExitCode = $LASTEXITCODE
    Assert-Condition -Condition ($diagnosticsExitCode -eq 0) -Message "Packaged offline diagnostics failed."
    $diagnostics = ($diagnosticsOutput -join [Environment]::NewLine) | ConvertFrom-Json
    Assert-Condition -Condition $diagnostics.ready_to_launch -Message "Packaged offline diagnostics did not report ready_to_launch."
    Assert-Condition -Condition ($diagnostics.spatial_analyzer_target -eq "2024.1.0508.5") -Message "Packaged diagnostics reported the wrong SpatialAnalyzer target."
    Assert-Condition -Condition (-not $diagnostics.spatial_analyzer_bundled) -Message "Packaged diagnostics must not claim to bundle SpatialAnalyzer."

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    $listener.Stop()

    $standardOutput = Join-Path $temporaryRoot "server.stdout.log"
    $standardError = Join-Path $temporaryRoot "server.stderr.log"
    $defaultLogDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Briosa\logs\2024.1.0508.5'
    $previousLogs = @(Get-ChildItem -LiteralPath $defaultLogDirectory -Filter 'briosa-*.jsonl' -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Name)
    $workerVariable = "Briosa__Worker__ExecutablePath"
    $previousWorkerPath = [Environment]::GetEnvironmentVariable($workerVariable)
    [Environment]::SetEnvironmentVariable(
        $workerVariable,
        (Join-Path $workerTestHostOutput "Briosa.Worker.TestHost.exe"))
    try {
        $processArguments = @{
            FilePath = $serverExecutable
            ArgumentList = @("--Briosa:Endpoint:Port=$port", "--Briosa:Desktop:Mode=Disabled")
            WorkingDirectory = $packageRoot
            WindowStyle = "Hidden"
            RedirectStandardOutput = $standardOutput
            RedirectStandardError = $standardError
            PassThru = $true
        }
        $startupStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $serverProcess = Start-Process @processArguments
    }
    finally {
        [Environment]::SetEnvironmentVariable($workerVariable, $previousWorkerPath)
    }

    $listening = $false
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    while ([DateTimeOffset]::UtcNow -lt $deadline -and -not $listening) {
        if ($serverProcess.HasExited) {
            break
        }

        $client = [Net.Sockets.TcpClient]::new()
        try {
            $connectTask = $client.ConnectAsync([Net.IPAddress]::Loopback, $port)
            $listening = $connectTask.Wait(250) -and $client.Connected
        }
        catch {
            $listening = $false
        }
        finally {
            $client.Dispose()
        }

        if (-not $listening) {
            Start-Sleep -Milliseconds 100
        }
    }

    $startupStopwatch.Stop()
    Assert-Condition -Condition $listening -Message "The packaged host did not open its configured loopback endpoint without SpatialAnalyzer."
    # The file writer is asynchronous: an open listener does not imply that its
    # startup record has reached disk. Verify the hidden host's default sink.
    $loggedStartup = $false
    $logDeadline = [DateTimeOffset]::UtcNow.AddSeconds(15)
    while (-not $loggedStartup -and [DateTimeOffset]::UtcNow -lt $logDeadline) {
        foreach ($file in Get-ChildItem -LiteralPath $defaultLogDirectory -Filter 'briosa-*.jsonl' -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -notin $previousLogs }) {
            try {
                foreach ($line in Get-Content -LiteralPath $file.FullName -ErrorAction Stop) {
                    $record = $line | ConvertFrom-Json -ErrorAction Stop
                    if ($record.MessageTemplate -eq 'ControlPlaneReady' -and
                        $record.Properties.SchemaVersion -eq 1 -and
                        $file.Name.Contains($record.Properties.ServerInstanceId)) {
                        $loggedStartup = $true
                    }
                }
            }
            catch { } # A writer may still be completing its first JSONL record.
        }
        if (-not $loggedStartup) { Start-Sleep -Milliseconds 100 }
    }
    Assert-Condition -Condition $loggedStartup -Message "The hidden packaged host did not persist a structured startup record in its default per-user log directory."
    $serverProcess.Refresh()

    if (-not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
        $serverProcess.WaitForExit()
    }
    $serverProcess.Dispose()
    $serverProcess = $null

    $unsafeStandardOutput = Join-Path $temporaryRoot "unsafe-server.stdout.log"
    $unsafeStandardError = Join-Path $temporaryRoot "unsafe-server.stderr.log"
    $unsafeProcessArguments = @{
        FilePath = $serverExecutable
        ArgumentList = @(
            "--Briosa:Endpoint:Address=0.0.0.0",
            "--Briosa:Desktop:Mode=Disabled",
            "--Briosa:Endpoint:Port=$port")
        WorkingDirectory = $packageRoot
        WindowStyle = "Hidden"
        RedirectStandardOutput = $unsafeStandardOutput
        RedirectStandardError = $unsafeStandardError
        PassThru = $true
    }
    $serverProcess = Start-Process @unsafeProcessArguments
    $unsafeProcessExited = $serverProcess.WaitForExit(10000)
    Assert-Condition `
        -Condition $unsafeProcessExited `
        -Message "The packaged host did not reject a non-loopback endpoint."
    $serverProcess.WaitForExit()
    Assert-Condition `
        -Condition ($serverProcess.ExitCode -ne 0) `
        -Message "The packaged host accepted a non-loopback endpoint."
    $serverProcess.Dispose()
    $serverProcess = $null
    $desktopSmoke = Join-Path $repositoryRoot "tests/Briosa.ControlCenter.Smoke/Briosa.ControlCenter.Smoke.csproj"
    Invoke-DotNet @("restore", $desktopSmoke, "--locked-mode")
    Invoke-DotNet @("build", $desktopSmoke, "-c", "Release", "--no-restore")
    Invoke-DotNet @("run", "--project", $desktopSmoke, "-c", "Release", "--no-build", "--no-restore", "--",
        "--package", $packageRoot, (Join-Path $workerTestHostOutput "Briosa.Worker.TestHost.exe"))
    Write-Host "Package reproducibility, checksums, diagnostics, desktop ownership, and launch smoke tests passed."
}
finally {
    if ($null -ne $serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
        $serverProcess.WaitForExit()
    }

    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-TemporaryTree -Path $resolvedTemporaryRoot
    }
}
