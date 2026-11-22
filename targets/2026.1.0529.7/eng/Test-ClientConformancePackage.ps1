[CmdletBinding()]
param(
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version = "0.2.0-ci",

    [string]$OutputDirectory = "artifacts\client-conformance-smoke"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$targetRoot = Split-Path -Parent $PSScriptRoot
$packageScript = Join-Path $PSScriptRoot "New-ClientConformancePackage.ps1"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-client-conformance-test-$([Guid]::NewGuid().ToString('N'))"
$firstOutput = [IO.Path]::GetFullPath($OutputDirectory, $targetRoot)
$secondOutput = Join-Path $temporaryRoot "second"
$extractRoot = Join-Path $temporaryRoot "extracted"

function Set-EnvironmentValue {
    param(
        [Parameter(Mandatory)][string]$Name,
        [AllowNull()][object]$Value
    )

    # A missing value removes the variable. Assigning $null, or passing it to
    # SetEnvironmentVariable, can leave an empty value on newer runtimes, and an
    # empty configuration value is invalid for the server that a later step in the
    # same session starts.
    if ($null -eq $Value -or [string]::IsNullOrEmpty([string]$Value)) {
        Remove-Item -LiteralPath "Env:$Name" -ErrorAction SilentlyContinue
    }
    else {
        Set-Item -LiteralPath "Env:$Name" -Value ([string]$Value)
    }
}

function Assert-Condition {
    param(
        [Parameter(Mandatory)][bool]$Condition,
        [Parameter(Mandatory)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$safeTargetRoot = $targetRoot.Replace('\', '/')
$sourceRevision = (& git -c "safe.directory=$safeTargetRoot" -C $targetRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceRevision -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Could not determine a complete source revision."
}

$artifactBase = "briosa-client-conformance-$Version-sa-2026.1.0529.7-win-x64"
$zipName = "$artifactBase.zip"
$firstZip = Join-Path $firstOutput $zipName
$secondZip = Join-Path $secondOutput $zipName

[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    & $packageScript `
        -Version $Version `
        -SourceRevision $sourceRevision `
        -OutputDirectory $firstOutput
    & $packageScript `
        -Version $Version `
        -SourceRevision $sourceRevision `
        -OutputDirectory $secondOutput `
        -NoRestore

    $firstHash = (Get-FileHash -LiteralPath $firstZip -Algorithm SHA256).Hash
    $secondHash = (Get-FileHash -LiteralPath $secondZip -Algorithm SHA256).Hash
    Assert-Condition ($firstHash -eq $secondHash) `
        "Two conformance package builds produced different SHA-256 hashes."
    $externalChecksum = (Get-Content -LiteralPath "$firstZip.sha256" -Raw).Trim()
    Assert-Condition ($externalChecksum -eq "$firstHash  $zipName") `
        "The external conformance package checksum is incorrect."

    Expand-Archive -LiteralPath $firstZip -DestinationPath $extractRoot
    $packageRoot = Join-Path $extractRoot $artifactBase
    Assert-Condition (Test-Path -LiteralPath $packageRoot -PathType Container) `
        "The conformance archive does not contain the expected root."

    $manifestPath = Join-Path $packageRoot "manifest.json"
    $contractPath = Join-Path $packageRoot "contract\scenarios.json"
    $schemaPath = Join-Path $packageRoot "contract\scenarios.schema.json"
    $runnerPath = Join-Path $packageRoot "runner\Invoke-BriosaClientConformance.ps1"
    $vocabularyPath = Join-Path $packageRoot "runner\digest-vocabulary.json"
    foreach ($requiredPath in @(
        $manifestPath,
        $contractPath,
        $schemaPath,
        $runnerPath,
        $vocabularyPath,
        (Join-Path $packageRoot "server\Briosa.Server.exe"),
        (Join-Path $packageRoot "fake-worker\Briosa.SmokeWorker.exe"))) {
        Assert-Condition (Test-Path -LiteralPath $requiredPath -PathType Leaf) `
            "The conformance package is missing '$requiredPath'."
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    Assert-Condition ($manifest.schemaVersion -eq 1) `
        "The conformance manifest schema version is incorrect."
    Assert-Condition ($manifest.artifactKind -eq "briosa_client_conformance") `
        "The conformance manifest artifact kind is incorrect."
    Assert-Condition ($manifest.sourceRevision -eq $sourceRevision.ToLowerInvariant()) `
        "The conformance manifest source revision is incorrect."
    Assert-Condition ($manifest.spatialAnalyzerTarget -eq "2026.1.0529.7") `
        "The conformance manifest target is incorrect."
    Assert-Condition ($manifest.scenarioContract -eq "briosa.first-party-client.v1") `
        "The conformance manifest contract identity is incorrect."
    Assert-Condition (-not $manifest.spatialAnalyzerBundled -and
        -not $manifest.spatialAnalyzerLicenseRequired -and
        -not $manifest.proprietaryDataBundled) `
        "The portable conformance package must not require or bundle vendor assets."

    $contractText = Get-Content -LiteralPath $contractPath -Raw
    Assert-Condition ($contractText | Test-Json -SchemaFile $schemaPath) `
        "The conformance scenario contract does not satisfy its schema."
    $contract = $contractText | ConvertFrom-Json
    $scenarioIds = @($contract.scenarios.id)
    Assert-Condition ($scenarioIds.Count -eq @($scenarioIds | Select-Object -Unique).Count) `
        "Conformance scenario identifiers must be unique."
    foreach ($requiredScenario in @(
        "control-plane-only",
        "default-ready",
        "attach-existing",
        "identity-mismatch",
        "capability-denied",
        "mp-failure",
        "output-failure",
        "deadline",
        "cancellation",
        "watchdog-recovery",
        "sdk-loss-recovery",
        "owned-cleanup")) {
        Assert-Condition ($requiredScenario -in $scenarioIds) `
            "The conformance contract is missing scenario '$requiredScenario'."
    }

    # A scripted hang never returns, so a watchdog budget only has to exceed a
    # cold worker's first execution. 250 ms expired on a legitimate first call
    # on a slow runner (#302); keep generous headroom.
    foreach ($watchdogScenario in @($contract.scenarios | Where-Object { $null -ne $_.watchdog_timeout })) {
        $watchdogBudget = [TimeSpan]::ParseExact(
            [string]$watchdogScenario.watchdog_timeout,
            "hh\:mm\:ss\.fff",
            [Globalization.CultureInfo]::InvariantCulture)
        Assert-Condition ($watchdogBudget -ge [TimeSpan]::FromSeconds(2)) `
            "Scenario '$($watchdogScenario.id)' has a watchdog budget too short for a cold worker's first execution."
    }

    # Contract-gated scenarios ship exactly when the target contract reaches
    # their major, without the gate field.
    $contractMajor = [int](Get-Content -LiteralPath (Join-Path $targetRoot "compatibility.json") -Raw |
        ConvertFrom-Json).major
    $sourceContractText = Get-Content -LiteralPath (Join-Path $targetRoot "conformance\client\v1\scenarios.json") -Raw
    Assert-Condition ($sourceContractText | Test-Json -SchemaFile (Join-Path $targetRoot "conformance\client\v1\scenarios.schema.json")) `
        "The source conformance scenario contract does not satisfy its schema."
    $sourceContract = $sourceContractText | ConvertFrom-Json
    Assert-Condition (@($contract.scenarios | Where-Object {
                $_.PSObject.Properties.Name -contains "minimum_contract_major" }).Count -eq 0) `
        "The packaged conformance contract must not contain contract gates."
    foreach ($sourceScenario in @($sourceContract.scenarios)) {
        $gated = $sourceScenario.PSObject.Properties.Name -contains "minimum_contract_major"
        $active = -not $gated -or [int]$sourceScenario.minimum_contract_major -le $contractMajor
        Assert-Condition ($active -eq ($sourceScenario.id -in $scenarioIds)) `
            "Scenario '$($sourceScenario.id)' is packaged contrary to its contract gate."
    }
    # The scenarios that contract major 3 activates are gated on it and ship with it.
    foreach ($majorThreeScenario in @("sdk-call-faulted", "mp-partial-success", "lifecycle-cancellation")) {
        Assert-Condition (@($sourceContract.scenarios | Where-Object {
                    $_.id -eq $majorThreeScenario -and [int]$_.minimum_contract_major -eq 3 }).Count -eq 1) `
            "The $majorThreeScenario scenario must be gated on contract major 3."
        if ($contractMajor -ge 3) {
            Assert-Condition ($majorThreeScenario -in $scenarioIds) `
                "The conformance contract is missing scenario '$majorThreeScenario'."
        }
    }

    $tokens = $null
    $parseErrors = $null
    [Management.Automation.Language.Parser]::ParseFile(
        $runnerPath,
        [ref]$tokens,
        [ref]$parseErrors) | Out-Null
    Assert-Condition ($parseErrors.Count -eq 0) `
        "The packaged conformance runner has PowerShell syntax errors."
    # The digest's closed lists are sorted, unique, and in the shapes the audit names.
    $vocabulary = Get-Content -LiteralPath $vocabularyPath -Raw | ConvertFrom-Json
    Assert-Condition ($vocabulary.schema_version -eq 1) `
        "The digest vocabulary schema version is incorrect."
    foreach ($list in @(
            @{ Name = "diagnostic_codes"; Pattern = '\A[a-z][a-z0-9]*(?:-[a-z][a-z0-9]*){1,11}\z' },
            @{ Name = "operation_ids"; Pattern = '\A[a-z]+(?:_[a-z]+)*_operations\.[a-z][a-z0-9]*(?:_[a-z0-9]+)*\z' })) {
        $values = [string[]]@($vocabulary.($list.Name))
        $sorted = [string[]]@($values)
        [Array]::Sort($sorted, [StringComparer]::Ordinal)
        Assert-Condition ($values.Count -gt 0 -and
            [Linq.Enumerable]::SequenceEqual($values, $sorted) -and
            [Collections.Generic.HashSet[string]]::new($values, [StringComparer]::Ordinal).Count -eq $values.Count -and
            @($values | Where-Object { $_ -cnotmatch $list.Pattern }).Count -eq 0) `
            "The digest vocabulary's $($list.Name) list must be sorted, unique and well formed."
    }

    $checksumPath = Join-Path $packageRoot "files.sha256"
    foreach ($line in Get-Content -LiteralPath $checksumPath) {
        $match = [regex]::Match($line, '^([0-9A-Fa-f]{64})  (.+)$')
        Assert-Condition $match.Success "Malformed entry in files.sha256."
        $filePath = Join-Path $packageRoot $match.Groups[2].Value
        Assert-Condition (Test-Path -LiteralPath $filePath -PathType Leaf) `
            "A file listed in files.sha256 is missing."
        $actualHash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
        Assert-Condition ($actualHash -eq $match.Groups[1].Value) `
            "An internal conformance package checksum does not match."
    }

    $fixturePath = Join-Path $temporaryRoot "fixture.ps1"
    $fixtureSource = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Scenario,
    [Parameter(Mandatory)][string]$Contract
)
$definition = Get-Content -LiteralPath $Contract -Raw | ConvertFrom-Json
[ordered]@{
    schema_version = 1
    contract_id = $definition.contract_id
    scenario = $Scenario
    success = $true
} | ConvertTo-Json -Compress
'@
    [IO.File]::WriteAllText(
        $fixturePath,
        $fixtureSource,
        [Text.UTF8Encoding]::new($false))
    # The digest audit belongs to the retained-client gate; these synthetic logs
    # must never reach a caller's audit file.
    $previousDigestAudit = $env:BRIOSA_CONFORMANCE_DIGEST_AUDIT
    Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIGEST_AUDIT" $null
    # An unusable optional diagnostics directory turns diagnostics off; it never
    # turns a passing run into a failure (#371).
    $runnerArguments = @{
        FixtureCommand = (Get-Process -Id $PID).Path
        FixtureArguments = @("-NoProfile", "-File", $fixturePath)
        FixtureTimeoutSeconds = 30
        DiagnosticsDirectory = "unusable`0diagnostics-directory"
    }
    $passedOutput = [Collections.Generic.List[string]]::new()
    & $runnerPath @runnerArguments *>&1 | ForEach-Object {
        $passedOutput.Add([string]$_)
        Write-Host $_
    }
    Assert-Condition ($passedOutput -contains
        "Conformance diagnostics are disabled: the diagnostics directory is not a usable path.") `
        "The conformance runner did not report an unusable diagnostics directory."
    # Each passed scenario reports its phase durations; this fixture starts no
    # server, so only the fixture's own time is known (#371).
    $passedLines = @($passedOutput | Where-Object { $_ -like "Passed client conformance scenario: *" })
    Assert-Condition ($passedLines.Count -eq $scenarioIds.Count -and
        @($passedLines | Where-Object { $_ -notmatch '^Passed client conformance scenario: [a-z0-9-]+ \(fixture [0-9]+\.[0-9] s\)$' }).Count -eq 0) `
        "The conformance runner did not report each passed scenario with its fixture time."

    # Every rejected scenario prints the fixture's complete stderr and, when a
    # diagnostics directory is configured, keeps it with the scenario's server
    # logs (#302). Rejection by exit code and by report validation must behave
    # alike. The gate supplies the directory through the environment.
    $failingFixtureSource = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Scenario,
    [Parameter(Mandatory)][string]$Contract
)
$logDirectory = $env:Briosa__Logging__File__Directory
if ([string]::IsNullOrWhiteSpace($logDirectory) -or
    -not $logDirectory.EndsWith("\$Scenario\server-logs", [StringComparison]::OrdinalIgnoreCase)) {
    [Console]::Error.WriteLine("server-log-directory-not-scenario-scoped")
    exit 3
}
if ($env:Briosa__Worker__StartupTimeout -ne "__STARTUP__") {
    [Console]::Error.WriteLine("worker-startup-timeout-not-applied")
    exit 4
}
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
[IO.File]::WriteAllText((Join-Path $logDirectory "server-log-marker.jsonl"), "server-log-marker")
# A server log in the real record shape: three early warnings; warnings that carry
# sensitive-looking text in printed properties, in an event name, a template and
# a level; more lower-severity records than the digest keeps; a property outside
# the digest allow-list; listed and unlisted diagnostic codes and operation IDs;
# values that only a linguistic comparison would accept; a warning written with
# whitespace after "Level":; the server's timestamp-free placeholder for an
# oversized record; warnings written out of time order; optionally a flood of
# warnings; and truncated last lines of a killed writer, one of them after a
# complete timestamp.
$started = [DateTimeOffset]::UtcNow
$records = [Collections.Generic.List[string]]::new()
function Add-ServerRecord([int]$Offset, [string]$Level, [string]$Name, [hashtable]$Properties,
    [int]$Id = 1, [string]$Template = $Name) {
    $Properties["ServerInstanceId"] = "0123456789abcdef0123456789abcdef"
    if ($Id -ge 0) { $Properties["EventId"] = @{ Id = $Id; Name = $Name } }
    # The server writes the timestamp first.
    $records.Add(([ordered]@{
        Timestamp = $started.AddMilliseconds($Offset).ToString("o")
        Level = $Level
        MessageTemplate = $Template
        Properties = $Properties
    } | ConvertTo-Json -Compress -Depth 5))
}
foreach ($index in 1..3) {
    Add-ServerRecord $index "Warning" "WorkerStartPhase" @{
        Phase = "startup-bound-expired"; ElapsedMilliseconds = 10000; Generation = $index } -Id 1203
}
Add-ServerRecord 4 "Warning" "WorkerTransition" @{
    WorkerState = "C:/Users/someone/secret"; DiagnosticCode = "../relative/secret/path"
    ConnectionState = "build-host.corp.example"; Generation = 4 } -Id 1201
Add-ServerRecord 5 "Warning" "LifecycleRejected" @{
    Rpc = "someone@example.com"; GrpcStatus = "DOMAIN/someone"; ElapsedMilliseconds = "12345"
    DiagnosticCode = "ghp_0123456789abcdefABCDEF" } -Id 1302
Add-ServerRecord 6 "Warning" "secret-name-text" @{ Generation = 6 } -Id 1201 -Template "Template with secret-template-text"
Add-ServerRecord 7 "Warning" "unused" @{ Generation = 7 } -Id -1 -Template "secret-template-only"
Add-ServerRecord 8 "Warning" "ExecutionDispatched" @{
    OperationId = "host.example.com"; Generation = 8.5; MpResultCode = 2 } -Id 1300
$records.Add('{"Timestamp":"' + $started.AddMilliseconds(9).ToString("o") +
    '","Level": "Warning","MessageTemplate":"WorkerStartPhase","Properties":{"Phase":"start-failed","Generation":77,' +
    '"ServerInstanceId":"0123456789abcdef0123456789abcdef","EventId":{"Id":1203,"Name":"WorkerStartPhase"}}}')
foreach ($index in 1..250) {
    Add-ServerRecord (10 + $index) "Information" "ReadinessNotReady" @{} -Id 1500
}
Add-ServerRecord 300 "Information" "LifecycleRpcStarted" @{
    Rpc = "LaunchSpatialAnalyzer"; Path = "secret-geometry-path" } -Id 1303
Add-ServerRecord 301 "secret-level-text" "LifecycleRpcStarted" @{ Rpc = "StopSpatialAnalyzerSdk" } -Id 1303
# A soft hyphen, a zero-width joiner and a trailing newline: equal to listed values
# only under a linguistic comparison.
Add-ServerRecord 302 "Information" "WorkerStartPhase" @{ Phase = "process-created$([char]0x00AD)" } -Id 1203
Add-ServerRecord 303 "Information$([char]0x200D)" "WorkerTransition" @{ DiagnosticCode = "worker-starting`n" } -Id 1201
Add-ServerRecord 304 "Information" "WorkerTransition" @{ DiagnosticCode = "worker-starting" } -Id 1201
Add-ServerRecord 305 "Information" "ExecutionDispatched" @{ OperationId = "file_operations.get_working_directory" } -Id 1300
Add-ServerRecord 306 "Information" "ExecutionDispatched" @{ OperationId = "unlisted_operations.example" } -Id 1300
# Shaped like a diagnostic code, but an internal host name: it must reach no output.
Add-ServerRecord 307 "Information" "WorkerTransition" @{ DiagnosticCode = "secret-build-host" } -Id 1201
$records.Add('{"Level":"Warning","MessageTemplate":"LogRecordTooLarge","Properties":{"SchemaVersion":1}}')
# Written in the order a concurrent producer can reach the sink: +0.374, +0.174, +0.274.
foreach ($offset in @(374, 174, 274)) {
    Add-ServerRecord $offset "Warning" "WorkerStartPhase" @{ Phase = "start-failed"; Generation = 90 + [int][Math]::Floor($offset / 100) } -Id 1203
}
$flood = __FLOOD__
for ($index = 1; $index -le $flood; $index++) {
    Add-ServerRecord (400 + $index) "Warning" "WorkerStartPhase" @{
        Phase = "start-failed"; Generation = 100000 + $index } -Id 1203
}
$records.Add('{"Timestamp":"' + $started.AddMilliseconds(2000).ToString("o") + '","Level":"Information","MessageTemplate":"Readin')
$records.Add('{"Timestamp":"2026-11-22T12:00:00')
[IO.File]::WriteAllLines(
    (Join-Path $logDirectory "briosa-0123456789abcdef0123456789abcdef-20261122.jsonl"), $records)
foreach ($index in 1..200) {
    [Console]::Error.WriteLine("__MODE__-trace-line-$index")
}
switch ("__MODE__") {
    "unsuccessful-report" {
        $definition = Get-Content -LiteralPath $Contract -Raw | ConvertFrom-Json
        [Console]::Out.WriteLine(([ordered]@{
            schema_version = 1
            contract_id = $definition.contract_id
            scenario = $Scenario
            success = $false
        } | ConvertTo-Json -Compress))
        exit 0
    }
    "empty-report" { exit 0 }
    "invalid-report" {
        [Console]::Out.WriteLine("{not-json")
        exit 0
    }
    "timeout" {
        Start-Sleep -Seconds 120
        exit 0
    }
    default { exit 1 }
}
'@

    # Dump expectations depend on the fixture runtime, which is this PowerShell.
    $fixtureRuntime = @((Get-Process -Id $PID).Modules | Where-Object ModuleName -eq "coreclr.dll")
    $fixtureShipsCreateDump = $fixtureRuntime.Count -eq 1 -and
        (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $fixtureRuntime[0].FileName) "createdump.exe"))
    $sensitiveMarkers = @("someone", "secret", "corp.example", "example.com", "ghp_", "DOMAIN", "relative")
    $rejectionCases = @(
        # A caller's own worker startup bound reaches the server unchanged.
        # This case also enables the digest audit.
        [pscustomobject]@{ Name = "exit-nonzero-saved"; Mode = "exit-nonzero"; Diagnostics = "directory"
            StartupTimeout = "00:00:12"; Audit = $true; Failure = "*failed scenario 'default-ready' with exit code 1*" },
        [pscustomobject]@{ Name = "unsuccessful-report-saved"; Mode = "unsuccessful-report"; Diagnostics = "directory"
            Failure = "*report did not satisfy scenario 'default-ready'*" },
        # A flood of warnings exercises the warning and error bound.
        [pscustomobject]@{ Name = "empty-report-saved"; Mode = "empty-report"; Diagnostics = "directory"; Flood = 1100
            Failure = "*returned no report for scenario 'default-ready'*" },
        [pscustomobject]@{ Name = "invalid-report-saved"; Mode = "invalid-report"; Diagnostics = "directory"
            Failure = "*returned an invalid JSON report for scenario 'default-ready'*" },
        # A dump only from a fixture whose diagnostics channel is known to be enabled.
        [pscustomobject]@{ Name = "timeout-dump-enabled"; Mode = "timeout"; Diagnostics = "directory"; Dump = "1"
            Channel = "enabled"; Failure = "*timed out after 5 s in scenario 'default-ready'*" },
        # A disabled diagnostics channel is reported and skipped.
        [pscustomobject]@{ Name = "timeout-dump-channel-disabled"; Mode = "timeout"; Diagnostics = "directory"; Dump = "1"
            Channel = "ipc-disabled"; Failure = "*timed out after 5 s in scenario 'default-ready'*" },
        # Only the documented opt-in value enables dumps.
        [pscustomobject]@{ Name = "timeout-dump-undocumented-value"; Mode = "timeout"; Diagnostics = "directory"; Dump = "true"
            Channel = "enabled"; Failure = "*timed out after 5 s in scenario 'default-ready'*" },
        # Without a diagnostics directory the runner deletes its temporary
        # files, so the printed stderr is the only remaining evidence.
        [pscustomobject]@{ Name = "unsuccessful-report-printed"; Mode = "unsuccessful-report"; Diagnostics = "none"
            Failure = "*report did not satisfy scenario 'default-ready'*" },
        # A destination that cannot be created never hides the scenario's own failure.
        [pscustomobject]@{ Name = "exit-nonzero-unwritable"; Mode = "exit-nonzero"; Diagnostics = "file"
            Failure = "*failed scenario 'default-ready' with exit code 1*" })
    $previousEnvironment = @{}
    foreach ($name in @("BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY", "Briosa__Worker__StartupTimeout",
            "BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT", "DOTNET_EnableDiagnostics", "DOTNET_EnableDiagnostics_IPC")) {
        $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
    }
    try {
        foreach ($case in $rejectionCases) {
            $caseName = $case.Name
            $property = { param($name) $(if ($case.PSObject.Properties.Name -contains $name) { $case.$name }) }
            $callerStartupTimeout = & $property "StartupTimeout"
            $dump = & $property "Dump"
            $flood = & $property "Flood"
            $channel = & $property "Channel"
            $auditPath = $(if (& $property "Audit") { Join-Path $temporaryRoot "digest-audit-$caseName.txt" })
            Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIGEST_AUDIT" $auditPath
            $failingFixturePath = Join-Path $temporaryRoot "failing-fixture-$caseName.ps1"
            # The runner gives the fake worker start 30 s unless the caller set a bound.
            [IO.File]::WriteAllText(
                $failingFixturePath,
                $failingFixtureSource.Replace("__MODE__", $case.Mode).Replace(
                    "__STARTUP__", $(if ($null -ne $callerStartupTimeout) { $callerStartupTimeout } else { "00:00:30" })).Replace(
                    "__FLOOD__", $(if ($null -ne $flood) { [string]$flood } else { "0" })),
                [Text.UTF8Encoding]::new($false))
            $diagnosticsRoot = Join-Path $temporaryRoot "conformance-diagnostics-$caseName"
            if ($case.Diagnostics -eq "file") {
                [IO.File]::WriteAllText($diagnosticsRoot, "not a directory")
            }
            Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY" $(if ($case.Diagnostics -ne "none") { $diagnosticsRoot })
            Set-EnvironmentValue "Briosa__Worker__StartupTimeout" $callerStartupTimeout
            Set-EnvironmentValue "BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT" $dump
            # The fixture inherits these; set them explicitly so the ambient
            # environment cannot decide the dump expectation.
            Set-EnvironmentValue "DOTNET_EnableDiagnostics" $null
            Set-EnvironmentValue "DOTNET_EnableDiagnostics_IPC" $(if ($channel -eq "ipc-disabled") { "0" })
            $failure = $null
            $runnerOutput = [Collections.Generic.List[string]]::new()
            try {
                $failingRunnerArguments = @{
                    FixtureCommand = (Get-Process -Id $PID).Path
                    FixtureArguments = @("-NoProfile", "-File", $failingFixturePath)
                    Scenario = @("default-ready")
                    FixtureTimeoutSeconds = $(if ($case.Mode -eq "timeout") { 5 } else { 30 })
                }
                & $runnerPath @failingRunnerArguments *>&1 |
                    ForEach-Object { $runnerOutput.Add([string]$_) }
            }
            catch {
                $failure = $_
            }
            Assert-Condition ($null -ne $failure -and "$failure" -like $case.Failure) `
                "The conformance runner did not reject the $caseName fixture as expected: $failure"
            $printedLines = @($runnerOutput | Where-Object { $_ -like "$($case.Mode)-trace-line-*" })
            Assert-Condition ($printedLines.Count -eq 200 -and
                $printedLines[0] -eq "$($case.Mode)-trace-line-1" -and
                $printedLines[-1] -eq "$($case.Mode)-trace-line-200") `
                "The conformance runner did not print the complete fixture stderr for $caseName."

            # The runner's timing marks and the server log digest follow the stderr
            # (#371). The digest keeps the latest 200 lower-severity records by
            # timestamp and every warning and error, and prints only values from
            # reviewed closed lists, compared ordinally.
            $timingLine = @($runnerOutput | Where-Object { $_ -like "Runner timing for default-ready (UTC): *" })
            Assert-Condition ($timingLine.Count -eq 1 -and
                $timingLine[0] -like "*scenario start *, fixture start * (+*), fixture end * (+*)*" -and
                ($case.Mode -ne "timeout" -or $timingLine[0] -like "*, fixture timed out.")) `
                "The conformance runner did not print its timing marks for $caseName."
            $digestStart = [Array]::IndexOf([string[]]$runnerOutput,
                "--- server log digest for default-ready (times from fixture start) ---")
            $digestEnd = [Array]::IndexOf([string[]]$runnerOutput, "--- end server log digest for default-ready ---")
            Assert-Condition ($digestStart -ge 0 -and $digestEnd -gt $digestStart -and
                $digestStart -gt [Array]::IndexOf([string[]]$runnerOutput, "$($case.Mode)-trace-line-200")) `
                "The conformance runner did not print the server log digest after the fixture stderr for $caseName."
            $digest = @($runnerOutput[($digestStart + 1)..($digestEnd - 1)])
            $digestEvents = @($digest | Where-Object { $_ -match '^[+-][0-9]+\.[0-9]{3} s ' -and $_ -notmatch ' s Runner ' })
            $runnerMarks = @($digest | Where-Object { $_ -match ' s Runner ' })
            $offsets = @($digest | Where-Object { $_ -match '^[+-][0-9]+\.[0-9]{3} s ' } | ForEach-Object {
                    [double]::Parse($_.Substring(0, $_.IndexOf(' s ')), [Globalization.CultureInfo]::InvariantCulture)
                })
            Assert-Condition ($runnerMarks.Count -eq 2 -and
                $runnerMarks[0] -eq "+0.000 s Runner FixtureStart" -and
                $runnerMarks[1] -like "+* s Runner $(if ($case.Mode -eq 'timeout') { 'FixtureTimedOut' } else { 'FixtureEnd' })" -and
                @(for ($index = 1; $index -lt $offsets.Count; $index++) {
                        if ($offsets[$index] -lt $offsets[$index - 1]) { $index }
                    }).Count -eq 0) `
                "The conformance runner did not place its fixture marks in the server log digest's timeline for $caseName."
            $warnings = @($digestEvents | Where-Object { $_ -match '^\S+ s Warning ' })
            $ordinary = @($digestEvents | Where-Object { $_ -notmatch '^\S+ s Warning ' })
            Assert-Condition ($ordinary.Count -eq 200 -and
                @($warnings | Where-Object {
                    $_ -like "* Warning WorkerStartPhase Phase=startup-bound-expired ElapsedMilliseconds=10000 Generation=*" }).Count -eq 3 -and
                @($ordinary | Where-Object { $_ -like "+* s Information ReadinessNotReady" }).Count -eq 192 -and
                @($ordinary | Where-Object { $_ -like "+* s Information LifecycleRpcStarted Rpc=LaunchSpatialAnalyzer" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s UnknownLevel LifecycleRpcStarted Rpc=StopSpatialAnalyzerSdk" }).Count -eq 1 -and
                $ordinary[-1] -like "+* s Information WorkerTransition DiagnosticCode=<withheld>" -and
                # The oversized-record placeholder takes the time of the record before it.
                $digestEvents[[Array]::IndexOf([string[]]$digestEvents, $ordinary[-1]) + 1] -like "+* s Warning LogRecordTooLarge" -and
                $digest -contains "(58 earlier lower-severity records omitted; every warning and error is kept)" -and
                # The two truncated lines and the marker file's line, none admitted.
                $digest -contains "(3 malformed server log records skipped)") `
                "The conformance runner printed an unexpected server log digest for $caseName."
            # Listed values print as the list's own entry; a value equal to a listed
            # one only linguistically, or one not on the list, is withheld.
            Assert-Condition (
                @($ordinary | Where-Object { $_ -like "+* s Information WorkerStartPhase Phase=<withheld>" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s UnknownLevel WorkerTransition DiagnosticCode=<withheld>" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s Information WorkerTransition DiagnosticCode=worker-starting" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s Information ExecutionDispatched OperationId=file_operations.get_working_directory" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s Information ExecutionDispatched OperationId=<withheld>" }).Count -eq 1 -and
                @($runnerOutput | Where-Object { $_ -clike "*secret-build-host*" -or $_ -clike "*unlisted_operations*" }).Count -eq 0) `
                "The conformance runner did not print only listed values for $caseName."
            # The level is read from the parsed record, and warnings print in time order
            # even when the log holds them out of order.
            $inverted = @(foreach ($generation in @(91, 92, 93)) {
                    $found = -1
                    for ($index = 0; $index -lt $digestEvents.Count; $index++) {
                        if ($digestEvents[$index] -like "* Warning WorkerStartPhase Phase=start-failed Generation=$generation") {
                            $found = $index
                            break
                        }
                    }
                    $found
                })
            Assert-Condition (@($warnings | Where-Object { $_ -like "* Warning WorkerStartPhase Phase=start-failed Generation=77" }).Count -eq 1 -and
                $inverted[0] -ge 0 -and $inverted[0] -lt $inverted[1] -and $inverted[1] -lt $inverted[2]) `
                "The conformance runner did not keep or order the warnings by time for $caseName."
            # Sensitive-looking text inside printed properties, an event name, a
            # template or a level is withheld, never printed.
            Assert-Condition (
                @($warnings | Where-Object { $_ -like "* Warning WorkerTransition WorkerState=<withheld> Generation=4 ConnectionState=<withheld> DiagnosticCode=<withheld>" }).Count -eq 1 -and
                @($warnings | Where-Object { $_ -like "* Warning LifecycleRejected Rpc=<withheld> GrpcStatus=<withheld> ElapsedMilliseconds=<withheld> DiagnosticCode=<withheld>" }).Count -eq 1 -and
                @($warnings | Where-Object { $_ -like "* Warning UnknownEvent EventId=1201 Generation=6" }).Count -eq 1 -and
                @($warnings | Where-Object { $_ -like "* Warning UnknownEvent Generation=7" }).Count -eq 1 -and
                @($warnings | Where-Object { $_ -like "* Warning ExecutionDispatched Generation=<withheld> OperationId=<withheld> MpResultCode=2" }).Count -eq 1) `
                "The conformance runner did not withhold unvalidated server log values for $caseName."
            foreach ($marker in $sensitiveMarkers) {
                Assert-Condition (@($runnerOutput | Where-Object { $_ -clike "*$marker*" }).Count -eq 0) `
                    "The conformance runner printed sensitive-looking server log text ($marker) for $caseName."
            }
            if ($null -ne $flood) {
                # A flood of warnings is kept in full, in order.
                $floodWarnings = @($warnings | Where-Object { $_ -like "* Warning WorkerStartPhase Phase=start-failed Generation=1*" })
                $floodGenerations = @($floodWarnings | ForEach-Object { [int]($_ -replace '^.*Generation=', '') })
                Assert-Condition ($warnings.Count -eq $flood + 13 -and $floodWarnings.Count -eq $flood -and
                    $floodGenerations[0] -eq 100001 -and $floodGenerations[-1] -eq 100000 + $flood -and
                    @(for ($index = 1; $index -lt $floodGenerations.Count; $index++) {
                            if ($floodGenerations[$index] -ne $floodGenerations[$index - 1] + 1) { $index }
                        }).Count -eq 0 -and
                    @($digest | Where-Object { $_ -like "*warnings and errors omitted*" }).Count -eq 0) `
                    "The conformance runner did not keep every warning for $caseName."
            }
            else {
                Assert-Condition ($warnings.Count -eq 13) `
                    "The conformance runner did not keep every warning for $caseName."
            }
            if ($case.Mode -eq "timeout") {
                Assert-Condition ($runnerOutput -contains
                    "--- processes alive at the fixture timeout in default-ready (start times from fixture start) ---" -and
                    @($runnerOutput | Where-Object { $_ -match '^fixture pid [0-9]+: started [+-][0-9]+\.[0-9]{3} s, CPU [0-9]+\.[0-9]{2} s$' }).Count -eq 1) `
                    "The conformance runner did not record the processes alive at the fixture timeout."
            }
            if ($null -ne $auditPath) {
                # The audit names the scenario, the property and how many unlisted values
                # it held, never a value: the planted host name reaches no output.
                $auditText = Get-Content -LiteralPath $auditPath -Raw
                Assert-Condition ([string]::Equals($auditText,
                        "default-ready DiagnosticCode 4$([Environment]::NewLine)default-ready OperationId 2$([Environment]::NewLine)",
                        [StringComparison]::Ordinal) -and
                    @(foreach ($marker in @($sensitiveMarkers) + @("secret-build-host", "unlisted_operations", "worker-starting")) {
                            if ($auditText.Contains($marker)) { $marker } }).Count -eq 0) `
                    "The conformance runner did not audit only counts for $($caseName): $auditText"
            }
            $dumpLines = @($runnerOutput | Where-Object { $_ -like "*heap dump*" })
            if ($case.Diagnostics -eq "none") {
                Assert-Condition (-not (Test-Path -LiteralPath $diagnosticsRoot)) `
                    "The conformance runner wrote diagnostics without a configured directory."
                continue
            }
            if ($case.Diagnostics -eq "file") {
                Assert-Condition ($runnerOutput -contains "(the scenario diagnostics could not be saved to the diagnostics directory)" -and
                    (Get-Content -LiteralPath $diagnosticsRoot -Raw) -eq "not a directory") `
                    "The conformance runner did not report an unwritable diagnostics directory for $caseName."
                continue
            }
            $scenarioDiagnostics = Join-Path $diagnosticsRoot "default-ready"
            $savedError = @(Get-Content -LiteralPath (Join-Path $scenarioDiagnostics "fixture.stderr.txt"))
            Assert-Condition ($savedError.Count -eq 200 -and
                $savedError[0] -eq "$($case.Mode)-trace-line-1" -and
                $savedError[-1] -eq "$($case.Mode)-trace-line-200") `
                "The conformance runner did not keep the complete fixture stderr for $caseName."
            Assert-Condition ((Get-Content -LiteralPath (Join-Path $scenarioDiagnostics "server-logs\server-log-marker.jsonl") -Raw) -eq
                "server-log-marker") `
                "The conformance runner did not keep the scenario's server logs for $caseName."
            Assert-Condition ((Test-Path -LiteralPath (Join-Path $scenarioDiagnostics "failure.txt") -PathType Leaf) -and
                (Test-Path -LiteralPath (Join-Path $scenarioDiagnostics "fixture.stdout.txt") -PathType Leaf) -and
                -not (Test-Path -LiteralPath (Join-Path $scenarioDiagnostics "fake-application"))) `
                "The conformance runner diagnostics have an unexpected shape for $caseName."
            $savedDigest = @(Get-Content -LiteralPath (Join-Path $scenarioDiagnostics "server-log-digest.txt"))
            Assert-Condition ($savedDigest.Count -eq $digestEnd - $digestStart + 2 -and
                $savedDigest[0] -eq $timingLine[0] -and
                $savedDigest[1] -eq $runnerOutput[$digestStart] -and
                @(foreach ($marker in $sensitiveMarkers) { $savedDigest | Where-Object { $_ -clike "*$marker*" } }).Count -eq 0) `
                "The conformance runner did not keep the server log digest for $caseName."
            $dumpFiles = @(Get-ChildItem -LiteralPath (Join-Path $scenarioDiagnostics "dumps") -Filter "fixture-*.dmp" -File -ErrorAction SilentlyContinue)
            if ($dump -ne "1") {
                Assert-Condition ($dumpLines.Count -eq 0 -and $dumpFiles.Count -eq 0) `
                    "The conformance runner wrote heap dumps without the documented opt-in for $caseName."
                continue
            }
            # The opt-in heap dump of the fixture, taken before the kill. This
            # fixture is PowerShell, which hosts the .NET runtime.
            $fixtureDumpLines = @($dumpLines | Where-Object { $_ -like "*heap dump of the fixture*" })
            $dumpExpected = $channel -eq "enabled" -and $fixtureShipsCreateDump
            Assert-Condition ($fixtureDumpLines.Count -eq 1 -and $(if ($dumpExpected) {
                        $fixtureDumpLines[0] -like "Wrote a heap dump of the fixture (pid *) to dumps/fixture-*.dmp (* MiB)." -and
                        $dumpFiles.Count -eq 1 -and $dumpFiles[0].Length -gt 0
                    } elseif ($channel -eq "ipc-disabled") {
                        $fixtureDumpLines[0] -like "No heap dump of the fixture (pid *): it offers no .NET diagnostics channel." -and
                        $dumpFiles.Count -eq 0
                    } else {
                        $fixtureDumpLines[0] -like "No heap dump of the fixture (pid *)*" -and $dumpFiles.Count -eq 0
                    })) `
                "The conformance runner did not handle the opt-in fixture heap dump as expected for $($caseName): $($fixtureDumpLines -join ' | ')"
            $dumpFiles | Remove-Item -Force
        }
    }
    finally {
        foreach ($entry in $previousEnvironment.GetEnumerator()) {
            Set-EnvironmentValue $entry.Key $entry.Value
        }
        Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIGEST_AUDIT" $null
    }

    # A missing digest vocabulary, or one with a list missing, withholds every
    # DiagnosticCode and OperationId but keeps the rest of the digest and the runner
    # marks (#371).
    $vocabularyFixturePath = Join-Path $temporaryRoot "failing-fixture-vocabulary.ps1"
    [IO.File]::WriteAllText(
        $vocabularyFixturePath,
        $failingFixtureSource.Replace("__MODE__", "exit-nonzero").Replace("__STARTUP__", "00:00:30").Replace("__FLOOD__", "0"),
        [Text.UTF8Encoding]::new($false))
    $vocabularyBackup = Join-Path $temporaryRoot "digest-vocabulary.backup.json"
    Copy-Item -LiteralPath $vocabularyPath -Destination $vocabularyBackup
    $previousVocabularyEnvironment = @{}
    foreach ($name in @("BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY", "Briosa__Worker__StartupTimeout")) {
        $previousVocabularyEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
        Set-EnvironmentValue $name $null
    }
    try {
        foreach ($variant in @("missing", "partial")) {
            if ($variant -eq "missing") {
                Remove-Item -LiteralPath $vocabularyPath -Force
            }
            else {
                $complete = Get-Content -LiteralPath $vocabularyBackup -Raw | ConvertFrom-Json
                [IO.File]::WriteAllText($vocabularyPath, ([ordered]@{
                            schema_version = 1
                            diagnostic_codes = @($complete.diagnostic_codes)
                        } | ConvertTo-Json -Depth 3), [Text.UTF8Encoding]::new($false))
            }
            $vocabularyOutput = [Collections.Generic.List[string]]::new()
            $vocabularyFailure = $null
            try {
                & $runnerPath -FixtureCommand (Get-Process -Id $PID).Path `
                    -FixtureArguments @("-NoProfile", "-File", $vocabularyFixturePath) `
                    -Scenario @("default-ready") -FixtureTimeoutSeconds 30 *>&1 |
                    ForEach-Object { $vocabularyOutput.Add([string]$_) }
            }
            catch {
                $vocabularyFailure = $_
            }
            Assert-Condition ($null -ne $vocabularyFailure -and
                "$vocabularyFailure" -like "*failed scenario 'default-ready' with exit code 1*" -and
                $vocabularyOutput -contains "(the digest vocabulary is missing or invalid; diagnostic codes and operation IDs print as <withheld>)" -and
                $vocabularyOutput -notcontains "(the runner timing and server log digest could not be produced)" -and
                $vocabularyOutput -contains "+0.000 s Runner FixtureStart" -and
                @($vocabularyOutput | Where-Object { $_ -like "+* s Runner FixtureEnd" }).Count -eq 1 -and
                @($vocabularyOutput | Where-Object {
                        $_ -like "* Warning WorkerStartPhase Phase=startup-bound-expired ElapsedMilliseconds=10000 Generation=*" }).Count -eq 3 -and
                @($vocabularyOutput | Where-Object { $_ -like "+* s Information WorkerTransition DiagnosticCode=<withheld>" }).Count -eq 2 -and
                @($vocabularyOutput | Where-Object { $_ -like "+* s Information ExecutionDispatched OperationId=<withheld>" }).Count -eq 2 -and
                @($vocabularyOutput | Where-Object {
                        $_ -clike "*DiagnosticCode=worker-starting*" -or $_ -clike "*OperationId=file_operations*" }).Count -eq 0) `
                "The conformance runner did not keep the digest with a $variant digest vocabulary."
        }
    }
    finally {
        Copy-Item -LiteralPath $vocabularyBackup -Destination $vocabularyPath -Force
        foreach ($entry in $previousVocabularyEnvironment.GetEnumerator()) {
            Set-EnvironmentValue $entry.Key $entry.Value
        }
    }

    # A server log that another process holds exclusively can be neither read,
    # copied nor deleted. That is diagnostics only: it never changes a passing
    # scenario's result or exit code, never replaces a failing fixture's error,
    # and never stops the outcome, the digest and the other files from being
    # saved (#371). The fixture leaves a detached process holding one of two log
    # files and records it, so the test can release it.
    $lockingFixtureSource = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Scenario,
    [Parameter(Mandatory)][string]$Contract
)
$logDirectory = $env:Briosa__Logging__File__Directory
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
$readable = Join-Path $logDirectory "briosa-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-20261122.jsonl"
$locked = Join-Path $logDirectory "briosa-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb-20261122.jsonl"
foreach ($file in @(@($readable, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 37), @($locked, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", 38))) {
    [IO.File]::WriteAllText($file[0], (([ordered]@{
        Timestamp = [DateTimeOffset]::UtcNow.ToString("o")
        Level = "Warning"
        MessageTemplate = "WorkerStartPhase"
        Properties = [ordered]@{ Phase = "start-failed"; Generation = $file[2]; ServerInstanceId = $file[1]
            EventId = @{ Id = 1203; Name = "WorkerStartPhase" } }
    } | ConvertTo-Json -Compress -Depth 5) + "`n"))
}
$ready = "$env:BRIOSA_TEST_LOCK_RECORD.ready"
$command = "`$stream = [IO.File]::Open('$locked', 'Open', 'Read', 'None'); " +
    "[IO.File]::WriteAllText('$ready', 'locked'); Start-Sleep -Seconds 600"
# Created through WMI, outside the fixture's process tree and without its handles.
$hidden = New-CimInstance -ClassName Win32_ProcessStartup -ClientOnly -Property @{ ShowWindow = [uint16]0 }
$created = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
    CommandLine = "`"$((Get-Process -Id $PID).Path)`" -NoProfile -NonInteractive -EncodedCommand " +
        [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
    ProcessStartupInformation = $hidden
}
[IO.File]::WriteAllLines($env:BRIOSA_TEST_LOCK_RECORD, [string[]]@([string]$created.ProcessId, $locked))
$deadline = [DateTime]::UtcNow.AddSeconds(30)
while (-not (Test-Path -LiteralPath $ready) -and [DateTime]::UtcNow -lt $deadline) {
    Start-Sleep -Milliseconds 100
}
if (-not (Test-Path -LiteralPath $ready)) {
    [Console]::Error.WriteLine("lock-not-taken")
    exit 5
}
if ("__MODE__" -eq "exit-37") {
    [Console]::Error.WriteLine("locked-log-fixture-error")
    exit 37
}
$definition = Get-Content -LiteralPath $Contract -Raw | ConvertFrom-Json
[ordered]@{
    schema_version = 1
    contract_id = $definition.contract_id
    scenario = $Scenario
    success = $true
} | ConvertTo-Json -Compress
'@
    foreach ($lockCase in @("pass", "exit-37")) {
        $lockFixturePath = Join-Path $temporaryRoot "locking-fixture-$lockCase.ps1"
        [IO.File]::WriteAllText($lockFixturePath, $lockingFixtureSource.Replace("__MODE__", $lockCase),
            [Text.UTF8Encoding]::new($false))
        $lockRecord = Join-Path $temporaryRoot "lock-record-$lockCase.txt"
        $lockDiagnostics = Join-Path $temporaryRoot "conformance-diagnostics-locked-$lockCase"
        # A separate process, so the exit code is the one a caller sees.
        $wrapperPath = Join-Path $temporaryRoot "locking-runner-$lockCase.ps1"
        [IO.File]::WriteAllText($wrapperPath, @"
`$ErrorActionPreference = 'Stop'
try {
    & '$runnerPath' -FixtureCommand '$((Get-Process -Id $PID).Path)' ``
        -FixtureArguments @('-NoProfile', '-File', '$lockFixturePath') ``
        -Scenario @('default-ready') -FixtureTimeoutSeconds 60
}
catch {
    Write-Host "Runner error: `$_"
    exit 1
}
"@, [Text.UTF8Encoding]::new($false))
        $previousLockRecord = $env:BRIOSA_TEST_LOCK_RECORD
        $previousDiagnostics = $env:BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY
        $lockAudit = Join-Path $temporaryRoot "digest-audit-locked-$lockCase.txt"
        Set-EnvironmentValue "BRIOSA_TEST_LOCK_RECORD" $lockRecord
        Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY" $lockDiagnostics
        Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIGEST_AUDIT" $lockAudit
        $lockOutput = @()
        $lockExitCode = $null
        try {
            $lockOutput = @(& (Get-Process -Id $PID).Path -NoProfile -NonInteractive -File $wrapperPath 2>&1 |
                ForEach-Object { [string]$_ })
            $lockExitCode = $LASTEXITCODE
            # The failing case's exit code must not become this script's.
            $global:LASTEXITCODE = 0
        }
        finally {
            Set-EnvironmentValue "BRIOSA_TEST_LOCK_RECORD" $previousLockRecord
            Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY" $previousDiagnostics
            Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIGEST_AUDIT" $null
            # Release the lock, then remove what the runner could not.
            if (Test-Path -LiteralPath $lockRecord -PathType Leaf) {
                $recorded = @(Get-Content -LiteralPath $lockRecord)
                $locker = Get-Process -Id ([int]$recorded[0]) -ErrorAction SilentlyContinue
                if ($null -ne $locker) {
                    Stop-Process -Id $locker.Id -Force -ErrorAction SilentlyContinue
                    $locker.WaitForExit(10000) | Out-Null
                }
                $leftover = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $recorded[1]))
                if ((Split-Path -Leaf $leftover) -cmatch '\Abriosa-conformance-[0-9a-f]{32}\z' -and
                    [string]::Equals([IO.Path]::GetFullPath((Split-Path -Parent $leftover) + [IO.Path]::DirectorySeparatorChar),
                        $temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
                    (Test-Path -LiteralPath $leftover)) {
                    Remove-Item -LiteralPath $leftover -Recurse -Force
                }
            }
        }
        $lockOutput | ForEach-Object { Write-Host $_ }
        Assert-Condition ($lockOutput -contains "(the runner could not remove all of its temporary files)") `
            "The conformance runner did not report the temporary files it could not remove ($lockCase)."
        # The audit file holds only vocabulary misses; an unreadable log is a notice.
        Assert-Condition ($lockOutput -contains "(1 server log files of default-ready could not be read for the digest audit)" -and
            (-not (Test-Path -LiteralPath $lockAudit) -or (Get-Item -LiteralPath $lockAudit).Length -eq 0)) `
            "The conformance runner put an unreadable log into the digest audit ($lockCase)."
        if ($lockCase -eq "pass") {
            Assert-Condition ($lockExitCode -eq 0 -and
                @($lockOutput | Where-Object { $_ -like "Passed client conformance scenario: default-ready (fixture *" }).Count -eq 1 -and
                $lockOutput -contains "All selected Briosa client conformance scenarios passed.") `
                "A locked server log changed the result of a passing scenario (exit code $lockExitCode)."
            continue
        }
        $savedFailure = Join-Path $lockDiagnostics "default-ready\failure.txt"
        $savedLockDigest = Join-Path $lockDiagnostics "default-ready\server-log-digest.txt"
        Assert-Condition ($lockExitCode -eq 1 -and
            $lockOutput -contains "Runner error: The client fixture failed scenario 'default-ready' with exit code 37: locked-log-fixture-error" -and
            $lockOutput -contains "(1 server log files could not be read)" -and
            $lockOutput -contains "(1 diagnostic files could not be copied)" -and
            @($lockOutput | Where-Object { $_ -like "+* s Warning ``[server 1``] WorkerStartPhase Phase=start-failed Generation=37" }).Count -eq 1 -and
            @($lockOutput | Where-Object { $_ -like "Passed client conformance scenario: *" }).Count -eq 0) `
            "A locked server log replaced or hid the failing fixture's error (exit code $lockExitCode)."
        Assert-Condition ((Test-Path -LiteralPath $savedFailure -PathType Leaf) -and
            (Get-Content -LiteralPath $savedFailure -Raw) -like "*exit code 37*" -and
            (Test-Path -LiteralPath $savedLockDigest -PathType Leaf) -and
            @(Get-Content -LiteralPath $savedLockDigest | Where-Object { $_ -like "*Generation=37" }).Count -eq 1 -and
            (Test-Path -LiteralPath (Join-Path $lockDiagnostics "default-ready\fixture.stderr.txt") -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $lockDiagnostics "default-ready\server-logs\briosa-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-20261122.jsonl") -PathType Leaf)) `
            "A locked server log stopped the outcome, the digest or the other files from being saved."
    }

    $applicationRoot = Join-Path $temporaryRoot "launchable-fake-application"
    [IO.Directory]::CreateDirectory($applicationRoot) | Out-Null
    Copy-Item -Path (Join-Path $packageRoot "fake-worker\*") `
        -Destination $applicationRoot -Recurse
    $applicationPath = Join-Path $applicationRoot "Spatial Analyzer64.exe"
    Move-Item -LiteralPath (Join-Path $applicationRoot "Briosa.SmokeWorker.exe") `
        -Destination $applicationPath
    $applicationProcess = $null
    try {
        $applicationProcess = Start-Process `
            -FilePath $applicationPath `
            -WorkingDirectory $applicationRoot `
            -PassThru
        $windowDeadline = [DateTimeOffset]::UtcNow.AddSeconds(5)
        do {
            Start-Sleep -Milliseconds 100
            Assert-Condition (-not $applicationProcess.HasExited) `
                "The packaged fake application did not survive a normal zero-argument launch."
            $applicationProcess.Refresh()
        } while (($applicationProcess.MainWindowHandle -eq [IntPtr]::Zero -or
            [string]::IsNullOrWhiteSpace($applicationProcess.MainWindowTitle)) -and
            [DateTimeOffset]::UtcNow -lt $windowDeadline)
        Assert-Condition ($applicationProcess.MainWindowHandle -ne [IntPtr]::Zero -and
            -not [string]::IsNullOrWhiteSpace($applicationProcess.MainWindowTitle)) `
            "The packaged fake application did not expose the real host's required ready window."
    }
    finally {
        if ($null -ne $applicationProcess -and -not $applicationProcess.HasExited) {
            Stop-Process -Id $applicationProcess.Id -Force
            $applicationProcess.WaitForExit()
        }
        if ($null -ne $applicationProcess) {
            $applicationProcess.Dispose()
        }
    }

    Write-Host "Client conformance package reproducibility, contract, checksums, and runner tests passed."
}
finally {
    if (Test-Path variable:previousDigestAudit) {
        Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIGEST_AUDIT" $previousDigestAudit
    }
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
