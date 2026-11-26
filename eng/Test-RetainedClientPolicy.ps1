Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RetainedClientPolicy.psm1') -Force

$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporary = Join-Path $temporaryBase "briosa-retained-policy-$([Guid]::NewGuid().ToString('N'))"
$policyPath = Join-Path $temporary 'policy.json'
function Write-Policy {
    param([hashtable]$Value)
    $Value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $policyPath -Encoding utf8
}
function Get-Plan {
    param([hashtable]$Value, [switch]$Release)
    Write-Policy $Value
    return Get-RetainedClientMatrix -Path $policyPath -ServerVersion '0.10.0' -RepositoryRoot $temporary -AllowSourceCandidates:(-not $Release)
}
function Assert-Rejected {
    param([hashtable]$Value, [string]$Message, [switch]$Release)
    $failure = $null
    try { Get-Plan $Value -Release:$Release | Out-Null }
    catch { $failure = $_.Exception.Message }
    if ($null -eq $failure) { throw "An invalid compatibility policy was accepted. Expected: $Message" }
    if (-not $failure.StartsWith($Message, [StringComparison]::Ordinal)) { throw "Expected the rejection '$Message', got '$failure'." }
}
function Copy-Policy {
    param([hashtable]$Value)
    return ($Value | ConvertTo-Json -Depth 12 | ConvertFrom-Json -AsHashtable)
}
function New-PublishedClient {
    param([string]$Language, [string]$Target, [string]$Version, [int]$Major)
    $hostName = @{ dotnet = 'api.nuget.org'; js = 'registry.npmjs.org'; py = 'files.pythonhosted.org' }[$Language]
    return @{
        language = $Language; repository = "spatialanalyzer/briosa-$Language"; target = $Target
        version = $Version; sourceRevision = 'a' * 40; sha256 = 'b' * 64
        url = "https://$hostName/test-package-$Version"; fileName = "test-package-$Version.nupkg"
        requiredContract = @{ major = $Major; minimumRevision = 0 }
    }
}
# The workflow's policy step serializes the plan, and the job passes toJSON of its list to the
# gate. Round-trip the same way, so a one-element list must survive as a JSON array.
function Get-WorkflowScenarioJson {
    param([hashtable]$Item)
    $job = (@{ include = @($Item) } | ConvertTo-Json -Depth 8 -Compress | ConvertFrom-Json).include[0]
    if ($job.PSObject.Properties.Name -cnotcontains 'requiredOptionalScenarios') { return 'null' }
    return ConvertTo-Json -InputObject $job.requiredOptionalScenarios -Compress
}
function Invoke-Gate {
    param([hashtable]$Item, [string[]]$RunnerOutput, [string]$Json = (Get-WorkflowScenarioJson $Item))
    Assert-OptionalScenarioPasses -RequiredScenarioJson $Json -RunnerOutput $RunnerOutput -ClientVersion $Item.version -ExpectIncompatible $Item.expectIncompatible
}
function Assert-GateRejected {
    param([hashtable]$Item, [string[]]$RunnerOutput, [string]$Message, [string]$Json = (Get-WorkflowScenarioJson $Item))
    $failure = $null
    try { Invoke-Gate $Item $RunnerOutput $Json }
    catch { $failure = $_.Exception.Message }
    if ($failure -cne $Message) { throw "Expected the gate failure '$Message', got '$failure'." }
}
try {
    $languages = @('dotnet', 'js', 'py')
    $targets = @('2026.1.0529.7', '2024.1.0508.5')
    $offeredScenarios = '{"optional_scenarios":[{"id":"large-result"}]}'
    foreach ($target in $targets) {
        $directory = Join-Path $temporary "targets/$target"
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        # Mirrors the current servers: Server 0.10.1 declares 3.1, and clients requiring 3.0 still run.
        '{"schemaVersion":1,"major":3,"revision":1}' | Set-Content (Join-Path $directory 'compatibility.json')
        [IO.Directory]::CreateDirectory((Join-Path $directory 'conformance/client/v1')) | Out-Null
        $offeredScenarios | Set-Content (Join-Path $directory 'conformance/client/v1/scenarios.json')
    }
    # The current fixture builds the major-3 source candidates and requests large-result. The
    # earlier fixture is the one that tested the published previous-major packages, so they keep
    # it as their own pin. It requests no optional scenario.
    $current = @{ dotnet = '1' * 40; js = '2' * 40; py = '3' * 40 }
    $earlier = @{ dotnet = '4' * 40; js = '5' * 40; py = '6' * 40 }
    $clients = @()
    $retired = @()
    $candidates = @()
    foreach ($language in $languages) {
        foreach ($target in $targets) {
            foreach ($version in @('0.2.0', '0.3.0')) { $retired += New-PublishedClient $language $target $version 1 }
            $previous = New-PublishedClient $language $target '0.4.0' 2
            $previous.fixtureSourceRevision = $earlier[$language]
            $clients += $previous
            $candidates += @{
                language = $language; repository = "spatialanalyzer/briosa-$language"; target = $target
                version = '0.5.0'; sourceRevision = $current[$language]
                requiredContract = @{ major = 3; minimumRevision = 0 }
            }
        }
    }
    $valid = @{
        schemaVersion = 3; clients = $clients; retiredClients = $retired; candidates = $candidates
        fixtureSources = @($languages | ForEach-Object { @{ language = $_; sourceRevision = $current[$_]; optionalScenarios = @('large-result') } })
        historicalFixtureSources = @($languages | ForEach-Object { @{ language = $_; sourceRevision = $earlier[$_] } })
    }

    # Development: six previous-major rejections on their own fixture pins and six source
    # candidates on the current fixture. Retired clients never run. Only accepted jobs require
    # their fixture's optional scenarios.
    $plan = Get-Plan $valid
    $rejections = @($plan.include | Where-Object expectIncompatible)
    $compatible = @($plan.include | Where-Object { -not $_.expectIncompatible })
    if ($plan.include.Count -ne 12 -or $rejections.Count -ne 6 -or $compatible.Count -ne 6 -or
        @($rejections | Where-Object { $_.version -cne '0.4.0' -or -not $_.published -or $_.fixtureSourceRevision -cne $earlier[$_.language] }).Count -ne 0 -or
        @($compatible | Where-Object { $_.version -cne '0.5.0' -or $_.published -or $_.fixtureSourceRevision -cne $current[$_.language] }).Count -ne 0) {
        throw 'The development matrix must run six pinned previous-major rejections and six current-fixture candidates, and no retired client.'
    }
    if (@($rejections | Where-Object { (Get-WorkflowScenarioJson $_) -cne '[]' }).Count -ne 0 -or
        @($compatible | Where-Object { (Get-WorkflowScenarioJson $_) -cne '["large-result"]' }).Count -ne 0) {
        throw 'Accepted jobs must require their fixture optional scenarios, and rejections none.'
    }
    Assert-Rejected $valid 'Server 0.10.0 cannot release until compatible clients are published' -Release

    $invalidRegistry = 'Retained packages must identify a hash-pinned public registry artifact.'
    $invalidIdentity = 'Invalid client identity or compatibility requirement.'
    $invalidCandidate = 'Source candidates must match the tested fixture revision and server contract.'
    $duplicate = 'Duplicate client release.'
    $outsideWindow = 'Live published clients must require the current or the previous contract major.'
    $notRetirable = 'Only clients of an older contract major can be retired.'
    $unknownPin = 'Published client fixture pins must name a current or historical fixture source of the same language.'
    $candidatePin = 'Source candidates use the current fixture source and cannot pin another.'
    $invalidHistory = 'Historical fixture sources must pin distinct immutable revisions.'
    $invalidOptional = 'Fixture optional scenarios must list distinct scenario IDs.'
    $unofferedOptional = 'Fixture optional scenarios must be offered by the scenario contract of every exact target.'
    $currentFloor = 'Current fixture sources must list large-result in optionalScenarios.'
    $cases = @(
        # Schema 3 only.
        @{ message = 'Unsupported retained-client policy.'; change = { param($p) $p.schemaVersion = 2 } }
        @{ message = 'Unsupported retained-client policy.'; change = { param($p) $p.Remove('retiredClients') } }
        # Previous-major rejections alone prove no compatible client.
        @{ message = 'Test a compatible client for every language and exact target.'; change = { param($p) $p.candidates = @() } }
        @{ message = 'Retain published clients for all languages and exact targets.'; change = { param($p) $p.clients = @($p.clients[0..4]) } }
        # Immutable identities and pins.
        @{ message = $duplicate; change = { param($p) $p.clients += $p.clients[0] } }
        @{ message = $invalidRegistry; change = { param($p) $p.clients[0].sha256 = 'invalid' } }
        @{ message = $invalidRegistry; change = { param($p) $p.clients[0].url = 'http://api.nuget.org/test-package' } }
        @{ message = $invalidIdentity; change = { param($p) $p.clients[0].sourceRevision = 'main' } }
        @{ message = $invalidIdentity; change = { param($p) $p.clients[0].requiredContract.major = 0 } }
        @{ message = 'Fixture sources must pin one immutable revision per language.'; change = { param($p) $p.fixtureSources[0].sourceRevision = 'main' } }
        @{ message = $invalidCandidate; change = { param($p) $p.candidates[0].sourceRevision = 'd' * 40 } }
        @{ message = $invalidCandidate; change = { param($p) $p.candidates[0].requiredContract.minimumRevision = 2 } }
        @{ message = $invalidCandidate; change = { param($p) $p.candidates[0].requiredContract.major = 2 } }
        # Retired records keep the published identity rules.
        @{ message = $invalidRegistry; change = { param($p) $p.retiredClients[0].sha256 = 'invalid' } }
        @{ message = $invalidRegistry; change = { param($p) $p.retiredClients[0].url = 'https://example.org/test-package' } }
        @{ message = $invalidRegistry; change = { param($p) $p.retiredClients[0].fileName = 'test-package.zip' } }
        @{ message = $invalidRegistry; change = { param($p) $p.retiredClients[0].Remove('url') } }
        @{ message = $invalidIdentity; change = { param($p) $p.retiredClients[0].sourceRevision = 'main' } }
        # A release appears in one list only.
        @{ message = $duplicate; change = { param($p) $p.retiredClients += $p.clients[0] } }
        @{ message = $duplicate; change = { param($p) $p.retiredClients += $p.retiredClients[0] } }
        # The #250 support window: live clients require the current or the previous major, and
        # only older clients can be retired.
        @{ message = $outsideWindow; change = { param($p) $p.clients += $p.retiredClients[0]; $p.retiredClients = @($p.retiredClients | Select-Object -Skip 1) } }
        @{ message = $outsideWindow; change = { param($p) $p.clients[0].requiredContract.major = 4 } }
        @{ message = $notRetirable; change = { param($p) $p.retiredClients[0].requiredContract.major = 3 } }
        @{ message = $notRetirable; change = { param($p) $p.retiredClients[0].requiredContract.major = 4 } }
        # A per-entry fixture pin names a known fixture of the same language. Candidates have none.
        @{ message = $unknownPin; change = { param($p) $p.clients[0].fixtureSourceRevision = 'd' * 40 } }
        @{ message = $unknownPin; change = { param($p) $p.clients[0].fixtureSourceRevision = $earlier.js } }
        @{ message = $unknownPin; change = { param($p) $p.retiredClients[0].fixtureSourceRevision = 'd' * 40 } }
        @{ message = $candidatePin; change = { param($p) $p.candidates[0].fixtureSourceRevision = $current.dotnet } }
        @{ message = $candidatePin; change = { param($p) $p.candidates[0].fixtureSourceRevision = $earlier.dotnet } }
        @{ message = $invalidHistory; change = { param($p) $p.historicalFixtureSources[0].sourceRevision = 'main' } }
        @{ message = $invalidHistory; change = { param($p) $p.historicalFixtureSources += $p.fixtureSources[0] } }
        # A fixture's optional scenarios are distinct contract IDs that both targets offer.
        @{ message = $invalidOptional; change = { param($p) $p.fixtureSources[0].optionalScenarios = @() } }
        @{ message = $invalidOptional; change = { param($p) $p.fixtureSources[0].optionalScenarios = 'large-result' } }
        @{ message = $invalidOptional; change = { param($p) $p.fixtureSources[0].optionalScenarios = @('large-result', 'large-result') } }
        @{ message = $invalidOptional; change = { param($p) $p.fixtureSources[0].optionalScenarios = @('Large_Result') } }
        @{ message = $invalidOptional; change = { param($p) $p.fixtureSources[0].optionalScenarios = @(1) } }
        @{ message = $unofferedOptional; change = { param($p) $p.fixtureSources[0].optionalScenarios = @('large-result', 'huge-result') } }
        @{ message = $unofferedOptional; change = { param($p) $p.historicalFixtureSources[0].optionalScenarios = @('huge-result') } }
        @{ message = $unofferedOptional; change = { param($p) $p.fixtureSources[1].optionalScenarios = @('large-reslut') } }
        # The floor in the module: the data cannot drop large-result from a current fixture.
        @{ message = $currentFloor; change = { param($p) $p.fixtureSources[1].Remove('optionalScenarios') } }
        @{ message = $currentFloor; change = { param($p) $p.fixtureSources[2].Remove('optionalScenarios'); $p.historicalFixtureSources[2].optionalScenarios = @('large-result') } }
    )
    foreach ($case in $cases) {
        $changed = Copy-Policy $valid
        & $case.change $changed
        Assert-Rejected $changed $case.message
    }
    # Both targets must offer the scenario, since a fixture tests both.
    $secondContract = Join-Path $temporary "targets/$($targets[1])/conformance/client/v1/scenarios.json"
    '{"scenarios":[]}' | Set-Content $secondContract
    try { Assert-Rejected $valid $unofferedOptional }
    finally { $offeredScenarios | Set-Content $secondContract }

    # A published client may also pin the current fixture of its language.
    $changed = Copy-Policy $valid
    $changed.clients[0].fixtureSourceRevision = $current.dotnet
    $item = @((Get-Plan $changed).include | Where-Object { $_.language -ceq 'dotnet' -and $_.target -ceq $changed.clients[0].target -and $_.version -ceq '0.4.0' })
    if ($item.Count -ne 1 -or $item[0].fixtureSourceRevision -cne $current.dotnet) { throw 'A published client must run with its own fixture pin.' }

    # Release: the published 0.5.0 packages replace the candidates, and the published 0.5.1
    # packages join them. Without a pin both run on the current fixture, beside the pinned
    # previous-major rejections.
    $published = Copy-Policy $valid
    foreach ($candidate in $published.candidates) {
        foreach ($version in @('0.5.0', '0.5.1')) {
            $client = New-PublishedClient $candidate.language $candidate.target $version 3
            $client.sourceRevision = $candidate.sourceRevision
            $published.clients += $client
        }
    }
    $published.candidates = @()
    $release = Get-Plan $published -Release
    $accepted = @($release.include | Where-Object { -not $_.expectIncompatible })
    if ($release.include.Count -ne 18 -or @($release.include | Where-Object { -not $_.published }).Count -ne 0 -or
        @($release.include | Where-Object { $_.version -ceq '0.4.0' -and $_.expectIncompatible -and $_.fixtureSourceRevision -ceq $earlier[$_.language] -and (Get-WorkflowScenarioJson $_) -ceq '[]' }).Count -ne 6 -or
        @($accepted | Where-Object { $_.version -ceq '0.5.0' -and $_.fixtureSourceRevision -ceq $current[$_.language] -and (Get-WorkflowScenarioJson $_) -ceq '["large-result"]' }).Count -ne 6 -or
        @($accepted | Where-Object { $_.version -ceq '0.5.1' -and $_.fixtureSourceRevision -ceq $current[$_.language] -and (Get-WorkflowScenarioJson $_) -ceq '["large-result"]' }).Count -ne 6) {
        throw 'Published positive and negative pairs must both remain covered, and every accepted job must require large-result.'
    }

    # The workflow gate: an accepted 0.5.0 or 0.5.1 job passes only when the runner printed its
    # large-result pass line exactly once. A pass line for another scenario does not count.
    $passLine = 'Passed client conformance scenario: large-result (startup 1.1 s)'
    $otherLines = @('Passed client conformance scenario: control-plane-only', 'Passed client conformance scenario: large-result-extended', '')
    foreach ($version in @('0.5.0', '0.5.1')) {
        $job = @($accepted | Where-Object { $_.version -ceq $version -and $_.language -ceq 'py' -and $_.target -ceq $targets[0] })[0]
        $missing = "Client $version must pass large-result exactly once."
        Invoke-Gate $job ($otherLines + $passLine)
        Assert-GateRejected $job $otherLines $missing
        Assert-GateRejected $job @() $missing
        Assert-GateRejected $job ($otherLines + $passLine + $passLine) $missing
        # The floor in the module: a list lost on the way to the job does not lower it.
        Assert-GateRejected $job @() "The retained-client plan for accepted client $version must require large-result." -Json '[]'
        Assert-GateRejected $job @($passLine) "The retained-client plan for accepted client $version must require large-result." -Json '["default-ready"]'
    }
    $job051 = @($accepted | Where-Object { $_.version -ceq '0.5.1' })[0]
    $planGap = 'The retained-client plan must list the optional scenarios each job requires.'
    Assert-GateRejected $job051 @($passLine) $planGap -Json 'null'
    Assert-GateRejected $job051 @($passLine) $planGap -Json '"large-result"'
    # A rejection job requests no optional scenario and requires none.
    $rejection = @($release.include | Where-Object expectIncompatible)[0]
    Invoke-Gate $rejection @()
    Assert-GateRejected $rejection @($passLine) 'A rejection job must require no optional scenario.' -Json '["large-result"]'

    # The requirement follows the fixture a client runs with, above the floor in the module:
    # pinning an accepted 0.5.0 or later client to a fixture that does not request large-result
    # rejects the plan. Below the floor, such a pin requires nothing.
    $pinFloor = 'Accepted client 0.5.1 jobs must run on a fixture that lists large-result in optionalScenarios.'
    $pinnedPolicy = Copy-Policy $published
    $pinnedClient = @($pinnedPolicy.clients | Where-Object { $_.version -ceq '0.5.1' -and $_.language -ceq 'js' })[0]
    $pinnedClient.fixtureSourceRevision = $earlier.js
    Assert-Rejected $pinnedPolicy $pinFloor -Release
    $pinnedClient.version = '0.5.0-dev.1'
    Assert-Rejected $pinnedPolicy 'Accepted client 0.5.0-dev.1 jobs must run on a fixture that lists large-result in optionalScenarios.' -Release
    $pinnedClient.version = '0.4.9'
    $pinnedJob = @((Get-Plan $pinnedPolicy -Release).include | Where-Object { $_.version -ceq '0.4.9' })
    if ($pinnedJob.Count -ne 1 -or $pinnedJob[0].expectIncompatible -or (Get-WorkflowScenarioJson $pinnedJob[0]) -cne '[]') {
        throw 'Below the floor, a client pinned to a fixture without optional scenarios must require none.'
    }
    Invoke-Gate $pinnedJob[0] @()
    $pinnedClient.version = '0.5.1'
    $earlierFixture = @($pinnedPolicy.historicalFixtureSources | Where-Object { $_.language -ceq 'js' })[0]
    $earlierFixture.optionalScenarios = @('large-result')
    $pinnedPlan = (Get-Plan $pinnedPolicy -Release).include
    $pinnedJob = @($pinnedPlan | Where-Object { $_.version -ceq '0.5.1' -and $_.language -ceq 'js' -and $_.target -ceq $pinnedClient.target })
    if ($pinnedJob.Count -ne 1 -or (Get-WorkflowScenarioJson $pinnedJob[0]) -cne '["large-result"]') {
        throw 'A client pinned to a historical fixture must require that fixture optional scenarios.'
    }
    # The js 0.4.0 rejections use that same fixture and still require none.
    $pinnedRejections = @($pinnedPlan | Where-Object { $_.version -ceq '0.4.0' -and $_.language -ceq 'js' })
    if ($pinnedRejections.Count -ne 2 -or @($pinnedRejections | Where-Object { -not $_.expectIncompatible -or (Get-WorkflowScenarioJson $_) -cne '[]' }).Count -ne 0) {
        throw 'A rejection must require no optional scenario, whichever fixture it runs with.'
    }

    # After that release, the previous-major clients may be retired too.
    $published.retiredClients += @($published.clients | Where-Object { $_.version -ceq '0.4.0' })
    $published.clients = @($published.clients | Where-Object { $_.version -cne '0.4.0' })
    $release = Get-Plan $published -Release
    if ($release.include.Count -ne 12 -or @($release.include | Where-Object expectIncompatible).Count -ne 0) {
        throw 'Retiring the previous-major clients must leave only the current-major release pairs.'
    }

    # The committed inventory. Its Server 0.10.1 release plan must be exactly these jobs, so
    # dropping or changing a committed entry fails here. Update the expected table with every
    # change to compatibility/retained-clients.json.
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $committed = Get-RetainedClientMatrix -Path (Join-Path $repositoryRoot 'compatibility/retained-clients.json') -ServerVersion '0.10.1' -RepositoryRoot $repositoryRoot
    $expectedJobs = @(foreach ($version in @('0.4.0', '0.5.0', '0.5.1')) {
            foreach ($language in $languages) {
                foreach ($target in $targets) {
                    $rejected = $version -ceq '0.4.0'
                    '{0} {1} {2} published=True {3} requires={4}' -f $language, $target, $version,
                        $(if ($rejected) { 'rejected' } else { 'accepted' }), $(if ($rejected) { '[]' } else { '["large-result"]' })
                }
            }
        })
    $plannedJobs = @($committed.include | ForEach-Object {
            '{0} {1} {2} published={3} {4} requires={5}' -f $_.language, $_.target, $_.version, $_.published,
                $(if ($_.expectIncompatible) { 'rejected' } else { 'accepted' }), (Get-WorkflowScenarioJson $_)
        })
    $missingJobs = @($expectedJobs | Where-Object { $plannedJobs -cnotcontains $_ })
    $unexpectedJobs = @($plannedJobs | Where-Object { $expectedJobs -cnotcontains $_ })
    if ($missingJobs.Count -ne 0 -or $unexpectedJobs.Count -ne 0 -or $plannedJobs.Count -ne $expectedJobs.Count) {
        throw ("The committed retained-client inventory must plan the $($expectedJobs.Count) expected Server 0.10.1 jobs; it plans $($plannedJobs.Count). " +
            "Missing: $(if ($missingJobs.Count) { $missingJobs -join '; ' } else { 'none' }). Unexpected: $(if ($unexpectedJobs.Count) { $unexpectedJobs -join '; ' } else { 'none' }).")
    }
    # Each committed accepted job fails the gate when its list arrives empty or the runner
    # never passed large-result, and each committed rejection passes with no output.
    foreach ($job in @($committed.include)) {
        if ($job.expectIncompatible) { Invoke-Gate $job @(); continue }
        Assert-GateRejected $job @() "The retained-client plan for accepted client $($job.version) must require large-result." -Json '[]'
        Assert-GateRejected $job @() "Client $($job.version) must pass large-result exactly once."
    }
    Write-Host "Verified the #250 support window, retired-client identities, per-entry fixture pins, fixture optional scenarios, the large-result floor and its gate, compatible coverage, the published-client release gate, and the $($expectedJobs.Count) jobs the committed inventory plans for Server 0.10.1."
}
finally {
    $resolved = [IO.Path]::GetFullPath($temporary)
    if (-not $resolved.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -or $resolved -eq $temporaryBase) {
        throw 'Refusing cleanup outside the temporary policy directory.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}