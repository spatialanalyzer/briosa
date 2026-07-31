# Health and discovery

Briosa exposes the standard gRPC health service and a read-only core discovery service. Reading either service does not invoke another SpatialAnalyzer MP command; readiness reflects the startup verification already performed for the current worker generation.

## Development-only reflection

A Debug build exposes standard gRPC server reflection only when the ASP.NET Core environment is `Development`. Registration and endpoint mapping both enforce the runtime environment check, while the server's Debug compilation controls whether the reflection implementation exists at all. Production and other runtime environments do not map the service, and the Release package excludes both the reflection host and protocol assemblies from its dependency closure.

Reflection describes every mapped health, discovery, and exact-target service. A reflected method is not an enabled capability: reflection never invokes a worker, changes readiness, supplies identity evidence, or bypasses request validation and the exact-operation allow/deny policy. Treat `ListCapabilities` as the authority for the current process's admitted operation set.

## Health checks

Use a standard `grpc.health.v1.Health` client with one of these service names:

| Service name | Meaning |
| --- | --- |
| `briosa.liveness` | The public Briosa host is serving. SpatialAnalyzer and worker state do not affect it. |
| `briosa.readiness` | The worker is control-ready, attached to SpatialAnalyzer, has completed the bounded execution-channel probe for its current generation, and has exact-match evidence for both runtime identities. |

The standard empty service name returns the aggregate health state. Deployment probes should use the explicit names so a SpatialAnalyzer outage does not restart an otherwise healthy public host.

### Execution-channel verification

Live SA 2026.1.0529.7 experiments showed that a second SDK client can report a successful `ConnectEx` while blocking indefinitely in `ExecuteStep`. Briosa therefore treats successful attachment as `Unverified` and does not admit ordinary MP work.

After worker attachment and exact-match identity gating, the server sends a dedicated private verification request. The worker performs Get Working Directory through the normal SDK sequence on its owning STA, validates MP result code `2` and the expected output shape, then discards the path before replying. If either identity is unavailable or mismatched, Briosa does not issue the probe. The server watchdog bounds an issued exchange. A timeout, cancellation, worker exit, or lost response moves through `CompetingClientSuspected` to `OperatorRecoveryRequired`, terminates the worker, and does not start an automatic reconnect loop. Establish a clean SpatialAnalyzer/SDK state and restart Briosa before trying again.

## Server information

`briosa.DiscoveryService/GetServerInfo` returns:

- Briosa and protocol build coordinates;
- the configured exact SpatialAnalyzer target;
- stable protocol package, exact SA target, and interop fingerprint;
- safe worker, SDK connection, and execution-readiness states;
- the target-isolation mode (`single_tenant` for the current release); and
- whether MP requests are currently ready.

The response carries separate evidence objects for the activated SDK engine/type library and connected SpatialAnalyzer application. Each contains an optional version plus:

- a source: `UNAVAILABLE`, `RUNTIME_VERIFICATION`, or `OPERATOR_ATTESTATION`; and
- a match state: `UNAVAILABLE`, `EXACT_MATCH`, or `MISMATCH`.

The configured target is never substituted for an unobserved runtime version. Runtime verification takes precedence for its own claim, so a configured attestation cannot hide a runtime mismatch. The legacy connected-version fields mirror the effective connected-SA claim for older clients and retain distinct runtime-verified and operator-attested states.

### Operator attestation when runtime evidence is unavailable

The current production adapter has no reviewed runtime query for either identity. With no explicit evidence, Briosa remains live but fails readiness and rejects MP admission with a not-started unavailable outcome. An operator may attest either missing claim independently:

```json
{
  "Briosa": {
    "SpatialAnalyzer": {
      "Identity": {
        "ActivatedSdk": {
          "OperatorAttestation": {
            "Version": "2026.1.0529.7",
            "Reference": "change-record:SDK-identity-review"
          }
        },
        "ConnectedSpatialAnalyzer": {
          "OperatorAttestation": {
            "Version": "2026.1.0529.7",
            "Reference": "change-record:SA-install-review"
          }
        }
      }
    }
  }
}
```

Each configured claim requires both `Version` and `Reference`; a partial pair fails startup. The reference identifies separately retained evidence and must not contain a path, credential, license value, or sensitive host detail. It is validated for presence but never returned by discovery or written to default logs. Record the version actually supported by the evidence rather than copying the package target as an assumption. See [ADR 0022](../../../../docs/architecture/0022-runtime-identity-and-attestation.md) for precedence and release-gate limits.

Debug source hosting enables the standard .NET user-secrets provider for these same four keys. User-secrets keeps the local values out of tracked settings and launch profiles, but it is not an evidence vault; retain the independently established evidence elsewhere. Release builds do not carry the user-secrets project identity. The complete source command and configuration procedure is tracked in issue #120.

## Capabilities

`briosa.DiscoveryService/ListCapabilities` lists only handwritten operations registered in the current build, supported by the current isolation mode, and enabled by runtime policy. Each entry includes its stable operation ID, gRPC service and RPC, fully qualified method, reviewed read-only/mutating/unknown effect classification, replay safety, and execution scope. A missing runtime allowlist produces an empty operation list. Unknown and `exclusive_workflow` scopes are not advertised in the current `single_tenant` mode.

Serialization covers one MP sequence, not a workflow spanning RPCs. See the [workflow-isolation guide](workflow-isolation.md) before coordinating multiple operations through one target.

Discovery does not expose hostnames, ports, process IDs, status codes, raw diagnostics, license information, credentials, MP arguments, returned values, or the complete installed SpatialAnalyzer command inventory.
