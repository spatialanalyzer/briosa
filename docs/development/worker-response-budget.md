# Worker response budget

Server 0.10.1 raises the private worker response budget from 64 KiB to 1 MiB
([#399](https://github.com/spatialanalyzer/briosa/issues/399)). The public
inbound limit and the private request budget stay at 64 KiB. This page records
the limits, what the synthetic measurements showed, what is bounded and what
is not, and what the licensed session still has to measure. User-facing limits
are in each target's `docs/operations/windows-package.md`.

## Limits

| Limit | Value | Where |
| --- | --- | --- |
| Public inbound request message | 64 KiB | `PublicGrpcHosting.MaximumReceiveMessageBytes` |
| Private request frame (host to worker) | 64 KiB | `WorkerControlProtocol.MaximumRequestBytes` |
| Private response frame (worker to host) | 1 MiB | `WorkerControlProtocol.MaximumResponseBytes` |
| Public response message | no server limit | gRPC default `MaxSendMessageSize` |
| First-party client receive limit | 4 MiB | grpc-dotnet, grpc-js and grpcio defaults; the 0.5.0 clients set no channel options |

Private frames are JSON, and public messages are protobuf. A single-digit
double costs 2 JSON bytes and 8 protobuf bytes, so the densest public response
is about four times its private frame. All seven operations that return double
lists were measured through their real mappers
(`EveryDoubleListResponseAtTheBudgetFitsTheClientsDefaultReceiveLimit`). Each
used a frame at the budget, holding zeros in the smallest legal envelope:

| Operation | Public response | Under 4,194,304 |
| --- | --- | --- |
| `robot_operations.get_robot_pose_for_a_frame` | 4,192,459 | **1,845** |
| `robot_operations.get_calibration_appliance_data` | 4,192,453 | 1,851 |
| `robot_calibration_appliance_node_operations.get_calibration_appliance_node_data` | 4,192,453 | 1,851 |
| `variables.get_named_double_list_variable` | 4,192,430 | 1,874 |
| `instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world` | 4,190,739 | 3,565 |
| `instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix` | 4,190,739 | 3,565 |
| `instrument_operations.calculate_tcp_fixture_uncertainties` | 4,189,434 | 4,870 |

**The smallest legal envelope.** Every byte the envelope saves fits another
value, so the smallest envelope gives the largest response. Each field of the
private execution result falls into one of three groups:
- fixed by the protocol or the operation: version, kind, status,
  discriminators, the output names and kinds, and the `Connected` and
  `ExecutionReady` values;
- fixed in width: the correlation ID and the timestamp;
- at its minimum: every optional field the channel omits when null (the
  connection status code, runtime identity and diagnostic codes), the required
  connection diagnostic code at the one non-whitespace character the channel
  allows, and every number at one digit.

The test sends that envelope through the production channel, which accepts it.
An earlier envelope (status code 0, diagnostic `sdk-connected`) left 1,949
bytes. The independent review found the 1,845-byte margin with a smaller
envelope.

Other output kinds encode to less protobuf than JSON. The gRPC receive limit
applies to the message length; the 5-byte length prefix is outside it. An
independent review confirmed with the six published 0.5.0 packages that each
accepts a message of exactly 4,194,304 bytes and rejects 4,194,305.

## Getter faults report more retrieved outputs

When an output getter faults, the worker's fallback keeps every output's status
and drops only values that cannot be encoded: first any value alone over the
budget, then the largest, until the frame fits. With a 1 MiB budget, more
values fit than at 64 KiB, so a failed call can report more outputs as
retrieved.

`AnSdkGetterFaultReportsEveryRetrievedOutputThatFitsTheBudget` reproduces the
review's case: `GetObservationInfo` with Text outputs of 530,000 and 540,000
characters and a later getter faulting.
- With the 64 KiB budget, 6 of 9 outputs were retrieved.
- With 1 MiB, 7 are: the 530,000-character value fits, and only the larger one
  is reported as `worker-output-encoding-rejected`.
- Everything else is unchanged: `DATA_LOSS`, `SDK_CALL_FAULTED`, `Completed`,
  `DoNotReplay`, recovery `None`, MP code 2 and diagnostic
  `sdk-output-getter-faulted`. The next call succeeds on the same worker.

## Private protocol version

`WorkerControlProtocol.CurrentVersion` is 26 from Server 0.10.1, so that a
host and a worker built for different budgets cannot run a command together.
The worker's first message, `Ready`, carries its version, and the host rejects
any other version:
- the start ends `Degraded`, with diagnostic `worker-startup-failed`, lifecycle
  failure `StartupFailed` and a `StartFailed` incident;
- the host cleans up the worker;
- an execution request then returns `Unavailable` with `NotStarted`.

The worker likewise exits with code 4 on a host frame of another version
before it touches the SDK. Both directions are tested
(`WorkerProtocolVersionTests`,
`TheProductionWorkerRejectsAHostFrameOnAnotherProtocolVersion`). Host and
worker ship in the same package, so this cannot happen in a released
installation, and nothing public changes.

## Memory

Synthetic measurements, with no SpatialAnalyzer. They are allocation counters
from scratch probes, not committed tests. They measure neither peak live memory
nor responses that are still waiting to be sent over HTTP/2.

**Worker encoding is bounded.**
- The channel encodes into `BoundedFrameBuffer`, which rents only 64 KiB
  chunks and stops the encoder at most one write past the budget.
- `WorkerControlStringConverter` writes long strings in 1,024-character
  segments, so a write never asks for more than 18,435 bytes. The writer's
  escaping scratch is a pooled array of at most 16 KiB.
- For a 1 MiB budget, encoding one result holds at most 25 chunks: up to 24
  for the frame, whose tails a segment can leave unused, and one discard chunk
  (below). That is 1.56 MiB, however large the result. The most measured was
  23 chunks, for escape-heavy text.

| Measured (warm pool, 1 MiB budget) | Allocated | Chunks held |
| --- | --- | --- |
| 1,048,000 ASCII characters, delivered | 1.11 MB of chunks on first use | 17 |
| 20,000,000 ASCII characters, rejected | 18 KB | 18 |
| 174,000 `<` characters, delivered | not measured | 22 |
| 3,000,000 `<` characters, rejected | 137 KB | 23 |
| 5,000,000 zeros, rejected | 18 KB | 17 |
| SDK-fault fallback with a 20,000,000-character output | 18 KB | not measured |

Before this change, the same 1,048,000 characters rented a 4 MiB buffer, and
3,000,000 characters rented 16 MiB before the size check rejected them.
`WorkerControlEncodingTests` asserts two things:
- each rejection above allocates under 512 KiB;
- the segmented bytes equal a single string write, for escaped, non-ASCII and
  split-surrogate strings among others.

**Pool rentals are always returned.** The JSON writer rents its escaping
scratch inside a string write and returns it only when that write returns.
Throwing from the buffer at that moment would leave the rental unreturned: the
review found one 8,192-character rental left by rejecting 3,000,000 `<`
characters. So during a string write the buffer never throws. It hands the
writer a reusable discard chunk instead, and the converter rejects the frame as
soon as the write has returned. Outside string writes the buffer still throws
at once, because nothing is rented then.

`EveryPooledBufferIsReturned` records `ArrayPool` rent and return events on the
test thread for every path:
- rejected text, escaped text, double list and string list;
- the SDK-fault fallback;
- a delivered escaped frame;
- a failing stream;
- a cancelled send.

Rentals and returns must match. Before the fix it failed for the escaped
rejection and for the fallback with an escaped output.

**Segment size and throughput.** The review removed the unreturned rental with
16-character segments, which escape on the stack. That costs throughput, so
the 1,024-character segments stay. Measured with `Utf8JsonWriter` alone, in
MiB per second of JSON, noisy to about ±20%:

| Text | One string write | 1,024-character segments | 16-character segments |
| --- | --- | --- | --- |
| 1,000,000 ASCII characters | 1,874 to 2,921 | 254 to 261 | 138 |
| 170,000 `<` | 123 | 120 to 143 | 127 |
| 170,000 `é` | 182 to 184 | 183 to 269 | 155 |
| 300,000 characters of mixed markup | 313 to 414 | 351 to 459 | 97 |

16-character segments are up to 4.7 times slower on mixed text. Segmented
writes of plain ASCII are slower than one string write at any segment size:
about 3.4 ms per MB of text, against about 0.4 ms. The segments buy the bounded
scratch.

**What the worker does not bound.**
- *SDK materialization:* SpatialAnalyzer's value in CLR form and the
  `ImmutableArray` copy each list value makes.
- *Sized getters:* a getter with an `ArraySize` allocates `new double[size]`
  before the SDK call. That is up to 8,000,000 bytes at the 1,000,000 cap,
  whatever the SDK returns.

Bounding these needs paging or a different SDK marshaling path; both are out
of scope ([#245](https://github.com/spatialanalyzer/briosa/issues/245)).

**Host per result.** Receive, decode, map to protobuf and serialize, through
the real `OperationExecutor` and each operation's mapper. These are totals
allocated for one result, not peak memory in use:

| Result | Frame | Allocated | × frame |
| --- | --- | --- | --- |
| Six double lists of zeros (`GetInstrumentBaseUncertaintyCovarianceMatrixWrtWorld`; rows 262,145, 131,073, 65,537, 32,769, 16,385 and 15,914), 2026 warm | 1,048,575 | 30,153,648 (28.76 MiB) | 28.8 |
| The same, 2024 warm | 1,048,575 | 30,157,312 (28.76 MiB) | 28.8 |
| The same, first call in a fresh process, with one-time initialization | 1,048,575 | 36,651,128 to 37,077,792 | 35.0 to 35.4 |
| One double list of zeros (`GetNamedDoubleListVariable`) | 1,048,575 | 22,023,920 | 21.0 |
| The same, 266,239-byte frame, warm | 266,239 | 7,659,224 | 28.8 |

The row lengths come from the independent review, which measured 30,145,896
and 30,147,808 bytes for the six-list case. Each row length sits just above a
power of two, where `List<T>` growth is most wasteful. Other shapes allocate
less, measured at 64 KiB and 1 MiB:
- one-character string lists: about 15.8 ×;
- point-name lists: about 6.3 ×;
- ASCII text: about 4 ×;
- full-precision doubles: about 3.3 ×.

Peak live memory is estimated at about 12 × the frame, not measured.

**Not accounted.** The retained-work admission budget counts requests only.
A response being decoded, mapped or held in Kestrel's output pipe for a slow
client counts against nothing, so concurrent responses are not bounded by any
Briosa budget.
[#418](https://github.com/spatialanalyzer/briosa/issues/418) tracks this.

## Conformance

The optional conformance scenario `large-result` returns a working directory
just under 1 MiB. The fixture reports the UTF-8 length and SHA-256 of the value
its client returned. The runner checks both exactly: the length as a JSON
integer, compared as a number, and the digest as a string, compared ordinally.
The runner cannot see the value, so the scenario shows receipt only when the
fixture computes both from what its client actually received.

A fixture runs `large-result` only by asking for it (`-OptionalScenario`), so
the published fixture revisions, which do not know it, keep passing.

What shows receipt:
- **Today:** the packaged client scenarios (`eng/Test-ClientScenarios.ps1`).
  This repository's generated .NET client computes the length and digest from
  the value it received through the packaged server.
- **Not receipt:** `eng/Test-ClientConformancePackage.ps1` checks the runner
  with fixtures that echo the contract's values, and with malformed reports.
- **For the first-party clients:** once each client repository has a fixture
  revision that opts in and computes both values from the value it received,
  and `fixtureSources` in `compatibility/retained-clients.json` pins that
  revision, the retained-client gate shows it for the published 0.5.0 packages.

## Licensed gaps

Every figure above is synthetic. The licensed session
([#402](https://github.com/spatialanalyzer/briosa/issues/402)) should measure:

1. **Real result sizes** for the list-returning operations on realistic jobs,
   as private frame bytes per operation, and how many come near or over 1 MiB:
   - point-name and collection-object-name lists from large collections;
   - edit text and string lists;
   - the robot, calibration and covariance double lists.
2. **The worker's working set** for a result near 1 MiB, and for a sized getter
   at large `ArraySize` values. This covers the SDK materialization that the
   budget does not bound.
3. **Time** from `GetMPStepResult` to the public response for a near-1 MiB
   result, against the quick execution budget.
4. **Escaping**, from names with non-ASCII or HTML-sensitive characters, and
   how much it shrinks what fits.
5. **Whether any real result exceeds 1 MiB**, which would show as the completed
   `DataLoss` outcome, and for which operations.
