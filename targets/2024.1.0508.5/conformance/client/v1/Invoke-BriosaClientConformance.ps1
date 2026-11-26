[CmdletBinding(DefaultParameterSetName = "Run")]
param(
    [Parameter(Mandatory, ParameterSetName = "Run")]
    [string]$FixtureCommand,

    [Parameter(ParameterSetName = "Run")]
    [string[]]$FixtureArguments = @(),

    [Parameter(ParameterSetName = "Run")]
    [string[]]$Scenario = @(),

    # Optional scenarios run only when a fixture asks for them by ID, so a fixture
    # revision that does not know one never receives it. They are listed under
    # optional_scenarios in the contract, apart from the scenarios every fixture runs.
    [Parameter(ParameterSetName = "Run")]
    [string[]]$OptionalScenario = @(),

    # Longer than a first-party client's startup wait under this runner's default
    # settings when the client's own startup timeout is omitted. Client 0.5.1 or
    # later waits for the startup allowance the server publishes (Server 0.10.1
    # and later) plus 20 s: with the 30 s worker startup bound set below, 160 s
    # with launch, so up to 180 s to start the SDK, launch SpatialAnalyzer and
    # connect. Client 0.5.0 waits 120 s. The remaining 60 s is room for fixture
    # setup and error reporting, so a slow start or a hang ends in the client's
    # own typed startup timeout, printed before the runner kills the fixture.
    [Parameter(ParameterSetName = "Run")]
    [ValidateRange(5, 600)]
    [int]$FixtureTimeoutSeconds = 240,

    # A failing scenario copies the full fixture output and the server's log
    # files here. Callers that cannot pass the parameter, such as a client
    # repository's test script, can set BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY.
    [string]$DiagnosticsDirectory = $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY,

    # Internal. On a fixture timeout the runner starts a copy of itself in this
    # mode, so that one deadline bounds every step taken before the kill,
    # including the process query and any file system work.
    [Parameter(Mandatory, ParameterSetName = "TimeoutDiagnostics")]
    [string]$TimeoutDiagnosticsScenario,

    [Parameter(Mandatory, ParameterSetName = "TimeoutDiagnostics")]
    [int]$TimeoutDiagnosticsFixtureProcessId,

    # The fixture's creation time in UTC ticks, read from the runner's own handle.
    [Parameter(Mandatory, ParameterSetName = "TimeoutDiagnostics")]
    [long]$TimeoutDiagnosticsFixtureStartTicks,

    # The runner's fixture start mark in UTC ticks, for the listed start offsets.
    [Parameter(Mandatory, ParameterSetName = "TimeoutDiagnostics")]
    [long]$TimeoutDiagnosticsReferenceTicks,

    # The end of the diagnostics deadline in UTC ticks.
    [Parameter(Mandatory, ParameterSetName = "TimeoutDiagnostics")]
    [long]$TimeoutDiagnosticsDeadlineTicks,

    [Parameter(ParameterSetName = "TimeoutDiagnostics")]
    [string]$TimeoutDiagnosticsApplicationPath
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
$vocabularyPath = Join-Path $PSScriptRoot "digest-vocabulary.json"
$applicationPath = $(if ($PSCmdlet.ParameterSetName -eq "TimeoutDiagnostics") { $TimeoutDiagnosticsApplicationPath })
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-conformance-$([Guid]::NewGuid().ToString('N'))"
$invariantCulture = [Globalization.CultureInfo]::InvariantCulture
$ordinal = [StringComparison]::Ordinal

function New-OrdinalSet {
    param([object[]]$Values = @())

    $set = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($value in $Values) {
        if ($value -is [string]) {
            [void]$set.Add($value)
        }
    }
    return , $set
}

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
# processes go to the diagnostics directory before anything is killed.
$dumpOnTimeout = [string]::Equals($env:BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT, "1", $ordinal)
# One deadline for all diagnostics taken before a timed-out fixture is killed.
$timeoutDiagnosticsMilliseconds = 120000
# The process tree census lists at most this many processes; membership, which decides
# the heap dumps, always covers the whole tree.
$processTreeDisplayLimit = 200

# Opt-in client-side gRPC tracing for one fixture language, through the switches its
# gRPC library reads from the environment: js (@grpc/grpc-js), py (grpcio) or dotnet
# (Grpc.Net.Client through an EventPipe file). The fixture is unchanged. A rejected
# scenario keeps the trace with its diagnostics; a passing one discards it. Tracing
# adds log volume and some time to each call, so the ordinary gate leaves it off.
# Tracing is diagnostics only: a trace that cannot be set up, split or saved is
# reported and never changes a scenario's result or replaces its error.
$clientTraceLanguages = @("js", "py", "dotnet")
$clientTrace = $env:BRIOSA_CONFORMANCE_CLIENT_TRACE
if ([string]::IsNullOrEmpty($clientTrace)) {
    $clientTrace = $null
}
elseif ($clientTrace -cnotin $clientTraceLanguages) {
    Write-Host "Client gRPC tracing is off: BRIOSA_CONFORMANCE_CLIENT_TRACE must be js, py or dotnet."
    $clientTrace = $null
}
# grpc-js prefers its GRPC_NODE_ names, so a JavaScript trace never turns on the
# grpcio trace of another process.
$clientTraceJavaScriptTracers = "channel,resolving_call,subchannel,subchannel_call,transport"
# Without grpcio's channel tracer, which writes about 170 lines for each call.
$clientTracePythonTracers = "api,call_error,client_channel_call,connectivity_state,http,subchannel"
# Only the gRPC client's own event source: its events carry method names and status
# codes, and no address, header, message or exception text.
$clientTraceDotNetProviders = "Grpc.Net.Client:0xFFFFFFFFFFFFFFFF:5"
# The library records the runner takes out of the fixture's stderr: a grpc-js trace
# record (always Debug) from one of the tracers above, the header lines that follow a
# "Received server headers:" or "Received server trailers:" record, and a grpcio (C-core)
# record at Info, which these tracers write and the default verbosity hides. Anything
# else, including library warnings and errors, stays in the fixture's stderr. The raw
# records are never saved: the runner keeps only the events it can render from
# validated names, numbers and times, and counts the rest.
$clientTraceJavaScriptRecord = '\AD (?<time>[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}Z) \| v[0-9]+\.[0-9]+\.[0-9]+ (?<pid>[0-9]{1,10}) \| (?<tracer>channel|resolving_call|subchannel|subchannel_call|transport) \| (?<text>.*)\z'
$clientTraceJavaScriptHeaderLine = '\A\t\t(?<name>:?[a-z0-9-]{1,64}): '
$clientTracePythonRecord = '\AI(?<date>[0-9]{4}) (?<time>[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{6}) +(?<thread>[0-9]{1,10}) (?<file>[a-z0-9_]{1,64}\.cc):[0-9]{1,6}\] (?<text>.*)\z'
$clientTraceMethod = '/[A-Za-z][A-Za-z0-9_.]{0,127}/[A-Za-z][A-Za-z0-9_]{0,127}'
$clientTraceTrailerNames = New-OrdinalSet @("grpc-status", "grpc-message", "briosa-operation-error-bin",
    "briosa-spatial-analyzer-sdk-lifecycle-error-bin", "briosa-spatial-analyzer-lifecycle-error-bin")
$clientTraceCoreStatuses = New-OrdinalSet @("OK", "CANCELLED", "UNKNOWN", "INVALID_ARGUMENT", "DEADLINE_EXCEEDED",
    "NOT_FOUND", "ALREADY_EXISTS", "PERMISSION_DENIED", "RESOURCE_EXHAUSTED", "FAILED_PRECONDITION", "ABORTED",
    "OUT_OF_RANGE", "UNIMPLEMENTED", "INTERNAL", "UNAVAILABLE", "DATA_LOSS", "UNAUTHENTICATED")
$clientTraceSubchannelStates = '(?:IDLE|CONNECTING|READY|TRANSIENT_FAILURE|SHUTDOWN)'

# The server records every unary call's arrival and end at Debug (#450). It is off
# by default; the conformance server turns it on for that one category, so a stalled
# scenario's digest shows which calls arrived and which were answered. A caller that
# sets the key itself keeps its value.
$unaryCallAuditLevelKey = "Logging__LogLevel__Briosa.Server.Services.UnaryCallAuditInterceptor"
$unaryCallAuditLevel = $(if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($unaryCallAuditLevelKey))) {
        "Debug"
    } else {
        [Environment]::GetEnvironmentVariable($unaryCallAuditLevelKey)
    })

# Opt-in: append every DiagnosticCode or OperationId that a scenario's server log
# holds but the digest vocabulary does not list to this file. The retained-client
# gate uses it so that a newly produced value fails a check instead of being
# silently withheld from the digest.
$digestAuditPath = $env:BRIOSA_CONFORMANCE_DIGEST_AUDIT

# The server log digest reaches public CI logs, so it prints only values from
# reviewed closed lists. Every comparison is ordinal, and the printed text is always
# the list's own entry, never the record's. Known event IDs map to fixed labels and
# no message template is printed. Numbers must be integers. A value that does not
# validate prints as <withheld>.
$digestEventNames = @{
    1000 = "ControlPlaneReady"; 1001 = "EndpointListening"
    1201 = "WorkerTransition"; 1202 = "InteractionModeApplied"; 1203 = "WorkerStartPhase"
    1300 = "ExecutionDispatched"; 1301 = "ExecutionResolved"; 1302 = "LifecycleRejected"
    1303 = "LifecycleRpcStarted"; 1304 = "LifecycleRpcCompleted"; 1305 = "LifecycleRpcFaulted"
    1400 = "ApplicationTransition"; 1401 = "LogSinkDegraded"; 1402 = "LaunchPreconditionSlow"
    1500 = "ReadinessNotReady"
    1600 = "UnaryCallStarted"; 1601 = "UnaryCallCompleted"; 1602 = "UnaryCallFaulted"; 1603 = "UnaryCallResponseEnded"
    1604 = "UnaryCallResponseAbandoned"
    2000 = "PolicyLoaded"; 2001 = "RequestStarted"; 2002 = "PolicyAllowed"; 2003 = "PolicyRejected"
    2004 = "RpcCompleted"; 2005 = "RpcFailed"
}
$digestLevels = New-OrdinalSet @("Verbose", "Debug", "Information", "Warning", "Error", "Fatal")
$digestWithheld = "<withheld>"
# DiagnosticCode and OperationId are open in the server, so their lists ship in the
# package (digest-vocabulary.json). Both lists are validated before either is used. A
# missing, malformed or partial file, or one with an empty list, leaves both empty, so
# every value of the two properties prints as <withheld>; the rest of the digest is
# unaffected.
$digestVocabularyShapes = [ordered]@{
    diagnostic_codes = '\A[a-z][a-z0-9]*(?:-[a-z][a-z0-9]*){1,11}\z'
    operation_ids = '\A[a-z]+(?:_[a-z]+)*_operations\.[a-z][a-z0-9]*(?:_[a-z0-9]+)*\z'
}
$digestVocabulary = @{
    diagnostic_codes = New-OrdinalSet
    operation_ids = New-OrdinalSet
}
try {
    $vocabularyJson = [Text.Json.JsonDocument]::Parse([IO.File]::ReadAllText($vocabularyPath))
    try {
        $root = $vocabularyJson.RootElement
        # Typed, so that PowerShell can pick the TryGetProperty overload.
        $version = [Text.Json.JsonElement]::new()
        if ($root.ValueKind -ne [Text.Json.JsonValueKind]::Object -or
            -not $root.TryGetProperty("schema_version", [ref]$version) -or
            -not [string]::Equals($version.GetRawText(), "1", $ordinal)) {
            throw "The digest vocabulary has an unknown schema."
        }
        $loaded = @{}
        foreach ($list in $digestVocabularyShapes.Keys) {
            $entries = [Text.Json.JsonElement]::new()
            if (-not $root.TryGetProperty($list, [ref]$entries) -or
                $entries.ValueKind -ne [Text.Json.JsonValueKind]::Array) {
                throw "The digest vocabulary lacks a list."
            }
            $set = New-OrdinalSet
            foreach ($entry in $entries.EnumerateArray()) {
                $value = $(if ($entry.ValueKind -eq [Text.Json.JsonValueKind]::String) { $entry.GetString() })
                if ($null -eq $value -or $value.Length -gt 128 -or $value -cnotmatch $digestVocabularyShapes[$list] -or
                    -not $set.Add($value)) {
                    throw "The digest vocabulary holds an invalid entry."
                }
            }
            if ($set.Count -eq 0) {
                throw "The digest vocabulary holds an empty list."
            }
            $loaded[$list] = $set
        }
        # Installed together, only once both lists are valid.
        foreach ($list in $digestVocabularyShapes.Keys) {
            $digestVocabulary[$list].UnionWith($loaded[$list])
        }
    }
    finally {
        $vocabularyJson.Dispose()
    }
}
catch {
    Write-Host "(the digest vocabulary is missing or invalid; diagnostic codes and operation IDs print as $digestWithheld)"
}
# Rpc names every server RPC that is not an MP operation; an MP operation is named by
# its OperationId. The server tests fail when a unary RPC is missing from this list.
$digestFields = [ordered]@{
    Rpc = @{ Allowed = (New-OrdinalSet @(
                "StartSpatialAnalyzerSdk", "ConnectToSpatialAnalyzer", "ReconnectToSpatialAnalyzer",
                "StopSpatialAnalyzerSdk", "RecoverSpatialAnalyzerSdk", "LaunchSpatialAnalyzer",
                "CloseOwnedSpatialAnalyzer", "DesktopStartSdk", "DesktopConnect", "DesktopReconnect",
                "DesktopStopSdk", "DesktopRecoverSdk", "GetServerInfo", "ListCapabilities",
                "GetSpatialAnalyzerSdkState", "GetSpatialAnalyzerState", "Check")) }
    GrpcStatus = @{ Allowed = (New-OrdinalSet @(
                "OK", "Cancelled", "Unknown", "InvalidArgument", "DeadlineExceeded", "NotFound",
                "AlreadyExists", "PermissionDenied", "ResourceExhausted", "FailedPrecondition", "Aborted",
                "OutOfRange", "Unimplemented", "Internal", "Unavailable", "DataLoss", "Unauthenticated")) }
    Phase = @{ Allowed = (New-OrdinalSet @(
                "process-created", "control-pipe-connected", "ready-received", "startup-bound-expired",
                "start-failed", "process-scan", "installation-check")) }
    ElapsedMilliseconds = @{ Integer = $true }
    WorkerState = @{ Allowed = (New-OrdinalSet @("Stopped", "Starting", "Ready", "Degraded", "Stopping")) }
    ApplicationState = @{ Allowed = (New-OrdinalSet @(
                "Unspecified", "NotRunning", "Starting", "Running", "Closing", "Exited", "Ambiguous", "Faulted")) }
    Ownership = @{ Allowed = (New-OrdinalSet @("Unspecified", "None", "External", "ServerLaunched")) }
    Generation = @{ Integer = $true }
    ConnectionState = @{ Allowed = (New-OrdinalSet @("Disconnected", "Connecting", "Connected", "Faulted", "Stopping")) }
    OperationId = @{ Allowed = $digestVocabulary["operation_ids"] }
    ExecutionDisposition = @{ Allowed = (New-OrdinalSet @("not_started", "started_outcome_unknown", "completed", "unspecified")) }
    MpResultCode = @{ Integer = $true }
    DiagnosticCode = @{ Allowed = $digestVocabulary["diagnostic_codes"] }
}
# The digest keeps the latest 200 lower-severity records by timestamp, across all of
# a scenario's log files, and every warning and error. A record is parsed, and its
# top-level Level read, before it enters either path. Warnings and errors are spooled
# to disk in bounded chunks, each sorted by timestamp and reading order, and the chunks
# are merged back, so memory stays bounded however many there are and records that
# reached the log out of time order still print in time order. Server records are at
# most 16 KiB, so a longer line is not a server record and is skipped as it streams
# past.
$digestTailLimit = 200
$digestMaximumLineLength = 65536
$digestReadBlockCharacters = 262144
$digestSpoolChunkRecords = 4096
$digestSpoolChunkCharacters = 8388608
$digestImportantLevels = New-OrdinalSet @("Warning", "Error", "Fatal")

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
        [Parameter(Mandatory)][long]$Ticks,
        [Parameter(Mandatory)][long]$ReferenceTicks
    )

    return Format-Invariant "{0:+0.000;-0.000} s" @(([TimeSpan]::FromTicks($Ticks - $ReferenceTicks)).TotalSeconds)
}

function Format-UtcTime {
    param([Parameter(Mandatory)][DateTimeOffset]$Time)

    return $Time.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", $invariantCulture)
}

function Get-TimingReference {
    param([Parameter(Mandatory)][hashtable]$Timing)

    if ($null -ne $Timing.FixtureStarted) {
        return [pscustomobject]@{ Ticks = ([DateTimeOffset]$Timing.FixtureStarted).UtcTicks; Name = "fixture start" }
    }
    return [pscustomobject]@{ Ticks = ([DateTimeOffset]$Timing.ScenarioStarted).UtcTicks; Name = "scenario start" }
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
            $parts.Add("$($mark[1]) $(Format-UtcTime $time) ($(Format-Offset $time.UtcTicks $scenarioStarted.UtcTicks))")
        }
        else {
            $parts.Add("$($mark[1]) not reached")
        }
    }
    $line = "Runner timing for $ScenarioId (UTC): $($parts -join ', ')$(if ($Timing.TimedOut) { ', fixture timed out' })."
    Write-Host $line
    return $line
}

function Get-RemainingMilliseconds {
    param([Parameter(Mandatory)][long]$DeadlineTicks)

    $remaining = ($DeadlineTicks - [DateTime]::UtcNow.Ticks) / [TimeSpan]::TicksPerMillisecond
    return [int][Math]::Max(0, [Math]::Min([double][int]::MaxValue, $remaining))
}

# --- Timeout diagnostics, run in a child copy of the runner under one deadline. ---

function Get-ProcessTable {
    param([Parameter(Mandatory)][long]$DeadlineTicks)

    # One snapshot of every process, for the fixture's process tree and ownership
    # checks. The path and command line identify package processes and script names;
    # neither is ever printed.
    $seconds = [Math]::Max(1, [int]((Get-RemainingMilliseconds $DeadlineTicks) / 1000))
    $table = @{}
    foreach ($entry in @(Get-CimInstance -ClassName Win32_Process -Property ProcessId, ParentProcessId, CreationDate,
                Name, ExecutablePath, CommandLine `
                -OperationTimeoutSec $seconds -ErrorAction Stop)) {
        $table[[int]$entry.ProcessId] = $entry
    }
    return $table
}

function Get-FixtureProcessTree {
    param(
        [Parameter(Mandatory)][int]$RootId,
        [Parameter(Mandatory)][long]$RootStartTicks,
        [Parameter(Mandatory)][hashtable]$ProcessTable
    )

    # The fixture the runner started and every process descended from it, each parent
    # before its children, oldest child first. A child is never older than its parent,
    # so a parent ID reused by a later process cannot adopt an unrelated one. The
    # runner and this copy hold the fixture open, so its own ID cannot be reused. The
    # walk visits each snapshot entry at most once, so it always ends; the display
    # limit applies only to printing.
    $root = $ProcessTable[$RootId]
    if ($null -eq $root -or -not (Test-SameCreation -LiveUtcTicks $RootStartTicks -Entry $root)) {
        throw "The fixture is not in the process snapshot."
    }
    $children = @{}
    foreach ($entry in $ProcessTable.Values) {
        $parentId = [int]$entry.ParentProcessId
        if ($parentId -eq [int]$entry.ProcessId) {
            continue
        }
        if (-not $children.ContainsKey($parentId)) {
            $children[$parentId] = [Collections.Generic.List[object]]::new()
        }
        $children[$parentId].Add($entry)
    }
    $tree = [Collections.Generic.List[object]]::new()
    $seen = [Collections.Generic.HashSet[int]]::new()
    $pending = [Collections.Generic.Stack[object]]::new()
    $pending.Push([pscustomobject]@{ Entry = $root; Depth = 0 })
    while ($pending.Count -gt 0) {
        $node = $pending.Pop()
        if (-not $seen.Add([int]$node.Entry.ProcessId)) {
            continue
        }
        $tree.Add($node)
        $own = $children[[int]$node.Entry.ProcessId]
        if ($null -eq $own) {
            continue
        }
        foreach ($child in @($own | Where-Object { $_.CreationDate -ge $node.Entry.CreationDate } |
                Sort-Object -Property CreationDate -Descending)) {
            $pending.Push([pscustomobject]@{ Entry = $child; Depth = $node.Depth + 1 })
        }
    }
    return , $tree
}

function Get-SafeProcessName {
    param([AllowNull()][string]$Name)

    return $(if ($null -ne $Name -and $Name -cmatch '\A[A-Za-z0-9 ._()+-]{1,64}\z') { $Name } else { $digestWithheld })
}

function Split-CommandLine {
    param([AllowNull()][string]$CommandLine)

    # The arguments of a Windows command line, by the rules the C runtime uses:
    # whitespace separates arguments outside double quotes, 2n backslashes before a
    # quote are n backslashes, and 2n+1 are n and a literal quote.
    $arguments = [Collections.Generic.List[string]]::new()
    if ([string]::IsNullOrEmpty($CommandLine)) {
        return , $arguments
    }
    $current = [Text.StringBuilder]::new()
    $quoted = $false
    $started = $false
    for ($index = 0; $index -lt $CommandLine.Length; $index++) {
        $character = $CommandLine[$index]
        if ($character -eq '\') {
            $slashes = 0
            while ($index -lt $CommandLine.Length -and $CommandLine[$index] -eq '\') {
                $slashes++
                $index++
            }
            if ($index -lt $CommandLine.Length -and $CommandLine[$index] -eq '"') {
                [void]$current.Append('\', [int][Math]::Floor($slashes / 2))
                if ($slashes % 2 -eq 1) {
                    [void]$current.Append('"')
                }
                else {
                    $quoted = -not $quoted
                }
            }
            else {
                [void]$current.Append('\', $slashes)
                $index--
            }
            $started = $true
            continue
        }
        if ($character -eq '"') {
            $quoted = -not $quoted
            $started = $true
            continue
        }
        if (-not $quoted -and [char]::IsWhiteSpace($character)) {
            if ($started) {
                $arguments.Add($current.ToString())
                [void]$current.Clear()
                $started = $false
            }
            continue
        }
        [void]$current.Append($character)
        $started = $true
    }
    if ($started) {
        $arguments.Add($current.ToString())
    }
    return , $arguments
}

function Get-ScriptNames {
    param(
        [AllowNull()][string]$Name,
        [AllowNull()][string]$CommandLine
    )

    # The file names of the scripts, loaders or assembly a known runtime was started
    # with, read only from the positions where that runtime takes them: Node's script
    # and its --require, --import and --loader modules, Python's script, the
    # assembly dotnet runs, and PowerShell's -File script. Arguments after the script
    # are the script's own and are never read, and nothing is read for any other
    # program, so an ordinary argument value never reaches the census. Each name must
    # look like a script or an assembly file name; at most three are returned.
    $names = [Collections.Generic.List[string]]::new()
    $arguments = Split-CommandLine $CommandLine
    $runtime = ([string]$Name).ToLowerInvariant()
    $candidates = [Collections.Generic.List[string]]::new()
    $valueOptions = @()
    $moduleOptions = @()
    $scriptOption = $null
    switch -Exact ($runtime) {
        "node.exe" {
            $moduleOptions = @("--require", "-r", "--import", "--loader", "--experimental-loader")
            $valueOptions = @("--eval", "-e", "--print", "-p", "--input-type", "--conditions", "-C", "--title",
                "--inspect-port", "--max-old-space-size", "--stack-size", "--env-file", "--watch-path")
        }
        { $_ -in @("python.exe", "pythonw.exe", "py.exe") } {
            $valueOptions = @("-W", "-X", "--check-hash-based-pycs")
        }
        "dotnet.exe" {
            $valueOptions = @("--depsfile", "--runtimeconfig", "--additionalprobingpath", "--additional-deps",
                "--fx-version", "--roll-forward", "--runtimeconfig")
        }
        { $_ -in @("pwsh.exe", "powershell.exe") } {
            $scriptOption = @("-File", "-f")
        }
        default {
            return , $names
        }
    }
    for ($index = 1; $index -lt $arguments.Count; $index++) {
        $argument = $arguments[$index]
        if ($null -ne $scriptOption) {
            # PowerShell: only the value of -File names a script; everything after it
            # belongs to the script.
            if ($argument -in $scriptOption -and $index + 1 -lt $arguments.Count) {
                $candidates.Add($arguments[$index + 1])
                break
            }
            if ($argument -in @("-Command", "-c", "-EncodedCommand", "-e", "-ec")) {
                break
            }
            continue
        }
        $option = $argument
        $value = $null
        if ($argument.StartsWith("--", [StringComparison]::Ordinal) -and $argument.Contains("=")) {
            $option = $argument.Substring(0, $argument.IndexOf("="))
            $value = $argument.Substring($argument.IndexOf("=") + 1)
        }
        if ($option -in $moduleOptions) {
            if ($null -eq $value -and $index + 1 -lt $arguments.Count) {
                $index++
                $value = $arguments[$index]
            }
            if ($null -ne $value) {
                $candidates.Add($value)
            }
            continue
        }
        if ($runtime -in @("python.exe", "pythonw.exe", "py.exe") -and $argument -in @("-c", "-m")) {
            # A command or a module, not a script file.
            break
        }
        if ($option -in $valueOptions) {
            if ($null -eq $value) {
                $index++
            }
            continue
        }
        if ($argument.StartsWith("-", [StringComparison]::Ordinal)) {
            continue
        }
        # The first argument that is not an option is the script or the assembly.
        $candidates.Add($argument)
        break
    }
    foreach ($candidate in $candidates) {
        $leaf = ($candidate -split '[\\/]')[-1]
        if ($leaf -cmatch '\A[A-Za-z0-9_-][A-Za-z0-9._-]{0,63}\.(?:mjs|cjs|js|mts|cts|ts|py|dll|ps1)\z' -and
            -not $names.Contains($leaf)) {
            $names.Add($leaf)
        }
        if ($names.Count -ge 3) {
            break
        }
    }
    return , $names
}

function Get-ThreadSummary {
    param([Parameter(Mandatory)][Diagnostics.Process]$Process)

    # The thread count and how many threads run, wait or are suspended.
    $states = [ordered]@{}
    $count = 0
    foreach ($thread in $Process.Threads) {
        $count++
        $state = "other"
        try {
            $state = switch ([string]$thread.ThreadState) {
                "Running" { "running" }
                "Ready" { "ready" }
                "Wait" { $(if ([string]$thread.WaitReason -eq "Suspended") { "suspended" } else { "waiting" }) }
                default { "other" }
            }
        }
        catch {
        }
        $states[$state] = 1 + [int]$states[$state]
    }
    $parts = @($states.GetEnumerator() | ForEach-Object { "$($_.Value) $($_.Key)" })
    return "threads $count ($($parts -join ', '))"
}

function Get-PackageRoleForPath {
    param([AllowNull()][string]$Path)

    foreach ($role in @(@("server", $serverPath), @("worker", $workerPath), @("application", $applicationPath))) {
        if (-not [string]::IsNullOrWhiteSpace($role[1]) -and
            [string]::Equals($Path, $role[1], [StringComparison]::OrdinalIgnoreCase)) {
            return $role[0]
        }
    }
    return $null
}

function Write-FixtureProcessTree {
    param(
        [Parameter(Mandatory)][Diagnostics.Process]$Fixture,
        [Parameter(Mandatory)][hashtable]$ProcessTable,
        [Parameter(Mandatory)][long]$DeadlineTicks
    )

    # One line per process in the fixture's tree, indented under its parent, for at
    # most $processTreeDisplayLimit processes. CPU is read twice, a second apart, so a
    # busy process shows its growth and an idle one none. Returns the process IDs of
    # the whole tree, listed or not; a package process is checked against the
    # snapshot again before it is treated as a member.
    $tree = Get-FixtureProcessTree -RootId $Fixture.Id -RootStartTicks $TimeoutDiagnosticsFixtureStartTicks `
        -ProcessTable $ProcessTable
    $members = [Collections.Generic.HashSet[int]]::new()
    foreach ($node in $tree) {
        [void]$members.Add([int]$node.Entry.ProcessId)
    }
    $rows = [Collections.Generic.List[object]]::new()
    foreach ($node in @($tree | Select-Object -First $processTreeDisplayLimit)) {
        $entry = $node.Entry
        $processId = [int]$entry.ProcessId
        $live = $null
        try {
            $candidate = $(if ($processId -eq $Fixture.Id) { $Fixture } else { [Diagnostics.Process]::GetProcessById($processId) })
            # Reading the handle keeps the process object, and so its ID, alive.
            [void]$candidate.Handle
            if (Test-SameCreation -LiveUtcTicks $candidate.StartTime.ToUniversalTime().Ticks -Entry $entry) {
                $live = $candidate
            }
        }
        catch {
        }
        $firstCpu = $null
        if ($null -ne $live) {
            try {
                $firstCpu = $live.TotalProcessorTime
            }
            catch {
            }
        }
        $role = $(if ($node.Depth -eq 0) { "fixture" } else {
                $packageRole = Get-PackageRoleForPath $entry.ExecutablePath
                $(if ($null -ne $packageRole) { $packageRole } else { "child" })
            })
        $rows.Add([pscustomobject]@{ Node = $node; Live = $live; FirstCpu = $firstCpu; Role = $role })
    }
    $sampleMilliseconds = [Math]::Min(1000, [Math]::Max(0, (Get-RemainingMilliseconds $DeadlineTicks) - 10000))
    $sampleStarted = [Diagnostics.Stopwatch]::StartNew()
    if ($sampleMilliseconds -gt 0) {
        Start-Sleep -Milliseconds $sampleMilliseconds
    }
    $sampleSeconds = $sampleStarted.Elapsed.TotalSeconds
    foreach ($row in $rows) {
        $entry = $row.Node.Entry
        $scripts = Get-ScriptNames -Name $entry.Name -CommandLine $entry.CommandLine
        $label = (@(Get-SafeProcessName $entry.Name) + @($scripts)) -join " "
        $prefix = Format-Invariant "{0}{1} pid {2} ({3}): parent {4}, started {5}" @(
            ("  " * $row.Node.Depth), $row.Role, $entry.ProcessId, $label, $entry.ParentProcessId,
            (Format-Offset $entry.CreationDate.ToUniversalTime().Ticks $TimeoutDiagnosticsReferenceTicks))
        if ($null -eq $row.Live) {
            Write-Host "$prefix, exited or unreadable"
            continue
        }
        try {
            $row.Live.Refresh()
            if ($row.Live.HasExited) {
                Write-Host "$prefix, exited during the census"
                continue
            }
            $cpu = $row.Live.TotalProcessorTime
            $growth = $(if ($null -ne $row.FirstCpu) {
                    Format-Invariant " (+{0:0.00} s in {1:0.0} s)" @(($cpu - $row.FirstCpu).TotalSeconds, $sampleSeconds)
                })
            Write-Host (Format-Invariant "{0}, CPU {1:0.00} s{2}, {3}" @(
                    $prefix, $cpu.TotalSeconds, $growth, (Get-ThreadSummary $row.Live)))
        }
        catch {
            Write-Host "$prefix, exited or unreadable"
        }
    }
    if ($tree.Count -gt $rows.Count) {
        Write-Host "($($tree.Count - $rows.Count) more processes in the fixture's tree are not listed)"
    }
    return , $members
}

function Test-SameCreation {
    param(
        [Parameter(Mandatory)][long]$LiveUtcTicks,
        [Parameter(Mandatory)][object]$Entry
    )

    # The process query reports creation times to the microsecond; a live handle reads
    # them to 100 ns. Within a microsecond, in UTC, the two name the same process.
    try {
        $snapshotUtcTicks = $Entry.CreationDate.ToUniversalTime().Ticks
        return [Math]::Abs($LiveUtcTicks - $snapshotUtcTicks) -le 10
    }
    catch {
        return $false
    }
}

function Test-TreeMember {
    param(
        [Parameter(Mandatory)][Diagnostics.Process]$Process,
        [Parameter(Mandatory)][AllowEmptyCollection()][Collections.Generic.HashSet[int]]$Members,
        [AllowNull()][hashtable]$ProcessTable
    )

    # A package process belongs to this scenario when it is in the fixture's process
    # tree and is still the process the snapshot saw, by creation time. Two callers
    # sharing one package extraction never see each other's processes as their own.
    if ($null -eq $ProcessTable -or -not $Members.Contains($Process.Id)) {
        return $false
    }
    try {
        [void]$Process.Handle
        $entry = $ProcessTable[$Process.Id]
        return $null -ne $entry -and
            (Test-SameCreation -LiveUtcTicks $Process.StartTime.ToUniversalTime().Ticks -Entry $entry)
    }
    catch {
        return $false
    }
}

function Get-PipeServerProcessId {
    param([Parameter(Mandatory)][IO.Pipes.NamedPipeClientStream]$Pipe)

    if (-not ("BriosaConformance.PipeNative" -as [type])) {
        Add-Type -Namespace BriosaConformance -Name PipeNative -MemberDefinition @'
[DllImport("kernel32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
public static extern bool GetNamedPipeServerProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe, out uint serverProcessId);
'@
    }
    $serverProcessId = [uint32]0
    if (-not [BriosaConformance.PipeNative]::GetNamedPipeServerProcessId($Pipe.SafePipeHandle, [ref]$serverProcessId)) {
        return [long]-1
    }
    return [long]$serverProcessId
}

function Wait-PendingIo {
    param(
        [Parameter(Mandatory)][Threading.Tasks.Task]$Task,
        [Parameter(Mandatory)][long]$DeadlineTicks
    )

    # completed, deadline (still pending, or cancelled at the deadline), or fault.
    try {
        if (-not $Task.Wait((Get-RemainingMilliseconds $DeadlineTicks))) {
            return "deadline"
        }
        return "completed"
    }
    catch {
        if ($Task.IsCanceled -or [DateTime]::UtcNow.Ticks -ge $DeadlineTicks) {
            return "deadline"
        }
        return "fault"
    }
}

function Request-RuntimeHeapDump {
    param(
        [Parameter(Mandatory)][int]$ProcessId,
        [Parameter(Mandatory)][string]$DumpPath,
        [Parameter(Mandatory)][long]$DeadlineTicks
    )

    # Current .NET runtimes no longer let createdump attach to a pid directly. The
    # runtime writes the dump itself, with the createdump beside it, when its
    # diagnostics channel receives GenerateCoreDump: the request dotnet-dump sends
    # (dotnet/diagnostics, docs/design-docs/ipc-protocol.md). The header is
    # "DOTNET_IPC_V1\0", the total size (uint16), the command set (0x01, dump), the
    # command (0x01), and a reserved uint16. The payload is the dump path (uint32
    # UTF-16 length with the terminator, then UTF-16LE), the dump type (2, with
    # heap), and a diagnostics flag (0). The reply's header has command set 0xFF and
    # command 0x00 on success, and is followed by an HRESULT. The request goes only
    # to a pipe whose server is the verified process.
    $remaining = Get-RemainingMilliseconds $DeadlineTicks
    if ($remaining -le 0) {
        return "the diagnostics deadline passing"
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
        if ((Get-PipeServerProcessId $pipe) -ne $ProcessId) {
            return "a diagnostics pipe that another process serves"
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
        $written = Wait-PendingIo -Task $pipe.WriteAsync($message, 0, $message.Length, $cancellation.Token) -DeadlineTicks $DeadlineTicks
        if ($written -ne "completed") {
            return $(if ($written -eq "deadline") { "the diagnostics deadline passing" } else { "a diagnostics pipe fault" })
        }
        $reply = [byte[]]::new(24)
        $read = 0
        while ($read -lt $reply.Length) {
            $pending = $pipe.ReadAsync($reply, $read, $reply.Length - $read, $cancellation.Token)
            $state = Wait-PendingIo -Task $pending -DeadlineTicks $DeadlineTicks
            if ($state -ne "completed") {
                return $(if ($state -eq "deadline") { "the diagnostics deadline passing" } else { "a diagnostics pipe fault" })
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

function Invoke-TimeoutDiagnosticsCollection {
    # The child side. Prints the processes still alive and takes the opt-in dumps; the
    # runner prints this output and kills the child at the deadline.
    $deadline = $TimeoutDiagnosticsDeadlineTicks
    $fixture = $null
    try {
        $fixture = [Diagnostics.Process]::GetProcessById($TimeoutDiagnosticsFixtureProcessId)
        if ($fixture.StartTime.ToUniversalTime().Ticks -ne $TimeoutDiagnosticsFixtureStartTicks) {
            Write-Host "(the fixture process could not be confirmed; no timeout diagnostics were taken)"
            return
        }
    }
    catch {
        Write-Host "(the fixture process could not be opened; no timeout diagnostics were taken)"
        return
    }
    $processTable = $null
    try {
        $processTable = Get-ProcessTable -DeadlineTicks $deadline
    }
    catch {
        Write-Host "(the process query failed; process ownership cannot be verified)"
    }
    $scenarioId = $TimeoutDiagnosticsScenario
    Write-Host "--- processes alive at the fixture timeout in $scenarioId (start times from fixture start) ---"
    # The fixture's whole tree: for a launcher such as tsx or a Python virtual
    # environment, the process that runs the fixture is a child of the one the runner
    # started. A failed census is reported and never ends the diagnostics.
    $members = [Collections.Generic.HashSet[int]]::new()
    $treeRecorded = $false
    if ($null -ne $processTable) {
        try {
            $members = Write-FixtureProcessTree -Fixture $fixture -ProcessTable $processTable -DeadlineTicks $deadline
            $treeRecorded = $true
        }
        catch {
            Write-Host "(the fixture's process tree could not be recorded)"
        }
    }
    $entries = [Collections.Generic.List[object]]::new()
    $entries.Add([pscustomobject]@{ Role = "fixture"; Process = $fixture; Package = $false; Owned = $true })
    foreach ($process in @(Get-PackageProcesses)) {
        $entries.Add([pscustomobject]@{ Role = (Get-PackageProcessRole $process); Process = $process; Package = $true
                Owned = (Test-TreeMember -Process $process -Members $members -ProcessTable $processTable) })
    }
    # Package processes outside the tree, or every process the runner knows of when
    # the tree is unavailable.
    foreach ($entry in $entries) {
        if ($treeRecorded -and ($entry.Owned -or -not $entry.Package)) {
            continue
        }
        try {
            [void]$entry.Process.Handle
            $startTicks = $entry.Process.StartTime.ToUniversalTime().Ticks
            Write-Host (Format-Invariant "{0} pid {1}: started {2}, CPU {3:0.00} s{4}" @(
                $entry.Role, $entry.Process.Id, (Format-Offset $startTicks $TimeoutDiagnosticsReferenceTicks),
                $entry.Process.TotalProcessorTime.TotalSeconds,
                $(if ($entry.Package) { ", not a verified descendant of the fixture" })))
        }
        catch {
            Write-Host "$($entry.Role) pid $($entry.Process.Id): exited or unreadable"
        }
    }
    Write-Host "--- end processes for $scenarioId ---"
    if (-not $dumpOnTimeout) {
        return
    }
    if ($null -eq $diagnosticsRoot) {
        Write-Host "Skipped timeout heap dumps for $($scenarioId): no usable diagnostics directory is configured."
        return
    }
    $dumpRoot = Join-Path (Join-Path $diagnosticsRoot $scenarioId) "dumps"
    try {
        [IO.Directory]::CreateDirectory($dumpRoot) | Out-Null
    }
    catch {
        Write-Host "Skipped timeout heap dumps for $($scenarioId): the dump directory could not be created."
        return
    }
    # The server first: its state at the timeout matters most. A process is paused
    # while its dump is written, typically for a few seconds.
    $targets = [Collections.Generic.List[object]]::new()
    foreach ($entry in @($entries | Where-Object { $_.Role -eq "server" })) {
        if ($entry.Owned) {
            $targets.Add($entry)
        }
        else {
            Write-Host "No heap dump of the server (pid $($entry.Process.Id)): it is not a verified descendant of this scenario's fixture."
        }
    }
    $targets.Add($entries[0])
    foreach ($target in $targets) {
        $processId = $target.Process.Id
        $name = "$($target.Role)-$processId.dmp"
        $dumpPath = Join-Path $dumpRoot $name
        try {
            $outcome = Request-RuntimeHeapDump -ProcessId $processId -DumpPath $dumpPath -DeadlineTicks $deadline
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

function Invoke-TimeoutDiagnostics {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][Diagnostics.Process]$Fixture,
        [Parameter(Mandatory)][hashtable]$Timing,
        [Parameter(Mandatory)][string]$ScenarioRoot
    )

    # The runner side. Every step before the kill, including the process query and the
    # file system work, runs in a child copy of this script, which is killed at the
    # deadline. The runner keeps its own handle to the fixture meanwhile. The printed
    # lines are also kept with the scenario's diagnostics, as timeout-processes.txt.
    $deadlineTicks = [DateTime]::UtcNow.AddMilliseconds($timeoutDiagnosticsMilliseconds).Ticks
    # The copy stops its own work a few seconds earlier, so it can still report why.
    $collectionDeadlineTicks = $deadlineTicks - 5 * [TimeSpan]::TicksPerSecond
    $arguments = [Collections.Generic.List[string]]::new()
    foreach ($argument in @(
            "-NoProfile", "-NonInteractive", "-File", $PSCommandPath,
            "-TimeoutDiagnosticsScenario", $ScenarioId,
            "-TimeoutDiagnosticsFixtureProcessId", [string]$Fixture.Id,
            "-TimeoutDiagnosticsFixtureStartTicks", [string]$Fixture.StartTime.ToUniversalTime().Ticks,
            "-TimeoutDiagnosticsReferenceTicks", [string](Get-TimingReference $Timing).Ticks,
            "-TimeoutDiagnosticsDeadlineTicks", [string]$collectionDeadlineTicks)) {
        $arguments.Add($argument)
    }
    if (-not [string]::IsNullOrWhiteSpace($applicationPath)) {
        $arguments.Add("-TimeoutDiagnosticsApplicationPath")
        $arguments.Add($applicationPath)
    }
    # Only a usable directory is passed on.
    $environment = @{ BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY = $diagnosticsRoot }
    $startInfo = New-ProcessStartInfo `
        -FilePath ([Environment]::ProcessPath) `
        -Arguments $arguments.ToArray() `
        -WorkingDirectory $PSScriptRoot `
        -Environment $environment `
        -RedirectOutput
    $collector = [Diagnostics.Process]::new()
    $collector.StartInfo = $startInfo
    try {
        if (-not $collector.Start()) {
            Write-Host "(the timeout diagnostics could not be started)"
            return
        }
        $outputTask = $collector.StandardOutput.ReadToEndAsync()
        $errorTask = $collector.StandardError.ReadToEndAsync()
        $finished = $collector.WaitForExit((Get-RemainingMilliseconds $deadlineTicks))
        if (-not $finished) {
            try {
                $collector.Kill($true)
            }
            catch {
                Write-Host "(the timeout diagnostics process could not be stopped)"
            }
        }
        [void][Threading.Tasks.Task]::WaitAll(@($outputTask, $errorTask), 5000)
        $printed = [Collections.Generic.List[string]]::new()
        if ($outputTask.IsCompletedSuccessfully) {
            foreach ($line in ($outputTask.Result -split "\r?\n")) {
                if (-not [string]::IsNullOrEmpty($line)) {
                    $printed.Add((Get-SafeText $line))
                }
            }
        }
        if (-not $finished) {
            $printed.Add("(the timeout diagnostics stopped at the $($timeoutDiagnosticsMilliseconds / 1000)-second deadline)")
        }
        elseif ($collector.ExitCode -ne 0 -or ($errorTask.IsCompletedSuccessfully -and
                -not [string]::IsNullOrWhiteSpace($errorTask.Result))) {
            $printed.Add("(the timeout diagnostics ended with an error)")
        }
        foreach ($line in $printed) {
            Write-Host $line
        }
        try {
            [IO.File]::WriteAllLines((Join-Path $ScenarioRoot "timeout-processes.txt"), [string[]]$printed.ToArray(),
                [Text.UTF8Encoding]::new($false))
        }
        catch {
            Write-Host "(the processes alive at the fixture timeout could not be saved)"
        }
    }
    finally {
        $collector.Dispose()
    }
}

if ($PSCmdlet.ParameterSetName -eq "TimeoutDiagnostics") {
    Invoke-TimeoutDiagnosticsCollection
    return
}

# --- Server log reading: streamed, with bounded memory. ---

function Get-JsonFields {
    param([Parameter(Mandatory)][Text.Json.JsonElement]$Element)

    $fields = [hashtable]::new([StringComparer]::Ordinal)
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
        if ($text -cmatch '\A-?[0-9]{1,15}\z') {
            return $text
        }
    }
    return $null
}

function Get-CanonicalValue {
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][Collections.Generic.HashSet[string]]$Set,
        [AllowNull()][string]$Value
    )

    # The list's own entry for an ordinally equal value, or $null.
    if ($null -eq $Value) {
        return $null
    }
    $canonical = $null
    if ($Set.TryGetValue($Value, [ref]$canonical)) {
        return $canonical
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
    $canonical = Get-CanonicalValue -Set $rule["Allowed"] -Value (Get-JsonString $Element)
    return $(if ($null -ne $canonical) { $canonical } else { $digestWithheld })
}

function Get-RecordHead {
    param([Parameter(Mandatory)][Text.Json.JsonElement]$Root)

    # A parsed record's UTC timestamp and its top-level Level text (null when absent),
    # or $null for a record that is not an object with a valid timestamp.
    if ($Root.ValueKind -ne [Text.Json.JsonValueKind]::Object) {
        return $null
    }
    # Typed, so that PowerShell can pick the TryGetProperty overload.
    $element = [Text.Json.JsonElement]::new()
    if (-not $Root.TryGetProperty("Timestamp", [ref]$element) -or
        $element.ValueKind -ne [Text.Json.JsonValueKind]::String) {
        return $null
    }
    $time = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse($element.GetString(), $invariantCulture,
            [Globalization.DateTimeStyles]::AssumeUniversal, [ref]$time)) {
        return $null
    }
    $level = $null
    if ($Root.TryGetProperty("Level", [ref]$element) -and $element.ValueKind -eq [Text.Json.JsonValueKind]::String) {
        $level = $element.GetString()
    }
    return [pscustomobject]@{ Ticks = $time.UtcTicks; Level = $level }
}

function Test-LogRecordTooLarge {
    param([Parameter(Mandatory)][string]$Line)

    # The server's own placeholder for a record over 16 KiB: a warning without a
    # timestamp (BriosaRetainedLogSink).
    return [string]::Equals($Line,
        '{"Level":"Warning","MessageTemplate":"LogRecordTooLarge","Properties":{"SchemaVersion":1}}', $ordinal)
}

function ConvertFrom-ServerLogLine {
    param(
        [Parameter(Mandatory)][string]$Line,
        [long]$TooLargeTicks = 0
    )

    if (Test-LogRecordTooLarge $Line) {
        return [pscustomobject]@{
            Ticks = $TooLargeTicks
            Level = "Warning"
            Name = "LogRecordTooLarge"
            Instance = $null
            Fields = [ordered]@{}
        }
    }
    try {
        $document = [Text.Json.JsonDocument]::Parse($Line)
    }
    catch {
        return $null
    }
    try {
        # The same timestamp and Level that admitted the record to the digest.
        $head = Get-RecordHead $document.RootElement
        if ($null -eq $head) {
            return $null
        }
        $fields = Get-JsonFields $document.RootElement
        $level = Get-CanonicalValue -Set $digestLevels -Value $head.Level
        if ($null -eq $level) {
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
            if ($null -ne $eventNumber -and $eventNumber -cmatch '\A[0-9]{1,9}\z' -and
                $digestEventNames.ContainsKey([int]$eventNumber) -and
                [string]::Equals($label, $digestEventNames[[int]$eventNumber], $ordinal)) {
                $name = $digestEventNames[[int]$eventNumber]
                $eventNumber = $null
            }
            elseif ([string]::Equals($label, "FrameworkEvent", $ordinal)) {
                $name = "FrameworkEvent"
            }
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
            Ticks = $head.Ticks
            Level = $level
            Name = $name
            Instance = $(if ($null -ne $instance -and $instance -cmatch '\A[0-9a-f]{32}\z') { $instance })
            Fields = $values
        }
    }
    finally {
        $document.Dispose()
    }
}

function Read-LogBlock {
    param(
        [Parameter(Mandatory)][IO.StreamReader]$Reader,
        [Parameter(Mandatory)][char[]]$Buffer,
        [Parameter(Mandatory)][Text.StringBuilder]$Pending,
        [Parameter(Mandatory)][hashtable]$State
    )

    # The next block of complete lines. A line longer than the limit is dropped as it
    # streams past and counted, so it is never held in memory.
    $lines = [Collections.Generic.List[string]]::new()
    $count = $Reader.Read($Buffer, 0, $Buffer.Length)
    if ($count -le 0) {
        if ($Pending.Length -gt 0 -and -not $State.Skipping) {
            $lines.Add($Pending.ToString().TrimEnd([char]13))
        }
        [void]$Pending.Clear()
        $State.Done = $true
        return , $lines
    }
    $chunk = [string]::new($Buffer, 0, $count)
    $start = 0
    while ($start -lt $count) {
        $newline = $chunk.IndexOf([char]10, $start)
        $end = $count
        if ($newline -ge 0) {
            $end = $newline
        }
        $length = $end - $start
        if (-not $State.Skipping) {
            if ($Pending.Length + $length -gt $digestMaximumLineLength) {
                $State.Skipping = $true
                $State.LongLines++
                [void]$Pending.Clear()
            }
            elseif ($newline -ge 0 -and $Pending.Length -eq 0) {
                # The common case: the whole line is inside this block.
                $lines.Add($chunk.Substring($start, $length).TrimEnd([char]13))
            }
            else {
                # Appending a substring: the (string, int, int) overload would be
                # resolved through a copy of the whole block on every call.
                [void]$Pending.Append($chunk.Substring($start, $length))
                if ($newline -ge 0) {
                    $lines.Add($Pending.ToString().TrimEnd([char]13))
                    [void]$Pending.Clear()
                }
            }
        }
        if ($newline -lt 0) {
            break
        }
        $State.Skipping = $false
        $start = $newline + 1
    }
    return , $lines
}

function Get-ServerLogFiles {
    param([Parameter(Mandatory)][string]$ScenarioRoot)

    $logRoot = Join-Path $ScenarioRoot "server-logs"
    if (-not (Test-Path -LiteralPath $logRoot -PathType Container)) {
        return @()
    }
    return @(Get-ChildItem -LiteralPath $logRoot -Filter "*.jsonl" -File | Sort-Object Name)
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
    if ([string]::Equals($record.Name, "EndpointListening", $ordinal) -and $null -eq $Log.Listening) {
        $Log.Listening = $record
    }
    elseif ([string]::Equals($record.Name, "LifecycleRpcCompleted", $ordinal) -and $record.Fields.Contains("Rpc") -and
        -not [string]::Equals($record.Fields["Rpc"], $digestWithheld, $ordinal) -and
        -not $Log.Completed.ContainsKey($record.Fields["Rpc"])) {
        $Log.Completed[$record.Fields["Rpc"]] = $record
    }
}

function Add-AuditValues {
    param(
        [Parameter(Mandatory)][object]$Log,
        [Parameter(Mandatory)][string]$Line
    )

    try {
        $document = [Text.Json.JsonDocument]::Parse($Line)
    }
    catch {
        return
    }
    try {
        $root = Get-JsonFields $document.RootElement
        if (-not $root.ContainsKey("Properties")) {
            return
        }
        $properties = Get-JsonFields $root["Properties"]
        foreach ($field in @("DiagnosticCode", "OperationId")) {
            if (-not $properties.ContainsKey($field) -or
                $properties[$field].ValueKind -eq [Text.Json.JsonValueKind]::Null) {
                continue
            }
            $value = Get-JsonString $properties[$field]
            if ($null -eq (Get-CanonicalValue -Set $digestFields[$field]["Allowed"] -Value $value)) {
                # Only the count: an unlisted value is never written anywhere.
                $Log.Unlisted[$field]++
            }
        }
    }
    catch {
    }
    finally {
        $document.Dispose()
    }
}

function Save-SpoolChunk {
    param(
        [Parameter(Mandatory)][object]$Log,
        [Parameter(Mandatory)][AllowEmptyCollection()][object]$Chunk,
        [Parameter(Mandatory)][string]$SpoolRoot
    )

    # Writes the buffered warnings and errors as one spool file, sorted by timestamp
    # and reading order, and empties the buffer.
    if ($Chunk.Count -eq 0) {
        return
    }
    $Chunk.Sort()
    $spoolPath = Join-Path $SpoolRoot "$($Log.Spools.Count).spool"
    $writer = [IO.StreamWriter]::new($spoolPath, $false, [Text.UTF8Encoding]::new($false))
    try {
        foreach ($entry in $Chunk) {
            $writer.WriteLine("$($entry.Item1)`t$($entry.Item2)`t$($entry.Item3)")
        }
    }
    finally {
        $writer.Dispose()
    }
    $Log.Spools.Add($spoolPath)
    $Chunk.Clear()
    $Log.ChunkCharacters = 0
}

# One streamed pass over a scenario's server log files. In digest mode it keeps the
# latest lower-severity records in one bounded, timestamp-ordered queue across all
# files and spools every warning and error in sorted chunks. In phase mode it keeps
# only the first endpoint and lifecycle completion records. In audit mode it counts
# the DiagnosticCode and OperationId values the vocabulary does not list. A file that
# cannot be read is counted and skipped; it never loses the rest.
function Read-ServerLog {
    param(
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [ValidateSet("Digest", "Phases", "Audit")][string]$Mode = "Digest",
        [string]$SpoolRoot,
        [long]$ReferenceTicks = 0
    )

    $log = [pscustomobject]@{
        Files = 0
        UnreadableFiles = 0
        MalformedRecords = 0
        LongLines = 0
        Instances = [Collections.Generic.List[string]]::new()
        # Ordered by timestamp, then by reading order, which keeps a file's own order.
        Tail = [Collections.Generic.PriorityQueue[string, ValueTuple[long, long]]]::new()
        OrdinarySeen = 0
        Sequence = [long]0
        Chunk = [Collections.Generic.List[ValueTuple[long, long, string]]]::new()
        ChunkCharacters = 0
        Spools = [Collections.Generic.List[string]]::new()
        Listening = $null
        Completed = @{}
        Unlisted = [ordered]@{ DiagnosticCode = 0; OperationId = 0 }
    }
    $files = @()
    try {
        $files = @(Get-ServerLogFiles -ScenarioRoot $ScenarioRoot)
    }
    catch {
        $log.UnreadableFiles++
    }
    $log.Files = $files.Count
    $buffer = [char[]]::new($digestReadBlockCharacters)
    for ($fileIndex = 0; $fileIndex -lt $files.Count; $fileIndex++) {
        $file = $files[$fileIndex]
        if ($file.Name -cmatch '\Abriosa-([0-9a-f]{32})-' -and -not $log.Instances.Contains($Matches[1])) {
            $log.Instances.Add($Matches[1])
        }
        $stream = $null
        $reader = $null
        $state = @{ Skipping = $false; Done = $false; LongLines = 0 }
        try {
            # A server that was killed, or one still shutting down, may hold the file open.
            $stream = [IO.FileStream]::new($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
            $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false))
            $pending = [Text.StringBuilder]::new()
            $previousTicks = $ReferenceTicks
            while (-not $state.Done) {
                foreach ($line in (Read-LogBlock -Reader $reader -Buffer $buffer -Pending $pending -State $state)) {
                    if ([string]::IsNullOrWhiteSpace($line)) {
                        continue
                    }
                    if ($Mode -eq "Phases") {
                        if ($line.Contains('"EndpointListening"') -or $line.Contains('"LifecycleRpcCompleted"')) {
                            Add-PhaseRecord -Log $log -Line $line
                        }
                        continue
                    }
                    if ($Mode -eq "Audit") {
                        Add-AuditValues -Log $log -Line $line
                        continue
                    }
                    $sequence = $log.Sequence++
                    $important = $false
                    if (Test-LogRecordTooLarge $line) {
                        # Placed at the time of the record before it in its file.
                        $ticks = $previousTicks
                        $important = $true
                    }
                    else {
                        # Converted in full before it may enter either path: a truncated,
                        # malformed or undecodable record never displaces a valid one, and
                        # never reaches the rendering.
                        $record = $null
                        try {
                            $record = ConvertFrom-ServerLogLine $line
                        }
                        catch {
                            $record = $null
                        }
                        if ($null -eq $record) {
                            $log.MalformedRecords++
                            continue
                        }
                        $ticks = $record.Ticks
                        $previousTicks = $ticks
                        $important = $null -ne (Get-CanonicalValue -Set $digestImportantLevels -Value $record.Level)
                    }
                    if ($important) {
                        # A lone carriage return would split a spooled line when it is read back.
                        $spoolLine = $line.Replace([char]13, [char]32)
                        $log.Chunk.Add([ValueTuple]::Create([long]$ticks, [long]$sequence, $spoolLine))
                        $log.ChunkCharacters += $spoolLine.Length
                        if ($log.Chunk.Count -ge $digestSpoolChunkRecords -or
                            $log.ChunkCharacters -ge $digestSpoolChunkCharacters) {
                            Save-SpoolChunk -Log $log -Chunk $log.Chunk -SpoolRoot $SpoolRoot
                        }
                        continue
                    }
                    $log.OrdinarySeen++
                    $order = [ValueTuple]::Create([long]$ticks, [long]$sequence)
                    if ($log.Tail.Count -lt $digestTailLimit) {
                        $log.Tail.Enqueue($line, $order)
                    }
                    else {
                        # Keeps the later of the oldest kept line and this one.
                        [void]$log.Tail.EnqueueDequeue($line, $order)
                    }
                }
            }
        }
        catch {
            # For example, a file another process holds exclusively.
            $log.UnreadableFiles++
        }
        finally {
            $log.LongLines += $state.LongLines
            if ($null -ne $reader) { $reader.Dispose() }
            if ($null -ne $stream) { $stream.Dispose() }
        }
    }
    if ($Mode -eq "Digest") {
        Save-SpoolChunk -Log $log -Chunk $log.Chunk -SpoolRoot $SpoolRoot
    }
    return $log
}

function Write-DigestAudit {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot
    )

    # Diagnostics only: never changes the scenario's result.
    if ([string]::IsNullOrWhiteSpace($digestAuditPath)) {
        return
    }
    try {
        $log = Read-ServerLog -ScenarioRoot $ScenarioRoot -Mode Audit
        # The file holds only vocabulary misses: the scenario, the property and how
        # many times it held an unlisted value, never the value itself.
        $lines = [Collections.Generic.List[string]]::new()
        foreach ($entry in $log.Unlisted.GetEnumerator()) {
            if ($entry.Value -gt 0) {
                $lines.Add("$ScenarioId $($entry.Key) $($entry.Value)")
            }
        }
        if ($log.UnreadableFiles -gt 0) {
            Write-Host "($($log.UnreadableFiles) server log files of $ScenarioId could not be read for the digest audit)"
        }
        if ($lines.Count -gt 0) {
            [IO.File]::AppendAllLines($digestAuditPath, [string[]]$lines.ToArray(), [Text.UTF8Encoding]::new($false))
        }
    }
    catch {
        Write-Host "(the digest audit could not be written for $ScenarioId)"
    }
}

function Write-DigestLine {
    param(
        [AllowNull()][IO.StreamWriter]$Writer,
        [Parameter(Mandatory)][string]$Line
    )

    Write-Host $Line
    if ($null -ne $Writer) {
        try {
            $Writer.WriteLine($Line)
        }
        catch {
        }
    }
}

function Read-SpoolRecord {
    param([Parameter(Mandatory)][IO.StreamReader]$Reader)

    # The next spooled warning or error, written as ticks, sequence and line,
    # tab-separated; Record is $null for one that does not parse.
    $line = $Reader.ReadLine()
    if ($null -eq $line) {
        return $null
    }
    $parts = $line.Split([char]9, 3)
    $ticks = [long]0
    $sequence = [long]0
    if ($parts.Count -ne 3 -or -not [long]::TryParse($parts[0], [ref]$ticks) -or
        -not [long]::TryParse($parts[1], [ref]$sequence)) {
        return [pscustomobject]@{ Order = [ValueTuple]::Create([long]0, [long]0); Record = $null }
    }
    $record = ConvertFrom-ServerLogLine -Line $parts[2] -TooLargeTicks $ticks
    return [pscustomobject]@{ Order = [ValueTuple]::Create($ticks, $sequence); Record = $record }
}

# One line per server event, relative to the fixture start: the phase timeline of a
# rejected scenario, readable in the CI log without downloading anything. The lines
# go to the host and to the saved digest as they are produced.
function Write-ServerLogDigest {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [Parameter(Mandatory)][hashtable]$Timing,
        [AllowNull()][IO.StreamWriter]$Writer
    )

    $reference = Get-TimingReference $Timing
    Write-DigestLine $Writer "--- server log digest for $ScenarioId (times from $($reference.Name)) ---"
    $spoolRoot = Join-Path $temporaryRoot "digest-spool-$([Guid]::NewGuid().ToString('N'))"
    $log = $null
    $spoolReaders = [Collections.Generic.List[IO.StreamReader]]::new()
    try {
        try {
            [IO.Directory]::CreateDirectory($spoolRoot) | Out-Null
            $log = Read-ServerLog -ScenarioRoot $ScenarioRoot -Mode Digest -SpoolRoot $spoolRoot -ReferenceTicks $reference.Ticks
        }
        catch {
            Write-DigestLine $Writer "(the server log could not be read)"
        }
        # Sources, merged by timestamp: the kept lower-severity records with the
        # runner's own fixture marks, and each spooled file of warnings and errors.
        $ordinary = [Collections.Generic.List[object]]::new()
        $unreadable = 0
        if ($null -ne $log) {
            if ($log.Files -eq 0) {
                Write-DigestLine $Writer "(no server log file was found)"
            }
            $element = $null
            $priority = [ValueTuple]::Create([long]0, [long]0)
            while ($log.Tail.TryDequeue([ref]$element, [ref]$priority)) {
                $record = ConvertFrom-ServerLogLine $element
                if ($null -eq $record) {
                    $unreadable++
                    continue
                }
                $ordinary.Add([pscustomobject]@{ Order = $priority; Record = $record })
            }
            $omitted = $log.OrdinarySeen - $ordinary.Count - $unreadable
            if ($omitted -gt 0) {
                Write-DigestLine $Writer "($omitted earlier lower-severity records omitted; every warning and error is kept)"
            }
            foreach ($spoolPath in $log.Spools) {
                $spoolReaders.Add([IO.StreamReader]::new($spoolPath, [Text.UTF8Encoding]::new($false)))
            }
        }
        foreach ($mark in @(
                @("FixtureStarted", "FixtureStart"),
                @("FixtureEnded", $(if ($Timing.TimedOut) { "FixtureTimedOut" } else { "FixtureEnd" })))) {
            if ($null -ne $Timing[$mark[0]]) {
                $markTicks = ([DateTimeOffset]$Timing[$mark[0]]).UtcTicks
                # After any server record with the same timestamp.
                $ordinary.Add([pscustomobject]@{
                    Order = [ValueTuple]::Create($markTicks, [long]::MaxValue)
                    Record = [pscustomobject]@{
                        Ticks = $markTicks
                        Level = "Runner"
                        Name = $mark[1]
                        Instance = $null
                        Fields = [ordered]@{}
                    }
                })
            }
        }
        $ordinarySorted = @($ordinary | Sort-Object -Property Order)
        # Assigned directly: a subexpression would unroll a one-entry list.
        $instances = [Collections.Generic.List[string]]::new()
        if ($null -ne $log) {
            $instances = $log.Instances
        }
        # Heads: source -1 is the ordinary list, 0..n-1 the spools.
        $heads = [Collections.Generic.PriorityQueue[int, ValueTuple[long, long]]]::new()
        $spoolHeads = @{}
        $ordinaryIndex = 0
        if ($ordinarySorted.Count -gt 0) {
            $heads.Enqueue(-1, $ordinarySorted[0].Order)
        }
        for ($index = 0; $index -lt $spoolReaders.Count; $index++) {
            $next = Read-SpoolRecord -Reader $spoolReaders[$index]
            if ($null -ne $next) {
                $spoolHeads[$index] = $next
                $heads.Enqueue($index, $next.Order)
            }
        }
        $source = 0
        $headOrder = [ValueTuple]::Create([long]0, [long]0)
        while ($heads.TryDequeue([ref]$source, [ref]$headOrder)) {
            if ($source -lt 0) {
                $record = $ordinarySorted[$ordinaryIndex].Record
                $ordinaryIndex++
                if ($ordinaryIndex -lt $ordinarySorted.Count) {
                    $heads.Enqueue(-1, $ordinarySorted[$ordinaryIndex].Order)
                }
            }
            else {
                $record = $spoolHeads[$source].Record
                $next = Read-SpoolRecord -Reader $spoolReaders[$source]
                if ($null -ne $next) {
                    $spoolHeads[$source] = $next
                    $heads.Enqueue($source, $next.Order)
                }
            }
            if ($null -eq $record) {
                $unreadable++
                continue
            }
            $server = ""
            if ($instances.Count -gt 1 -and $null -ne $record.Instance) {
                $server = " [server $($instances.IndexOf($record.Instance) + 1)]"
            }
            $values = @($record.Fields.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" })
            Write-DigestLine $Writer (("{0} {1}{2} {3} {4}" -f (Format-Offset $record.Ticks $reference.Ticks), $record.Level,
                    $server, $record.Name, ($values -join " ")).TrimEnd())
        }
        if ($null -ne $log) {
            $malformed = $unreadable + $log.MalformedRecords
            if ($malformed -gt 0) {
                Write-DigestLine $Writer "($malformed malformed server log records skipped)"
            }
            if ($log.LongLines -gt 0) {
                Write-DigestLine $Writer "($($log.LongLines) server log lines over 64 KiB skipped)"
            }
            if ($log.UnreadableFiles -gt 0) {
                Write-DigestLine $Writer "($($log.UnreadableFiles) server log files could not be read)"
            }
        }
    }
    finally {
        foreach ($spoolReader in $spoolReaders) {
            $spoolReader.Dispose()
        }
        try {
            if (Test-Path -LiteralPath $spoolRoot) {
                Remove-Item -LiteralPath $spoolRoot -Recurse -Force
            }
        }
        catch {
        }
        Write-DigestLine $Writer "--- end server log digest for $ScenarioId ---"
    }
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
        $log = Read-ServerLog -ScenarioRoot $ScenarioRoot -Mode Phases
        if ($null -ne $log.Listening -and $null -ne $Timing.FixtureStarted) {
            $parts.Add("server listening $(Format-Offset $log.Listening.Ticks ([DateTimeOffset]$Timing.FixtureStarted).UtcTicks)")
        }
        foreach ($phase in @(
                @("StartSpatialAnalyzerSdk", "worker start"),
                @("LaunchSpatialAnalyzer", "launch"),
                @("ConnectToSpatialAnalyzer", "connect"))) {
            $completed = $log.Completed[$phase[0]]
            if ($null -ne $completed -and $completed.Fields.Contains("ElapsedMilliseconds") -and
                -not [string]::Equals($completed.Fields["ElapsedMilliseconds"], $digestWithheld, $ordinal)) {
                $parts.Add("$($phase[1]) $($completed.Fields["ElapsedMilliseconds"]) ms")
            }
        }
        return ($parts -join "; ")
    }
    catch {
        return ""
    }
}

function Get-ClientTraceEnvironment {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot
    )

    # The fixture inherits these, and so do the processes it starts. Each library
    # reads only its own names, except EventPipe, which every .NET process in the
    # tree reads; its {pid} gives each process its own file. A trace that cannot be
    # set up is reported and left off; the scenario runs as it would without it.
    try {
        switch -CaseSensitive ($clientTrace) {
            "js" {
                return @{ GRPC_NODE_VERBOSITY = "DEBUG"; GRPC_NODE_TRACE = $clientTraceJavaScriptTracers }
            }
            "py" {
                return @{ GRPC_VERBOSITY = "DEBUG"; GRPC_TRACE = $clientTracePythonTracers }
            }
            "dotnet" {
                $traceRoot = Join-Path $ScenarioRoot "client-trace"
                [IO.Directory]::CreateDirectory($traceRoot) | Out-Null
                return @{
                    DOTNET_EnableEventPipe = "1"
                    DOTNET_EventPipeOutputPath = (Join-Path $traceRoot "dotnet-{pid}.nettrace")
                    DOTNET_EventPipeConfig = $clientTraceDotNetProviders
                    # Written as it is produced, so a killed fixture still leaves its events.
                    DOTNET_EventPipeOutputStreaming = "1"
                    DOTNET_EventPipeRundown = "0"
                }
            }
        }
    }
    catch {
        Write-Host "(client gRPC tracing is off for $($ScenarioId): its trace could not be set up)"
    }
    return @{}
}

function ConvertFrom-JavaScriptTraceRecord {
    param(
        [Parameter(Mandatory)][Text.RegularExpressions.Match]$Record,
        [Parameter(Mandatory)][AllowEmptyCollection()][Collections.Generic.List[object]]$Calls
    )

    # One grpc-js record as an event built only from validated numbers, method names
    # and its time, or $null for a record whose shape is not on this list.
    $head = "$($Record.Groups['time'].Value) pid $($Record.Groups['pid'].Value)"
    $text = $Record.Groups["text"].Value
    $match = $null
    switch -CaseSensitive ($Record.Groups["tracer"].Value) {
        "channel" {
            $match = [regex]::Match($text, "\A\([0-9]{1,10}\) [^ ]+ createResolvingCall \[(?<call>[0-9]{1,10})\] method=`"(?<method>$clientTraceMethod)`", deadline=")
            if ($match.Success) {
                $Calls.Add([pscustomobject]@{
                    Process = $Record.Groups["pid"].Value; Number = $match.Groups["call"].Value
                    Method = $match.Groups["method"].Value; Created = $Record.Groups["time"].Value; Ended = $null; Code = $null
                })
                return "$head call $($match.Groups['call'].Value) created $($match.Groups['method'].Value)"
            }
        }
        "resolving_call" {
            $match = [regex]::Match($text, '\A\[(?<call>[0-9]{1,10})\] Created child \[(?<child>[0-9]{1,10})\]\z')
            if ($match.Success) {
                return "$head call $($match.Groups['call'].Value) uses stream $($match.Groups['child'].Value)"
            }
            $match = [regex]::Match($text, '\A\[(?<call>[0-9]{1,10})\] ended with status: code=(?<code>[0-9]{1,2}) details=')
            if ($match.Success) {
                foreach ($call in $Calls) {
                    if ($call.Process -ceq $Record.Groups["pid"].Value -and $call.Number -ceq $match.Groups["call"].Value -and
                        $null -eq $call.Ended) {
                        $call.Ended = $Record.Groups["time"].Value
                        $call.Code = [int]$match.Groups["code"].Value
                    }
                }
                return "$head call $($match.Groups['call'].Value) ended with status $($match.Groups['code'].Value)"
            }
        }
        "subchannel_call" {
            foreach ($shape in @(
                    @('\A\[(?<stream>[0-9]{1,10})\] Received server headers:\z', 'received headers'),
                    @('\A\[(?<stream>[0-9]{1,10})\] Received server trailers:\z', 'received trailers'),
                    @('\A\[(?<stream>[0-9]{1,10})\] received status code (?<code>[0-9]{1,2}) from server\z', 'received status {0}'),
                    @('\A\[(?<stream>[0-9]{1,10})\] HTTP/2 stream closed with code (?<code>[0-9]{1,3})\z', 'closed with HTTP/2 code {0}'),
                    @('\A\[(?<stream>[0-9]{1,10})\] ended with status: code=(?<code>[0-9]{1,2}) details=', 'ended with status {0}'))) {
                $match = [regex]::Match($text, $shape[0])
                if ($match.Success) {
                    return "$head stream $($match.Groups['stream'].Value) $([string]::Format($invariantCulture, $shape[1], $match.Groups['code'].Value))"
                }
            }
        }
        "subchannel" {
            $match = [regex]::Match($text, "\A\((?<id>[0-9]{1,10})\) [^ ]+ (?<from>$clientTraceSubchannelStates) -> (?<to>$clientTraceSubchannelStates)\z")
            if ($match.Success) {
                return "$head subchannel $($match.Groups['id'].Value) $($match.Groups['from'].Value) -> $($match.Groups['to'].Value)"
            }
        }
        "transport" {
            foreach ($shape in @(
                    @(' Established TCP connection\z', 'connection established'),
                    @(' session closed\z', 'session closed'),
                    @(' connection closed by GOAWAY with code (?<code>[0-9]{1,3})\z', 'connection closed by GOAWAY with code {0}'),
                    @(' connection closed with error ', 'connection closed with an error'),
                    @(' connection closed\. hadError=(?<code>true|false)\z', 'connection closed, error {0}'))) {
                $match = [regex]::Match($text, $shape[0])
                if ($match.Success) {
                    return "$head transport $([string]::Format($invariantCulture, $shape[1], $match.Groups['code'].Value))"
                }
            }
        }
    }
    return $null
}

function ConvertFrom-PythonTraceRecord {
    param([Parameter(Mandatory)][Text.RegularExpressions.Match]$Record)

    # One grpcio record as an event built only from a validated method name, a status
    # name and its local time, or $null for any other record.
    $head = "$($Record.Groups['date'].Value) $($Record.Groups['time'].Value) local thread $($Record.Groups['thread'].Value)"
    $text = $Record.Groups["text"].Value
    switch -CaseSensitive ($Record.Groups["file"].Value) {
        "client_channel_filter.cc" {
            $match = [regex]::Match($text, "batch started from above: +SEND_INITIAL_METADATA\{:path: (?<method>$clientTraceMethod)[,}]")
            if ($match.Success) {
                return "$head call started $($match.Groups['method'].Value)"
            }
        }
        "filter_stack_call.cc" {
            $match = [regex]::Match($text, '\Aset_final_status CLI (?<status>[A-Z_]{2,32})(?::|\z)')
            if ($match.Success -and $clientTraceCoreStatuses.Contains($match.Groups["status"].Value)) {
                return "$head call ended with status $($match.Groups['status'].Value)"
            }
        }
    }
    return $null
}

function Split-ClientTrace {
    param(
        [AllowEmptyString()][string]$ErrorOutput,
        [Parameter(Mandatory)][string]$ScenarioId
    )

    # With client tracing on, the gRPC library's records leave the fixture's stderr,
    # so the stderr printed and kept, and the failure message, stay the fixture's own.
    # Only proven library records are taken; an ambiguous line stays in the stderr.
    # The trace keeps only events rendered from validated fields, never a record's
    # text, so no address, header value or status message reaches it.
    $result = [pscustomobject]@{ ErrorOutput = $ErrorOutput; Trace = $null }
    if ($null -eq $clientTrace -or $clientTrace -ceq "dotnet") {
        return $result
    }
    try {
        $kept = [Collections.Generic.List[string]]::new()
        $events = [Collections.Generic.List[string]]::new()
        $calls = [Collections.Generic.List[object]]::new()
        $other = 0
        # A trailers record waits for its header lines, whose allow-listed names it lists.
        $headers = $null
        foreach ($line in ($ErrorOutput -split "\r?\n")) {
            if ($null -ne $headers) {
                # grpc-js ends a header list with an empty line.
                if ($line.Length -eq 0) {
                    $headers.Ended = $true
                    continue
                }
                $header = $(if ($headers.Ended) { [regex]::Match('', '\A\z(?!)') } else {
                        [regex]::Match($line, $clientTraceJavaScriptHeaderLine) })
                if ($header.Success) {
                    if ($headers.Trailers -and $clientTraceTrailerNames.Contains($header.Groups["name"].Value)) {
                        $headers.Names.Add($header.Groups["name"].Value)
                    }
                    continue
                }
                if ($headers.Trailers) {
                    $events[$headers.Index] += $(if ($headers.Names.Count -gt 0) { ": " + ($headers.Names -join ", ") } else { "" })
                }
                $headers = $null
            }
            if ($clientTrace -ceq "js") {
                $record = [regex]::Match($line, $clientTraceJavaScriptRecord)
                if (-not $record.Success) {
                    $kept.Add($line)
                    continue
                }
                # A recognized record leaves the stderr even if it cannot be rendered.
                $event = $null
                try {
                    $event = ConvertFrom-JavaScriptTraceRecord -Record $record -Calls $calls
                }
                catch {
                }
                if ($null -eq $event) {
                    $other++
                    continue
                }
                $events.Add($event)
                if ($event.EndsWith(" received headers", [StringComparison]::Ordinal) -or
                    $event.EndsWith(" received trailers", [StringComparison]::Ordinal)) {
                    $headers = [pscustomobject]@{
                        Index = $events.Count - 1
                        Trailers = $event.EndsWith(" received trailers", [StringComparison]::Ordinal)
                        Names = [Collections.Generic.List[string]]::new()
                        Ended = $false
                    }
                }
                continue
            }
            $record = [regex]::Match($line, $clientTracePythonRecord)
            if (-not $record.Success) {
                $kept.Add($line)
                continue
            }
            $event = $null
            try {
                $event = ConvertFrom-PythonTraceRecord -Record $record
            }
            catch {
            }
            if ($null -eq $event) {
                $other++
            }
            else {
                $events.Add($event)
            }
        }
        if ($null -ne $headers -and $headers.Trailers -and $headers.Names.Count -gt 0) {
            $events[$headers.Index] += ": " + ($headers.Names -join ", ")
        }
        $library = $(if ($clientTrace -ceq "js") { "@grpc/grpc-js" } else { "grpcio" })
        $lines = [Collections.Generic.List[string]]::new()
        $lines.Add("# Client gRPC events from the $library trace. Only validated method names, numbers, states and times are kept.")
        $lines.AddRange($events)
        $lines.Add("($other other library trace records were not kept)")
        $result.ErrorOutput = ($kept -join [Environment]::NewLine).Trim()
        $result.Trace = ($lines -join [Environment]::NewLine) + [Environment]::NewLine
    }
    catch {
        Write-Host "(the client gRPC trace of $ScenarioId could not be separated from the fixture's stderr; the stderr is kept whole)"
        $result.ErrorOutput = $ErrorOutput
        $result.Trace = $null
    }
    return $result
}

function Save-FixtureOutput {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [AllowEmptyString()][string]$Output,
        [AllowEmptyString()][string]$ErrorOutput,
        [AllowNull()][string]$Trace
    )

    $encoding = [Text.UTF8Encoding]::new($false)
    [IO.File]::WriteAllText(
        (Join-Path $ScenarioRoot "fixture.stdout.txt"), (Get-SafeText $Output), $encoding)
    [IO.File]::WriteAllText(
        (Join-Path $ScenarioRoot "fixture.stderr.txt"), (Get-SafeText $ErrorOutput), $encoding)
    # A [string] parameter turns $null into an empty string, so the language decides.
    # The trace is optional: a file that cannot be written is reported and nothing else.
    if ($null -ne $clientTrace -and $clientTrace -cne "dotnet") {
        try {
            [IO.File]::WriteAllText((Join-Path $ScenarioRoot "client-trace.txt"), $Trace, $encoding)
        }
        catch {
            Write-Host "(the client gRPC trace of $ScenarioId could not be saved)"
        }
    }
}

function Write-ClientCallSummary {
    param(
        [Parameter(Mandatory)][string]$ScenarioId,
        [Parameter(Mandatory)][string]$ScenarioRoot,
        [Parameter(Mandatory)][hashtable]$Timing
    )

    # Diagnostics only. From a grpc-js trace, one line per client call: its method,
    # when it was created, and its status and when it ended, or that it had not ended
    # when the fixture stopped.
    if ($null -eq $clientTrace) {
        return
    }
    if ($clientTrace -cne "js") {
        $saved = @(Get-ChildItem -LiteralPath $ScenarioRoot -Recurse -File -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -ceq "client-trace.txt" -or $_.Directory.Name -ceq "client-trace" })
        Write-Host "(client gRPC trace: $($saved.Count) files are kept with the diagnostics of $ScenarioId)"
        return
    }
    $tracePath = Join-Path $ScenarioRoot "client-trace.txt"
    if (-not (Test-Path -LiteralPath $tracePath -PathType Leaf)) {
        Write-Host "(no client gRPC trace was saved for $ScenarioId)"
        return
    }
    $reference = Get-TimingReference $Timing
    $statusNames = @("OK", "Cancelled", "Unknown", "InvalidArgument", "DeadlineExceeded", "NotFound", "AlreadyExists",
        "PermissionDenied", "ResourceExhausted", "FailedPrecondition", "Aborted", "OutOfRange", "Unimplemented",
        "Internal", "Unavailable", "DataLoss", "Unauthenticated")
    $created = "\A(?<time>[0-9T:.Z-]{24}) pid (?<pid>[0-9]{1,10}) call (?<call>[0-9]{1,10}) created (?<method>$clientTraceMethod)\z"
    $ended = '\A(?<time>[0-9T:.Z-]{24}) pid (?<pid>[0-9]{1,10}) call (?<call>[0-9]{1,10}) ended with status (?<code>[0-9]{1,2})\z'
    $calls = [Collections.Generic.List[object]]::new()
    $byKey = @{}
    try {
        foreach ($line in [IO.File]::ReadLines($tracePath)) {
            $match = [regex]::Match($line, $created)
            if ($match.Success) {
                $call = [pscustomobject]@{
                    Number = $match.Groups["call"].Value
                    Method = $match.Groups["method"].Value
                    Created = [DateTimeOffset]::Parse($match.Groups["time"].Value, $invariantCulture,
                        [Globalization.DateTimeStyles]::AssumeUniversal)
                    Ended = $null
                    Code = $null
                }
                $byKey["$($match.Groups['pid'].Value)/$($call.Number)"] = $call
                $calls.Add($call)
                continue
            }
            $match = [regex]::Match($line, $ended)
            if ($match.Success) {
                $call = $byKey["$($match.Groups['pid'].Value)/$($match.Groups['call'].Value)"]
                if ($null -ne $call -and $null -eq $call.Ended) {
                    $call.Ended = [DateTimeOffset]::Parse($match.Groups["time"].Value, $invariantCulture,
                        [Globalization.DateTimeStyles]::AssumeUniversal)
                    $call.Code = [int]$match.Groups["code"].Value
                }
            }
        }
    }
    catch {
        Write-Host "(the client gRPC trace of $ScenarioId could not be read)"
        return
    }
    # Consecutive calls to one method that ended with one status, such as a client
    # polling the server while it starts, share a line.
    $groups = [Collections.Generic.List[object]]::new()
    foreach ($call in $calls) {
        $last = $(if ($groups.Count -gt 0) { $groups[$groups.Count - 1] })
        if ($null -ne $last -and $null -ne $call.Ended -and $null -ne $last.Last.Ended -and
            [string]::Equals($last.Last.Method, $call.Method, $ordinal) -and $last.Last.Code -eq $call.Code) {
            $last.Last = $call
            $last.Size++
            continue
        }
        $groups.Add([pscustomobject]@{ First = $call; Last = $call; Size = 1 })
    }
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add("--- client gRPC calls in $ScenarioId (from the client trace; times from $($reference.Name)) ---")
    $shown = @($groups | Select-Object -Last 200)
    if ($groups.Count -gt $shown.Count) {
        $lines.Add("($($groups.Count - $shown.Count) earlier lines omitted)")
    }
    foreach ($group in $shown) {
        $call = $group.Last
        $end = $(if ($null -eq $call.Ended) { "no status by the end of the fixture" } else {
                $name = $(if ($call.Code -lt $statusNames.Count) { $statusNames[$call.Code] } else { $digestWithheld })
                "status $($call.Code) ($name) at $(Format-Offset $call.Ended.UtcTicks $reference.Ticks)"
            })
        $start = Format-Offset $group.First.Created.UtcTicks $reference.Ticks
        if ($group.Size -eq 1) {
            $lines.Add("$start call $($call.Number) $($call.Method): $end")
        }
        else {
            $lines.Add("$start calls $($group.First.Number) to $($call.Number) $($call.Method) ($($group.Size) calls): each $($end -replace ' at ', ', the last at ')")
        }
    }
    if ($calls.Count -eq 0) {
        $lines.Add("(the trace records no client calls)")
    }
    $lines.Add("--- end client gRPC calls in $ScenarioId ---")
    foreach ($line in $lines) {
        Write-Host $line
    }
    try {
        [IO.File]::WriteAllLines((Join-Path $ScenarioRoot "client-calls.txt"), [string[]]$lines.ToArray(),
            [Text.UTF8Encoding]::new($false))
    }
    catch {
    }
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
    # The outcome and the digest first, so that a file which cannot be copied later
    # never loses them. Every other file is copied on its own.
    [IO.File]::WriteAllText(
        (Join-Path $destination "failure.txt"),
        (Get-SafeText $Failure) + [Environment]::NewLine,
        [Text.UTF8Encoding]::new($false))
    $uncopied = 0
    $files = [Collections.Generic.List[string]]::new()
    foreach ($name in @("server-log-digest.txt", "fixture.stderr.txt", "fixture.stdout.txt")) {
        $path = Join-Path $ScenarioRoot $name
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $files.Add($path)
        }
    }
    # The fake application directory holds only copied package binaries.
    $applicationDirectory = (Join-Path $ScenarioRoot "fake-application") + [IO.Path]::DirectorySeparatorChar
    try {
        foreach ($item in @(Get-ChildItem -LiteralPath $ScenarioRoot -Recurse -File -Force)) {
            if (-not $item.FullName.StartsWith($applicationDirectory, [StringComparison]::OrdinalIgnoreCase) -and
                -not $files.Contains($item.FullName)) {
                $files.Add($item.FullName)
            }
        }
    }
    catch {
        $uncopied++
    }
    foreach ($file in $files) {
        try {
            $target = Join-Path $destination ([IO.Path]::GetRelativePath($ScenarioRoot, $file))
            [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
            Copy-Item -LiteralPath $file -Destination $target -Force
        }
        catch {
            # For example, a log file another process holds exclusively.
            $uncopied++
        }
    }
    if ($uncopied -gt 0) {
        Write-Host "($uncopied diagnostic files could not be copied)"
    }
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
        Write-ClientCallSummary -ScenarioId $ScenarioId -ScenarioRoot $ScenarioRoot -Timing $Timing
    }
    catch {
        Write-Host "(the client gRPC call summary could not be produced)"
    }
    $writer = $null
    try {
        try {
            $writer = [IO.StreamWriter]::new((Join-Path $ScenarioRoot "server-log-digest.txt"), $false,
                [Text.UTF8Encoding]::new($false))
        }
        catch {
            $writer = $null
        }
        $timingLine = Write-RunnerTiming -ScenarioId $ScenarioId -Timing $Timing
        if ($null -ne $writer) {
            $writer.WriteLine($timingLine)
        }
        Write-ServerLogDigest -ScenarioId $ScenarioId -ScenarioRoot $ScenarioRoot -Timing $Timing -Writer $writer
    }
    catch {
        Write-Host "(the runner timing and server log digest could not be produced)"
    }
    finally {
        if ($null -ne $writer) {
            try {
                $writer.Dispose()
            }
            catch {
            }
        }
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

    $scenarioId = [string]$ScenarioDefinition.id
    $arguments = @($FixtureArguments) + @(
        "--scenario", $scenarioId,
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
            # whether or not these diagnostics succeed, and a failed kill is reported.
            try {
                Invoke-TimeoutDiagnostics -ScenarioId ([string]$ScenarioDefinition.id) -Fixture $process -Timing $Timing `
                    -ScenarioRoot $ScenarioRoot
            }
            catch {
                Write-Host "(the processes alive at the fixture timeout could not be recorded)"
            }
            finally {
                try {
                    $process.Kill($true)
                }
                catch {
                    if (-not $process.HasExited) {
                        Write-Host "(the runner could not terminate the fixture process tree: $($_.Exception.GetType().Name))"
                    }
                }
                Stop-PackageProcesses
            }
            if ([Threading.Tasks.Task]::WaitAll(@($outputTask, $errorTask), 5000)) {
                $split = Split-ClientTrace -ErrorOutput $errorTask.GetAwaiter().GetResult() -ScenarioId $scenarioId
                Save-FixtureOutput `
                    -ScenarioId $scenarioId `
                    -ScenarioRoot $ScenarioRoot `
                    -Output $outputTask.GetAwaiter().GetResult() `
                    -ErrorOutput $split.ErrorOutput `
                    -Trace $split.Trace
            }
            throw "The client fixture timed out after $FixtureTimeoutSeconds s in scenario '$($ScenarioDefinition.id)'."
        }
        $output = $outputTask.GetAwaiter().GetResult().Trim()
        $split = Split-ClientTrace -ErrorOutput $errorTask.GetAwaiter().GetResult().Trim() -ScenarioId $scenarioId
        $errorOutput = $split.ErrorOutput
        Save-FixtureOutput -ScenarioId $scenarioId -ScenarioRoot $ScenarioRoot -Output $output -ErrorOutput $errorOutput `
            -Trace $split.Trace
        if ($process.ExitCode -ne 0) {
            $safeError = Get-SafeText $errorOutput
            throw "The client fixture failed scenario '$($ScenarioDefinition.id)' with exit code $($process.ExitCode): $safeError"
        }
        if ([string]::IsNullOrWhiteSpace($output)) {
            throw "The client fixture returned no report for scenario '$($ScenarioDefinition.id)'."
        }
        try {
            $null = $output | ConvertFrom-Json
        }
        catch {
            throw "The client fixture returned an invalid JSON report for scenario '$($ScenarioDefinition.id)'."
        }

        # The original report text: a check that needs the report's exact JSON
        # shape reads it with System.Text.Json.
        return $outputTask.GetAwaiter().GetResult()
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

$optionalDefinitions = @(
    if ($contract.PSObject.Properties.Name -contains "optional_scenarios") { $contract.optional_scenarios })
$optionalIds = @($optionalDefinitions | ForEach-Object { [string]$_.id })
$requestedOptional = @($OptionalScenario | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
foreach ($requested in $requestedOptional) {
    if ($requested -notin $optionalIds) {
        throw "Unknown optional conformance scenario '$requested'."
    }
}
$selectedScenarios += @($optionalDefinitions | Where-Object { $_.id -in $requestedOptional })

# A soak package, built only by the conformance soak workflow (#450), names one
# scenario in runner/soak-scenario.json. Of the scenarios this run selected, it runs
# only that one, then always ends with an error, even when the scenario passed, so a
# client's test script never records the run as compatibility evidence. A release or
# CI package has no such file, and nothing in the environment can narrow a run.
$soakScenario = $null
$soakPath = Join-Path $PSScriptRoot "soak-scenario.json"
if (Test-Path -LiteralPath $soakPath) {
    $soak = Get-Content -LiteralPath $soakPath -Raw | ConvertFrom-Json
    if ($soak.schema_version -ne 1 -or $soak.scenario -isnot [string] -or
        $soak.scenario -cnotmatch '\A[a-z0-9]+(?:-[a-z0-9]+)*\z') {
        throw "The soak package's scenario file is invalid."
    }
    $selectedScenarios = @($selectedScenarios | Where-Object { [string]::Equals([string]$_.id, $soak.scenario, $ordinal) })
    if ($selectedScenarios.Count -ne 1) {
        throw "The soak package names a scenario that this run did not select."
    }
    $soakScenario = [string]$selectedScenarios[0].id
    Write-Host "Running only conformance scenario '$soakScenario': this soak package repeats one scenario, and its runs are not compatibility evidence."
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
            $unaryCallAuditLevelKey = $unaryCallAuditLevel
        }
        foreach ($entry in (Get-ClientTraceEnvironment -ScenarioId ([string]$scenarioDefinition.id) `
                    -ScenarioRoot $scenarioRoot).GetEnumerator()) {
            $environment[$entry.Key] = $entry.Value
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

            $reportJson = Invoke-Fixture `
                -ScenarioDefinition $scenarioDefinition `
                -Environment $environment `
                -ScenarioRoot $scenarioRoot `
                -Timing $timing
            $report = $reportJson.Trim() | ConvertFrom-Json
            if ($report.schema_version -ne 1 -or
                $report.contract_id -ne $contract.contract_id -or
                $report.scenario -ne $scenarioDefinition.id -or
                $report.success -ne $true) {
                throw "The client fixture report did not satisfy scenario '$($scenarioDefinition.id)'."
            }

            # A scenario that returns a known working directory also checks what the
            # fixture reports it received: the UTF-8 length and SHA-256 of the value,
            # never the value. The runner verifies the report only; the fixture must
            # compute both from the value its client returned. The check reads the
            # original JSON, because ConvertFrom-Json and PowerShell property access
            # unwrap singleton arrays and ignore the case of property names.
            if ($scenarioDefinition.PSObject.Properties.Name -contains "expected_working_directory") {
                $expectedDirectory = $scenarioDefinition.expected_working_directory
                try {
                    $reportDocument = [Text.Json.JsonDocument]::Parse($reportJson)
                }
                catch {
                    throw "The client fixture returned an invalid JSON report for scenario '$($scenarioDefinition.id)'."
                }
                try {
                    # An object at the root and at working_directory; exact property
                    # names; utf8_length a JSON number that is an Int64 equal to the
                    # contract's; sha256 a JSON string equal to it ordinally.
                    $reportRoot = $reportDocument.RootElement
                    $observedDirectory = [Text.Json.JsonElement]::new()
                    $observedLength = [Text.Json.JsonElement]::new()
                    $observedDigest = [Text.Json.JsonElement]::new()
                    $observedLengthValue = [long]0
                    $receiptMatches =
                        $reportRoot.ValueKind -eq [Text.Json.JsonValueKind]::Object -and
                        $reportRoot.TryGetProperty([string]"working_directory", [ref]$observedDirectory) -and
                        $observedDirectory.ValueKind -eq [Text.Json.JsonValueKind]::Object -and
                        $observedDirectory.TryGetProperty([string]"utf8_length", [ref]$observedLength) -and
                        $observedLength.ValueKind -eq [Text.Json.JsonValueKind]::Number -and
                        $observedLength.TryGetInt64([ref]$observedLengthValue) -and
                        $observedLengthValue -eq [long]$expectedDirectory.utf8_length -and
                        $observedDirectory.TryGetProperty([string]"sha256", [ref]$observedDigest) -and
                        $observedDigest.ValueKind -eq [Text.Json.JsonValueKind]::String -and
                        [string]::Equals($observedDigest.GetString(), [string]$expectedDirectory.sha256,
                            [StringComparison]::Ordinal)
                }
                finally {
                    $reportDocument.Dispose()
                }
                if (-not $receiptMatches) {
                    throw "The client fixture did not report receiving the whole working directory in scenario '$($scenarioDefinition.id)': its UTF-8 length and SHA-256 must match the scenario contract."
                }
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

        Write-DigestAudit -ScenarioId ([string]$scenarioDefinition.id) -ScenarioRoot $scenarioRoot
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
    if ($null -ne $soakScenario) {
        throw "This soak run repeated conformance scenario '$soakScenario' only, so it is not compatibility evidence."
    }
}
finally {
    Stop-PackageProcesses
    # Secondary: a temporary file that cannot be removed, for example one another
    # process still holds, never changes the scenario result or the exit code.
    try {
        $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
        if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
            Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
        }
    }
    catch {
        Write-Host "(the runner could not remove all of its temporary files)"
    }
}

Write-Host "All selected Briosa client conformance scenarios passed."
