# Run the local gRPC server with SpatialAnalyzer

This guide exercises Briosa's real public gRPC host and separately supervised
worker from source against a licensed SpatialAnalyzer instance. This guide uses
an application the developer started before Briosa; the lifecycle API can also
launch a server-owned instance. It is a developer success-path check, not a portable test, a failure
injection environment, or protected release evidence.

The workflow uses the Debug-only `SpatialAnalyzer` launch profile and
Development-only gRPC reflection. It does not require a Windows package,
archive extraction, custom smoke client, or a manually configured worker path.

## Prerequisites

Before Briosa connects, confirm all of the following:

- Use a Windows x64 workstation with the .NET SDK selected by the repository `global.json`.
- Install and license SpatialAnalyzer 2026.1.0529.7 and its matching SDK
  separately. Briosa does not install, license, or redistribute them. The
  lifecycle API can start the configured exact-target application and SDK.
- Independently establish the activated SDK engine/type-library version and the
  connected SpatialAnalyzer application version. Both must be exactly
  `2026.1.0529.7` for this target.
- Install the standard [`grpcurl`](https://github.com/fullstorydev/grpcurl)
  client and make its executable available on `PATH`. Optionally install
  [`grpcui`](https://github.com/fullstorydev/grpcui) for the browser-based
  examples; it is not required for the acceptance path.
- Reserve the loopback endpoint `127.0.0.1:50051`. The current server is
  intentionally loopback-only and uses cleartext HTTP/2.
- Close every other Briosa server or worker, ObjectiveSA probe, SDK experiment,
  and `SpatialAnalyzerSDK` client. Do not use a second SDK client to inspect or
  diagnose this run.
- Close any extra SpatialAnalyzer instances. Start exactly one matching
  instance before Briosa, and wait for it to finish starting and acquire the
  SDK communication ports. Closing the first port owner does not transfer
  ownership to an already-open instance.

Run commands below from `targets/2026.1.0529.7`. A locked restore is a useful
one-time checkout check and does not contact SpatialAnalyzer:

```powershell
dotnet restore Briosa.slnx --locked-mode
```

## Configure independent identity evidence once

The current production adapter has no reviewed runtime query for either
identity. Store the two independently established claims in the server
project's .NET user-secrets store:

```powershell
dotnet user-secrets set "Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Version" "2026.1.0529.7" --project src/Briosa.Server
dotnet user-secrets set "Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Reference" "<non-sensitive-activated-SDK-evidence-reference>" --project src/Briosa.Server
dotnet user-secrets set "Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Version" "2026.1.0529.7" --project src/Briosa.Server
dotnet user-secrets set "Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Reference" "<non-sensitive-connected-SA-evidence-reference>" --project src/Briosa.Server
```

Replace each placeholder with a short identifier for separately retained
evidence, such as an approved change or validation record. Do not put the
evidence itself, a filesystem path, hostname, credential, license value, or
returned SpatialAnalyzer data in a reference. Do not copy the configured target
version into either claim merely to make readiness succeed.

Each claim requires both its `Version` and `Reference`; a partial pair fails
server startup. User-secrets keeps these local values out of tracked settings
and the launch profile, but it is not an evidence vault. Runtime evidence, when
available, takes precedence over the corresponding attestation and a runtime
mismatch still fails closed. Update the secrets whenever the installed or
connected software changes.

## Start the daily workflow

After the prerequisite clean start of exactly one SpatialAnalyzer instance,
start the Briosa control plane with one command:

```powershell
dotnet run --project src/Briosa.Server --launch-profile SpatialAnalyzer
```

The profile sets `ASPNETCORE_ENVIRONMENT=Development`. A Debug build places the
complete real `Briosa.Worker` cohort beside the server, but it does not start
that worker, activate the SDK, launch SpatialAnalyzer, or call `ConnectEx`.
The profile does not supply identity evidence, change the endpoint, or widen
the operation allowlist.

Leave this terminal open. In another PowerShell terminal, use the commands in
the following sections.

## Exercise reflection and the public API with grpcurl

List every reflected service:

```powershell
grpcurl -plaintext 127.0.0.1:50051 list
```

The list includes `grpc.reflection.v1alpha.ServerReflection`,
`grpc.health.v1.Health`, `briosa.DiscoveryService`, and the
exact-target services. Reflection proves only that a schema is
mapped; it does not authorize an operation or prove SpatialAnalyzer readiness.

Check host liveness and MP readiness separately:

```powershell
'{"service":"briosa.liveness"}' | grpcurl -plaintext -d '@' 127.0.0.1:50051 grpc.health.v1.Health/Check
'{"service":"briosa.readiness"}' | grpcurl -plaintext -d '@' 127.0.0.1:50051 grpc.health.v1.Health/Check
```

Passing the named health request through standard input avoids Windows PowerShell's native-argument handling removing the embedded JSON quotes before `grpcurl` receives them.

Inspect the value-free runtime summary and effective capabilities:

```powershell
grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.DiscoveryService/GetServerInfo
grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.DiscoveryService/ListCapabilities
```

The initial summary reports a stopped SDK generation and `readyForMp: false`.
Start the SDK explicitly, retain its generation, and connect it to the
already-running local application:

```powershell
grpcurl -plaintext -d '{}' 127.0.0.1:50051 `
  briosa.SpatialAnalyzerSdkLifecycle/StartSpatialAnalyzerSdk

# Replace 1 with the sdkGeneration returned above.
grpcurl -plaintext -d '{"expectedSdkGeneration":1}' 127.0.0.1:50051 `
  briosa.SpatialAnalyzerSdkLifecycle/ConnectToSpatialAnalyzer
```

Starting the SDK creates a disconnected generation. Only the second call uses
`ConnectEx("localhost", ...)`, performs exact-target checks, and runs the MP
readiness proof.

A ready source run reports:

- liveness and readiness health status `SERVING`;
- `workerState` equal to `WORKER_RUNTIME_STATE_READY`;
- `spatialAnalyzerConnectionState` equal to
  `SPATIAL_ANALYZER_CONNECTION_STATE_CONNECTED`;
- `spatialAnalyzerExecutionReadinessState` equal to
  `SPATIAL_ANALYZER_EXECUTION_READINESS_STATE_EXECUTION_READY`;
- `readyForMp` equal to `true`;
- both identity `matchState` values equal to
  `RUNTIME_IDENTITY_MATCH_STATE_EXACT_MATCH`, with each source truthfully
  identifying runtime verification or operator attestation; and
- the reviewed operations admitted by the committed runtime policy, matching
  the operation IDs reported by `ListCapabilities`.

Finally, call the reviewed read-only operations:

```powershell
grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.FileOperations/GetWorkingDirectory

grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.AnalysisOperations/GetNumberOfCollections

grpcurl -plaintext -d '{"collectionIndex":0}' 127.0.0.1:50051 briosa.AnalysisOperations/GetIthCollectionName

grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.ConstructionOperations/GetActiveCollectionName

grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.UtilityOperations/GetActiveUnits

grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.UtilityOperations/GetWorkingFrameProperties
```

The responses contain developer-visible SpatialAnalyzer data. Confirm that the
calls succeed, but do not paste returned values into logs, screenshots, public
issues, or validation reports.

## Optionally exercise the same API with grpcui

Start the reflection-driven local UI:

```powershell
grpcui -plaintext 127.0.0.1:50051
```

The populated service and method selectors prove reflection is available. Use
the UI to make the same calls:

| Service and method | Request data |
| --- | --- |
| `grpc.health.v1.Health/Check` | `{"service":"briosa.liveness"}` |
| `grpc.health.v1.Health/Check` | `{"service":"briosa.readiness"}` |
| `briosa.DiscoveryService/GetServerInfo` | `{}` |
| `briosa.DiscoveryService/ListCapabilities` | `{}` |
| `briosa.AnalysisOperations/GetNumberOfCollections` | `{}` |
| `briosa.AnalysisOperations/GetIthCollectionName` | `{"collectionIndex":0}` |
| `briosa.ConstructionOperations/GetActiveCollectionName` | `{}` |
| `briosa.FileOperations/GetWorkingDirectory` | `{}` |
| `briosa.UtilityOperations/GetActiveUnits` | `{}` |
| `briosa.UtilityOperations/GetWorkingFrameProperties` | `{}` |

Apply the same data-handling rule to the UI: do not retain or share returned
SpatialAnalyzer values.

## Keep the operation allowlist narrow

The handwritten operation registry is the maximum API surface the binary can
express, not an authorization list. `ListCapabilities` is the authority for the
operations the current process admits. The committed runtime policy in
`src/Briosa.Server/appsettings.json` is the source of truth for defaults;
reflection and Development mode do not expand it. A reflected operation that
is absent from `ListCapabilities` remains denied before worker or SDK execution.

Never copy all reflected operations into a real-SA allowlist. Before
any per-run expansion, review each exact operation ID, its inputs, effect,
execution scope, replay safety, and expected result. Add only those reviewed
IDs to the indexed `Briosa:Security:Operations:Allow` configuration, restart
the server, and confirm the intended intersection with `ListCapabilities`.
`Briosa:Security:Operations:Deny` takes precedence. Unknown, empty, duplicate,
or non-array policy values fail startup. In particular, do not enable mutation,
failure-injection, hang, crash, malformed-result, or getter-failure scenarios
against this real production session.

See the [command policy and auditing guide](../operations/command-policy-and-auditing.md)
for the exact configuration shape and policy semantics.

## Troubleshoot with value-free states

Use explicit health status and `GetServerInfo` state names when recording a
problem. Do not capture target hostnames, SDK status codes, process IDs, local
paths, license details, evidence contents, raw arguments, returned values, or
complete logs in a public report.

| Observed safe state | Meaning | Operator action |
| --- | --- | --- |
| Liveness is `SERVING`; readiness is not `SERVING`; SDK is `STOPPED` or connection is `DISCONNECTED` | The control plane is available but the explicit SDK startup/connect sequence has not completed. | Start one exact-target SpatialAnalyzer instance if needed, call `StartSpatialAnalyzerSdk`, then connect the returned generation. |
| SDK is `FAULTED`; recovery is available; `readyForMp` is `false` | The SDK engine, worker, control channel, or bounded execution exchange was lost. | Reconcile any ambiguous operation outcome, call `RecoverSpatialAnalyzerSdk` with the faulted generation and `REPLACE_WITHOUT_REPLAY`, then explicitly connect the replacement. |
| Either identity source or match state is `UNAVAILABLE`; execution readiness stays `UNVERIFIED`; `readyForMp` is `false` | Briosa lacks one independently established identity claim and does not issue the execution-channel probe. | Establish the missing claim and configure its complete user-secret pair, then restart Briosa. |
| Either identity match state is `MISMATCH`; readiness is not `SERVING`; `readyForMp` is `false` | The activated SDK or connected application does not exactly match the configured target. | Correct the installation, COM registration, connection target, or independently established attestation. Never alter a claim or target to conceal the mismatch. |
| Execution readiness is `COMPETING_CLIENT_SUSPECTED` or `OPERATOR_RECOVERY_REQUIRED`; readiness is not `SERVING` | An execution-channel probe timed out, failed ambiguously, or indicated unsafe SDK ownership. Replacement cannot make replay or port ownership safe. | Perform the required operator cleanup first. Then use the explicit recover-and-connect sequence; restart the control plane only if its own lifecycle is unhealthy. Reboot if ownership remains uncertain. |
| Reflection or grpcui reports the reflection service as unimplemented | The host is not both a Debug build and in the `Development` environment. | Start the source host with the documented `SpatialAnalyzer` profile. Do not try to enable reflection in a Release package. |
| An operation is reflected but returns `PERMISSION_DENIED` or is absent from `ListCapabilities` | Reflection described a compiled schema that runtime policy does not admit. | Leave it denied unless its exact operation has received a deliberate real-SA review. Do not broaden policy automatically. |

The clean recovery sequence is also required after a timeout, cancellation,
worker crash, lost response, or uncertain port ownership. Do not automatically
replay a call whose completion is ambiguous. Never induce these failures during
this manual real-SA workflow; use the portable fake harness instead.

## Shut down cleanly

Before ending a normal session, call `StopSpatialAnalyzerSdk` with the current
SDK generation. Press Ctrl+C once in the Briosa server terminal and wait for
bounded cleanup. Server shutdown also stops its owned SDK generation, but it
does not stop SpatialAnalyzer. If a worker or SDK process appears to remain, do not start a
second run; follow the clean recovery sequence before reconnecting.

## Understand what this run proves

A successful manual run shows that this developer checkout can traverse the
public host, worker process, SDK connection, identity gate, execution-channel
probe, command policy, standard gRPC transport adapter, and the implemented
real read-only MP commands.
Record only the source commit, target version, time, successful state names, and
successful operation IDs. Do not record returned operation values or evidence
contents.

This workstation run is not the protected licensed-SA validation defined by the
[validation architecture](../../../../docs/architecture/validation-and-distribution.md), does not satisfy
the protected runner requirement, and is not release-readiness or authoritative
runtime-identity evidence. The protected workflow uses a reviewed package,
generated smoke client, dedicated licensed machine, trusted `main`, approval
environment, preflight/postflight checks, and the existing
`eng/Test-LicensedSpatialAnalyzer.ps1` path. See the
[licensed runner guide](../operations/licensed-sa-runner.md); do not run that
workflow from a personal development workstation.

Portable fake-backed tests remain the normal path for CI, offline development,
failure scenarios, hangs, crashes, cancellation, malformed results, and getter
failures. Run `./eng/Test-LocalSpatialAnalyzerHost.ps1` to verify the Debug
worker composition and graceful cleanup without SDK activation, and see the
[fake SDK harness guide](../testing/fake-sdk-harness.md) for deterministic
failure coverage.
