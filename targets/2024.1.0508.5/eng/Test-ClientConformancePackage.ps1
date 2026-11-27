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

$artifactBase = "briosa-client-conformance-$Version-sa-2024.1.0508.5-win-x64"
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
    Assert-Condition ($manifest.spatialAnalyzerTarget -eq "2024.1.0508.5") `
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

    # large-result is optional: a fixture runs it only by asking for it, so the
    # published fixtures, which do not know it, never receive it. Its expected
    # working directory is the smoke worker's fixed text: 1 MiB minus 2 KiB of a
    # 36-character ASCII pattern.
    $optionalScenarios = @($contract.optional_scenarios)
    Assert-Condition ($optionalScenarios.Count -eq 1 -and
        $optionalScenarios[0].id -eq "large-result" -and
        $optionalScenarios[0].worker_scenario -eq "large-result" -and
        $optionalScenarios[0].id -notin $scenarioIds) `
        "The conformance contract must list large-result as its only optional scenario."
    $largeResultPattern = "abcdefghijklmnopqrstuvwxyz0123456789"
    $largeResultLength = 1048576 - 2048
    $largeResultText = ($largeResultPattern * [Math]::Ceiling($largeResultLength / $largeResultPattern.Length)).Substring(0, $largeResultLength)
    $largeResultBytes = [Text.Encoding]::UTF8.GetBytes($largeResultText)
    $largeResultHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($largeResultBytes)).ToLowerInvariant()
    Assert-Condition ([long]$optionalScenarios[0].expected_working_directory.utf8_length -eq $largeResultBytes.Length -and
        [string]$optionalScenarios[0].expected_working_directory.sha256 -ceq $largeResultHash) `
        "The large-result scenario must publish the length and SHA-256 of the smoke worker's text."
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

    Assert-Condition (@($passedLines | Where-Object { $_ -like "*: large-result *" }).Count -eq 0) `
        "The conformance runner ran an optional scenario that no fixture asked for."

    # An optional scenario runs only on request, and the runner checks the
    # working directory the fixture reports: the UTF-8 length and SHA-256 must
    # match the contract exactly. These fixtures only echo a report built from
    # the contract, so they test the runner's check, not a client's receipt.
    $largeResultFixtureSource = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Scenario,
    [Parameter(Mandatory)][string]$Contract
)
$definition = Get-Content -LiteralPath $Contract -Raw | ConvertFrom-Json
$report = [ordered]@{
    schema_version = 1
    contract_id = $definition.contract_id
    scenario = $Scenario
    success = $true
}
$optional = @($definition.optional_scenarios | Where-Object { $_.id -eq $Scenario })
$json = $report | ConvertTo-Json -Compress
if ($optional.Count -eq 1) {
    # The reported working directory, as raw JSON, so a case can use any type.
    $json = $json.Substring(0, $json.Length - 1) + ',"working_directory":__WORKING_DIRECTORY__}'
    if ('__WRAP_REPORT__' -eq 'true') { $json = "[$json]" }
}
$json
'@
    # Malformed reports. The soft hyphen is written as a JSON escape, so it reaches
    # the runner unchanged; a linguistic comparison would ignore it.
    $expectedLength = [long]$optionalScenarios[0].expected_working_directory.utf8_length
    $expectedDigest = [string]$optionalScenarios[0].expected_working_directory.sha256
    $softHyphen = '\' + 'u00ad'
    $largeResultCases = [ordered]@{
        "exact" = "{`"utf8_length`":$expectedLength,`"sha256`":`"$expectedDigest`"}"
        "short" = "{`"utf8_length`":$($expectedLength - 1),`"sha256`":`"$expectedDigest`"}"
        "length-as-string" = "{`"utf8_length`":`"$expectedLength`",`"sha256`":`"$expectedDigest`"}"
        "length-as-fraction" = "{`"utf8_length`":$expectedLength.0,`"sha256`":`"$expectedDigest`"}"
        "length-with-soft-hyphen" = "{`"utf8_length`":`"$expectedLength$softHyphen`",`"sha256`":`"$expectedDigest`"}"
        "digest-with-soft-hyphen" = "{`"utf8_length`":$expectedLength,`"sha256`":`"$($expectedDigest.Substring(0, 8))$softHyphen$($expectedDigest.Substring(8))`"}"
        "digest-upper-case" = "{`"utf8_length`":$expectedLength,`"sha256`":`"$($expectedDigest.ToUpperInvariant())`"}"
        "digest-as-number" = "{`"utf8_length`":$expectedLength,`"sha256`":12345}"
        "digest-missing" = "{`"utf8_length`":$expectedLength}"
        "length-missing" = "{`"sha256`":`"$expectedDigest`"}"
        "not-an-object" = "`"$expectedDigest`""
        "length-huge" = "{`"utf8_length`":99999999999999999999999,`"sha256`":`"$expectedDigest`"}"
        "length-null" = "{`"utf8_length`":null,`"sha256`":`"$expectedDigest`"}"
        "length-empty-array" = "{`"utf8_length`":[],`"sha256`":`"$expectedDigest`"}"
        # Singleton arrays, which PowerShell would unwrap, and property names in
        # another case, which PowerShell property access would match.
        "length-in-array" = "{`"utf8_length`":[$expectedLength],`"sha256`":`"$expectedDigest`"}"
        "digest-in-array" = "{`"utf8_length`":$expectedLength,`"sha256`":[`"$expectedDigest`"]}"
        "directory-in-array" = "[{`"utf8_length`":$expectedLength,`"sha256`":`"$expectedDigest`"}]"
        "report-in-array" = "{`"utf8_length`":$expectedLength,`"sha256`":`"$expectedDigest`"}"
        "length-name-upper-case" = "{`"UTF8_LENGTH`":$expectedLength,`"sha256`":`"$expectedDigest`"}"
        "digest-name-upper-case" = "{`"utf8_length`":$expectedLength,`"SHA256`":`"$expectedDigest`"}"
    }
    foreach ($caseName in $largeResultCases.Keys) {
        $largeResultFixturePath = Join-Path $temporaryRoot "large-result-fixture-$caseName.ps1"
        [IO.File]::WriteAllText(
            $largeResultFixturePath,
            $largeResultFixtureSource.Replace("__WORKING_DIRECTORY__", $largeResultCases[$caseName]).Replace(
                "__WRAP_REPORT__", $(if ($caseName -eq "report-in-array") { "true" } else { "false" })),
            [Text.UTF8Encoding]::new($false))
        $largeResultOutput = [Collections.Generic.List[string]]::new()
        $largeResultFailure = $null
        try {
            $largeResultArguments = @{
                FixtureCommand = (Get-Process -Id $PID).Path
                FixtureArguments = @("-NoProfile", "-File", $largeResultFixturePath)
                Scenario = @("control-plane-only")
                OptionalScenario = @("large-result")
                FixtureTimeoutSeconds = 30
            }
            & $runnerPath @largeResultArguments *>&1 | ForEach-Object { $largeResultOutput.Add([string]$_) }
        }
        catch {
            $largeResultFailure = $_
        }
        if ($caseName -eq "exact") {
            Assert-Condition ($null -eq $largeResultFailure -and
                @($largeResultOutput | Where-Object { $_ -like "Passed client conformance scenario: large-result *" }).Count -eq 1) `
                "The conformance runner did not run the requested optional scenario: $largeResultFailure"
        }
        else {
            Assert-Condition ($null -ne $largeResultFailure -and
                "$largeResultFailure" -like "*did not report receiving the whole working directory in scenario 'large-result'*") `
                "The conformance runner accepted a $caseName working directory report: $largeResultFailure"
        }
    }
    $unknownOptionalFailure = $null
    try {
        & $runnerPath -FixtureCommand (Get-Process -Id $PID).Path -FixtureArguments @("-NoProfile", "-File", $fixturePath) `
            -Scenario @("control-plane-only") -OptionalScenario @("no-such-scenario") *>&1 | Out-Null
    }
    catch {
        $unknownOptionalFailure = $_
    }
    Assert-Condition ($null -ne $unknownOptionalFailure -and
        "$unknownOptionalFailure" -like "*Unknown optional conformance scenario 'no-such-scenario'*") `
        "The conformance runner did not reject an unknown optional scenario."

    # A release or CI package runs every selected scenario, whatever the environment
    # says (#450): the variable that narrowed runs in the first revision is ignored.
    Assert-Condition (-not (Test-Path -LiteralPath (Join-Path $packageRoot "runner\soak-scenario.json"))) `
        "A release or CI conformance package must not name a soak scenario."
    $previousOnlyScenario = $env:BRIOSA_CONFORMANCE_ONLY_SCENARIO
    try {
        Set-EnvironmentValue "BRIOSA_CONFORMANCE_ONLY_SCENARIO" "capability-denied"
        $immuneOutput = @(& $runnerPath -FixtureCommand (Get-Process -Id $PID).Path `
                -FixtureArguments @("-NoProfile", "-File", $fixturePath) -FixtureTimeoutSeconds 30 *>&1 |
            ForEach-Object { [string]$_ })
        Assert-Condition (@($immuneOutput | Where-Object { $_ -like "Passed client conformance scenario: *" }).Count -eq $scenarioIds.Count -and
            $immuneOutput -contains "All selected Briosa client conformance scenarios passed.") `
            "The environment narrowed an ordinary conformance run."
    }
    finally {
        Set-EnvironmentValue "BRIOSA_CONFORMANCE_ONLY_SCENARIO" $previousOnlyScenario
    }

    # A soak package names one scenario (#450). Its runner runs that scenario only and
    # then always fails, so a client's test script never writes compatibility evidence
    # for a run that skipped the others. The soak checks for exactly one pass of the
    # named scenario and this error.
    $soakFile = Join-Path $packageRoot "runner\soak-scenario.json"
    try {
        [IO.File]::WriteAllText($soakFile, '{"schema_version":1,"scenario":"capability-denied"}', [Text.UTF8Encoding]::new($false))
        $soakOutput = [Collections.Generic.List[string]]::new()
        $soakFailure = $null
        try {
            & $runnerPath -FixtureCommand (Get-Process -Id $PID).Path -FixtureArguments @("-NoProfile", "-File", $fixturePath) `
                -OptionalScenario @("large-result") -FixtureTimeoutSeconds 30 *>&1 | ForEach-Object { $soakOutput.Add([string]$_) }
        }
        catch {
            $soakFailure = $_
        }
        Assert-Condition ($null -ne $soakFailure -and
            "$soakFailure" -ceq "This soak run repeated conformance scenario 'capability-denied' only, so it is not compatibility evidence." -and
            @($soakOutput | Where-Object { $_ -like "Passed client conformance scenario: *" }).Count -eq 1 -and
            @($soakOutput | Where-Object { $_ -like "Passed client conformance scenario: capability-denied (*" }).Count -eq 1 -and
            $soakOutput -notcontains "All selected Briosa client conformance scenarios passed.") `
            "A soak package did not run its one scenario and end without evidence: $soakFailure"
        # The scenario must be one the run selected.
        $notSelectedFailure = $null
        try {
            & $runnerPath -FixtureCommand (Get-Process -Id $PID).Path -FixtureArguments @("-NoProfile", "-File", $fixturePath) `
                -Scenario @("control-plane-only") *>&1 | Out-Null
        }
        catch {
            $notSelectedFailure = $_
        }
        Assert-Condition ("$notSelectedFailure" -ceq "The soak package names a scenario that this run did not select.") `
            "A soak package ran a scenario the run did not select: $notSelectedFailure"
        [IO.File]::WriteAllText($soakFile, '{"schema_version":1,"scenario":"Capability-Denied"}', [Text.UTF8Encoding]::new($false))
        $invalidSoakFailure = $null
        try {
            & $runnerPath -FixtureCommand (Get-Process -Id $PID).Path -FixtureArguments @("-NoProfile", "-File", $fixturePath) *>&1 | Out-Null
        }
        catch {
            $invalidSoakFailure = $_
        }
        Assert-Condition ("$invalidSoakFailure" -ceq "The soak package's scenario file is invalid.") `
            "A soak package with an invalid scenario file ran: $invalidSoakFailure"
    }
    finally {
        Remove-Item -LiteralPath $soakFile -Force -ErrorAction SilentlyContinue
    }

    # A connection capture package (#450). A release or CI package carries neither the
    # marker nor the client module, and its runner passes nothing of a capture to the
    # fixture, whatever the caller's environment holds. A capture package's runner turns
    # the server's connection records on, preloads the module into a Node fixture through
    # NODE_OPTIONS after the caller's own options, names the scenario's own capture file,
    # and always ends with its own error, so no client script records evidence from it.
    # The package gets the marker and the module as New-ClientConformancePackage.ps1
    # -ConnectionCapture writes them.
    $captureMarker = Join-Path $packageRoot "runner\connection-capture.json"
    $captureModule = Join-Path $packageRoot "runner\connection-capture.cjs"
    Assert-Condition (-not (Test-Path -LiteralPath $captureMarker) -and -not (Test-Path -LiteralPath $captureModule)) `
        "A release or CI conformance package must not capture the client connection."
    $runnerText = [IO.File]::ReadAllText($runnerPath)
    $captureLevels = [regex]::Match($runnerText, '\$connectionCaptureLevels = \[ordered\]@\{(?<entries>[^}]*)\}')
    $levels = [ordered]@{}
    foreach ($entry in [regex]::Matches($captureLevels.Groups["entries"].Value, '"(?<category>[A-Za-z0-9.]+)" = "(?<level>[A-Za-z]+)"')) {
        $levels["Logging__LogLevel__$($entry.Groups['category'].Value)"] = $entry.Groups["level"].Value
    }
    Assert-Condition ($levels.Count -eq 6) "The conformance runner's connection capture levels did not parse."
    $captureFixturePath = Join-Path $temporaryRoot "capture-fixture.ps1"
    $captureFixtureSource = @'
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Scenario,
    [Parameter(Mandatory)][string]$Contract
)
$logDirectory = $env:Briosa__Logging__File__Directory
$scenarioRoot = Split-Path -Parent $logDirectory
$expected = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('__EXPECTED__')) | ConvertFrom-Json
$mismatches = [Collections.Generic.List[string]]::new()
foreach ($entry in $expected.PSObject.Properties) {
    $actual = [Environment]::GetEnvironmentVariable($entry.Name)
    $want = $(if ($entry.Value -ceq '<capture-file>') { Join-Path $scenarioRoot 'client-connection.jsonl' } else { [string]$entry.Value })
    if ([string]::IsNullOrEmpty($want)) {
        if (-not [string]::IsNullOrEmpty($actual)) { $mismatches.Add("$($entry.Name) is set") }
    }
    elseif ($actual -cne $want) { $mismatches.Add("$($entry.Name) is '$actual'") }
}
if ($mismatches.Count -gt 0) {
    [Console]::Error.WriteLine("capture-environment-mismatch: $($mismatches -join '; ')")
    exit 9
}
if ('__MODE__' -eq 'fail-unwritable') {
    # The timeline's destination is taken by a directory.
    [IO.Directory]::CreateDirectory((Join-Path $scenarioRoot 'connection-timeline.txt')) | Out-Null
    [Console]::Error.WriteLine('capture-fixture-failed')
    exit 7
}
if ('__MODE__' -eq 'fail') {
    # What the server and the client module write, beside values that must never print.
    [IO.Directory]::CreateDirectory($logDirectory) | Out-Null
    $started = [DateTimeOffset]::UtcNow
    $trace = '0123456789abcdef0123456789abcdef'
    $server = [Collections.Generic.List[string]]::new()
    function Add-Record([int]$Offset, [string]$Level, [int]$Id, [string]$Name, [hashtable]$Properties) {
        $Properties['ServerInstanceId'] = 'fedcba9876543210fedcba9876543210'
        $Properties['EventId'] = @{ Id = $Id; Name = $Name }
        $server.Add(([ordered]@{ Timestamp = $started.AddMilliseconds($Offset).ToString('o'); Level = $Level
            MessageTemplate = $Name; Properties = $Properties } | ConvertTo-Json -Compress -Depth 5))
    }
    Add-Record 1 'Debug' 39 'ConnectionAccepted' @{ ConnectionId = '0HNAAAAAAAAAA' }
    Add-Record 2 'Verbose' 37 'Http2FrameReceived' @{ ConnectionId = '0HNAAAAAAAAAA'; StreamId = 1; FrameType = 'HEADERS'; FrameLength = 40; FrameFlags = 4 }
    Add-Record 3 'Information' 1 'HostingRequestStarting' @{ HttpProtocol = 'HTTP/2'; HttpMethod = 'POST'; Rpc = 'GetServerInfo'; TraceId = $trace }
    Add-Record 4 'Debug' 25 'RequestBodyStart' @{ ConnectionId = '0HNAAAAAAAAAA'; StreamId = 1; TraceId = $trace }
    Add-Record 5 'Debug' 1600 'UnaryCallStarted' @{ Rpc = 'GetServerInfo'; TraceId = $trace }
    Add-Record 6 'Debug' 1601 'UnaryCallCompleted' @{ Rpc = 'GetServerInfo'; GrpcStatus = 'OK'; ElapsedMilliseconds = 1; TraceId = $trace }
    Add-Record 7 'Verbose' 49 'Http2FrameSending' @{ ConnectionId = '0HNAAAAAAAAAA'; StreamId = 1; FrameType = 'HEADERS'; FrameLength = 15; FrameFlags = 5 }
    Add-Record 8 'Verbose' 37 'Http2FrameReceived' @{ ConnectionId = 'C:/Users/someone/secret'; StreamId = 'someone@example.com'
        FrameType = 'secret-frame-type'; Host = 'secret-host.example.com'; FrameLength = 1 }
    [IO.File]::WriteAllLines((Join-Path $logDirectory 'briosa-fedcba9876543210fedcba9876543210-20261127.jsonl'), $server)
    $client = @(
        [ordered]@{ t = $started.AddMilliseconds(2).UtcDateTime.ToString('yyyy-MM-ddTHH:mm:ss.fffZ'); ms = 1.5; src = 'http2'; ev = 'stream-start'
            session = 1; stream = 1; method = '/briosa.DiscoveryService/GetServerInfo' },
        [ordered]@{ t = $started.AddMilliseconds(8).UtcDateTime.ToString('yyyy-MM-ddTHH:mm:ss.fffZ'); ms = 7.5; src = 'grpc'; ev = 'ended'
            tracer = 'subchannel_call'; call = 7; code = 0 },
        [ordered]@{ t = $started.AddMilliseconds(9).UtcDateTime.ToString('yyyy-MM-ddTHH:mm:ss.fffZ'); ms = 8.5; src = 'http2'; ev = 'stream-start'
            method = '/secret path/C:\Users\someone'; peer = '10.1.2.3:443'; address = 'secret-host.example.com:443'
            settings = [ordered]@{ initialWindowSize = 1; secretKey = 5 }; code = 'someone@example.com' },
        [ordered]@{ t = $started.AddMilliseconds(10).UtcDateTime.ToString('yyyy-MM-ddTHH:mm:ss.fffZ'); src = 'secret-source'; ev = 'x' }
    ) | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 5 }
    [IO.File]::WriteAllLines((Join-Path $scenarioRoot 'client-connection.jsonl'), [string[]](@($client) + 'not json secret-line'))
    [Console]::Error.WriteLine('capture-fixture-failed')
    exit 7
}
$definition = Get-Content -LiteralPath $Contract -Raw | ConvertFrom-Json
[ordered]@{ schema_version = 1; contract_id = $definition.contract_id; scenario = $Scenario; success = $true } | ConvertTo-Json -Compress
'@
    $previousCaptureEnvironment = @{}
    foreach ($name in @("NODE_OPTIONS", "BRIOSA_CONFORMANCE_CLIENT_CAPTURE", "Briosa__Logging__QueueCapacity") + @($levels.Keys)) {
        $previousCaptureEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
    }
    try {
        # The caller's own Node options, and a capture file it names itself.
        Set-EnvironmentValue "NODE_OPTIONS" "--max-old-space-size=4096"
        Set-EnvironmentValue "BRIOSA_CONFORMANCE_CLIENT_CAPTURE" (Join-Path $temporaryRoot "caller-capture.jsonl")
        Set-EnvironmentValue "Briosa__Logging__QueueCapacity" $null
        foreach ($name in $levels.Keys) { Set-EnvironmentValue $name $null }
        $off = [ordered]@{ NODE_OPTIONS = "--max-old-space-size=4096"; BRIOSA_CONFORMANCE_CLIENT_CAPTURE = ""; Briosa__Logging__QueueCapacity = "" }
        foreach ($name in $levels.Keys) { $off[$name] = "" }
        $moduleOption = "--require `"$([IO.Path]::GetFullPath($captureModule).Replace('\', '/'))`""
        $on = [ordered]@{ NODE_OPTIONS = "--max-old-space-size=4096 $moduleOption"; BRIOSA_CONFORMANCE_CLIENT_CAPTURE = "<capture-file>"
            Briosa__Logging__QueueCapacity = "65536" }
        foreach ($name in $levels.Keys) { $on[$name] = $levels[$name] }
        function Invoke-CaptureRun([Collections.IDictionary]$Expected, [string]$Mode, [string]$Diagnostics) {
            $source = $captureFixtureSource.Replace("__EXPECTED__",
                [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($Expected | ConvertTo-Json -Compress)))).Replace("__MODE__", $Mode)
            [IO.File]::WriteAllText($captureFixturePath, $source, [Text.UTF8Encoding]::new($false))
            $output = [Collections.Generic.List[string]]::new()
            $failure = $null
            try {
                $arguments = @{
                    FixtureCommand = (Get-Process -Id $PID).Path
                    FixtureArguments = @("-NoProfile", "-File", $captureFixturePath)
                    Scenario = @("control-plane-only", "default-ready")
                    FixtureTimeoutSeconds = 30
                }
                if ($Diagnostics) { $arguments.DiagnosticsDirectory = $Diagnostics }
                & $runnerPath @arguments *>&1 | ForEach-Object { $output.Add([string]$_) }
            }
            catch {
                $failure = "$_"
            }
            [pscustomobject]@{ Output = $output; Failure = $failure }
        }

        $ordinary = Invoke-CaptureRun $off "pass" $null
        Assert-Condition ($null -eq $ordinary.Failure -and
            @($ordinary.Output | Where-Object { $_ -like "Passed client conformance scenario: *" }).Count -eq 2 -and
            $ordinary.Output -contains "All selected Briosa client conformance scenarios passed." -and
            -not (Test-Path -LiteralPath (Join-Path $temporaryRoot "caller-capture.jsonl"))) `
            "A release or CI conformance package passed part of a capture to the fixture: $($ordinary.Failure)"

        [IO.File]::WriteAllText($captureMarker, '{"schema_version":1}', [Text.UTF8Encoding]::new($false))
        Copy-Item -LiteralPath (Join-Path $targetRoot "conformance\client\v1\connection-capture.cjs") -Destination $captureModule
        $captured = Invoke-CaptureRun $on "pass" $null
        Assert-Condition ($captured.Failure -ceq "This soak run recorded the HTTP/2 connection of every scenario, so it is not compatibility evidence." -and
            @($captured.Output | Where-Object { $_ -like "Passed client conformance scenario: *" }).Count -eq 2 -and
            $captured.Output -notcontains "All selected Briosa client conformance scenarios passed." -and
            $captured.Output -contains "Recording each scenario's HTTP/2 connection at both ends: this soak package's runs are not compatibility evidence.") `
            "A connection capture package did not pass its scenarios and end without evidence: $($captured.Failure)"

        # A failing scenario keeps both ends' records and a merged timeline, which prints
        # only reviewed names and validated values. The digest's tail keeps the call
        # records and points to the timeline for the connection records.
        $captureDiagnostics = Join-Path $temporaryRoot "capture-diagnostics"
        $failed = Invoke-CaptureRun $on "fail" $captureDiagnostics
        $kept = Join-Path $captureDiagnostics "control-plane-only"
        $timelinePath = Join-Path $kept "connection-timeline.txt"
        Assert-Condition ($failed.Failure -like "*failed scenario 'control-plane-only' with exit code 7*" -and
            (Test-Path -LiteralPath $timelinePath) -and (Test-Path -LiteralPath (Join-Path $kept "client-connection.jsonl")) -and
            (Test-Path -LiteralPath (Join-Path $kept "server-logs\briosa-fedcba9876543210fedcba9876543210-20261127.jsonl"))) `
            "A failing connection capture scenario did not keep both ends' records: $($failed.Failure)"
        $timeline = [IO.File]::ReadAllText($timelinePath)
        $digest = [IO.File]::ReadAllText((Join-Path $kept "server-log-digest.txt"))
        $printed = $failed.Output -join "`n"
        foreach ($line in @(
                "client http2 stream-start session=1 stream=1 method=/briosa.DiscoveryService/GetServerInfo",
                "client grpc ended tracer=subchannel_call call=7 code=0",
                "server ConnectionAccepted ConnectionId=0HNAAAAAAAAAA",
                "server Http2FrameReceived ConnectionId=0HNAAAAAAAAAA StreamId=1 FrameType=HEADERS FrameLength=40 FrameFlags=4",
                "server HostingRequestStarting HttpProtocol=HTTP/2 HttpMethod=POST Rpc=GetServerInfo stream=0HNAAAAAAAAAA/1",
                "server RequestBodyStart ConnectionId=0HNAAAAAAAAAA StreamId=1",
                "server UnaryCallStarted Rpc=GetServerInfo stream=0HNAAAAAAAAAA/1",
                "server UnaryCallCompleted Rpc=GetServerInfo GrpcStatus=OK ElapsedMilliseconds=1 stream=0HNAAAAAAAAAA/1",
                "server Http2FrameSending ConnectionId=0HNAAAAAAAAAA StreamId=1 FrameType=HEADERS FrameLength=15 FrameFlags=5",
                "server Http2FrameReceived ConnectionId=<withheld> StreamId=<withheld> FrameType=<withheld> FrameLength=1",
                "client http2 stream-start method=<withheld> peer=<withheld> address=<withheld> settings={initialWindowSize=1,<withheld>} code=<withheld>",
                "(2 records or files could not be read and were skipped)")) {
            Assert-Condition ($timeline.Contains($line, [StringComparison]::Ordinal) -and $printed.Contains($line, [StringComparison]::Ordinal)) `
                "The connection timeline does not show '$line'."
        }
        Assert-Condition ($digest.Contains("(6 HTTP/2 connection records are listed in the connection timeline, not here)", [StringComparison]::Ordinal) -and
            $digest.Contains("Debug UnaryCallStarted Rpc=GetServerInfo", [StringComparison]::Ordinal) -and
            -not $digest.Contains("Http2Frame", [StringComparison]::Ordinal)) `
            "The server log digest did not keep its call records apart from the connection records."
        foreach ($text in @($timeline, $digest, $printed)) {
            foreach ($secret in @("secret", "someone", "example.com", "10.1.2.3")) {
                Assert-Condition (-not $text.Contains($secret, [StringComparison]::OrdinalIgnoreCase)) `
                    "The connection timeline or digest printed '$secret'."
            }
        }

        # A timeline that cannot be saved is reported and never hides the scenario's failure.
        $unwritable = Invoke-CaptureRun $on "fail-unwritable" (Join-Path $temporaryRoot "capture-diagnostics-unwritable")
        Assert-Condition ($unwritable.Failure -like "*failed scenario 'control-plane-only' with exit code 7: capture-fixture-failed*" -and
            $unwritable.Output -contains "(the connection timeline could not be saved for control-plane-only)") `
            "An unwritable connection timeline replaced the scenario's failure: $($unwritable.Failure)"

        foreach ($invalid in @('{"schema_version":2}', '{"schema_version":1,"scope":"all"}', 'missing-module')) {
            if ($invalid -ceq 'missing-module') {
                [IO.File]::WriteAllText($captureMarker, '{"schema_version":1}', [Text.UTF8Encoding]::new($false))
                Remove-Item -LiteralPath $captureModule -Force
            }
            else {
                [IO.File]::WriteAllText($captureMarker, $invalid, [Text.UTF8Encoding]::new($false))
            }
            $rejected = Invoke-CaptureRun $on "pass" $null
            Assert-Condition ($rejected.Failure -ceq "The soak package's connection capture file is invalid.") `
                "A connection capture package with an invalid marker ($invalid) ran: $($rejected.Failure)"
        }

        # The client module itself, under Node: it keeps the reviewed fields of grpc-js's
        # trace and of Node's HTTP/2 events and none of the markers it was given, and the
        # fixture's own output is unchanged. CI installs Node for this step; elsewhere a
        # missing or older Node skips it and says so.
        $node = Get-Command node -ErrorAction SilentlyContinue
        $nodeVersion = $(if ($null -ne $node) { (& $node.Source -p "process.versions.node").Trim() })
        if ($null -ne $nodeVersion -and [version]$nodeVersion -ge [version]"22.15.0") {
            $moduleScratch = Join-Path $temporaryRoot "capture-module"
            [IO.Directory]::CreateDirectory($moduleScratch) | Out-Null
            & $node.Source (Join-Path $PSScriptRoot "Test-ConnectionCapture.mjs") `
                (Join-Path $targetRoot "conformance\client\v1\connection-capture.cjs") $moduleScratch
            Assert-Condition ($LASTEXITCODE -eq 0) "The client connection capture module failed its checks."
        }
        elseif ($env:GITHUB_ACTIONS -ceq "true") {
            throw "The client connection capture module needs Node 22.15 or later on the CI runner."
        }
        else {
            Write-Host "SKIPPED: the client connection capture module checks need Node 22.15 or later on PATH."
        }
    }
    finally {
        Remove-Item -LiteralPath $captureMarker, $captureModule -Force -ErrorAction SilentlyContinue
        foreach ($entry in $previousCaptureEnvironment.GetEnumerator()) {
            Set-EnvironmentValue $entry.Key $entry.Value
        }
    }

    # Runner functions on their own (#450), defined from the runner's own source: the
    # process tree walk on a synthetic snapshot larger than the display limit. The
    # census names each process by its executable only and reads no command line, so
    # no argument can reach it. The runner traces no client: it names no gRPC
    # library's trace switch, so it can set none for any language.
    $runnerAst = [Management.Automation.Language.Parser]::ParseFile($runnerPath, [ref]$null, [ref]$null)
    $runnerText = [IO.File]::ReadAllText($runnerPath)
    Assert-Condition (-not $runnerText.Contains("CommandLine", [StringComparison]::OrdinalIgnoreCase)) `
        "The conformance runner reads a process command line."
    foreach ($switch in @("CLIENT_TRACE", "GRPC_NODE_", "GRPC_TRACE", "GRPC_VERBOSITY", "EventPipe")) {
        Assert-Condition (-not $runnerText.Contains($switch, [StringComparison]::OrdinalIgnoreCase)) `
            "The conformance runner names the client tracing switch '$switch'."
    }
    foreach ($functionName in @("Test-SameCreation", "Get-FixtureProcessTree")) {
        $definition = $runnerAst.Find({
                param($node)
                $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq $functionName
            }, $true)
        Assert-Condition ($null -ne $definition) "The conformance runner has no $functionName function."
        . ([scriptblock]::Create($definition.Extent.Text))
    }
    $base = [DateTime]::new(2026, 11, 26, 12, 0, 0, [DateTimeKind]::Utc).ToLocalTime()
    $snapshot = @{ 1000 = [pscustomobject]@{ ProcessId = 1000; ParentProcessId = 4; CreationDate = $base } }
    foreach ($index in 1..300) {
        $snapshot[2000 + $index] = [pscustomobject]@{ ProcessId = 2000 + $index; ParentProcessId = 1000; CreationDate = $base.AddSeconds($index) }
    }
    # An older process whose parent ID was later reused by the fixture.
    $snapshot[9000] = [pscustomobject]@{ ProcessId = 9000; ParentProcessId = 1000; CreationDate = $base.AddSeconds(-5) }
    $walked = Get-FixtureProcessTree -RootId 1000 -RootStartTicks $base.ToUniversalTime().Ticks -ProcessTable $snapshot
    $walkedIds = @($walked | ForEach-Object { [int]$_.Entry.ProcessId })
    Assert-Condition ($walked.Count -eq 301 -and $walkedIds[0] -eq 1000 -and $walkedIds -contains 2300 -and
        $walkedIds -notcontains 9000) `
        "The process tree walk did not visit every descendant and only descendants (it returned $($walked.Count))."

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
# The runner turns on the server's Debug unary call records (#450).
if ([Environment]::GetEnvironmentVariable("Logging__LogLevel__Briosa.Server.Services.UnaryCallAuditInterceptor") -cne "Debug") {
    [Console]::Error.WriteLine("unary-call-audit-not-enabled")
    exit 6
}
# The runner traces no client (#450): no gRPC library's trace switch reaches the
# fixture, whatever the caller's environment asked for.
foreach ($name in @("GRPC_NODE_TRACE", "GRPC_NODE_VERBOSITY", "GRPC_TRACE", "GRPC_VERBOSITY",
        "DOTNET_EnableEventPipe", "DOTNET_EventPipeOutputPath", "DOTNET_EventPipeConfig")) {
    if (-not [string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name))) {
        [Console]::Error.WriteLine("client-trace-switch-set")
        exit 8
    }
}
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
[IO.File]::WriteAllText((Join-Path $logDirectory "server-log-marker.jsonl"), "server-log-marker")
# A server log in the real record shape: three early warnings; warnings that carry
# sensitive-looking text in printed properties, in an event name, a template and
# a level; more lower-severity records than the digest keeps; a property outside
# the digest allow-list; listed and unlisted diagnostic codes and operation IDs;
# values that only a linguistic comparison would accept; a warning written with
# whitespace after "Level":; the server's timestamp-free placeholder for an
# oversized record; warnings written out of time order; governed names written
# with JSON escapes, and beside a differently cased name; optionally a flood of
# warnings; truncated last lines of a killed writer, one of them after a complete
# timestamp; and a newest record whose string cannot be decoded.
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
# The server's unary call records (#450), at Debug: a call named by its RPC, one
# named by its operation ID, and one with a name the digest does not list.
Add-ServerRecord 270 "Debug" "UnaryCallStarted" @{ Rpc = "GetServerInfo"; OperationId = $null } -Id 1600
Add-ServerRecord 271 "Debug" "UnaryCallCompleted" @{
    Rpc = "GetServerInfo"; OperationId = $null; GrpcStatus = "OK"; ElapsedMilliseconds = 1 } -Id 1601
Add-ServerRecord 272 "Debug" "UnaryCallResponseEnded" @{
    Rpc = "GetServerInfo"; OperationId = $null; GrpcStatus = "OK"; ElapsedMilliseconds = 2 } -Id 1603
Add-ServerRecord 273 "Debug" "UnaryCallStarted" @{ Rpc = $null; OperationId = "file_operations.get_working_directory" } -Id 1600
Add-ServerRecord 274 "Debug" "UnaryCallFaulted" @{ Rpc = "secret-rpc-name"; ElapsedMilliseconds = 3 } -Id 1602
Add-ServerRecord 275 "Debug" "UnaryCallResponseAbandoned" @{ Rpc = "StopSpatialAnalyzerSdk"; OperationId = $null; ElapsedMilliseconds = 4 } -Id 1604
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
# Governed names written with JSON escapes: still DiagnosticCode and OperationId.
$records.Add('{"Timestamp":"' + $started.AddMilliseconds(308).ToString("o") +
    '","Level":"Warning","MessageTemplate":"WorkerTransition","Properties":{"Diagnostic\u0043ode":"escaped-unlisted-code",' +
    '"Operation\u0049d":"escaped_operations.unlisted","ServerInstanceId":"0123456789abcdef0123456789abcdef",' +
    '"EventId":{"Id":1201,"Name":"WorkerTransition"}}}')
# A listed code beside a differently cased name that the digest does not govern.
$records.Add('{"Timestamp":"' + $started.AddMilliseconds(309).ToString("o") +
    '","Level":"Warning","MessageTemplate":"WorkerTransition","Properties":{"DiagnosticCode":"worker-starting",' +
    '"diagnosticcode":"review-case-marker","ServerInstanceId":"0123456789abcdef0123456789abcdef",' +
    '"EventId":{"Id":1201,"Name":"WorkerTransition"}}}')
$flood = __FLOOD__
for ($index = 1; $index -le $flood; $index++) {
    Add-ServerRecord (400 + $index) "Warning" "WorkerStartPhase" @{
        Phase = "start-failed"; Generation = 100000 + $index } -Id 1203
}
$records.Add('{"Timestamp":"' + $started.AddMilliseconds(2000).ToString("o") + '","Level":"Information","MessageTemplate":"Readin')
# Valid JSON whose string cannot be decoded: it must not erase the timeline.
$records.Add('{"Timestamp":"' + $started.AddMilliseconds(2500).ToString("o") +
    '","Level":"Information","MessageTemplate":"WorkerTransition","Properties":{"DiagnosticCode":"\uD800",' +
    '"EventId":{"Id":1201,"Name":"WorkerTransition"}}}')
$records.Add('{"Timestamp":"2026-11-22T12:00:00')
[IO.File]::WriteAllLines(
    (Join-Path $logDirectory "briosa-0123456789abcdef0123456789abcdef-20261122.jsonl"), $records)
foreach ($index in 1..200) {
    [Console]::Error.WriteLine("__MODE__-trace-line-$index")
    if ($index -ne 100) {
        continue
    }
    # Lines shaped like a gRPC library's trace records, each written with one call as
    # grpc-js writes a record, in the middle of the fixture's own stderr (#450). The
    # runner traces no client, so they are the fixture's own and reach every output
    # exactly as written.
    foreach ($line in @(__STDERR_SHAPES__)) {
        [Console]::Error.WriteLine([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($line)))
    }
}switch ("__MODE__") {
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
    "timeout-tree" {
        # A launcher whose child has a child of its own, as tsx starts the Node
        # process that runs a JavaScript fixture.
        # The grandchild runs a script with an ordinary argument shaped like a file
        # name, which the census must never print.
        $shell = (Get-Process -Id $PID).Path
        $sleeper = Join-Path (Split-Path -Parent $logDirectory) "sleeper.ps1"
        [IO.File]::WriteAllText($sleeper, "param([string]`$Label)`nStart-Sleep -Seconds 120`n")
        $child = "Start-Process -FilePath '$shell' -ArgumentList @('-NoProfile', '-File', '$sleeper', '-Label', 'review-only.ts') " +
            "-NoNewWindow; Start-Sleep -Seconds 120"
        Start-Process -FilePath $shell -ArgumentList @("-NoProfile", "-Command", $child) -NoNewWindow
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
            LegacyTrace = "dotnet"; Failure = "*report did not satisfy scenario 'default-ready'*" },
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
            Failure = "*failed scenario 'default-ready' with exit code 1*" },
        # The timeout census covers the fixture's whole process tree, here a launcher
        # with a child and a grandchild, as tsx starts Node (#450). The first revision's
        # census-failure switch and its client tracing switch are ignored by the
        # shipping runner, and the fixture's library-shaped lines are kept as written.
        [pscustomobject]@{ Name = "timeout-process-tree"; Mode = "timeout-tree"; Diagnostics = "directory"
            LegacyTrace = "py"; StderrShapes = $true; IgnoredSwitch = "1"; Failure = "*timed out after 5 s in scenario 'default-ready'*" },
        # A census that fails is reported and never hides the timeout. The failure is
        # injected into a test copy of the runner, never into the shipping one.
        [pscustomobject]@{ Name = "timeout-census-failure"; Mode = "timeout"; Diagnostics = "directory"
            CensusFailureCopy = $true; Failure = "*timed out after 5 s in scenario 'default-ready'*" },
        # The runner traces no client (#450). A fixture's lines shaped like a gRPC
        # library's trace records reach the saved stderr, the printed stderr and the
        # failure message exactly as written, whatever the caller's environment asked for.
        [pscustomobject]@{ Name = "exit-nonzero-library-shaped-stderr"; Mode = "exit-nonzero"; Diagnostics = "directory"
            LegacyTrace = "js"; StderrShapes = $true; Failure = "*failed scenario 'default-ready' with exit code 1*" })
    # The reviewer's inputs (#450), written by the fixture as its own stderr: a status
    # message with a quote inside it and no trailer announcing it, an only explanation
    # shaped like a tracer record, an interrupted multi-line record followed by
    # ordinary output, a header-shaped line, a blank line and a native log record.
    $stderrShapes = @(
        "D 2026-11-27T12:00:00.000Z | v1.14.6 4242 | resolving_call | [7] cancelWithStatus code: 1 details: `"part one`nquoted section`"`nstatus-tail-line`"",
        "D 2026-11-27T12:00:00.000Z | v1.14.6 4242 | resolving_call | [7] ended with status: code=1 details=`"part one`nquoted section`"`nstatus-tail-line`"",
        "D 2026-11-27T12:00:00.000Z | v1.14.6 4242 | resolving_call | fixture-only-explanation: expected PermissionDenied",
        "D 2026-11-27T12:00:00.000Z | v1.14.6 4242 | resolving_call | [9] ended with status: code=14 details=`"interrupted first line",
        "fixture-error-after-partial-record",
        "    at fixture.ts:42",
        "`t`tx_test: header-shaped-fixture-line",
        "",
        "I1126 20:55:21.000001    4242 fixture.cc:42] native-style-fixture-explanation")
    # The switches the removed client tracing set; the fixture fails if one reaches it.
    $traceSwitches = @("GRPC_NODE_TRACE", "GRPC_NODE_VERBOSITY", "GRPC_TRACE", "GRPC_VERBOSITY",
        "DOTNET_EnableEventPipe", "DOTNET_EventPipeOutputPath", "DOTNET_EventPipeConfig")
    $unaryCallAuditKey = "Logging__LogLevel__Briosa.Server.Services.UnaryCallAuditInterceptor"
    $previousEnvironment = @{}
    foreach ($name in @("BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY", "Briosa__Worker__StartupTimeout",
            "BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT", "DOTNET_EnableDiagnostics", "DOTNET_EnableDiagnostics_IPC",
            "BRIOSA_CONFORMANCE_CLIENT_TRACE", "BRIOSA_CONFORMANCE_TEST_FAIL_PROCESS_TREE", $unaryCallAuditKey) + $traceSwitches) {
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
            $shapes = @(if (& $property "StderrShapes") { $stderrShapes })
            $censusFailureCopy = [bool](& $property "CensusFailureCopy")
            $auditPath = $(if (& $property "Audit") { Join-Path $temporaryRoot "digest-audit-$caseName.txt" })
            Set-EnvironmentValue "BRIOSA_CONFORMANCE_DIGEST_AUDIT" $auditPath
            # A caller's environment that still asks for the removed client tracing.
            Set-EnvironmentValue "BRIOSA_CONFORMANCE_CLIENT_TRACE" (& $property "LegacyTrace")
            foreach ($name in $traceSwitches) {
                Set-EnvironmentValue $name $null
            }
            Set-EnvironmentValue "BRIOSA_CONFORMANCE_TEST_FAIL_PROCESS_TREE" (& $property "IgnoredSwitch")
            Set-EnvironmentValue $unaryCallAuditKey $null
            $failingFixturePath = Join-Path $temporaryRoot "failing-fixture-$caseName.ps1"
            # The runner gives the fake worker start 30 s unless the caller set a bound.
            [IO.File]::WriteAllText(
                $failingFixturePath,
                $failingFixtureSource.Replace("__MODE__", $case.Mode).Replace(
                    "__STARTUP__", $(if ($null -ne $callerStartupTimeout) { $callerStartupTimeout } else { "00:00:30" })).Replace(
                    "__FLOOD__", $(if ($null -ne $flood) { [string]$flood } else { "0" })).Replace(
                    "__STDERR_SHAPES__", (@($shapes | ForEach-Object {
                                "'" + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($_)) + "'" }) -join ", ")),
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
                    FixtureTimeoutSeconds = $(if ($case.Mode -like "timeout*") { 5 } else { 30 })
                }
                $caseRunner = $runnerPath
                if ($censusFailureCopy) {
                    # A copy beside the shipping runner, so it finds the package, whose
                    # census throws.
                    $caseRunner = Join-Path (Split-Path -Parent $runnerPath) "Invoke-BriosaClientConformance.census-failure.ps1"
                    $anchor = "    `$root = `$ProcessTable[`$RootId]"
                    $runnerText = [IO.File]::ReadAllText($runnerPath)
                    Assert-Condition ($runnerText.Contains($anchor)) "The census-failure injection point is missing from the runner."
                    [IO.File]::WriteAllText($caseRunner,
                        $runnerText.Replace($anchor, "    throw `"The census failed (injected by the package tests).`"`n$anchor"),
                        [Text.UTF8Encoding]::new($false))
                }
                try {
                    & $caseRunner @failingRunnerArguments *>&1 |
                        ForEach-Object { $runnerOutput.Add([string]$_) }
                }
                finally {
                    if ($censusFailureCopy) {
                        Remove-Item -LiteralPath $caseRunner -Force -ErrorAction SilentlyContinue
                    }
                }
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
                ($case.Mode -notlike "timeout*" -or $timingLine[0] -like "*, fixture timed out.")) `
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
                $runnerMarks[1] -like "+* s Runner $(if ($case.Mode -like 'timeout*') { 'FixtureTimedOut' } else { 'FixtureEnd' })" -and
                @(for ($index = 1; $index -lt $offsets.Count; $index++) {
                        if ($offsets[$index] -lt $offsets[$index - 1]) { $index }
                    }).Count -eq 0) `
                "The conformance runner did not place its fixture marks in the server log digest's timeline for $caseName."
            $warnings = @($digestEvents | Where-Object { $_ -match '^\S+ s Warning ' })
            $ordinary = @($digestEvents | Where-Object { $_ -notmatch '^\S+ s Warning ' })
            Assert-Condition ($ordinary.Count -eq 200 -and
                @($warnings | Where-Object {
                    $_ -like "* Warning WorkerStartPhase Phase=startup-bound-expired ElapsedMilliseconds=10000 Generation=*" }).Count -eq 3 -and
                @($ordinary | Where-Object { $_ -like "+* s Information ReadinessNotReady" }).Count -eq 186 -and
                @($ordinary | Where-Object { $_ -like "+* s Information LifecycleRpcStarted Rpc=LaunchSpatialAnalyzer" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s UnknownLevel LifecycleRpcStarted Rpc=StopSpatialAnalyzerSdk" }).Count -eq 1 -and
                $ordinary[-1] -like "+* s Information WorkerTransition DiagnosticCode=<withheld>" -and
                # The oversized-record placeholder takes the time of the record before it.
                $digestEvents[[Array]::IndexOf([string[]]$digestEvents, $ordinary[-1]) + 1] -like "+* s Warning LogRecordTooLarge" -and
                $digest -contains "(64 earlier lower-severity records omitted; every warning and error is kept)" -and
                # The two truncated lines, the undecodable record and the marker file's
                # line, none admitted.
                $digest -contains "(4 malformed server log records skipped)") `
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
                # Escaped governed names are read; a differently cased name is not governed.
                @($warnings | Where-Object { $_ -like "* Warning WorkerTransition OperationId=<withheld> DiagnosticCode=<withheld>" }).Count -eq 1 -and
                @($warnings | Where-Object { $_ -like "* Warning WorkerTransition DiagnosticCode=worker-starting" }).Count -eq 1 -and
                @($runnerOutput | Where-Object { $_ -clike "*review-case-marker*" -or $_ -clike "*escaped-unlisted*" -or $_ -clike "*escaped_operations*" }).Count -eq 0 -and
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
                Assert-Condition ($warnings.Count -eq $flood + 15 -and $floodWarnings.Count -eq $flood -and
                    $floodGenerations[0] -eq 100001 -and $floodGenerations[-1] -eq 100000 + $flood -and
                    @(for ($index = 1; $index -lt $floodGenerations.Count; $index++) {
                            if ($floodGenerations[$index] -ne $floodGenerations[$index - 1] + 1) { $index }
                        }).Count -eq 0 -and
                    @($digest | Where-Object { $_ -like "*warnings and errors omitted*" }).Count -eq 0) `
                    "The conformance runner did not keep every warning for $caseName."
            }
            else {
                Assert-Condition ($warnings.Count -eq 15) `
                    "The conformance runner did not keep every warning for $caseName."
            }
            # The unary call records (#450) print with the digest's listed names only.
            Assert-Condition (
                @($ordinary | Where-Object { $_ -like "+* s Debug UnaryCallStarted Rpc=GetServerInfo" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s Debug UnaryCallCompleted Rpc=GetServerInfo GrpcStatus=OK ElapsedMilliseconds=1" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s Debug UnaryCallResponseEnded Rpc=GetServerInfo GrpcStatus=OK ElapsedMilliseconds=2" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s Debug UnaryCallStarted OperationId=file_operations.get_working_directory" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s Debug UnaryCallFaulted Rpc=<withheld> ElapsedMilliseconds=3" }).Count -eq 1 -and
                @($ordinary | Where-Object { $_ -like "+* s Debug UnaryCallResponseAbandoned Rpc=StopSpatialAnalyzerSdk ElapsedMilliseconds=4" }).Count -eq 1) `
                "The conformance runner did not print the server's unary call records for $caseName."
            if ($case.Mode -like "timeout*") {
                $censusStart = [Array]::IndexOf([string[]]$runnerOutput,
                    "--- processes alive at the fixture timeout in default-ready (start times from fixture start) ---")
                $censusEnd = [Array]::IndexOf([string[]]$runnerOutput, "--- end processes for default-ready ---")
                Assert-Condition ($censusStart -ge 0 -and $censusEnd -gt $censusStart) `
                    "The conformance runner did not record the processes alive at the fixture timeout for $caseName."
                $census = @($runnerOutput[$censusStart..$censusEnd])
                $lineTail = 'started [+-][0-9]+\.[0-9]{3} s, CPU [0-9]+\.[0-9]{2} s \(\+[0-9]+\.[0-9]{2} s in [0-9]+\.[0-9] s\), threads [1-9][0-9]* \([0-9]+ [a-z]+(?:, [0-9]+ [a-z]+)*\)$'
                if ($censusFailureCopy) {
                    Assert-Condition ($census -contains "(the fixture's process tree could not be recorded)" -and
                        @($census | Where-Object { $_ -cmatch '^fixture pid [0-9]+: started [+-][0-9]+\.[0-9]{3} s, CPU [0-9]+\.[0-9]{2} s$' }).Count -eq 1) `
                        "The conformance runner did not fall back to the launched fixture when its process tree census failed."
                }
                else {
                    # Each process is named by its executable alone: no script, argument
                    # or path from its command line.
                    $fixtureLine = @($census | Where-Object { $_ -cmatch "^fixture pid [0-9]+ \(pwsh\.exe\): parent [0-9]+, $lineTail" })
                    Assert-Condition ($fixtureLine.Count -eq 1 -and $census -notcontains "(the fixture's process tree could not be recorded)" -and
                        @($census | Where-Object { $_ -like "*.ps1*" -or $_ -like "*failing-fixture*" }).Count -eq 0) `
                        "The conformance runner did not record the fixture with its parent, CPU sample and threads for $caseName."
                    if ($case.Mode -eq "timeout-tree") {
                        $fixturePid = [regex]::Match($fixtureLine[0], '^fixture pid ([0-9]+) ').Groups[1].Value
                        $childLine = @($census | Where-Object { $_ -cmatch "^  child pid [0-9]+ \(pwsh\.exe\): parent $fixturePid, $lineTail" })
                        $childPid = $(if ($childLine.Count -eq 1) { [regex]::Match($childLine[0], '^  child pid ([0-9]+) ').Groups[1].Value })
                        Assert-Condition ($childLine.Count -eq 1 -and
                            @($census | Where-Object { $_ -cmatch "^    child pid [0-9]+ \(pwsh\.exe\): parent $childPid, $lineTail" }).Count -eq 1 -and
                            @($census | Where-Object { $_ -like "*sleeper*" -or $_ -like "*review-only*" }).Count -eq 0) `
                            "The conformance runner did not record the fixture's child and grandchild by executable alone: $($census -join ' | ')"
                    }
                }
                $savedCensus = @(Get-Content -LiteralPath (Join-Path (Join-Path $diagnosticsRoot "default-ready") "timeout-processes.txt"))
                Assert-Condition (@($census | Where-Object { $savedCensus -notcontains $_ }).Count -eq 0) `
                    "The conformance runner did not keep the processes alive at the fixture timeout for $caseName."
            }
            # The runner traces no client (#450): no trace, call summary or EventPipe
            # file anywhere, and nothing printed about one, whatever the caller's
            # environment asked for. The fixture checked that no trace switch reached it.
            $scenarioDiagnosticsRoot = Join-Path $diagnosticsRoot "default-ready"
            Assert-Condition (@($runnerOutput | Where-Object { $_ -like "*client gRPC*" -or $_ -like "*client trace*" }).Count -eq 0 -and
                @(if (Test-Path -LiteralPath $diagnosticsRoot -PathType Container) {
                        Get-ChildItem -LiteralPath $diagnosticsRoot -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
                            $_.Name -in @("client-trace.txt", "client-calls.txt") -or $_.Extension -eq ".nettrace" }
                    }).Count -eq 0) `
                "The conformance runner traced a client for $caseName."
            if ($shapes.Count -gt 0) {
                # The fixture's stderr exactly as it wrote it, library-shaped lines and all:
                # saved byte for byte (trimmed, as before, unless the fixture timed out),
                # printed line for line, and carried whole by an exit-code failure.
                $written = @(1..100 | ForEach-Object { "$($case.Mode)-trace-line-$_" }) + $shapes +
                    @(101..200 | ForEach-Object { "$($case.Mode)-trace-line-$_" })
                $expectedStderr = ($written | ForEach-Object { $_ + [Environment]::NewLine }) -join ""
                if ($case.Mode -notlike "timeout*") {
                    $expectedStderr = $expectedStderr.Trim()
                }
                $savedStderrPath = Join-Path $scenarioDiagnosticsRoot "fixture.stderr.txt"
                $printedStart = [Array]::IndexOf([string[]]$runnerOutput, "--- client fixture stderr for default-ready ---")
                $printedEnd = [Array]::IndexOf([string[]]$runnerOutput, "--- end client fixture stderr for default-ready ---")
                Assert-Condition ([string]::Equals([IO.File]::ReadAllText($savedStderrPath), $expectedStderr, [StringComparison]::Ordinal) -and
                    $printedStart -ge 0 -and $printedEnd -gt $printedStart -and
                    [string]::Equals(($runnerOutput[($printedStart + 1)..($printedEnd - 1)] -join "`n"),
                        ([IO.File]::ReadAllLines($savedStderrPath) -join "`n"), [StringComparison]::Ordinal) -and
                    ($case.Mode -like "timeout*" -or "$failure".EndsWith(": $expectedStderr", [StringComparison]::Ordinal))) `
                    "The conformance runner did not keep the fixture's stderr exactly as written for $caseName."
            }
            # No ordinary argument from a command line reaches the failure message, the
            # printed output or any saved diagnostic file.
            if ($case.Diagnostics -eq "directory") {
                $savedText = [string]::Join("`n", @(Get-ChildItem -LiteralPath $scenarioDiagnosticsRoot -File -Recurse -ErrorAction SilentlyContinue |
                        Where-Object { $_.Extension -in @(".txt") } | ForEach-Object { [IO.File]::ReadAllText($_.FullName) }))
                foreach ($marker in @("review-only", "sleeper")) {
                    Assert-Condition (-not $savedText.Contains($marker, [StringComparison]::Ordinal) -and
                        -not "$failure".Contains($marker, [StringComparison]::Ordinal) -and
                        @($runnerOutput | Where-Object { $_.Contains($marker, [StringComparison]::Ordinal) }).Count -eq 0) `
                        "The conformance runner kept or printed '$marker' for $caseName."
                }
            }            if ($null -ne $auditPath) {
                # The audit names the scenario, the property and how many unlisted values
                # it held, never a value: the planted host name reaches no output.
                $auditText = Get-Content -LiteralPath $auditPath -Raw
                Assert-Condition ([string]::Equals($auditText,
                        "default-ready DiagnosticCode 5$([Environment]::NewLine)default-ready OperationId 3$([Environment]::NewLine)",
                        [StringComparison]::Ordinal) -and
                    @(foreach ($marker in @($sensitiveMarkers) + @("secret-build-host", "unlisted_operations", "worker-starting",
                                "review-case-marker", "escaped-unlisted", "escaped_operations")) {
                            if ($auditText.Contains($marker)) { $marker } }).Count -eq 0) `
                    "The conformance runner did not audit only counts for $caseName."
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
            # The fixture's 200 numbered lines; a traced case also keeps its genuine lines.
            $savedError = @(Get-Content -LiteralPath (Join-Path $scenarioDiagnostics "fixture.stderr.txt") |
                Where-Object { $_ -like "$($case.Mode)-trace-line-*" })
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

    # A missing digest vocabulary, or one with a list missing or empty, withholds every
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
        foreach ($variant in @("missing", "partial", "empty")) {
            if ($variant -eq "missing") {
                Remove-Item -LiteralPath $vocabularyPath -Force
            }
            else {
                $complete = Get-Content -LiteralPath $vocabularyBackup -Raw | ConvertFrom-Json
                $variantDocument = [ordered]@{
                    schema_version = 1
                    diagnostic_codes = @($complete.diagnostic_codes)
                }
                if ($variant -eq "empty") {
                    $variantDocument["operation_ids"] = @()
                }
                [IO.File]::WriteAllText($vocabularyPath, ($variantDocument | ConvertTo-Json -Depth 3),
                    [Text.UTF8Encoding]::new($false))
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
            -not (Test-Path -LiteralPath $lockAudit)) `
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
