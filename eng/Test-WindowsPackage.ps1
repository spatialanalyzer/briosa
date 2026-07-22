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
$coveragePath = Join-Path $repositoryRoot "generated\catalog\sa\2026.1.0529.7\coverage.json"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-package-test-$([Guid]::NewGuid().ToString('N'))"
$firstOutput = [IO.Path]::GetFullPath($OutputDirectory, $repositoryRoot)
$secondOutput = Join-Path $temporaryRoot "second"
$extractRoot = Join-Path $temporaryRoot "extracted"
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

$sourceRevision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Could not determine a complete source revision."
}

$coverage = Get-Content -LiteralPath $coveragePath -Raw | ConvertFrom-Json
$artifactBase = "briosa-$Version-sa-$($coverage.spatial_analyzer_target)-win-x64"
$zipName = "$artifactBase.zip"
$firstZip = Join-Path $firstOutput $zipName
$secondZip = Join-Path $secondOutput $zipName

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
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
    Assert-Condition -Condition ($manifest.supportedSpatialAnalyzerReleases.Count -eq 1) -Message "The package must declare exactly one supported SpatialAnalyzer release."
    Assert-Condition -Condition ($manifest.supportedSpatialAnalyzerReleases[0] -eq "2026.1.0529.7") -Message "The package declares the wrong SpatialAnalyzer release."
    Assert-Condition -Condition (-not $manifest.spatialAnalyzerBundled) -Message "The package must not claim to bundle SpatialAnalyzer."

    $configuration = Get-Content -LiteralPath (Join-Path $packageRoot "appsettings.json") -Raw | ConvertFrom-Json
    Assert-Condition -Condition ($configuration.Briosa.Endpoint.Address -eq "127.0.0.1") -Message "The packaged loopback address is incorrect."
    Assert-Condition -Condition ($configuration.Briosa.Endpoint.Port -eq 50051) -Message "The packaged endpoint port is incorrect."
    Assert-Condition -Condition ($configuration.Briosa.SpatialAnalyzer.Host -eq "localhost") -Message "The packaged SpatialAnalyzer target must default to localhost."
    Assert-Condition -Condition ($configuration.Briosa.Worker.ExecutionWatchdogTimeout -eq "00:00:30") -Message "The packaged execution watchdog default is incorrect."

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
    Assert-Condition -Condition ($diagnostics.spatial_analyzer_target -eq "2026.1.0529.7") -Message "Packaged diagnostics reported the wrong SpatialAnalyzer target."
    Assert-Condition -Condition (-not $diagnostics.spatial_analyzer_bundled) -Message "Packaged diagnostics must not claim to bundle SpatialAnalyzer."

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    $listener.Stop()

    $standardOutput = Join-Path $temporaryRoot "server.stdout.log"
    $standardError = Join-Path $temporaryRoot "server.stderr.log"
    $workerVariable = "Briosa__Worker__ExecutablePath"
    $previousWorkerPath = [Environment]::GetEnvironmentVariable($workerVariable)
    [Environment]::SetEnvironmentVariable($workerVariable, (Join-Path $temporaryRoot "intentionally-missing-worker.exe"))
    try {
        $processArguments = @{
            FilePath = $serverExecutable
            ArgumentList = @("--Briosa:Endpoint:Port=$port")
            WorkingDirectory = $packageRoot
            WindowStyle = "Hidden"
            RedirectStandardOutput = $standardOutput
            RedirectStandardError = $standardError
            PassThru = $true
        }
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

    Assert-Condition -Condition $listening -Message "The packaged host did not open its configured loopback endpoint without SpatialAnalyzer."

    if (-not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
        $serverProcess.WaitForExit()
    }
    $serverProcess = $null

    $unsafeStandardOutput = Join-Path $temporaryRoot "unsafe-server.stdout.log"
    $unsafeStandardError = Join-Path $temporaryRoot "unsafe-server.stderr.log"
    $unsafeProcessArguments = @{
        FilePath = $serverExecutable
        ArgumentList = @(
            "--Briosa:Endpoint:Address=0.0.0.0",
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
    $serverProcess = $null
    Write-Host "Package reproducibility, checksums, diagnostics, and launch smoke tests passed."
}
finally {
    if ($null -ne $serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
        $serverProcess.WaitForExit()
    }

    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
