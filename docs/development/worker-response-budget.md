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
lists were measured through their real mappers with a 1,048,576-byte frame of
zeros (`EveryDoubleListResponseAtTheBudgetFitsTheClientsDefaultReceiveLimit`):

| Operation | Public response | Under 4,194,304 |
| --- | --- | --- |
| `robot_operations.get_robot_pose_for_a_frame` | 4,192,355 | 1,949 |
| `robot_operations.get_calibration_appliance_data` | 4,192,349 | 1,955 |
| `robot_calibration_appliance_node_operations.get_calibration_appliance_node_data` | 4,192,349 | 1,955 |
| `variables.get_named_double_list_variable` | 4,192,318 | 1,986 |
| `instrument_operations.get_instrument_base_uncertainty_covariance_matrix_wrt_world` | 4,190,627 | 3,677 |
| `instrument_operations.get_last_solved_tcp_fixture_uncertainty_covariance_matrix` | 4,190,627 | 3,677 |
| `instrument_operations.calculate_tcp_fixture_uncertainties` | 4,189,330 | 4,974 |

Other output kinds encode to less protobuf than JSON. The gRPC receive limit
applies to the message length; the 5-byte length prefix is outside it. An
independent review confirmed with the six published 0.5.0 packages that each
accepts a message of exactly 4,194,304 bytes and rejects 4,194,305.

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

**Worker encoding is bounded.** The channel encodes into `BoundedFrameBuffer`,
which rents only 64 KiB chunks and stops the encoder once the frame is over its
budget. `WorkerControlStringConverter` writes long strings in 1,024-character
segments, so the encoder's escaping scratch stays below 19 KiB for any string.
For a 1 MiB budget, encoding one result holds at most 24 chunks (1.5 MiB),
however large the result. That ceiling comes from the chunk tails that
escape-heavy text can leave unused.

| Measured (warm pool, 1 MiB budget) | Allocated | Largest rental |
| --- | --- | --- |
| 1,048,000 ASCII characters, delivered | 1.11 MB of chunks on first use | 64 KiB |
| 20,000,000 ASCII characters, rejected | 18 KB | 64 KiB |
| 3,000,000 `<` characters, rejected | 137 KB | 64 KiB |
| 5,000,000 zeros, rejected | 18 KB | 64 KiB |
| SDK-fault fallback with a 20,000,000-character output | 18 KB | 64 KiB |

Before this change, the same 1,048,000 characters rented a 4 MiB buffer, and
3,000,000 characters rented 16 MiB before the size check rejected them.
`WorkerControlEncodingTests` asserts that each rejection above allocates under
512 KiB. Its strings include escaped, non-ASCII and split-surrogate text, and
the test asserts that the segmented bytes equal a single string write.

**What the worker does not bound.**
- *SDK materialization:* SpatialAnalyzer's value in CLR form and the
  `ImmutableArray` copy each list value makes.
- *Sized getters:* a getter with an `ArraySize` allocates `new double[size]`
  before the SDK call. That is up to 8,000,000 bytes at the 1,000,000 cap,
  whatever the SDK returns.

Bounding these needs paging or a different SDK marshaling path; both are out
of scope ([#245](https://github.com/spatialanalyzer/briosa/issues/245)).

**Host per result.** Receive, decode, map to protobuf and serialize:

| Shape | Allocation |
| --- | --- |
| Single-digit double list | 21.0 to 28.75 × the frame, measured at 64 KiB, 260 KiB, 384 KiB, 512 KiB, 540,000 bytes and 1 MiB |
| One-character string list | about 15.8 × |
| Point-name list | about 6.3 × |
| ASCII text | about 4 × |
| Full-precision doubles | about 3.3 × |

The double-list ratio depends on `List<T>`'s capacity steps, and no ceiling has
been established. Peak live memory is estimated at about 12 × the frame, not
measured.

**Not accounted.** The retained-work admission budget counts requests only. A
response being decoded, mapped or held in Kestrel's output pipe for a slow
client counts against nothing. Concurrent responses are therefore not bounded
by any Briosa budget. A follow-up issue tracks this.

## Conformance

The optional conformance scenario `large-result` shows that a client receives
a working directory just under 1 MiB whole. The fixture reports the UTF-8
length and SHA-256 of what it received, and the runner checks both against the
contract. A fixture runs it only by asking for it (`-OptionalScenario`), so the
published fixture revisions, which do not know it, keep passing. The
retained-client gate proves the published 0.5.0 packages once each client
repository has a fixture revision that runs it and `fixtureSources` in
`compatibility/retained-clients.json` pins that revision. The packaged client
scenarios (`eng/Test-ClientScenarios.ps1`) already run it against this
repository's generated .NET client.

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
