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
    # #450: a soak run repeats one accepted job of the committed plan, unchanged, once per
    # iteration, optionally narrowed to one scenario that the job runs.
    $soakSource = @($committed.include | Where-Object { $_.language -ceq 'js' -and $_.target -ceq '2024.1.0508.5' -and $_.version -ceq '0.5.1' })
    $soak = Get-RetainedClientSoakMatrix -Plan $committed -Client 'js/2024.1.0508.5/0.5.1' -Iterations 3 -Scenario 'capability-denied' -RepositoryRoot $repositoryRoot
    if ($soakSource.Count -ne 1 -or $soak.include.Count -ne 3 -or (@($soak.include | ForEach-Object { $_.iteration }) -join ',') -cne '1,2,3' -or
        @($soak.include | Where-Object { $_.soakScenario -cne 'capability-denied' }).Count -ne 0 -or
        @($soak.include | Where-Object {
                $item = $_
                @($soakSource[0].Keys | Where-Object {
                        (ConvertTo-Json -InputObject $item[$_] -Compress -Depth 8) -cne (ConvertTo-Json -InputObject $soakSource[0][$_] -Compress -Depth 8)
                    }).Count -ne 0 }).Count -ne 0) {
        throw 'A soak plan must repeat the committed job unchanged, once per iteration.'
    }
    $whole = Get-RetainedClientSoakMatrix -Plan $committed -Client 'py/2026.1.0529.7/0.5.1' -Iterations 50 -RepositoryRoot $repositoryRoot
    $optional = Get-RetainedClientSoakMatrix -Plan $committed -Client 'dotnet/2024.1.0508.5/0.5.0' -Iterations 1 -Scenario 'large-result' -RepositoryRoot $repositoryRoot
    if ($whole.include.Count -ne 50 -or @($whole.include | Where-Object { $_.soakScenario -cne '' }).Count -ne 0 -or $optional.include.Count -ne 1) {
        throw 'A soak plan must run every scenario of the job, or an optional scenario its fixture requests.'
    }
    foreach ($case in @(
            @{ Client = 'js/2024.1.0508.5/0.5.1'; Iterations = 0; Message = 'A soak run takes 1 to 50 iterations.' },
            @{ Client = 'js/2024.1.0508.5/0.5.1'; Iterations = 51; Message = 'A soak run takes 1 to 50 iterations.' },
            @{ Client = 'js/2024.1.0508.5'; Iterations = 1; Message = 'A soak run names one client as language/target/version.' },
            @{ Client = 'js/2024.1.0508.5/9.9.9'; Iterations = 1; Message = 'The retained-client plan has no job for client js/2024.1.0508.5/9.9.9.' },
            @{ Client = 'js/2024.1.0508.5/0.4.0'; Iterations = 1; Message = 'A soak run repeats an accepted client; the gate expects js/2024.1.0508.5/0.4.0 to be rejected.' },
            @{ Client = 'js/2024.1.0508.5/0.5.1'; Iterations = 1; Scenario = 'no-such-scenario'; Message = 'A soak run of js/2024.1.0508.5/0.5.1 can repeat only a scenario its job runs.' },
            @{ Client = 'js/2024.1.0508.5/0.5.1'; Iterations = 1; Scenario = 'Capability-Denied'; Message = 'A soak run of js/2024.1.0508.5/0.5.1 can repeat only a scenario its job runs.' })) {
        $failure = $null
        try {
            Get-RetainedClientSoakMatrix -Plan $committed -Client $case.Client -Iterations $case.Iterations `
                -Scenario $(if ($case.ContainsKey('Scenario')) { $case.Scenario } else { '' }) -RepositoryRoot $repositoryRoot | Out-Null
        }
        catch { $failure = $_.Exception.Message }
        if ($failure -cne $case.Message) { throw "Expected the soak rejection '$($case.Message)', got '$failure'." }
    }
    # A soak of one scenario passes only with one validated pass of it and the soak package's
    # own end, so a run that reported more, or that ended normally and could have written
    # evidence, fails.
    $soakEnd = "This soak run repeated conformance scenario 'capability-denied' only, so it is not compatibility evidence."
    Assert-SoakScenarioPass -Scenario 'capability-denied' -RunEnd $soakEnd `
        -RunnerOutput @('Running only conformance scenario', 'Passed client conformance scenario: capability-denied (fixture 3.6 s)')
    foreach ($case in @(
            @{ Output = @(); End = $soakEnd; Message = 'A soak of capability-denied must pass exactly that one scenario; the runner passed 0.' },
            @{ Output = @('Passed client conformance scenario: capability-denied (fixture 3.6 s)', 'Passed client conformance scenario: default-ready (fixture 1.0 s)')
                End = $soakEnd; Message = 'A soak of capability-denied must pass exactly that one scenario; the runner passed 2.' },
            @{ Output = @('Passed client conformance scenario: default-ready (fixture 1.0 s)'); End = $soakEnd
                Message = 'A soak of capability-denied must pass exactly that one scenario; the runner passed 1.' },
            @{ Output = @('Passed client conformance scenario: capability-denied (fixture 3.6 s)'); End = $null
                Message = 'A soak of capability-denied must end with the soak package''s own error, not: ' },
            @{ Output = @('Passed client conformance scenario: capability-denied (fixture 3.6 s)'); End = 'The client fixture timed out.'
                Message = 'A soak of capability-denied must end with the soak package''s own error, not: The client fixture timed out.' })) {
        $failure = $null
        try { Assert-SoakScenarioPass -Scenario 'capability-denied' -RunnerOutput $case.Output -RunEnd $case.End }
        catch { $failure = $_.Exception.Message }
        if ($failure -cne $case.Message) { throw "Expected the soak check '$($case.Message)', got '$failure'." }
    }
    # The soak workflow's own steps (#450), run from the workflow file. Its preflight
    # plans an accepted dispatch and rejects any other before the package is built.
    # Neither it nor the summary prints a rejected value, since the runner reads
    # workflow commands such as ::error:: from the log.
    Import-Module (Join-Path $PSScriptRoot 'WorkflowYaml.psm1') -Force
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $soakWorkflow = ConvertFrom-WorkflowYaml -Path (Join-Path $repositoryRoot '.github/workflows/conformance-soak.yml')
    $stepRoot = Join-Path $temporary 'soak-steps'
    $null = New-Item -ItemType Directory -Path $stepRoot
    $preflightPath = Join-Path $stepRoot 'preflight.ps1'
    $summaryPath = Join-Path $stepRoot 'summary.ps1'
    [IO.File]::WriteAllText($preflightPath, @($soakWorkflow.jobs.package.steps | Where-Object { $_['name'] -ceq 'Check the dispatch inputs' })[0]['run'])
    [IO.File]::WriteAllText($summaryPath, @($soakWorkflow.jobs.summary.steps | Where-Object { $_['name'] -ceq 'Write the summary' })[0]['run'])
    $shell = (Get-Process -Id $PID).Path
    function Invoke-SoakStep([string]$Script, [hashtable]$Environment, [string]$WorkingDirectory) {
        $saved = @{}
        foreach ($name in $Environment.Keys) {
            $saved[$name] = [Environment]::GetEnvironmentVariable($name)
            [Environment]::SetEnvironmentVariable($name, $Environment[$name])
        }
        Push-Location -LiteralPath $WorkingDirectory
        try {
            $output = & $shell -NoProfile -File $Script 2>&1 | Out-String
            [pscustomobject]@{ Exit = $LASTEXITCODE; Output = $output }
        }
        finally {
            # A rejected step's exit code must not become this script's own.
            $global:LASTEXITCODE = 0
            Pop-Location
            foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
        }
    }
    function Assert-NothingEchoed([string]$Text, [string]$Case) {
        if ($Text.Contains('injected', [StringComparison]::Ordinal) -or
            @($Text -split '\r?\n' | Where-Object { $_.TrimStart().StartsWith('::', [StringComparison]::Ordinal) }).Count -gt 0) {
            throw "The soak workflow printed a rejected input for $Case."
        }
    }
    $dispatch = @{ ITERATIONS = '20'; LANGUAGE = 'js'; TARGET = '2024.1.0508.5'; CLIENT_VERSION = '0.5.1'; SOAK_SCENARIO = '' }
    foreach ($case in @(
            @{ Change = @{}; Runs = 'The soak will run 20 iterations of js/2024.1.0508.5/0.5.1.' },
            @{ Change = @{ ITERATIONS = '1'; SOAK_SCENARIO = 'capability-denied' }; Runs = 'The soak will run 1 iterations of js/2024.1.0508.5/0.5.1.' },
            @{ Change = @{ ITERATIONS = '50'; LANGUAGE = 'py'; TARGET = '2026.1.0529.7' }; Runs = 'The soak will run 50 iterations of py/2026.1.0529.7/0.5.1.' })) {
        $environment = $dispatch.Clone()
        foreach ($name in $case.Change.Keys) { $environment[$name] = $case.Change[$name] }
        $result = Invoke-SoakStep $preflightPath $environment $repositoryRoot
        if ($result.Exit -ne 0 -or -not $result.Output.Contains($case.Runs, [StringComparison]::Ordinal)) {
            throw "The soak preflight did not accept a valid dispatch: $($result.Output)"
        }
    }
    $countMessage = 'The iterations input must be a whole number from 1 to 50, without leading zeroes.'
    foreach ($case in @(
            @{ Change = @{ ITERATIONS = '020' }; Message = $countMessage },
            @{ Change = @{ ITERATIONS = '008' }; Message = $countMessage },
            @{ Change = @{ ITERATIONS = '0' }; Message = $countMessage },
            @{ Change = @{ ITERATIONS = '51' }; Message = $countMessage },
            @{ Change = @{ ITERATIONS = '-1' }; Message = $countMessage },
            @{ Change = @{ ITERATIONS = '2.5' }; Message = $countMessage },
            @{ Change = @{ ITERATIONS = '1e1' }; Message = $countMessage },
            @{ Change = @{ ITERATIONS = '' }; Message = $countMessage },
            @{ Change = @{ ITERATIONS = "20`n::error::injected-count" }; Message = $countMessage },
            @{ Change = @{ LANGUAGE = "js`n::error::injected-language" }; Message = 'The language input must be js, dotnet or py.' },
            @{ Change = @{ TARGET = '::error::injected-target' }; Message = 'The target input must be an exact SpatialAnalyzer target.' },
            @{ Change = @{ CLIENT_VERSION = "0.5.1`n::error::injected-client-version" }; Message = 'The client-version input must be a version, such as 0.5.1.' },
            @{ Change = @{ CLIENT_VERSION = '::error::injected-client-version' }; Message = 'The client-version input must be a version, such as 0.5.1.' },
            @{ Change = @{ SOAK_SCENARIO = "capability-denied`n::error::injected-scenario" }; Message = 'The scenario input must be empty or a scenario ID, such as capability-denied.' },
            @{ Change = @{ CLIENT_VERSION = '9.9.9' }; Message = 'The retained-client plan has no job for client js/2024.1.0508.5/9.9.9.' },
            @{ Change = @{ CLIENT_VERSION = '0.4.0' }; Message = 'A soak run repeats an accepted client; the gate expects js/2024.1.0508.5/0.4.0 to be rejected.' },
            @{ Change = @{ SOAK_SCENARIO = 'no-such-scenario' }; Message = 'A soak run of js/2024.1.0508.5/0.5.1 can repeat only a scenario its job runs.' })) {
        $environment = $dispatch.Clone()
        foreach ($name in $case.Change.Keys) { $environment[$name] = $case.Change[$name] }
        $label = ($case.Change.GetEnumerator() | ForEach-Object { "$($_.Key) $($_.Value -replace '[^A-Za-z0-9.-]', '?')" }) -join ', '
        $result = Invoke-SoakStep $preflightPath $environment $repositoryRoot
        if ($result.Exit -eq 0 -or -not $result.Output.Contains($case.Message, [StringComparison]::Ordinal)) {
            throw "The soak preflight did not reject $label with '$($case.Message)': $($result.Output)"
        }
        Assert-NothingEchoed $result.Output "the preflight with $label"
    }
    # The summary of a soak whose preflight rejected its inputs: a fixed message only.
    $summaryDispatch = @{ ITERATIONS = '20'; LANGUAGE = 'js'; TARGET = '2024.1.0508.5'; CLIENT_VERSION = '0.5.1'; SCENARIO = ''
        SOAK_RESULT = 'skipped'; DOWNLOAD = 'failure' }
    foreach ($change in @(@{ SCENARIO = "capability-denied`n::error::injected-scenario" }, @{ CLIENT_VERSION = '::error::injected-client-version' },
            @{ ITERATIONS = '020' })) {
        $environment = $summaryDispatch.Clone()
        foreach ($name in $change.Keys) { $environment[$name] = $change[$name] }
        $summaryFile = Join-Path $stepRoot "summary-$([Guid]::NewGuid().ToString('N')).md"
        $environment.GITHUB_STEP_SUMMARY = $summaryFile
        $result = Invoke-SoakStep $summaryPath $environment $stepRoot
        $written = [IO.File]::ReadAllText($summaryFile)
        if ($result.Exit -eq 0 -or -not $written.Contains('The dispatch inputs were not valid, so no iteration ran.', [StringComparison]::Ordinal)) {
            throw "The soak summary did not report invalid dispatch inputs: $($result.Output)"
        }
        Assert-NothingEchoed ($result.Output + $written) 'the summary of a rejected dispatch'
    }

    Write-Host "Verified the #250 support window, retired-client identities, per-entry fixture pins, fixture optional scenarios, the large-result floor and its gate, compatible coverage, the published-client release gate, the $($expectedJobs.Count) jobs the committed inventory plans for Server 0.10.1, the soak plan, and the soak workflow's input checks."
}
finally {
    $resolved = [IO.Path]::GetFullPath($temporary)
    if (-not $resolved.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -or $resolved -eq $temporaryBase) {
        throw 'Refusing cleanup outside the temporary policy directory.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}