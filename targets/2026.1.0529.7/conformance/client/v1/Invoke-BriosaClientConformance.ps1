[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$FixtureCommand,

    [string[]]$FixtureArguments = @(),

    [string[]]$Scenario = @(),

    # Longer than the first-party clients' 120 s startup budget, so a slow start
    # or a hang ends in the client's own typed startup timeout, whose error names
    # the pending call, before the runner has to kill the fixture.
    [ValidateRange(5, 600)]
    [int]$FixtureTimeoutSeconds = 180,

    # A failing scenario copies the full fixture output and the server's log
    # files here. Callers that cannot pass the parameter, such as a client
    # repository's test script, can set BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY.
    [string]$DiagnosticsDirectory = $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw "The Briosa client conformance host requires 64-bit Windows."
}

$packageRoot = Split-Path -Parent $PSScriptRoot
$contractPath = Join-Path $packageRoot "contract\scenarios.json"
$manifestPath = Join-Path $packageRoot "manifest.json"
$serverPath = Join-Path $packageRoot "server\Briosa.Server.exe"
$workerPath = Join-Path $packageRoot "fake-worker\Briosa.SmokeWorker.exe"
$applicationPath = $null
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-conformance-$([Guid]::NewGuid().ToString('N'))"
$invariantCulture = [Globalization.CultureInfo]::InvariantCulture

# The fake SDK host tests client behaviour, not start latency, so its worker start
# gets more room than the server's 10 s default. The first fake-worker start after
# a fresh package extraction can miss 10 s on a slow runner. A caller that sets
# the key itself, for example to reproduce that timeout, keeps its value.
$workerStartupTimeout = $(if ([string]::IsNullOrWhiteSpace($env:Briosa__Worker__StartupTimeout)) {
        "00:00:30"
    } else {
        $env:Briosa__Worker__StartupTimeout
    })

# Opt-in: on a fixture timeout, write heap dumps of the server and of a .NET
# fixture to the diagnostics directory before anything is killed.
$dumpOnTimeout = "$env:BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT".Trim() -in @("1", "true")

# The failure digest prints only these server log properties. The server already
# admits only reviewed metadata; this list keeps the digest to the phase timeline.
$digestProperties = @(
    "Rpc", "GrpcStatus", "Phase", "ElapsedMilliseconds",
    "WorkerState", "ApplicationState", "Ownership", "Generation", "ConnectionState",
    "OperationId", "ExecutionDisposition", "MpResultCode", "DiagnosticCode")
$digestEventLimit = 200
$digestImportantLevels = @("Warning", "Error", "Fatal")

function Get-PackageProcesses {
    $expectedPaths = @($serverPath, $workerPath, $applicationPath) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    return @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        try {
            $_.Path -in $expectedPaths
        }
        catch {
            $false
        }
    })
}

function Stop-PackageProcesses {
    foreach ($process in @(Get-PackageProcesses)) {
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction Stop
            $process.WaitForExit(5000) | Out-Null
        }
        catch {
        }
    }
}

function New-ProcessStartInfo {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [hashtable]$Environment = @{},
        [switch]$RedirectOutput
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }
    foreach ($entry in $Environment.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            $startInfo.Environment.Remove($entry.Key) | Out-Null
        }
        else {
            $startInfo.Environment[$entry.Key] = [string]$entry.Value
        }
    }
    if ($RedirectOutput) {
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
    }
    return $startInfo
}

function Get-SafeText {
    param([AllowEmptyString()][string]$Text)

    $safe = $Text.Replace($temporaryRoot, "<temporary-root>")
    if (-not [string]::IsNullOrWhiteSpace($DiagnosticsDirectory)) {
        # A runtime that writes a requested dump reports its path on stdout.
        $safe = $safe.Replace([IO.Path]::GetFullPath($DiagnosticsDirectory), "<diagnostics-directory>")
    }
    return $safe
}

function Format-Invariant {
    param(
        [Parameter(Mandatory)][string]$Format,
        [object[]]$Values = @()
    )

    return [string]::Format($invariantCulture, $Format, $Values)
}

function Format-Offset {
    param(
        [Parameter(Mandatory)][DateTimeOffset]$Time,
        [Parameter(Mandatory)][DateTimeOffset]$Reference
    )

    return Format-Invariant "{0:+0.000;-0.000} s" @(($Time - $Reference).TotalSeconds)
}

function Format-UtcTime {
    param([Parameter(Mandatory)][DateTimeOffset]$Time)

    return $Time.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", $invariantCulture)
}

function Get-TimingReference {
    param([Parameter(Mandatory)][hashtable]$Timing)

    if ($null -ne $Timing.FixtureStarted) {
        return [pscustomobject]@{ Time = [DateTimeOffset]$Timing.FixtureStarted; Name = "fixture start" }
    }
    return [pscustomobject]@{ Time = [DateTimeOffset]$Timing.ScenarioStarted; Name = "scenario start" }
}

function Get-PackageProcessRole {
    param([Parameter(Mandatory)][Diagnostics.Process]$Process)

    try {
        $path = $Process.Path
    }
    catch {
        return "unknown"
    }
    if ([string]::Equals($path, $serverPath, [StringComparison]::OrdinalIgnoreCase)) { return "server" }
    if ([string]::Equals($path, $workerPath, [StringComparison]::OrdinalIgnoreCase)) { return "worker" }
    return "application"
}

function Write-RunnerTiming {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][hashtable]$Timing
    )

    $scenarioStarted = [DateTimeOffset]$Timing.ScenarioStarted
    $parts = [Collections.Generic.List[string]]::new()
    $parts.Add("scenario start $(Format-UtcTime $scenarioStarted)")
    foreach ($mark in @(@("FixtureStarted", "fixture start"), @("FixtureEnded", "fixture end"))) {
        if ($null -ne $Timing[$mark[0]]) {
            $time = [DateTimeOffset]$Timing[$mark[0]]
            $parts.Add("$($mark[1]) $(Format-UtcTime $time) ($(Format-Offset $time $scenarioStarted))")
        }
        else {
            $parts.Add("$($mark[1]) not reached")
        }
    }
    $line = "Runner timing for $ScenarioId (UTC): $($parts -join ', ')$(if ($Timing.TimedOut) { ', fixture timed out' })."
    Write-Host $line
    return $line
}

# Called on a fixture timeout before anything is killed: the client owns the server,
# so killing the fixture tree also ends the server and its worker.
function Write-TimeoutProcesses {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][Diagnostics.Process]$Fixture,
        [Parameter(Mandatory)][hashtable]$Timing
    )

    $reference = Get-TimingReference $Timing
    Write-Host "--- processes alive at the fixture timeout in $ScenarioId (start times from $($reference.Name)) ---"
    $entries = @([pscustomobject]@{ Role = "fixture"; Process = $Fixture }) +
        @(Get-PackageProcesses | ForEach-Object {
            [pscustomobject]@{ Role = (Get-PackageProcessRole $_); Process = $_ }
        })
    foreach ($entry in $entries) {
        try {
            $entry.Process.Refresh()
            $started = [DateTimeOffset]$entry.Process.StartTime
            Write-Host (Format-Invariant "{0} pid {1}: started {2}, CPU {3:0.00} s" @(
                $entry.Role, $entry.Process.Id, (Format-Offset $started $reference.Time),
                $entry.Process.TotalProcessorTime.TotalSeconds))
        }
        catch {
            Write-Host "$($entry.Role) pid $($entry.Process.Id): exited or unreadable"
        }
    }
    Write-Host "--- end processes for $ScenarioId ---"
}

function Request-RuntimeHeapDump {
    param(
        [Parameter(Mandatory)][int]$ProcessId,
        [Parameter(Mandatory)][string]$DumpPath
    )

    # Current .NET runtimes no longer let createdump attach to a pid directly. The
    # runtime writes the dump itself, with the createdump beside it, when its
    # diagnostics channel receives GenerateCoreDump: the request dotnet-dump sends
    # (dotnet/diagnostics, docs/design-docs/ipc-protocol.md). The header is
    # "DOTNET_IPC_V1\0", the total size (uint16), the command set (0x01, dump), the
    # command (0x01), and a reserved uint16. The payload is the dump path (uint32
    # UTF-16 length with the terminator, then UTF-16LE), the dump type (2, with
    # heap), and a diagnostics flag (0). The reply's header has command set 0xFF and
    # command 0x00 on success, and is followed by an HRESULT.
    $pipe = [IO.Pipes.NamedPipeClientStream]::new(".", "dotnet-diagnostic-$ProcessId",
        [IO.Pipes.PipeDirection]::InOut, [IO.Pipes.PipeOptions]::Asynchronous)
    try {
        try {
            $pipe.Connect(3000)
        }
        catch [TimeoutException] {
            return "no-channel"
        }
        $payload = [IO.MemoryStream]::new()
        $writer = [IO.BinaryWriter]::new($payload)
        $writer.Write([uint32]($DumpPath.Length + 1))
        $writer.Write([Text.Encoding]::Unicode.GetBytes($DumpPath + [char]0))
        $writer.Write([uint32]2)
        $writer.Write([uint32]0)
        $writer.Flush()
        $body = $payload.ToArray()
        $message = [byte[]]::new(20 + $body.Length)
        $magic = [Text.Encoding]::ASCII.GetBytes("DOTNET_IPC_V1")
        [Array]::Copy($magic, 0, $message, 0, $magic.Length)
        [BitConverter]::GetBytes([uint16]$message.Length).CopyTo($message, 14)
        $message[16] = 0x01
        $message[17] = 0x01
        [Array]::Copy($body, 0, $message, 20, $body.Length)
        $pipe.Write($message, 0, $message.Length)
        $pipe.Flush()
        $reply = [byte[]]::new(24)
        $read = 0
        while ($read -lt $reply.Length) {
            $pending = $pipe.ReadAsync($reply, $read, $reply.Length - $read)
            if (-not $pending.Wait(120000)) {
                return "timed-out"
            }
            if ($pending.Result -eq 0) {
                break
            }
            $read += $pending.Result
        }
        if ($read -lt $reply.Length) {
            return "no-reply"
        }
        $result = [BitConverter]::ToUInt32($reply, 20)
        if ($reply[16] -eq 0xFF -and $reply[17] -eq 0x00 -and $result -eq 0) {
            return "written"
        }
        return Format-Invariant "HRESULT 0x{0:X8}" @($result)
    }
    finally {
        $pipe.Dispose()
    }
}

function Save-TimeoutDumps {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][Diagnostics.Process]$Fixture
    )

    if (-not $dumpOnTimeout) {
        return
    }
    if ([string]::IsNullOrWhiteSpace($DiagnosticsDirectory)) {
        Write-Host "Skipped timeout heap dumps for $($ScenarioId): no diagnostics directory is configured."
        return
    }
    $dumpRoot = Join-Path (Join-Path ([IO.Path]::GetFullPath($DiagnosticsDirectory)) $ScenarioId) "dumps"
    [IO.Directory]::CreateDirectory($dumpRoot) | Out-Null
    # The server first: its state at the timeout matters most. A process is paused
    # while its dump is written, typically for a few seconds.
    $targets = @(Get-PackageProcesses | Where-Object { (Get-PackageProcessRole $_) -eq "server" } | ForEach-Object {
            [pscustomobject]@{ Role = "server"; Process = $_ }
        }) +
        @([pscustomobject]@{ Role = "fixture"; Process = $Fixture })
    foreach ($target in $targets) {
        $processId = $target.Process.Id
        $name = "$($target.Role)-$processId.dmp"
        $dumpPath = Join-Path $dumpRoot $name
        try {
            $outcome = Request-RuntimeHeapDump -ProcessId $processId -DumpPath $dumpPath
        }
        catch {
            $outcome = "request-failed"
        }
        if ($outcome -eq "written" -and (Test-Path -LiteralPath $dumpPath -PathType Leaf)) {
            Write-Host (Format-Invariant "Wrote a heap dump of the {0} (pid {1}) to dumps/{2} ({3:0.0} MiB)." @(
                $target.Role, $processId, $name, ((Get-Item -LiteralPath $dumpPath).Length / 1MB)))
        }
        elseif ($outcome -eq "no-channel") {
            Write-Host "No heap dump of the $($target.Role) (pid $processId): it offers no .NET diagnostics channel."
        }
        else {
            Write-Host "No heap dump of the $($target.Role) (pid $processId): the dump request ended with $outcome."
        }
    }
}

function Read-SharedText {
    param([Parameter(Mandatory)][string]$Path)

    # A server that was killed, or one still shutting down, may hold the file open.
    $stream = [IO.FileStream]::new($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read,
        [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try {
        $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false))
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Get-JsonScalar {
    param([Parameter(Mandatory)][Text.Json.JsonElement]$Element)

    switch ($Element.ValueKind) {
        ([Text.Json.JsonValueKind]::String) { return $Element.GetString() }
        ([Text.Json.JsonValueKind]::Number) { return $Element.GetRawText() }
        ([Text.Json.JsonValueKind]::True) { return "true" }
        ([Text.Json.JsonValueKind]::False) { return "false" }
    }
    return $null
}

function Get-JsonFields {
    param([Parameter(Mandatory)][Text.Json.JsonElement]$Element)

    $fields = @{}
    if ($Element.ValueKind -eq [Text.Json.JsonValueKind]::Object) {
        foreach ($property in $Element.EnumerateObject()) {
            $fields[$property.Name] = $property.Value
        }
    }
    return $fields
}

function Get-DigestText {
    param([AllowNull()][string]$Value)

    # Server records hold reviewed metadata only. The digest still prints nothing
    # but identifier characters, so no path or free text can reach a CI log.
    if ([string]::IsNullOrEmpty($Value)) {
        return $null
    }
    $text = $Value -replace '[^A-Za-z0-9_.,/-]', '?'
    if ($text.Length -gt 96) {
        $text = $text.Substring(0, 96)
    }
    return $text
}

function ConvertFrom-ServerLogLine {
    param([Parameter(Mandatory)][string]$Line)

    try {
        $document = [Text.Json.JsonDocument]::Parse($Line)
    }
    catch {
        return $null
    }
    try {
        $fields = Get-JsonFields $document.RootElement
        if (-not $fields.ContainsKey("Timestamp")) {
            return $null
        }
        $time = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse(
                (Get-JsonScalar $fields["Timestamp"]), $invariantCulture,
                [Globalization.DateTimeStyles]::AssumeUniversal, [ref]$time)) {
            return $null
        }
        $level = $(if ($fields.ContainsKey("Level")) { Get-DigestText (Get-JsonScalar $fields["Level"]) })
        $properties = $(if ($fields.ContainsKey("Properties")) { Get-JsonFields $fields["Properties"] } else { @{} })
        $name = $null
        if ($properties.ContainsKey("EventId")) {
            $eventId = Get-JsonFields $properties["EventId"]
            if ($eventId.ContainsKey("Name")) {
                $name = Get-DigestText (Get-JsonScalar $eventId["Name"])
            }
        }
        if ($null -eq $name -and $fields.ContainsKey("MessageTemplate")) {
            $name = Get-DigestText (Get-JsonScalar $fields["MessageTemplate"])
        }
        $values = [ordered]@{}
        foreach ($property in $digestProperties) {
            if ($properties.ContainsKey($property)) {
                $value = Get-DigestText (Get-JsonScalar $properties[$property])
                if ($null -ne $value) {
                    $values[$property] = $value
                }
            }
        }
        return [pscustomobject]@{
            Time = $time
            Level = $(if ($null -ne $level) { $level } else { "Information" })
            Name = $(if ($null -ne $name) { $name } else { "UnnamedEvent" })
            Instance = $(if ($properties.ContainsKey("ServerInstanceId")) {
                    Get-DigestText (Get-JsonScalar $properties["ServerInstanceId"])
                })
            Values = $values
        }
    }
    finally {
        $document.Dispose()
    }
}

function Read-ServerLogEvents {
    param([Parameter(Mandatory)][string]$ScenarioRoot)

    $events = [Collections.Generic.List[object]]::new()
    $unreadable = 0
    $logRoot = Join-Path $ScenarioRoot "server-logs"
    $files = @()
    if (Test-Path -LiteralPath $logRoot -PathType Container) {
        $files = @(Get-ChildItem -LiteralPath $logRoot -Filter "*.jsonl" -File | Sort-Object Name)
    }
    foreach ($file in $files) {
        foreach ($line in ((Read-SharedText $file.FullName) -split "\r?\n")) {
            if ([string]::IsNullOrWhiteSpace($line)) {
                continue
            }
            $record = ConvertFrom-ServerLogLine $line
            if ($null -eq $record) {
                # For example, the last line of a file whose writer was killed.
                $unreadable++
                continue
            }
            $events.Add($record)
        }
    }
    return [pscustomobject]@{
        Files = $files.Count
        Events = @($events | Sort-Object -Property Time -Stable)
        Unreadable = $unreadable
    }
}

# One line per server event, relative to the fixture start: the phase timeline of
# a rejected scenario, readable in the CI log without downloading anything.
function Write-ServerLogDigest {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [Parameter(Mandatory)][hashtable]$Timing
    )

    $lines = [Collections.Generic.List[string]]::new()
    $reference = Get-TimingReference $Timing
    $lines.Add("--- server log digest for $ScenarioId (times from $($reference.Name)) ---")
    try {
        $log = Read-ServerLogEvents -ScenarioRoot $ScenarioRoot
        $events = @($log.Events)
        if ($log.Files -eq 0) {
            $lines.Add("(no server log file was found)")
        }
        # Keep the latest events, but never drop a warning or an error.
        $kept = $events
        if ($events.Count -gt $digestEventLimit) {
            $important = @($events | Where-Object { $_.Level -in $digestImportantLevels }).Count
            $budget = [Math]::Max(0, $digestEventLimit - $important)
            $keep = [bool[]]::new($events.Count)
            for ($index = $events.Count - 1; $index -ge 0; $index--) {
                if ($events[$index].Level -in $digestImportantLevels) {
                    $keep[$index] = $true
                }
                elseif ($budget -gt 0) {
                    $keep[$index] = $true
                    $budget--
                }
            }
            $kept = @(for ($index = 0; $index -lt $events.Count; $index++) {
                    if ($keep[$index]) { $events[$index] }
                })
            $lines.Add("($($events.Count - $kept.Count) earlier lower-severity events omitted; warnings and errors are always kept)")
        }
        $instances = @($events | ForEach-Object { $_.Instance } | Where-Object { $null -ne $_ } | Select-Object -Unique)
        # The runner's own fixture marks, in the same timeline. Events after the
        # fixture end come from processes that ran on until the runner killed them.
        $marks = @(
            @("FixtureStarted", "FixtureStart"),
            @("FixtureEnded", $(if ($Timing.TimedOut) { "FixtureTimedOut" } else { "FixtureEnd" }))) |
            Where-Object { $null -ne $Timing[$_[0]] } |
            ForEach-Object {
                [pscustomobject]@{
                    Time = [DateTimeOffset]$Timing[$_[0]]
                    Level = "Runner"
                    Name = $_[1]
                    Instance = $null
                    Values = [ordered]@{}
                }
            }
        foreach ($event in @(@($kept) + @($marks) | Sort-Object -Property Time -Stable)) {
            $server = ""
            if ($instances.Count -gt 1 -and $null -ne $event.Instance) {
                $server = " [server $([Array]::IndexOf($instances, $event.Instance) + 1)]"
            }
            $values = @($event.Values.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" })
            $lines.Add(("{0} {1}{2} {3} {4}" -f (Format-Offset $event.Time $reference.Time), $event.Level,
                $server, $event.Name, ($values -join " ")).TrimEnd())
        }
        if ($log.Unreadable -gt 0) {
            $lines.Add("($($log.Unreadable) unreadable server log lines skipped)")
        }
    }
    catch {
        $lines.Add("(the server log digest could not be produced)")
    }
    $lines.Add("--- end server log digest for $ScenarioId ---")
    foreach ($line in $lines) {
        Write-Host $line
    }
    return $lines
}

# Phase durations for a passed scenario, so CI logs accumulate a phase-level
# timing history. Missing phases are left out; this never fails a scenario.
function Get-ScenarioPhaseSummary {
    param(
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [Parameter(Mandatory)][hashtable]$Timing
    )

    try {
        $parts = [Collections.Generic.List[string]]::new()
        if ($null -ne $Timing.FixtureStarted -and $null -ne $Timing.FixtureEnded) {
            $parts.Add((Format-Invariant "fixture {0:0.0} s" @(
                ([DateTimeOffset]$Timing.FixtureEnded - [DateTimeOffset]$Timing.FixtureStarted).TotalSeconds)))
        }
        $events = @((Read-ServerLogEvents -ScenarioRoot $ScenarioRoot).Events)
        $listening = @($events | Where-Object { $_.Name -eq "EndpointListening" }) | Select-Object -First 1
        if ($null -ne $listening -and $null -ne $Timing.FixtureStarted) {
            $parts.Add("server listening $(Format-Offset $listening.Time ([DateTimeOffset]$Timing.FixtureStarted))")
        }
        foreach ($phase in @(
                @("StartSpatialAnalyzerSdk", "worker start"),
                @("LaunchSpatialAnalyzer", "launch"),
                @("ConnectToSpatialAnalyzer", "connect"))) {
            $completed = @($events | Where-Object {
                    $_.Name -eq "LifecycleRpcCompleted" -and $_.Values["Rpc"] -eq $phase[0] -and
                    $_.Values.Contains("ElapsedMilliseconds")
                }) | Select-Object -First 1
            if ($null -ne $completed) {
                $parts.Add("$($phase[1]) $($completed.Values["ElapsedMilliseconds"]) ms")
            }
        }
        return ($parts -join "; ")
    }
    catch {
        return ""
    }
}

function Save-FixtureOutput {
    param(
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [AllowEmptyString()][string]$Output,
        [AllowEmptyString()][string]$ErrorOutput
    )

    $encoding = [Text.UTF8Encoding]::new($false)
    [IO.File]::WriteAllText(
        (Join-Path $ScenarioRoot "fixture.stdout.txt"), (Get-SafeText $Output), $encoding)
    [IO.File]::WriteAllText(
        (Join-Path $ScenarioRoot "fixture.stderr.txt"), (Get-SafeText $ErrorOutput), $encoding)
}

function Write-FixtureError {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot
    )

    # Print every saved line so a CI log keeps the complete client traceback.
    Write-Host "--- client fixture stderr for $ScenarioId ---"
    $errorPath = Join-Path $ScenarioRoot "fixture.stderr.txt"
    if (Test-Path -LiteralPath $errorPath -PathType Leaf) {
        foreach ($line in [IO.File]::ReadAllLines($errorPath)) {
            Write-Host $line
        }
    }
    else {
        Write-Host "(no fixture stderr was captured)"
    }
    Write-Host "--- end client fixture stderr for $ScenarioId ---"
}

function Save-ScenarioDiagnostics {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [Parameter(Mandatory)][string]$Failure
    )

    if ([string]::IsNullOrWhiteSpace($DiagnosticsDirectory)) {
        return
    }

    $destination = Join-Path ([IO.Path]::GetFullPath($DiagnosticsDirectory)) $ScenarioId
    [IO.Directory]::CreateDirectory($destination) | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $ScenarioRoot -Force)) {
        # The fake application directory holds only copied package binaries.
        if ($item.Name -ne "fake-application") {
            Copy-Item -LiteralPath $item.FullName -Destination $destination -Recurse -Force
        }
    }
    [IO.File]::WriteAllText(
        (Join-Path $destination "failure.txt"),
        (Get-SafeText $Failure) + [Environment]::NewLine,
        [Text.UTF8Encoding]::new($false))
    Write-Host "Saved diagnostics for conformance scenario '$ScenarioId' to '$destination'."
}

function Publish-ScenarioFailure {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [Parameter(Mandatory)][string]$Failure,
        [Parameter(Mandatory)][hashtable]$Timing
    )

    # The single path for every rejected scenario, whether the fixture failed,
    # timed out, or returned an unacceptable report, or cleanup found a problem.
    # The server writes its file log unbuffered, so the log still holds its
    # events after a forced kill.
    Write-FixtureError -ScenarioId $ScenarioId -ScenarioRoot $ScenarioRoot
    # Diagnostics only: nothing here may replace the scenario's own failure.
    try {
        $timingLine = Write-RunnerTiming -ScenarioId $ScenarioId -Timing $Timing
        $digest = @(Write-ServerLogDigest -ScenarioId $ScenarioId -ScenarioRoot $ScenarioRoot -Timing $Timing)
        [IO.File]::WriteAllLines(
            (Join-Path $ScenarioRoot "server-log-digest.txt"),
            [string[]](@($timingLine) + $digest),
            [Text.UTF8Encoding]::new($false))
    }
    catch {
        Write-Host "(the runner timing and server log digest could not be produced)"
    }
    Save-ScenarioDiagnostics -ScenarioId $ScenarioId -ScenarioRoot $ScenarioRoot -Failure $Failure
}

function Invoke-Fixture {
    param(
        [Parameter(Mandatory)][object]$ScenarioDefinition,
        [Parameter(Mandatory)][hashtable]$Environment,
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [Parameter(Mandatory)][hashtable]$Timing
    )

    $arguments = @($FixtureArguments) + @(
        "--scenario", [string]$ScenarioDefinition.id,
        "--contract", $contractPath)
    $startInfo = New-ProcessStartInfo `
        -FilePath $FixtureCommand `
        -Arguments $arguments `
        -WorkingDirectory (Get-Location).Path `
        -Environment $Environment `
        -RedirectOutput
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "The client fixture could not be started."
        }
        $Timing.FixtureStarted = [DateTimeOffset]::UtcNow
        $outputTask = $process.StandardOutput.ReadToEndAsync()
        $errorTask = $process.StandardError.ReadToEndAsync()
        $exited = $process.WaitForExit($FixtureTimeoutSeconds * 1000)
        $Timing.FixtureEnded = [DateTimeOffset]::UtcNow
        if (-not $exited) {
            $Timing.TimedOut = $true
            # Record what is still running before the kill, which also ends the
            # client-owned server and its worker.
            try {
                Write-TimeoutProcesses -ScenarioId ([string]$ScenarioDefinition.id) -Fixture $process -Timing $Timing
                Save-TimeoutDumps -ScenarioId ([string]$ScenarioDefinition.id) -Fixture $process
            }
            catch {
                Write-Host "(the processes alive at the fixture timeout could not be recorded)"
            }
            try {
                $process.Kill($true)
            }
            catch [InvalidOperationException] {
                # The fixture exited after the wait elapsed.
            }
            Stop-PackageProcesses
            if ([Threading.Tasks.Task]::WaitAll(@($outputTask, $errorTask), 5000)) {
                Save-FixtureOutput `
                    -ScenarioRoot $ScenarioRoot `
                    -Output $outputTask.GetAwaiter().GetResult() `
                    -ErrorOutput $errorTask.GetAwaiter().GetResult()
            }
            throw "The client fixture timed out after $FixtureTimeoutSeconds s in scenario '$($ScenarioDefinition.id)'."
        }
        $output = $outputTask.GetAwaiter().GetResult().Trim()
        $errorOutput = $errorTask.GetAwaiter().GetResult().Trim()
        Save-FixtureOutput -ScenarioRoot $ScenarioRoot -Output $output -ErrorOutput $errorOutput
        if ($process.ExitCode -ne 0) {
            $safeError = Get-SafeText $errorOutput
            throw "The client fixture failed scenario '$($ScenarioDefinition.id)' with exit code $($process.ExitCode): $safeError"
        }
        if ([string]::IsNullOrWhiteSpace($output)) {
            throw "The client fixture returned no report for scenario '$($ScenarioDefinition.id)'."
        }
        try {
            return $output | ConvertFrom-Json
        }
        catch {
            throw "The client fixture returned an invalid JSON report for scenario '$($ScenarioDefinition.id)'."
        }
    }
    finally {
        $process.Dispose()
    }
}

foreach ($requiredPath in @(
    $contractPath,
    $manifestPath,
    $serverPath,
    $workerPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "The conformance package is missing '$requiredPath'."
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
if ($manifest.artifactKind -ne "briosa_client_conformance" -or
    $manifest.scenarioContract -ne $contract.contract_id -or
    $manifest.spatialAnalyzerTarget -ne $contract.spatial_analyzer_target) {
    throw "The conformance package manifest and scenario contract do not agree."
}

$requestedScenarios = @($Scenario | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$selectedScenarios = @($contract.scenarios | Where-Object {
    $requestedScenarios.Count -eq 0 -or $_.id -in $requestedScenarios
})
if ($selectedScenarios.Count -eq 0) {
    throw "No matching conformance scenarios were selected."
}
foreach ($requested in $requestedScenarios) {
    if ($requested -notin @($selectedScenarios.id)) {
        throw "Unknown conformance scenario '$requested'."
    }
}

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    foreach ($scenarioDefinition in $selectedScenarios) {
        Stop-PackageProcesses
        $scenarioRoot = Join-Path $temporaryRoot $scenarioDefinition.id
        [IO.Directory]::CreateDirectory($scenarioRoot) | Out-Null
        $statePath = Join-Path $scenarioRoot "worker-state"
        $exitSignalPath = Join-Path $scenarioRoot "worker-exit"
        $applicationRoot = Join-Path $scenarioRoot "fake-application"
        $applicationPath = Join-Path $applicationRoot "Spatial Analyzer64.exe"
        $externalApplication = $null
        $timing = @{
            ScenarioStarted = [DateTimeOffset]::UtcNow
            FixtureStarted = $null
            FixtureEnded = $null
            TimedOut = $false
        }
        $environment = @{
            "BRIOSA_SERVER_PATH" = $serverPath
            "BRIOSA_CONFORMANCE_SCENARIO" = [string]$scenarioDefinition.id
            "BRIOSA_CONFORMANCE_CONTRACT_PATH" = $contractPath
            "BRIOSA_CONFORMANCE_WORKER_EXIT_SIGNAL_PATH" = $exitSignalPath
            "BRIOSA_TEST_WORKER_SCENARIO" = [string]$scenarioDefinition.worker_scenario
            "BRIOSA_TEST_WORKER_STATE_PATH" = $statePath
            "BRIOSA_TEST_WORKER_EXIT_SIGNAL_PATH" = $exitSignalPath
            "Briosa__Worker__ExecutablePath" = $workerPath
            "Briosa__Worker__ExecutionWatchdogTimeout" = $scenarioDefinition.watchdog_timeout
            "Briosa__Worker__StartupTimeout" = $workerStartupTimeout
            "Briosa__SpatialAnalyzer__ExecutablePath" = $applicationPath
            "Briosa__Security__Operations__Overrides__file_operations__get_working_directory" = $(if ($scenarioDefinition.deny_get_working_directory) { "deny" } else { $null })
            "Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Version" = [string]$scenarioDefinition.activated_sdk_version
            "Briosa__SpatialAnalyzer__Identity__ActivatedSdk__OperatorAttestation__Reference" = "portable-conformance-host"
            "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Version" = [string]$scenarioDefinition.connected_sa_version
            "Briosa__SpatialAnalyzer__Identity__ConnectedSpatialAnalyzer__OperatorAttestation__Reference" = "portable-conformance-host"
            # Keep the server's log files with this scenario's diagnostics.
            "Briosa__Logging__File__Directory" = (Join-Path $scenarioRoot "server-logs")
        }
        $scenarioFailure = $null
        try {
            [IO.Directory]::CreateDirectory($applicationRoot) | Out-Null
            Copy-Item -Path (Join-Path $packageRoot "fake-worker\*") `
                -Destination $applicationRoot -Recurse
            Move-Item -LiteralPath (Join-Path $applicationRoot "Briosa.SmokeWorker.exe") `
                -Destination $applicationPath
            if ($scenarioDefinition.start_external_application) {
                $applicationStartInfo = New-ProcessStartInfo `
                    -FilePath $applicationPath `
                    -Arguments @("--hold") `
                    -WorkingDirectory (Split-Path -Parent $applicationPath)
                $externalApplication = [Diagnostics.Process]::Start($applicationStartInfo)
            }

            $report = Invoke-Fixture `
                -ScenarioDefinition $scenarioDefinition `
                -Environment $environment `
                -ScenarioRoot $scenarioRoot `
                -Timing $timing
            if ($report.schema_version -ne 1 -or
                $report.contract_id -ne $contract.contract_id -or
                $report.scenario -ne $scenarioDefinition.id -or
                $report.success -ne $true) {
                throw "The client fixture report did not satisfy scenario '$($scenarioDefinition.id)'."
            }

            if ($null -ne $externalApplication -and $externalApplication.HasExited) {
                throw "The client terminated an externally owned fake application in scenario '$($scenarioDefinition.id)'."
            }
            $unexpectedProcesses = @(Get-PackageProcesses | Where-Object {
                $null -eq $externalApplication -or $_.Id -ne $externalApplication.Id
            })
            if ($unexpectedProcesses.Count -ne 0) {
                throw "The client left package-owned processes running after scenario '$($scenarioDefinition.id)'."
            }
        }
        catch {
            $scenarioFailure = $_
        }
        finally {
            if ($null -ne $externalApplication -and -not $externalApplication.HasExited) {
                Stop-Process -Id $externalApplication.Id -Force -ErrorAction SilentlyContinue
                $externalApplication.WaitForExit(5000) | Out-Null
            }
            if ($null -ne $externalApplication) {
                $externalApplication.Dispose()
            }
            Stop-PackageProcesses
        }

        if ($null -ne $scenarioFailure) {
            # After cleanup, so the server has released its log files.
            Publish-ScenarioFailure `
                -ScenarioId $scenarioDefinition.id `
                -ScenarioRoot $scenarioRoot `
                -Failure "$scenarioFailure" `
                -Timing $timing
            throw $scenarioFailure
        }

        $phaseSummary = Get-ScenarioPhaseSummary -ScenarioRoot $scenarioRoot -Timing $timing
        Write-Host "Passed client conformance scenario: $($scenarioDefinition.id)$(if ($phaseSummary) { " ($phaseSummary)" })"
    }
}
finally {
    Stop-PackageProcesses
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}

Write-Host "All selected Briosa client conformance scenarios passed."
