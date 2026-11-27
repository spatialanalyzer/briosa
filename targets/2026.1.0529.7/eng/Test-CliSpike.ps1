[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath,
    [Parameter(Mandatory)][string]$ProtocolPath,
    [Parameter(Mandatory)][string]$RunDirectory,
    [string]$CliPath,
    [switch]$Timing
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$run = [IO.Path]::GetFullPath($RunDirectory)
[void][IO.Directory]::CreateDirectory($run)
$package = Join-Path $run 'extracted'
Expand-Archive -LiteralPath $PackagePath -DestinationPath $package -Force
$package = (Get-ChildItem -LiteralPath $package -Directory | Select-Object -First 1).FullName
if (-not $CliPath) {
    $CliPath = Join-Path $package 'Briosa.Cli.exe'
    if (-not (Test-Path -LiteralPath $CliPath -PathType Leaf)) {
        $CliPath = Join-Path $package 'cli/Briosa.Cli.exe'
    }
}

if ($Timing) {
    for ($pair = 0; $pair -lt 5; $pair++) {
        # Distinct apphost/runtime directories give first launch and immediate
        # relaunch samples without changing system-wide cache state.
        $timingRoot = Join-Path $run "timing-$pair"
        & robocopy (Split-Path -Parent $CliPath) $timingRoot /E /J /R:0 /W:0 /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Unbuffered timing copy failed with robocopy exit $LASTEXITCODE." }
        $executable = Join-Path $timingRoot 'Briosa.Cli.exe'
        foreach ($phase in 'first', 'warm') {
            $watch = [Diagnostics.Stopwatch]::StartNew()
            $version = & $executable --version
            $watch.Stop()
            if ($LASTEXITCODE -ne 0 -or -not $version) { throw 'Version probe failed.' }
            Write-Host ("version {0} {1} {2:F2} ms" -f $phase, $pair, $watch.Elapsed.TotalMilliseconds)
        }
    }
}

$mcp = [Diagnostics.Process]::new()
$mcp.StartInfo.FileName = $CliPath
$mcp.StartInfo.ArgumentList.Add('mcp')
$mcp.StartInfo.UseShellExecute = $false
$mcp.StartInfo.CreateNoWindow = $true
$mcp.StartInfo.RedirectStandardInput = $true
$mcp.StartInfo.RedirectStandardOutput = $true
$mcp.StartInfo.RedirectStandardError = $true
try {
    [void]$mcp.Start()
    $stderr = $mcp.StandardError.ReadToEndAsync()
    $mcp.StandardInput.WriteLine('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"spike","version":"0.0.0"}}}')
    $mcp.StandardInput.Flush()
    $read = $mcp.StandardOutput.ReadLineAsync()
    if (-not $read.Wait(10000) -or -not $read.Result) {
        if ($mcp.HasExited) { Write-Host $stderr.GetAwaiter().GetResult() }
        throw 'MCP did not return an initialize response.'
    }
    $initialize = $read.Result | ConvertFrom-Json
    if ($initialize.id -ne 1 -or $initialize.result.serverInfo.name -cne 'Briosa.Cli.Spike') {
        throw "Unexpected initialize response: $($read.Result)"
    }
    Write-Host "MCP initialize: $($read.Result)"
    $mcp.StandardInput.Close()
    if (-not $mcp.WaitForExit(10000) -or $mcp.ExitCode -ne 0) {
        if ($mcp.HasExited) { Write-Host $stderr.GetAwaiter().GetResult() }
        throw 'MCP process did not exit successfully after EOF.'
    }
}
finally {
    if (-not $mcp.HasExited) { $mcp.Kill($true); $mcp.WaitForExit() }
    $mcp.Dispose()
}

$descriptor = Join-Path $run 'briosa.protoset'
$archive = [IO.Compression.ZipFile]::OpenRead($ProtocolPath)
try {
    $entry = $archive.Entries | Where-Object FullName -Match '(^|/)descriptor/briosa\.protoset$'
    $inputStream = $entry.Open()
    try {
        $outputStream = [IO.File]::Create($descriptor)
        try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose() }
    }
    finally { $inputStream.Dispose() }
}
finally { $archive.Dispose() }

$fakeRoot = Join-Path $run 'fake-application'
[void][IO.Directory]::CreateDirectory($fakeRoot)
$smokeRoot = Join-Path $root 'tests/Briosa.SmokeWorker/bin/Release/net10.0-windows'
$fakeApp = Join-Path $fakeRoot 'Spatial Analyzer64.exe'
foreach ($leaf in 'Briosa.SmokeWorker.dll', 'Briosa.SmokeWorker.deps.json',
    'Briosa.SmokeWorker.runtimeconfig.json', 'Briosa.Worker.Control.dll') {
    Copy-Item -LiteralPath (Join-Path $smokeRoot $leaf) -Destination $fakeRoot
}
Copy-Item -LiteralPath (Join-Path $smokeRoot 'Briosa.SmokeWorker.exe') -Destination $fakeApp
$worker = Join-Path $root 'tests/Briosa.Worker.TestHost/bin/Release/net10.0-windows/Briosa.Worker.TestHost.exe'
if (-not (Test-Path -LiteralPath $worker -PathType Leaf)) { throw 'Build the fake worker first.' }
$env:Briosa__Worker__ExecutablePath = $worker
$env:Briosa__SpatialAnalyzer__ExecutablePath = $fakeApp
$env:Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Version = '2026.1.0529.7'
$env:Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Reference = 'spike-fake-worker'
$env:Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Version = '2026.1.0529.7'
$env:Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Reference = 'spike-fake-worker'
$env:Briosa__Logging__File__Directory = Join-Path $run 'server-logs'
$env:Briosa__Desktop__Mode = 'Disabled'
$env:Briosa__Security__Operations__Overrides__file_operations__get_working_directory = 'deny'
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
$listener.Stop()
$server = $null
$fake = Start-Process -FilePath $fakeApp -ArgumentList '--hold' -WorkingDirectory $fakeRoot -WindowStyle Hidden -PassThru
try {
    $server = Start-Process -FilePath (Join-Path $package 'Briosa.Server.exe') -WorkingDirectory $package `
        -ArgumentList "--Briosa:Endpoint:Address=127.0.0.1", "--Briosa:Endpoint:Port=$port" `
        -RedirectStandardOutput (Join-Path $run 'server.stdout.log') `
        -RedirectStandardError (Join-Path $run 'server.stderr.log') -WindowStyle Hidden -PassThru
    $ready = $false
    for ($attempt = 0; $attempt -lt 100 -and -not $server.HasExited; $attempt++) {
        $probe = [Net.Sockets.TcpClient]::new()
        try {
            $probe.Connect('127.0.0.1', $port)
            $ready = $true
            break
        }
        catch [Net.Sockets.SocketException] { Start-Sleep -Milliseconds 100 }
        finally { $probe.Dispose() }
    }
    if (-not $ready) { throw 'Fake-backed server did not listen.' }
    $address = "http://127.0.0.1:$port"
    & $CliPath verify $descriptor $address
    if ($LASTEXITCODE -ne 0) { throw 'Descriptor verification failed.' }
}
finally {
    foreach ($process in $server, $fake) {
        if ($null -ne $process) {
            if (-not $process.HasExited) {
                $process.Kill($true)
                $process.WaitForExit()
            }
            $process.Dispose()
        }
    }
}
