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

# Optional diagnostics never change a scenario's result. An unusable diagnostics
# directory turns them off with a notice; it never fails a scenario.
$diagnosticsRoot = $null
if (-not [string]::IsNullOrWhiteSpace($DiagnosticsDirectory)) {
    try {
        $diagnosticsRoot = [IO.Path]::GetFullPath($DiagnosticsDirectory)
    }
    catch {
        Write-Host "Conformance diagnostics are disabled: the diagnostics directory is not a usable path."
    }
}

# The fake SDK host tests client behaviour, not start latency, so its worker start
# gets more room than the server's 10 s default. The first fake-worker start after
# a fresh package extraction can miss 10 s on a slow runner. A caller that sets
# the key itself, for example to reproduce that timeout, keeps its value.
$workerStartupTimeout = $(if ([string]::IsNullOrWhiteSpace($env:Briosa__Worker__StartupTimeout)) {
        "00:00:30"
    } else {
        $env:Briosa__Worker__StartupTimeout
    })

# Opt-in, only with exactly BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT=1 and a usable
# diagnostics directory: on a fixture timeout, heap dumps of this scenario's own .NET
# processes go to the diagnostics directory before anything is killed. The whole
# collection shares one deadline.
$dumpOnTimeout = $env:BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT -ceq "1"
$dumpDeadlineMilliseconds = 120000

# The server log digest reaches public CI logs, so it prints nothing it has not
# validated. Known event IDs map to fixed labels, and no message template is ever
# printed. Levels and enum-like properties must belong to closed vocabularies,
# numbers must be integers, and the two open identifiers must match strict
# patterns. A value that does not validate prints as <withheld>.
$digestEventNames = @{
    1000 = "ControlPlaneReady"; 1001 = "EndpointListening"
    1201 = "WorkerTransition"; 1202 = "InteractionModeApplied"; 1203 = "WorkerStartPhase"
    1300 = "ExecutionDispatched"; 1301 = "ExecutionResolved"; 1302 = "LifecycleRejected"
    1303 = "LifecycleRpcStarted"; 1304 = "LifecycleRpcCompleted"; 1305 = "LifecycleRpcFaulted"
    1400 = "ApplicationTransition"; 1401 = "LogSinkDegraded"; 1402 = "LaunchPreconditionSlow"
    1500 = "ReadinessNotReady"
    2000 = "PolicyLoaded"; 2001 = "RequestStarted"; 2002 = "PolicyAllowed"; 2003 = "PolicyRejected"
    2004 = "RpcCompleted"; 2005 = "RpcFailed"
}
$digestLevels = @("Verbose", "Debug", "Information", "Warning", "Error", "Fatal")
$digestImportantLevels = @("Warning", "Error", "Fatal")
$digestWithheld = "<withheld>"
$digestFields = [ordered]@{
    Rpc = @{ Allowed = @(
            "StartSpatialAnalyzerSdk", "ConnectToSpatialAnalyzer", "ReconnectToSpatialAnalyzer",
            "StopSpatialAnalyzerSdk", "RecoverSpatialAnalyzerSdk", "LaunchSpatialAnalyzer",
            "CloseOwnedSpatialAnalyzer", "DesktopStartSdk", "DesktopConnect", "DesktopReconnect",
            "DesktopStopSdk", "DesktopRecoverSdk") }
    GrpcStatus = @{ Allowed = @(
            "OK", "Cancelled", "Unknown", "InvalidArgument", "DeadlineExceeded", "NotFound",
            "AlreadyExists", "PermissionDenied", "ResourceExhausted", "FailedPrecondition", "Aborted",
            "OutOfRange", "Unimplemented", "Internal", "Unavailable", "DataLoss", "Unauthenticated") }
    Phase = @{ Allowed = @(
            "process-created", "control-pipe-connected", "ready-received", "startup-bound-expired",
            "start-failed", "process-scan", "installation-check") }
    ElapsedMilliseconds = @{ Integer = $true }
    WorkerState = @{ Allowed = @("Stopped", "Starting", "Ready", "Degraded", "Stopping") }
    ApplicationState = @{ Allowed = @(
            "Unspecified", "NotRunning", "Starting", "Running", "Closing", "Exited", "Ambiguous", "Faulted") }
    Ownership = @{ Allowed = @("Unspecified", "None", "External", "ServerLaunched") }
    Generation = @{ Integer = $true }
    ConnectionState = @{ Allowed = @("Disconnected", "Connecting", "Connected", "Faulted", "Stopping") }
    # Every operation ID is <service>_operations.<operation> in lower snake case.
    OperationId = @{ MaxLength = 128; Pattern = '^[a-z]+(?:_[a-z]+)*_operations\.[a-z][a-z0-9]*(?:_[a-z0-9]+)*$' }
    ExecutionDisposition = @{ Allowed = @("not_started", "started_outcome_unknown", "completed", "unspecified") }
    MpResultCode = @{ Integer = $true }
    # Diagnostic codes are lower-case kebab identifiers. Not every code comes from a
    # closed list, so this pattern is the boundary: two to twelve segments, each
    # starting with a letter. No path, host name, address or token form passes it.
    DiagnosticCode = @{ MaxLength = 96; Pattern = '^[a-z][a-z0-9]*(?:-[a-z][a-z0-9]*){1,11}$' }
}
# The digest keeps the latest 200 lower-severity records, and every warning and
# error up to 1,000: the first 100 and the latest 900. Server records are at most
# 16 KiB, so a longer line is not a server record.
$digestTailLimit = 200
$digestImportantHead = 100
$digestImportantTail = 900
$digestMaximumLineLength = 65536

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
    if ($null -ne $diagnosticsRoot) {
        # A runtime that writes a requested dump reports its path on stdout.
        $safe = $safe.Replace($diagnosticsRoot, "<diagnostics-directory>")
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

function Get-ProcessTable {
    # One snapshot of every process's parent and creation time, for ownership checks.
    $table = @{}
    foreach ($entry in @(Get-CimInstance -ClassName Win32_Process -Property ProcessId, ParentProcessId, CreationDate -ErrorAction Stop)) {
        $table[[int]$entry.ProcessId] = $entry
    }
    return $table
}

function Test-FixtureDescendant {
    param(
        [Parameter(Mandatory)][int]$ProcessId,
        [Parameter(Mandatory)][Diagnostics.Process]$Fixture,
        [AllowNull()][hashtable]$ProcessTable
    )

    # A process belongs to this scenario when its parent chain reaches the fixture the
    # runner started. Each parent must be older than its child, and the fixture is
    # still running, so a reused process ID cannot pass. Two callers sharing one
    # package extraction therefore never see each other's processes as their own.
    if ($null -eq $ProcessTable) { return $false }
    try {
        if ($Fixture.HasExited) { return $false }
        $fixtureStarted = $Fixture.StartTime
        $current = $ProcessTable[$ProcessId]
        for ($depth = 0; $null -ne $current -and $depth -lt 4; $depth++) {
            $parentId = [int]$current.ParentProcessId
            if ($parentId -eq $Fixture.Id) {
                return $fixtureStarted -le $current.CreationDate
            }
            $parent = $ProcessTable[$parentId]
            if ($null -eq $parent -or $parent.CreationDate -gt $current.CreationDate) {
                return $false
            }
            $current = $parent
        }
    }
    catch {
    }
    return $false
}

# Called on a fixture timeout before anything is killed: the client owns the server,
# so killing the fixture tree also ends the server and its worker.
function Write-TimeoutProcesses {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][Diagnostics.Process]$Fixture,
        [Parameter(Mandatory)][hashtable]$Timing,
        [AllowNull()][hashtable]$ProcessTable
    )

    $reference = Get-TimingReference $Timing
    Write-Host "--- processes alive at the fixture timeout in $ScenarioId (start times from $($reference.Name)) ---"
    $entries = @([pscustomobject]@{ Role = "fixture"; Process = $Fixture; Package = $false }) +
        @(Get-PackageProcesses | ForEach-Object {
            [pscustomobject]@{ Role = (Get-PackageProcessRole $_); Process = $_; Package = $true }
        })
    foreach ($entry in $entries) {
        try {
            $entry.Process.Refresh()
            $started = [DateTimeOffset]$entry.Process.StartTime
            $ownership = $(if ($entry.Package -and -not (Test-FixtureDescendant -ProcessId $entry.Process.Id `
                            -Fixture $Fixture -ProcessTable $ProcessTable)) {
                    ", not a verified descendant of the fixture"
                })
            Write-Host (Format-Invariant "{0} pid {1}: started {2}, CPU {3:0.00} s{4}" @(
                $entry.Role, $entry.Process.Id, (Format-Offset $started $reference.Time),
                $entry.Process.TotalProcessorTime.TotalSeconds, $ownership))
        }
        catch {
            Write-Host "$($entry.Role) pid $($entry.Process.Id): exited or unreadable"
        }
    }
    Write-Host "--- end processes for $ScenarioId ---"
}

function Wait-PendingIo {
    param(
        [Parameter(Mandatory)][Threading.Tasks.Task]$Task,
        [Parameter(Mandatory)][Diagnostics.Stopwatch]$Clock
    )

    $remaining = [int][Math]::Max(0, $dumpDeadlineMilliseconds - $Clock.ElapsedMilliseconds)
    try {
        return $Task.Wait($remaining)
    }
    catch {
        # Cancelled at the deadline, or the pipe failed.
        return $false
    }
}

function Request-RuntimeHeapDump {
    param(
        [Parameter(Mandatory)][int]$ProcessId,
        [Parameter(Mandatory)][string]$DumpPath,
        [Parameter(Mandatory)][Diagnostics.Stopwatch]$Clock
    )

    # Current .NET runtimes no longer let createdump attach to a pid directly. The
    # runtime writes the dump itself, with the createdump beside it, when its
    # diagnostics channel receives GenerateCoreDump: the request dotnet-dump sends
    # (dotnet/diagnostics, docs/design-docs/ipc-protocol.md). The header is
    # "DOTNET_IPC_V1\0", the total size (uint16), the command set (0x01, dump), the
    # command (0x01), and a reserved uint16. The payload is the dump path (uint32
    # UTF-16 length with the terminator, then UTF-16LE), the dump type (2, with
    # heap), and a diagnostics flag (0). The reply's header has command set 0xFF and
    # command 0x00 on success, and is followed by an HRESULT. Every step shares the
    # collection's deadline, and the pipe is closed when it passes.
    $remaining = [int][Math]::Max(0, $dumpDeadlineMilliseconds - $Clock.ElapsedMilliseconds)
    if ($remaining -le 0) {
        return "the dump deadline passing"
    }
    $cancellation = [Threading.CancellationTokenSource]::new([TimeSpan]::FromMilliseconds($remaining))
    $pipe = [IO.Pipes.NamedPipeClientStream]::new(".", "dotnet-diagnostic-$ProcessId",
        [IO.Pipes.PipeDirection]::InOut, [IO.Pipes.PipeOptions]::Asynchronous)
    try {
        try {
            $pipe.Connect([Math]::Min(3000, $remaining))
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
        if (-not (Wait-PendingIo -Task $pipe.WriteAsync($message, 0, $message.Length, $cancellation.Token) -Clock $Clock)) {
            return "the dump deadline passing"
        }
        $reply = [byte[]]::new(24)
        $read = 0
        while ($read -lt $reply.Length) {
            $pending = $pipe.ReadAsync($reply, $read, $reply.Length - $read, $cancellation.Token)
            if (-not (Wait-PendingIo -Task $pending -Clock $Clock)) {
                return "the dump deadline passing"
            }
            if ($pending.Result -eq 0) {
                break
            }
            $read += $pending.Result
        }
        if ($read -lt $reply.Length) {
            return "no reply"
        }
        $result = [BitConverter]::ToUInt32($reply, 20)
        if ($reply[16] -eq 0xFF -and $reply[17] -eq 0x00 -and $result -eq 0) {
            return "written"
        }
        return Format-Invariant "HRESULT 0x{0:X8}" @($result)
    }
    finally {
        $cancellation.Cancel()
        $pipe.Dispose()
        $cancellation.Dispose()
    }
}

function Save-TimeoutDumps {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][Diagnostics.Process]$Fixture,
        [AllowNull()][hashtable]$ProcessTable
    )

    if (-not $dumpOnTimeout) {
        return
    }
    if ($null -eq $diagnosticsRoot) {
        Write-Host "Skipped timeout heap dumps for $($ScenarioId): no usable diagnostics directory is configured."
        return
    }
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $dumpRoot = Join-Path (Join-Path $diagnosticsRoot $ScenarioId) "dumps"
    [IO.Directory]::CreateDirectory($dumpRoot) | Out-Null
    # Only this scenario's own processes: the fixture the runner started, and a
    # server only when it is a verified descendant of that fixture. The server goes
    # first, since its state at the timeout matters most. A process is paused while
    # its dump is written, typically for a few seconds.
    $targets = [Collections.Generic.List[object]]::new()
    foreach ($server in @(Get-PackageProcesses | Where-Object { (Get-PackageProcessRole $_) -eq "server" })) {
        if (Test-FixtureDescendant -ProcessId $server.Id -Fixture $Fixture -ProcessTable $ProcessTable) {
            $targets.Add([pscustomobject]@{ Role = "server"; Process = $server })
        }
        else {
            Write-Host "No heap dump of the server (pid $($server.Id)): it is not a verified descendant of this scenario's fixture."
        }
    }
    $targets.Add([pscustomobject]@{ Role = "fixture"; Process = $Fixture })
    foreach ($target in $targets) {
        $processId = $target.Process.Id
        $name = "$($target.Role)-$processId.dmp"
        $dumpPath = Join-Path $dumpRoot $name
        try {
            $outcome = Request-RuntimeHeapDump -ProcessId $processId -DumpPath $dumpPath -Clock $clock
        }
        catch {
            $outcome = "a failed request"
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

function Get-JsonString {
    param([AllowNull()][object]$Element)

    if ($null -ne $Element -and $Element.ValueKind -eq [Text.Json.JsonValueKind]::String) {
        return $Element.GetString()
    }
    return $null
}

function Get-JsonInteger {
    param([AllowNull()][object]$Element)

    if ($null -ne $Element -and $Element.ValueKind -eq [Text.Json.JsonValueKind]::Number) {
        $text = $Element.GetRawText()
        if ($text -cmatch '^-?[0-9]{1,15}$') {
            return $text
        }
    }
    return $null
}

function ConvertTo-DigestValue {
    param(
        [Parameter(Mandatory)][string]$Field,
        [Parameter(Mandatory)][Text.Json.JsonElement]$Element
    )

    # The printable value, the withheld placeholder for a value that does not
    # validate, or $null when the record has no value for the field.
    if ($Element.ValueKind -eq [Text.Json.JsonValueKind]::Null) {
        return $null
    }
    $rule = $digestFields[$Field]
    if ($rule.ContainsKey("Integer")) {
        $number = Get-JsonInteger $Element
        return $(if ($null -ne $number) { $number } else { $digestWithheld })
    }
    $text = Get-JsonString $Element
    if ($null -eq $text) {
        return $digestWithheld
    }
    if ($rule.ContainsKey("Allowed")) {
        return $(if ($rule["Allowed"] -ccontains $text) { $text } else { $digestWithheld })
    }
    if ($text.Length -le $rule["MaxLength"] -and $text -cmatch $rule["Pattern"]) {
        return $text
    }
    return $digestWithheld
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
        $timestamp = Get-JsonString $fields["Timestamp"]
        $time = [DateTimeOffset]::MinValue
        if ($null -eq $timestamp -or -not [DateTimeOffset]::TryParse(
                $timestamp, $invariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal, [ref]$time)) {
            return $null
        }
        $level = Get-JsonString $fields["Level"]
        if ($digestLevels -cnotcontains $level) {
            $level = "UnknownLevel"
        }
        $properties = $(if ($fields.ContainsKey("Properties")) { Get-JsonFields $fields["Properties"] } else { @{} })
        # A fixed label for a known event, never the record's own text. A framework
        # event keeps only its numeric ID.
        $name = "UnknownEvent"
        $eventNumber = $null
        if ($properties.ContainsKey("EventId")) {
            $eventId = Get-JsonFields $properties["EventId"]
            $eventNumber = Get-JsonInteger $eventId["Id"]
            $label = Get-JsonString $eventId["Name"]
            if ($null -ne $eventNumber -and $eventNumber -cmatch '^[0-9]{1,9}$' -and
                $digestEventNames.ContainsKey([int]$eventNumber) -and $label -ceq $digestEventNames[[int]$eventNumber]) {
                $name = $label
                $eventNumber = $null
            }
            elseif ($label -ceq "FrameworkEvent") {
                $name = "FrameworkEvent"
            }
        }
        elseif ((Get-JsonString $fields["MessageTemplate"]) -ceq "LogRecordTooLarge") {
            $name = "LogRecordTooLarge"
        }
        $values = [ordered]@{}
        if ($null -ne $eventNumber) {
            $values["EventId"] = $eventNumber
        }
        foreach ($field in $digestFields.Keys) {
            if ($properties.ContainsKey($field)) {
                $value = ConvertTo-DigestValue -Field $field -Element $properties[$field]
                if ($null -ne $value) {
                    $values[$field] = $value
                }
            }
        }
        $instance = Get-JsonString $properties["ServerInstanceId"]
        return [pscustomobject]@{
            Time = $time
            Level = $level
            Name = $name
            Instance = $(if ($instance -cmatch '^[0-9a-f]{32}$') { $instance })
            Fields = $values
        }
    }
    finally {
        $document.Dispose()
    }
}

function Add-PhaseRecord {
    param(
        [Parameter(Mandatory)][object]$Log,
        [Parameter(Mandatory)][string]$Line
    )

    $record = ConvertFrom-ServerLogLine $Line
    if ($null -eq $record) {
        return
    }
    if ($record.Name -ceq "EndpointListening" -and $null -eq $Log.Listening) {
        $Log.Listening = $record
    }
    elseif ($record.Name -ceq "LifecycleRpcCompleted" -and $record.Fields.Contains("Rpc") -and
        $record.Fields["Rpc"] -cne $digestWithheld -and -not $Log.Completed.ContainsKey($record.Fields["Rpc"])) {
        $Log.Completed[$record.Fields["Rpc"]] = $record
    }
}

# Streams the scenario's server log files. Memory stays bounded: the latest
# lower-severity lines are kept as text and parsed at the end, and warnings and
# errors are kept up to the head and tail limits. A file that cannot be read is
# counted and skipped; it never loses the rest of the timeline.
function Read-ServerLog {
    param(
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [switch]$PhasesOnly
    )

    $log = [pscustomobject]@{
        Files = 0
        UnreadableFiles = 0
        UnreadableLines = 0
        Ordinary = @()
        OrdinaryOmitted = 0
        Important = @()
        ImportantOmitted = 0
        Listening = $null
        Completed = @{}
    }
    $files = @()
    try {
        $logRoot = Join-Path $ScenarioRoot "server-logs"
        if (Test-Path -LiteralPath $logRoot -PathType Container) {
            $files = @(Get-ChildItem -LiteralPath $logRoot -Filter "*.jsonl" -File | Sort-Object Name)
        }
    }
    catch {
        $log.UnreadableFiles++
    }
    $log.Files = $files.Count
    $importantHead = [Collections.Generic.List[object]]::new()
    $importantTail = [Collections.Generic.Queue[object]]::new()
    $tailLines = [Collections.Generic.List[string]]::new()
    $ordinarySeen = 0
    foreach ($file in $files) {
        $ring = [Collections.Generic.Queue[string]]::new()
        $stream = $null
        $reader = $null
        try {
            # A server that was killed, or one still shutting down, may hold the file open.
            $stream = [IO.FileStream]::new($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
            $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false))
            while ($null -ne ($line = $reader.ReadLine())) {
                if ([string]::IsNullOrWhiteSpace($line)) {
                    continue
                }
                if ($line.Length -gt $digestMaximumLineLength) {
                    $log.UnreadableLines++
                    continue
                }
                if ($line.Contains('"EndpointListening"') -or $line.Contains('"LifecycleRpcCompleted"')) {
                    Add-PhaseRecord -Log $log -Line $line
                }
                if ($PhasesOnly) {
                    continue
                }
                if ($line.Contains('"Level":"Warning"') -or $line.Contains('"Level":"Error"') -or
                    $line.Contains('"Level":"Fatal"')) {
                    $record = ConvertFrom-ServerLogLine $line
                    if ($null -eq $record) {
                        $log.UnreadableLines++
                        continue
                    }
                    if ($digestImportantLevels -ccontains $record.Level) {
                        if ($importantHead.Count -lt $digestImportantHead) {
                            $importantHead.Add($record)
                        }
                        else {
                            $importantTail.Enqueue($record)
                            if ($importantTail.Count -gt $digestImportantTail) {
                                [void]$importantTail.Dequeue()
                                $log.ImportantOmitted++
                            }
                        }
                        continue
                    }
                }
                $ordinarySeen++
                $ring.Enqueue($line)
                if ($ring.Count -gt $digestTailLimit) {
                    [void]$ring.Dequeue()
                }
            }
        }
        catch {
            # For example, a file another process holds exclusively.
            $log.UnreadableFiles++
        }
        finally {
            if ($null -ne $reader) { $reader.Dispose() }
            if ($null -ne $stream) { $stream.Dispose() }
        }
        $tailLines.AddRange($ring)
    }
    if ($PhasesOnly) {
        return $log
    }
    $parsed = [Collections.Generic.List[object]]::new()
    foreach ($line in $tailLines) {
        $record = ConvertFrom-ServerLogLine $line
        if ($null -eq $record) {
            # For example, the truncated last line of a writer that was killed.
            $log.UnreadableLines++
        }
        else {
            $parsed.Add($record)
        }
    }
    $log.Ordinary = @($parsed | Sort-Object -Property Time -Stable | Select-Object -Last $digestTailLimit)
    $log.OrdinaryOmitted = $ordinarySeen - $tailLines.Count + $parsed.Count - $log.Ordinary.Count
    $log.Important = @($importantHead) + @($importantTail)
    return $log
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
    $log = $null
    try {
        $log = Read-ServerLog -ScenarioRoot $ScenarioRoot
    }
    catch {
        $lines.Add("(the server log could not be read)")
    }
    $events = [Collections.Generic.List[object]]::new()
    if ($null -ne $log) {
        if ($log.Files -eq 0) {
            $lines.Add("(no server log file was found)")
        }
        if ($log.OrdinaryOmitted -gt 0) {
            $lines.Add("($($log.OrdinaryOmitted) earlier lower-severity records omitted; warnings and errors are always kept)")
        }
        if ($log.ImportantOmitted -gt 0) {
            $lines.Add("($($log.ImportantOmitted) warnings and errors omitted after the first $digestImportantHead; the latest $digestImportantTail are shown)")
        }
        foreach ($entry in @($log.Ordinary) + @($log.Important)) {
            $events.Add($entry)
        }
    }
    # The runner's own fixture marks, always in the same timeline. Events after the
    # fixture end come from processes that ran on until the runner killed them.
    foreach ($mark in @(
            @("FixtureStarted", "FixtureStart"),
            @("FixtureEnded", $(if ($Timing.TimedOut) { "FixtureTimedOut" } else { "FixtureEnd" })))) {
        if ($null -ne $Timing[$mark[0]]) {
            $events.Add([pscustomobject]@{
                Time = [DateTimeOffset]$Timing[$mark[0]]
                Level = "Runner"
                Name = $mark[1]
                Instance = $null
                Fields = [ordered]@{}
            })
        }
    }
    $instances = @($events | ForEach-Object { $_.Instance } | Where-Object { $null -ne $_ } | Select-Object -Unique)
    foreach ($entry in @($events | Sort-Object -Property Time -Stable)) {
        $server = ""
        if ($instances.Count -gt 1 -and $null -ne $entry.Instance) {
            $server = " [server $([Array]::IndexOf($instances, $entry.Instance) + 1)]"
        }
        $values = @($entry.Fields.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" })
        $lines.Add(("{0} {1}{2} {3} {4}" -f (Format-Offset $entry.Time $reference.Time), $entry.Level,
            $server, $entry.Name, ($values -join " ")).TrimEnd())
    }
    if ($null -ne $log -and $log.UnreadableLines -gt 0) {
        $lines.Add("($($log.UnreadableLines) unreadable server log lines skipped)")
    }
    if ($null -ne $log -and $log.UnreadableFiles -gt 0) {
        $lines.Add("($($log.UnreadableFiles) server log files could not be read)")
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
        $log = Read-ServerLog -ScenarioRoot $ScenarioRoot -PhasesOnly
        if ($null -ne $log.Listening -and $null -ne $Timing.FixtureStarted) {
            $parts.Add("server listening $(Format-Offset $log.Listening.Time ([DateTimeOffset]$Timing.FixtureStarted))")
        }
        foreach ($phase in @(
                @("StartSpatialAnalyzerSdk", "worker start"),
                @("LaunchSpatialAnalyzer", "launch"),
                @("ConnectToSpatialAnalyzer", "connect"))) {
            $completed = $log.Completed[$phase[0]]
            if ($null -ne $completed -and $completed.Fields.Contains("ElapsedMilliseconds") -and
                $completed.Fields["ElapsedMilliseconds"] -cne $digestWithheld) {
                $parts.Add("$($phase[1]) $($completed.Fields["ElapsedMilliseconds"]) ms")
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

    if ($null -eq $diagnosticsRoot) {
        return
    }

    $destination = Join-Path $diagnosticsRoot $ScenarioId
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
    # events after a forced kill. Everything after the stderr is diagnostics only:
    # none of it may replace or hide the scenario's own failure.
    Write-FixtureError -ScenarioId $ScenarioId -ScenarioRoot $ScenarioRoot
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
    try {
        Save-ScenarioDiagnostics -ScenarioId $ScenarioId -ScenarioRoot $ScenarioRoot -Failure $Failure
    }
    catch {
        Write-Host "(the scenario diagnostics could not be saved to the diagnostics directory)"
    }
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
            # client-owned server and its worker. The kill and the cleanup run
            # whether or not these diagnostics succeed.
            try {
                $processTable = $null
                try {
                    $processTable = Get-ProcessTable
                }
                catch {
                }
                Write-TimeoutProcesses -ScenarioId ([string]$ScenarioDefinition.id) -Fixture $process `
                    -Timing $Timing -ProcessTable $processTable
                Save-TimeoutDumps -ScenarioId ([string]$ScenarioDefinition.id) -Fixture $process `
                    -ProcessTable $processTable
            }
            catch {
                Write-Host "(the processes alive at the fixture timeout could not be recorded)"
            }
            finally {
                try {
                    $process.Kill($true)
                }
                catch {
                    # The fixture exited after the wait elapsed.
                }
                Stop-PackageProcesses
            }
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
