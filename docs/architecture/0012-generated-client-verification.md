# ADR 0012: Generated-client and licensed-SA verification boundary

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-22
- Issue: [#18](https://github.com/spatialanalyzer/briosa/issues/18)

## Context

Unit and worker-process tests prove Briosa's internal mappings, but they do not prove that a separately generated client can use the packaged Kestrel endpoint. The vertical slice also needs one controlled success check against the real SpatialAnalyzer SDK without making licensed software a prerequisite for ordinary builds or injecting destructive failure behavior into a real SA session.

The protected runner trust and licensing strategy is issue #20. This decision defines the reusable test payload that such a runner can invoke; it does not register a runner or expose licensed infrastructure to pull requests.

## Decision

Briosa includes a separate `Briosa.SmokeClient` executable that references only the public generated protocol and `Grpc.Net.Client`. It connects through loopback HTTP/2 and verifies discovery, exact-target compatibility, advertised capability identity, canonical gRPC status, typed Briosa error details, and the `GetWorkingDirectory` result shape.

The client emits only safe coordinates, state enums, booleans, and failure classifications. It never emits the returned working-directory value, raw arguments, target hostname, process IDs, license data, or server status details.

Ordinary Windows CI launches the packaged server with a separate fake worker process and runs these external scenarios:

- successful result-only retrieval;
- disconnected SDK;
- MP failure;
- output-getter failure after MP success;
- caller deadline;
- caller cancellation;
- execution-watchdog replacement followed by success; and
- an RPC for an unsupported exact-target service.

Fake scenarios use invented results and cannot establish SpatialAnalyzer behavior. Adapter tests remain authoritative for the SDK call sequence and prove that MP failure suppresses result-only getters.

The execution watchdog retains its 30-second default and gains a bounded configuration override. Portable process tests shorten it to exercise real public-host replacement without slowing CI. Invalid, non-positive, or greater-than-ten-minute values fail server startup.

The licensed smoke script requires explicit confirmation, exactly one already-running SA 2026.1.0529.7 x64 process, no competing Briosa worker or standalone SDK client, a matching package, and a free loopback port. It exercises only the successful read-only operation. It does not inject failures, hangs, or crashes into SpatialAnalyzer.

## Consequences

- The same generated client crosses the packaged public boundary in ordinary CI and on a licensed machine.
- All expected failure mappings remain reproducible without SA or a license.
- The licensed check proves real `GetStringArg` result retrieval while redacting the returned value.
- A protected runner can later invoke one documented script without changing the test payload.
- Runtime connected-version discovery remains unavailable until a separately reviewed version probe exists.
