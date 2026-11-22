# First-party client conformance host

This target-owned test composition runs the real public Briosa gRPC server over
deterministic fake worker and application processes. It lets the .NET, Python,
and JavaScript/TypeScript repositories exercise their supported public clients
through identical process-level lifecycle, compatibility, failure, recovery,
and cleanup scenarios.

The composition is not a SpatialAnalyzer emulator. It does not reproduce
geometry, application state, devices, or general MP behavior. It supplies only
the deterministic responses required to validate Briosa client boundaries. It
requires Windows x64 but no SpatialAnalyzer installation, SDK binary, license,
vendor documentation, or proprietary data.

The v1/scenarios.json file is handwritten test source. It is not a generated
operation manifest and is deliberately excluded from the public protocol
artifact.

Each client repository owns a thin fixture that accepts --scenario and
--contract arguments, uses only that package's public API, prints one value-free
JSON result, and exits. The package runner owns fake-process configuration and
exact cleanup so clients never recreate fake MP semantics.

The fixture report contract is deliberately small:

```json
{"schema_version":1,"contract_id":"briosa.first-party-client.v1","scenario":"default-ready","success":true}
```

The runner rejects a failed report, a mismatched scenario or contract, a fixture
timeout, termination of an externally owned fake application, and any server or
worker process left behind by the fixture. A client repository normally invokes
the packaged runner from its own test script:

```powershell
./runner/Invoke-BriosaClientConformance.ps1 `
  -FixtureCommand dotnet `
  -FixtureArguments @("path/to/Client.Conformance.dll")
```

Whatever rejects a scenario, including a fixture that exits successfully with a
missing, invalid, or unsuccessful report, the runner prints the fixture's
complete stderr. If `-DiagnosticsDirectory` or the
`BRIOSA_CONFORMANCE_DIAGNOSTICS_DIRECTORY` environment variable names a
directory, the runner also copies the fixture's stdout and stderr, the server's
log files, and the failure message to `<directory>/<scenario>`. The runner
points each scenario's server log files at that scenario's temporary directory.
A fixture that passes explicit logging options to its client overrides that
location.

After the stderr, a rejected scenario's output also shows:

- the runner's timing marks: scenario start, fixture start, and fixture end,
  in UTC;
- a digest of the scenario's server log, one line per event: the time from the
  fixture start, the level, the event name, and only these properties: `Rpc`,
  `GrpcStatus`, `Phase`, `ElapsedMilliseconds`, `WorkerState`,
  `ApplicationState`, `Ownership`, `Generation`, `ConnectionState`,
  `OperationId`, `ExecutionDisposition`, `MpResultCode`, and `DiagnosticCode`.
  The digest keeps the last 200 events and never drops a warning or an error.
  The runner's fixture start and end marks appear in the same timeline as
  `Runner` lines. The server writes its file log unbuffered, so the log still
  holds its events after a forced kill. The digest is also saved as
  `server-log-digest.txt` with the other diagnostics.

The server's lifecycle records give the digest its phase timeline: when the
endpoint started listening, each worker start sub-phase, and each lifecycle
RPC's arrival and completion with its elapsed time. A scenario that passes
prints the same phases on its result line, for example
`Passed client conformance scenario: default-ready (fixture 5.1 s; server listening +1.2 s; worker start 1140 ms; launch 520 ms; connect 300 ms)`.
A phase that the scenario does not reach is left out.

`-FixtureTimeoutSeconds` defaults to 180 seconds, longer than the first-party
clients' 120-second startup budget. A slow start or a hang then ends in the
client's own typed startup timeout, which names the pending call. If the
fixture still times out, the runner first prints the package processes that are
still alive (fixture, server, worker, and fake application), with their start
times and CPU times, and only then kills them. Set
`BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT=1` together with a diagnostics directory to
also write heap dumps of the server and of a .NET fixture to
`<directory>/<scenario>/dumps` before the kill. Nothing needs installing: the
runner sends each process's .NET runtime the same dump request that
`dotnet-dump collect` sends over the runtime's diagnostics channel, and the
runtime writes the dump with the `createdump.exe` beside it. The package's
server ships one. Current runtimes no longer accept a process ID when
`createdump.exe` is run directly. A process without a .NET diagnostics
channel, such as a Node.js or Python fixture, is reported and skipped.
`dotnet-dump analyze` (`clrstack -all`, `dumpasync`) shows the blocked frames.
The server is dumped first. A process is paused while its dump is written,
typically for a few seconds, and the processes keep running until the runner
kills them, so digest events after the `FixtureTimedOut` mark can show that
pause. A heap dump is typically 25 to 300 MB, so upload the folder only when a
job fails.

The runner gives every scenario's server `Briosa__Worker__StartupTimeout=00:00:30`.
The fake SDK host tests client behavior, not start latency, and the first fake
worker start after a fresh package extraction can miss the server's 10-second
default on a slow runner. A caller that sets `Briosa__Worker__StartupTimeout`
itself keeps its value, for example to reproduce that timeout.

A scenario may declare `minimum_contract_major`. The package includes it only
once the target's compatibility contract reaches that major, and it removes the
field from the packaged contract. Retained clients for the current major never
receive a scenario their fixture cannot know. `sdk-call-faulted` (#294),
`mp-partial-success` (#351), and `lifecycle-cancellation` (#305) are defined
with `minimum_contract_major: 3`. This target declares contract major 3, so
its package includes all three, and major-3 client fixtures must support them.

In `mp-partial-success`, the fake worker returns MP code 4 (PARTIAL SUCCESS)
with the working directory for the first execution, code 2 for the second, and
code 4 without its output for the third. The fixture calls
`get_working_directory` three times and asserts, through its public API, that
the first two calls return the directory without an error, that the first
call's MP completion is `PartialSuccess` with code 4, and that the second
call's is `Succeeded` with code 2. The third call fails with the typed output
retrieval failure (DATA_LOSS, `OUTPUT_RETRIEVAL_FAILURE`, Completed,
DoNotReplay), and its MP execution details still report `PartialSuccess` with
code 4.

In `lifecycle-cancellation`, the runner starts the external fake application,
and the fake worker holds its reply to the first Connect for 3 seconds. The
fixture starts in `control-plane-only` mode, drives the SDK lifecycle itself,
and asserts through its public API that:

1. after it starts the SDK, a Connect with a deadline of about 1 second
   fails with the language's native deadline or cancellation outcome. The
   client invents no execution disposition and does not retry;
2. a second Connect, sent at once, fails with the typed lifecycle error:
   ABORTED, `sdk-lifecycle-transition-in-progress`, and recovery guidance
   REFRESH_STATE. The server finishes the first Connect on its own once it has
   accepted it;
3. refreshing SDK state, polling for at most about 20 seconds, shows the same
   SDK generation ready for MP work, with no incident: the detached Connect
   finished without a fault or quarantine;
4. `get_working_directory` then succeeds; and
5. the fixture stops the SDK and shuts down cleanly. The runner checks that the
   external fake application is still running.

The deadline and the hold leave room on both sides. A slow runner has about a
second for the server to accept the first Connect before the deadline expires,
and the second Connect then has about 2 seconds before the hold ends. The hold
stays well inside the server's `StartupTimeout`, 30 seconds here, which bounds an
accepted Connect, and its 5-second heartbeat timeout.

The `capability-denied` scenario runs the packaged `standard` profile with one
per-operation override that denies `file_operations.get_working_directory`.
Fixtures for every contract major assert that the operation is absent from the
admitted capability set and that calling it returns the typed policy denial.
Major-3 fixtures also assert, through their public API, that the admission
profile is `standard`, that every admitted capability carries a reviewed
duration class and validation status, and that the diagnostic listing of denied
operations (`include_denied`) reports `get_working_directory` as denied by an
override. The server sends these discovery fields to every client; earlier
fixtures ignore them.

Scenario definitions describe only portable setup and the behavior to exercise.
A scenario's `watchdog_timeout` must leave headroom for a cold worker's first
execution on a slow or busy runner. The `hang-first-execute` worker never
returns from its scripted hang, so a longer budget only lengthens the scenario,
while a tight one can expire on a legitimate first execution (#302).
The fixture remains responsible for asserting its language-specific public
states, results, and exception types before it returns `success: true`.
