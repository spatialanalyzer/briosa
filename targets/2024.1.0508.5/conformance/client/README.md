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
directory, the runner also saves the failure message, the server log digest,
the fixture's stdout and stderr, and the server's log files to
`<directory>/<scenario>`, and prints that absolute destination. The runner
points each scenario's server log files at that scenario's temporary directory.
A fixture that passes explicit logging options to its client overrides that
location.

Diagnostics never change a scenario's result or the runner's exit code. The
failure message and the digest are saved first, and every other file is copied
on its own, so a file that cannot be copied, for example a log another process
holds exclusively, is counted in a notice and loses nothing else. An unusable
diagnostics directory turns diagnostics off with a notice, and a destination
that cannot be written is reported without hiding the scenario's own failure.
If the runner cannot remove all of its temporary files at the end, it prints a
notice; a passing run still passes and a failing run keeps its own error.

After the stderr, a rejected scenario's output also shows:

- the runner's timing marks: scenario start, fixture start, and fixture end,
  in UTC;
- a digest of the scenario's server log, one line per event: the time from the
  fixture start, the level, the event name, and these properties only: `Rpc`,
  `GrpcStatus`, `Phase`, `ElapsedMilliseconds`, `WorkerState`,
  `ApplicationState`, `Ownership`, `Generation`, `ConnectionState`,
  `OperationId`, `ExecutionDisposition`, `MpResultCode`, and `DiagnosticCode`.

The digest reaches public CI logs, so it prints only values from reviewed
closed lists. Every comparison is ordinal, and the printed text is always the
list's own entry, never the record's:

- A known event ID maps to a fixed label. A record's message template or
  event name is never printed; a framework event prints as `FrameworkEvent`
  with its numeric ID, and anything else as `UnknownEvent`.
- The level must be a Serilog level name, or it prints as `UnknownLevel`.
- Each property is checked on its own. Numbers must be integers, and every
  other property must be on its list. The lists for enum-like properties are
  in the runner. The lists for `DiagnosticCode` and `OperationId` ship beside
  it in `runner/digest-vocabulary.json`. A value missing from them prints as
  `<withheld>`. If the file is missing, or fails validation (its schema
  version, both lists present and non-empty, and each entry's shape), both
  lists are left empty with a notice: every value of those two properties
  prints as `<withheld>`, and the rest of the digest and the runner marks still
  print.

The closed lists govern what the runner prints to stdout and stderr and writes
to the digest and the audit file. They do not govern the server log files the
runner copies to the diagnostics directory for a rejected scenario: those hold
whatever the server logged, as before, and this repository's retained-client
workflow uploads that directory when a job fails. The fixture's own stderr is
also printed in full, as before.

`digest-vocabulary.json` is maintained by hand. It lists only values that the
server, the worker or the fake worker can log in those two properties: the
diagnostic code literals in their sources, the
`worker-stop-<phase>-<outcome>` codes that
`WorkerProcessSupervisor.StopDiagnosticCode` composes, and the operation IDs
the scenarios call. Literals that only look like codes, such as scenario names
or protocol type tags, stay out. When a change adds a code, add it in both
targets. If a producer writes sensitive data into `DiagnosticCode` or
`OperationId`, correct the producer; do not list the value.

Set `BRIOSA_CONFORMANCE_DIGEST_AUDIT` to a file path to have the runner append,
for each scenario whose server log holds a `DiagnosticCode` or `OperationId`
that the vocabulary does not list, one `<scenario> <property> <count>` line.
The file never holds a value: an unlisted value may be anything, and the file
can reach public CI logs. A server log file that cannot be read is reported as
a notice, not in the file. This repository's retained-client gate sets it for
every scenario it runs with the retained clients and fails when the file is not
empty, so a code the server starts producing shows up as a failure there rather
than being withheld silently. To find the value, compare the vocabulary with
the diagnostic code literals in the sources and the codes
`WorkerProcessSupervisor.StopDiagnosticCode` composes, or with the operation IDs
the named scenario calls, and add it to `digest-vocabulary.json`. If the audit
file cannot be written, the runner prints a notice and the gate sees no misses.

The digest keeps the latest 200 lower-severity records by timestamp across all
of a scenario's log files, says how many earlier ones it omitted, and keeps
every warning and error. It reads each log file as a stream and skips any line
longer than 64 KiB as it streams past; server records are at most 16 KiB. Each
record is converted in full, with its top-level `Level`, before it enters
either path, so a truncated, malformed or undecodable record, such as the last
write of a killed server, never displaces a valid one and never reaches the
rendering; such records are counted in their own notice. Only the exact
top-level `Level` property decides a record's path: the last one wins if it is
repeated, an escaped property name is recognized, and a nested `Level` does not
count. Property names are compared ordinally, so a differently cased name is not
a governed property. Warnings and errors are spooled to the runner's temporary
directory in chunks of up to 4,096 records, or until a chunk reaches 8 Mi
characters, checked after each record is added. Each chunk is sorted by
timestamp and then reading order, and the merge holds one open reader and one
current record per chunk, so records that reached the log out of time order, as
concurrent producers can write them, still print in time order. The server's timestamp-free
`LogRecordTooLarge` placeholder for an oversized record is shown as a warning
at the time of the record before it. A file it cannot read is counted and
skipped. The runner's fixture start and end marks always appear in the same
timeline as `Runner` lines. The server writes its file log unbuffered, so the
log still holds its events after a forced kill. The digest is also saved as
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
fixture still times out, the runner first prints the processes that are still
alive (fixture, server, worker, and fake application), with their start times
and CPU times, and marks any that is not a verified descendant of the fixture.
These timeout diagnostics run in a separate copy of the runner under one
120-second deadline that covers every step, including the process query and
any file system work. The runner kills that copy if it overruns, and reports
whether the diagnostics stopped at the deadline or ended with an error. Only
then does it kill the fixture's process tree, whether or not the diagnostics
succeeded, and it reports a tree it could not terminate.

Set `BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT=1`, exactly, together with a
diagnostics directory to also write heap dumps to `<directory>/<scenario>/dumps`
before the kill:

- Only this scenario's own processes are dumped: the fixture the runner
  started, and a server only when its parent chain reaches that fixture. Each
  process is held open while it is checked, and the process snapshot must
  name the same process: its creation time, in UTC, must match the live
  process at the snapshot's microsecond precision, each parent must be older
  than its child, and the chain must end at the fixture within four steps. A
  server it cannot verify, for example one started by another caller sharing
  the package extraction, is reported and skipped.
- Nothing needs installing. The runner sends each process's .NET runtime the
  same dump request that `dotnet-dump collect` sends over the runtime's
  diagnostics channel, and the runtime writes the dump with the
  `createdump.exe` beside it. The package's server ships one. Current runtimes
  no longer accept a process ID when `createdump.exe` is run directly. Before
  sending the request, the runner checks that the process serving the
  connected diagnostics pipe is the verified process.
- A process without an enabled .NET diagnostics channel is reported and
  skipped: a Node.js or Python fixture, or one started with
  `DOTNET_EnableDiagnostics=0` or `DOTNET_EnableDiagnostics_IPC=0`.
- The server is dumped first. A request that does not finish by the deadline
  is reported as stopped by the deadline, and a pipe failure as a pipe fault.
  A process is paused while its dump is written, typically for a few seconds,
  and the processes keep running until the runner kills them, so digest events
  after the `FixtureTimedOut` mark can show that pause.
- `dotnet-dump analyze` (`clrstack -all`, `dumpasync`) shows the blocked frames.

A heap dump holds the process's memory, typically 25 to 300 MB. Enabling dumps
also opts them into whatever the calling workflow already does with the
diagnostics directory: this repository's retained-client workflow, for
example, uploads the whole directory as an artifact when a job fails. Enable
dumps only where that upload is acceptable.

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

## Optional scenarios

`optional_scenarios` in the contract lists scenarios that a fixture runs only
when it asks for them. The fixture's test script passes their IDs to the
runner with `-OptionalScenario`, for example `-OptionalScenario large-result`.
Without that parameter the runner runs only `scenarios`, so a fixture revision
that does not know an optional scenario never receives it. A script that lists
the scenarios it ran in its evidence should read both lists and record only the
optional ones it requested. An optional entry has the same fields as a scenario
and the same `minimum_contract_major` gate.

`large-result` (#399) shows that a client receives a result at the 1 MiB
worker response budget whole. The fake worker returns a working directory of
1,046,528 characters of a fixed ASCII pattern, so its private frame is just
under 1,048,576 bytes. The public response is a little larger and stays well
under the 4 MiB default receive limit of the first-party clients. The contract
publishes the UTF-8 length and SHA-256 of that text as
`expected_working_directory`. The fixture starts in `default` mode, calls
`get_working_directory` once, and:

1. asserts through its public API that the call succeeded, and that the UTF-8
   length and lowercase hexadecimal SHA-256 of the returned directory equal
   `expected_working_directory`;
2. adds them to its report, never the value itself:

   ```json
   {"schema_version":1,"contract_id":"briosa.first-party-client.v1","scenario":"large-result","success":true,"working_directory":{"utf8_length":1046528,"sha256":"<hex>"}}
   ```

The runner rejects the scenario unless the reported length and SHA-256 match
the contract. A fixture that discards the value cannot pass, so the scenario
shows that the client received all of it, not only that the call succeeded.
The packaged client scenarios (`eng/Test-ClientScenarios.ps1`) run the same
worker scenario against this repository's generated .NET client.

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
