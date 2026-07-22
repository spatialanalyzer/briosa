# ADR 0010: Health, version, and capability discovery

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-22
- Issue: [#12](https://github.com/spatialanalyzer/briosa/issues/12)

## Context

Operators need to distinguish a running public host from a host that can currently execute MP work. Thin clients also need reproducible build coordinates and an exact list of operations exposed by the build without probing arbitrary commands or learning sensitive machine and license details.

The worker process can be control-ready while its SpatialAnalyzer SDK connection is disconnected or faulted. A successful SDK connection also does not prove which SpatialAnalyzer release is connected because the approved SDK interface has no direct version query. Reporting the configured target as the observed connected version would turn an unverified assumption into a compatibility claim.

## Decision

Briosa exposes the standard `grpc.health.v1.Health` protocol through `Grpc.AspNetCore.HealthChecks` with two named services:

- `briosa.liveness` is healthy while the public host can serve requests. It does not depend on the worker or SpatialAnalyzer.
- `briosa.readiness` is healthy only when the supervisor state is `Ready` and its current SDK connection state is `Connected`.

The empty health-service name retains the standard aggregate behavior. Clients and deployment probes use the explicit names when they need a precise liveness/readiness distinction.

The stable core package adds `DiscoveryService`:

- `GetServerInfo` returns `VersionCoordinates`, safe worker/connection enums, readiness, and an optional connected-SA version with an explicit verification state.
- `ListCapabilities` returns catalog identity and only the operations in the reviewed exact-target allowlist.

The configured exact SA target is always reported. The connected-SA version remains absent with state `UNAVAILABLE` until a separately reviewed runtime mechanism can establish it. Future support may report a verified match or mismatch without changing existing field meanings.

Catalog generation emits an immutable runtime capability descriptor beside operation adapters. Discovery therefore uses the same operation ID, service, RPC, fully qualified method, and reviewed effect classification as the public generated surface. It does not parse the catalog on the request path and does not maintain a second allowlist.

## Information boundary

Discovery returns no target hostname, network port, worker process ID, SDK status code, attempt count, restart count, timestamps, raw diagnostic code, license information, credentials, MP arguments, output values, or installed-SA command inventory. Assembly/source coordinates are present only when authoritative build metadata supplies them. The approved canonical interop API hash is used as the interop fingerprint.

Health and discovery remain bound by the server's existing loopback default. Remote authentication, authorization, and broader operational audit policy remain issues #22 and #24.

## Testing

Portable tests verify:

- liveness naming is independent of worker state;
- readiness requires both worker readiness and a connected SDK snapshot;
- connected-version absence is distinct from its verification state;
- capability discovery exactly reflects generated catalog metadata; and
- protocol descriptors contain no prohibited operational or license fields.

These tests require neither SpatialAnalyzer nor a license.

## Consequences

- Kubernetes, services, installers, and clients can use a standard health client.
- A running but disconnected Briosa host remains live and not ready.
- Compatibility discovery fails honest: configured target and observed connected version are never conflated.
- Adding a reviewed catalog operation automatically updates capability discovery through deterministic generation.
