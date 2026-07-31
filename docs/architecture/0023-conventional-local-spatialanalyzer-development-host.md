# ADR 0023: Conventional local SpatialAnalyzer development host

- Status: Accepted for the v0.1 local development workflow; implementation and live validation remain in issues #118-#120
- Date: 2026-07-29
- Issue: [#117](https://github.com/spatialanalyzer/briosa/issues/117)
- Preserves: [ADR 0002](0002-worker-process-lifecycle.md), [ADR 0011](0011-windows-package-identity.md), [ADR 0014](0014-loopback-only-public-endpoint.md), [ADR 0015](0015-command-policy-and-audit-events.md), [ADR 0017](0017-execution-channel-readiness.md), and [ADR 0022](0022-runtime-identity-and-attestation.md)

## Context

Briosa has portable fake-worker tests and a packaged licensed-SA smoke path, but a licensed developer cannot yet exercise the real public gRPC surface through the conventional ASP.NET Core source workflow. Running the server from source currently requires a separately composed worker location, the server does not expose reflection, and the identity attestations required by ADR 0022 have no standard non-committed local configuration procedure.

The primary local workflow needs to exercise the real production boundaries rather than a development substitute: the public host, separately supervised `Briosa.Worker` process, worker-owned STA, SDK connection, exact-target identity gate, execution-channel probe, command policy, and standard gRPC service. It must not weaken packaged Production behavior or turn a maintainer workstation into the protected runner defined by [ADR 0013](0013-protected-licensed-runner.md).

## Decision

### Primary launch contract

`SpatialAnalyzer` is the first and default `Project` profile in `src/Briosa.Server/Properties/launchSettings.json`. These commands start the same primary development workflow:

```powershell
dotnet run --project src/Briosa.Server
dotnet run --project src/Briosa.Server --launch-profile SpatialAnalyzer
```

The explicit form is the stable command for documentation and automation. The plain form is the shortest developer command and is protected by a test that verifies the profile ordering and semantics.

The profile sets only `ASPNETCORE_ENVIRONMENT=Development`. It does not set `applicationUrl`, a worker executable path, SpatialAnalyzer identity evidence, a command allowlist, or any secret. Briosa continues to own its HTTP/2 listener through `Briosa:Endpoint`; generic ASP.NET Core URL configuration remains rejected. The profile neither installs nor starts SpatialAnalyzer.

The committed `appsettings.json` remains unchanged. No `appsettings.Development.json` is added initially because this contract has no safe committed configuration difference beyond the environment name. If a later development setting is needed, it must be reviewed as non-sensitive, safe for every contributor, and prohibited from changing the Production package contract.

### Debug worker composition

A Debug build of `Briosa.Server` builds the real `Briosa.Worker` project and colocates its complete runnable cohort in the server output. The cohort includes `Briosa.Worker.exe`, its assembly, dependency and runtime configuration files, managed dependencies, and the approved exact-target interop assembly. The development server resolves that colocated executable by default, so the source workflow requires no package build, archive extraction, custom smoke client, or `Briosa__Worker__ExecutablePath` setting.

This is build-time composition, not a relaxation of the process boundary. The server still starts, monitors, and replaces a separate worker process; the worker still owns one SDK connection and one serialized STA. A partial copy, an in-process SDK client, or a development-only fake behind the production worker name does not satisfy the contract.

The Debug composition is additive to the source build only. Release publishing and packaging continue to use the existing reviewed, self-contained Windows package process. No new Debug launch profile, reflection dependency, user-secret value, or source-layout assumption may appear in the release archive.

### Typed configuration and startup validation

Issue #118 replaces ad hoc worker and identity configuration reads with typed options rooted at the existing keys:

- `Briosa:Endpoint` owns the loopback address and port;
- `Briosa:Worker` owns the optional executable path and execution watchdog timeout;
- `Briosa:SpatialAnalyzer` owns the SDK target host; and
- `Briosa:SpatialAnalyzer:Identity` owns independent activated-SDK and connected-SA evidence.

Options are bound once and validated during host startup. Invalid endpoint values, an incomplete identity attestation pair, an invalid watchdog interval, or an explicitly configured missing worker executable fails startup with a setting identity and value-free diagnostic. When the executable path is omitted, Debug source hosting selects the colocated worker and packaged hosting retains its adjacent-worker rule. Failure to connect to a running SpatialAnalyzer is runtime unavailability rather than configuration synthesis: the public host can remain live, but readiness and MP admission fail honest.

Local operator attestations use the four independent ADR 0022 fields in .NET user-secrets:

```powershell
dotnet user-secrets set "Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Version" "2026.1.0529.7" --project src/Briosa.Server
dotnet user-secrets set "Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Reference" "<non-sensitive-activated-SDK-evidence-reference>" --project src/Briosa.Server
dotnet user-secrets set "Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Version" "2026.1.0529.7" --project src/Briosa.Server
dotnet user-secrets set "Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Reference" "<non-sensitive-connected-SA-evidence-reference>" --project src/Briosa.Server
```

The example versions are valid only when the developer has independently established those exact observations. A developer must not copy the configured target into either field merely to make readiness succeed. The two references identify separately retained, non-sensitive evidence; they are not paths, credentials, license values, or returned SpatialAnalyzer data. Runtime evidence continues to take precedence over an attestation for the same claim, and both effective claims must match before Briosa issues the execution-channel probe.

User-secrets provides a conventional non-committed configuration source, not a secure evidence vault. The launch profile, tracked settings, logs, discovery responses, and package must not contain the evidence references.

### Development reflection and command policy

Server reflection is available only when both of these conditions are true:

1. `Briosa.Server` was compiled in Debug configuration; and
2. the ASP.NET Core environment is `Development`.

Issue #119 enforces both conditions at service registration and endpoint mapping. A Release build does not include the reflection runtime in its published dependency closure, and changing an environment variable cannot enable reflection in the Production package.

Reflection provides schema discovery only. It does not bypass readiness, identity, command policy, request validation, or the worker boundary. The committed allowlist remains exactly `file_operations.get_working_directory`, the reviewed read-only vertical slice. No launch profile or Development settings file expands it. Exercising any additional exact operation against real SpatialAnalyzer requires a deliberate implementation and validation review; mutating operations are never implicitly authorized by reflection or reference-evidence membership.

The existing fake-worker scripts and portable tests remain the ordinary-CI and failure-injection path. A fake launch profile is not added initially. Any later convenience profile must be named explicitly as fake, remain secondary to `SpatialAnalyzer`, and never produce licensed-SA or release evidence.

## Intended manual workflow

The commands in this section are the contract that issues #118 and #119 implement and issue #120 verifies and publishes in the developer guide. Until those issues merge, they are not expected to work on `main`.

Before starting Briosa, the developer must:

1. use a Windows x64 workstation with a separately installed and licensed SpatialAnalyzer 2026.1.0529.7 and matching SDK;
2. establish the two identity claims independently and configure their four user-secret fields;
3. start exactly one SpatialAnalyzer instance and allow it to acquire the SDK communication ports; and
4. ensure no other Briosa worker, ObjectiveSA probe, SDK experiment, or `SpatialAnalyzerSDK` client is connected.

From the repository root, start the source server:

```powershell
dotnet run --project src/Briosa.Server --launch-profile SpatialAnalyzer
```

In another PowerShell session, use an installed `grpcurl` client over the loopback cleartext HTTP/2 endpoint:

```powershell
grpcurl -plaintext 127.0.0.1:50051 list
'{"service":"briosa.liveness"}' | grpcurl -plaintext -d '@' 127.0.0.1:50051 grpc.health.v1.Health/Check
'{"service":"briosa.readiness"}' | grpcurl -plaintext -d '@' 127.0.0.1:50051 grpc.health.v1.Health/Check
grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.core.v1alpha1.DiscoveryService/GetServerInfo
grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.core.v1alpha1.DiscoveryService/ListCapabilities
grpcurl -plaintext -d '{}' 127.0.0.1:50051 briosa.sa.v2026_1_0529_7.v1alpha1.FileOperations/GetWorkingDirectory
```

The expected sequence is a live host, independently matched identities, a successful execution-channel proof for the current worker generation, healthy readiness, one advertised capability, and a successful working-directory response. The returned directory is developer-visible command output and must not be copied into logs or validation reports.

Stop the server with Ctrl+C. Shutdown must be bounded and must terminate the supervised worker without terminating SpatialAnalyzer.

## Failure and recovery boundary

- If SpatialAnalyzer is absent or unreachable, liveness may remain healthy, but readiness stays unhealthy and MP work is rejected as not started. Start one matching instance and allow the supervisor to reconnect; do not invent identity evidence.
- If either identity claim is unavailable or mismatched, Briosa does not issue the execution-channel probe and does not admit even `GetWorkingDirectory`. Correct the installation, registration, connection target, or independently established attestation; do not change the configured target to conceal the mismatch.
- If more than one SpatialAnalyzer instance or another SDK client may own execution, stop Briosa, close all SDK clients, close every SpatialAnalyzer instance, and start exactly one clean matching SpatialAnalyzer instance so it can reacquire the SDK ports. Then restart Briosa. Closing only the first instance does not transfer ownership to an already-open instance.
- After a timeout, cancellation, worker crash, lost response, or uncertain port ownership, do not automatically replay an ambiguously completed command. Stop Briosa and perform the clean-instance recovery above when ownership is uncertain. Although the default operation is read-only, the [ADR 0018](0018-uncertain-completion-and-replay.md) completion and replay contract still applies.
- Real-SA development runs use only safe success-path calls. Hang, crash, malformed-result, getter-failure, competing-client, and mutation experiments remain portable fake-worker tests or separately reviewed protected work.

The local workflow is developer evidence only. It does not satisfy the protected runner requirement in issue #20, the remaining authoritative identity work in issue #70, or release-readiness evidence.

## Implementation and validation plan

The implementation remains split into three reviewable tasks:

1. [#118](https://github.com/spatialanalyzer/briosa/issues/118) adds the Debug-only complete worker composition, first/default `SpatialAnalyzer` launch profile, user-secrets project identity, typed options, startup validation, and portable tests. Release packaging and the packaged adjacent-worker contract must remain byte-separate from this development path.
2. [#119](https://github.com/spatialanalyzer/briosa/issues/119) adds Debug-and-Development-gated reflection, verifies the health, discovery, and exact-target services are discoverable, proves Production exclusion, and confirms the default capability remains only `GetWorkingDirectory`.
3. [#120](https://github.com/spatialanalyzer/briosa/issues/120) adds the prominent README quick start and complete local gRPC developer guide, verifies the source-run contract with portable checks, and records one explicitly authorized manual success-path run against an already-running licensed exact-target SpatialAnalyzer.

Issues #118 and #119 may be developed concurrently after this decision is accepted, but their overlapping server project, package-lock, startup, and test changes are rebased and merged sequentially. Issue #120 follows their integrated behavior. Ordinary validation for all three issues requires no SpatialAnalyzer; live validation requires fresh permission for that task and follows the least-privileged success path above.

## Consequences

- A licensed developer gets one conventional source command and standard reflection-aware client commands without packaging or manual worker-path composition.
- The workflow exercises the production server/worker architecture instead of a fake substitute.
- Identity remains fail-honest and independently evidenced even in Development.
- Reflection cannot be activated in a packaged Release host, and reflection never grants command authorization.
- The safe committed local capability remains one reviewed read-only operation while broader real-SA testing stays explicit.
- Portable fake testing remains the default for CI and destructive failure scenarios.
- Developer workstation evidence is useful for #120 but cannot be presented as protected runner or release evidence.
