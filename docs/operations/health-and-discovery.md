# Health and discovery

Briosa exposes the standard gRPC health service and a read-only core discovery service. Both are available without invoking a SpatialAnalyzer MP command.

## Health checks

Use a standard `grpc.health.v1.Health` client with one of these service names:

| Service name | Meaning |
| --- | --- |
| `briosa.liveness` | The public Briosa host is serving. SpatialAnalyzer and worker state do not affect it. |
| `briosa.readiness` | In v0.1, the worker is ready and its SDK connection snapshot is connected. This is provisional attachment readiness, not proof that this SDK client owns the MP execution channel. |

The standard empty service name returns the aggregate health state. Deployment probes should use the explicit names so a SpatialAnalyzer outage does not restart an otherwise healthy public host.

### Execution-channel limitation

Live SA 2026.1.0529.7 experiments showed that a second SDK client can report a successful `ConnectEx` while blocking indefinitely in `ExecuteStep`. Until issue [#91](https://github.com/spatialanalyzer/briosa/issues/91) implements [ADR 0017](../architecture/0017-execution-channel-readiness.md), operators must not interpret v0.1 readiness as proof of command execution ownership.

The v0.2 target state distinguishes attached-but-unverified from execution-ready. It performs one bounded Get Working Directory probe on the current worker generation, discards the returned path, and reports not ready after ambiguous ownership until explicit operator recovery.

## Server information

`briosa.core.v1alpha1.DiscoveryService/GetServerInfo` returns:

- Briosa and protocol build coordinates;
- the configured exact SpatialAnalyzer target;
- catalog revision and interop fingerprint;
- safe worker and SDK connection states; and
- whether MP requests are currently ready.

The connected SpatialAnalyzer version is optional. An SDK connection does not itself establish the connected release, so Briosa reports the version as unavailable until a reviewed runtime probe verifies it. It never substitutes the configured target for an unobserved connected version. The activated SDK engine/type-library version is also a separate identity claim because machine-wide COM registration can select a different installed SDK. Issue [#70](https://github.com/spatialanalyzer/briosa/issues/70) owns runtime verification or explicit operator attestation for both identities.

## Capabilities

`briosa.core.v1alpha1.DiscoveryService/ListCapabilities` lists only reviewed operations built into the exact-target catalog and enabled by the server's runtime operation policy. Each entry includes its stable operation ID, gRPC service and RPC, fully qualified method, and reviewed read-only/mutating/unknown effect classification. A missing runtime allowlist produces an empty operation list.

Discovery does not expose hostnames, ports, process IDs, status codes, raw diagnostics, license information, credentials, MP arguments, returned values, or the complete installed SpatialAnalyzer command inventory.
