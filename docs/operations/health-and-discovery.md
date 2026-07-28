# Health and discovery

Briosa exposes the standard gRPC health service and a read-only core discovery service. Reading either service does not invoke another SpatialAnalyzer MP command; readiness reflects the startup verification already performed for the current worker generation.

## Health checks

Use a standard `grpc.health.v1.Health` client with one of these service names:

| Service name | Meaning |
| --- | --- |
| `briosa.liveness` | The public Briosa host is serving. SpatialAnalyzer and worker state do not affect it. |
| `briosa.readiness` | The worker is control-ready, attached to SpatialAnalyzer, and has completed the bounded execution-channel probe for its current generation. |

The standard empty service name returns the aggregate health state. Deployment probes should use the explicit names so a SpatialAnalyzer outage does not restart an otherwise healthy public host.

### Execution-channel verification

Live SA 2026.1.0529.7 experiments showed that a second SDK client can report a successful `ConnectEx` while blocking indefinitely in `ExecuteStep`. Briosa therefore treats successful attachment as `Unverified` and does not admit ordinary MP work.

The server sends a dedicated private verification request after worker attachment. The worker performs Get Working Directory through the normal SDK sequence on its owning STA, validates MP result code `2` and the expected output shape, then discards the path before replying. The server watchdog bounds the exchange. A timeout, cancellation, worker exit, or lost response moves through `CompetingClientSuspected` to `OperatorRecoveryRequired`, terminates the worker, and does not start an automatic reconnect loop. Establish a clean SpatialAnalyzer/SDK state and restart Briosa before trying again.

## Server information

`briosa.core.v1alpha1.DiscoveryService/GetServerInfo` returns:

- Briosa and protocol build coordinates;
- the configured exact SpatialAnalyzer target;
- catalog revision and interop fingerprint;
- safe worker, SDK connection, and execution-readiness states;
- the target-isolation mode (`single_tenant` for the current release); and
- whether MP requests are currently ready.

The connected SpatialAnalyzer version is optional. An SDK connection does not itself establish the connected release, so Briosa reports the version as unavailable until a reviewed runtime probe verifies it. It never substitutes the configured target for an unobserved connected version. The activated SDK engine/type-library version is also a separate identity claim because machine-wide COM registration can select a different installed SDK. Issue [#70](https://github.com/spatialanalyzer/briosa/issues/70) owns runtime verification or explicit operator attestation for both identities.

## Capabilities

`briosa.core.v1alpha1.DiscoveryService/ListCapabilities` lists only reviewed operations built into the exact-target catalog, supported by the current isolation mode, and enabled by the server's runtime operation policy. Each entry includes its stable operation ID, gRPC service and RPC, fully qualified method, reviewed read-only/mutating/unknown effect classification, replay safety, and execution scope. A missing runtime allowlist produces an empty operation list. Unknown and `exclusive_workflow` scopes are not advertised in the current `single_tenant` mode.

Serialization covers one MP sequence, not a workflow spanning RPCs. See the [workflow-isolation guide](workflow-isolation.md) before coordinating multiple operations through one target.

Discovery does not expose hostnames, ports, process IDs, status codes, raw diagnostics, license information, credentials, MP arguments, returned values, or the complete installed SpatialAnalyzer command inventory.
