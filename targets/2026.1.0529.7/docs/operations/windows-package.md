# Windows x64 package

Briosa release archives contain a self-contained .NET 10 Windows x64 server and worker for one exact SpatialAnalyzer target. The initial artifact name is:

```text
briosa-<briosa-version>-sa-2026.1.0529.7-win-x64.zip
```

## Prerequisites

- 64-bit Windows supported by .NET 10.
- SpatialAnalyzer 2026.1.0529.7 installed separately.
- A valid SpatialAnalyzer license appropriate for the operations you perform.
- SpatialAnalyzer running before Briosa can become ready for MP execution.
- Runtime-verified exact identity for the activated SDK and connected application, or an explicit per-claim operator attestation backed by retained evidence.

SpatialAnalyzer, its installer, SDK executable, original type library, license material, and vendor documentation are not included. Briosa can start and report liveness without SpatialAnalyzer, but readiness remains not serving until its worker connects and completes the bounded execution-channel probe.

## Verify and extract

Verify the downloaded ZIP against its adjacent `.sha256` file before extraction. The archive also includes `files.sha256`, which covers every packaged file except that checksum list itself.

The archive contains one top-level directory. Extract that complete directory; do not move `Briosa.Server.exe`, `Briosa.Worker.exe`, or `Briosa.SpatialAnalyzer.Interop.dll` away from one another.

## Diagnostics

Run the offline diagnostics command before starting the server:

```powershell
./Briosa.Server.exe diagnostics
```

It prints JSON containing Briosa, protocol, target-SA, source, and interop identities plus booleans for required packaged files. It does not start the web host, activate COM, connect to SpatialAnalyzer, or expose paths, hostnames, ports, license information, or raw diagnostics. Exit code `0` means the static package layout and platform checks passed; exit code `2` means a required file or platform condition is missing.

## Start the server

The package also includes `Briosa.ControlCenter.exe`. Interactive launches show
an optional tray companion; use `--Briosa:Desktop:Mode=Disabled` for headless use.
See [Control Center](control-center.md) (or `CONTROL-CENTER.md` in the ZIP) for
ownership, lifecycle controls, activity, and support export.

The packaged default binds unencrypted HTTP/2 to loopback only at `127.0.0.1:50051`:

```powershell
./Briosa.Server.exe
```

The server reads its packaged `appsettings.json`, including the operation admission profile, from the package directory regardless of the current working directory. Only an explicit `--contentRoot`, `ASPNETCORE_CONTENTROOT`, or `DOTNET_CONTENTROOT` selects a different configuration directory.

The default SpatialAnalyzer SDK target is `localhost`. That configured target identifies where Briosa connects; it does not identify the activated SDK or connected application release. The worker observes the file/product version of the SDK process uniquely created during activation. The connected SA version still requires independent operator evidence; an SDK attestation is also needed when its runtime version cannot be observed. Supply the version/reference pairs through Control Center's Connection setup or the configuration documented in [the health and discovery guide](health-and-discovery.md). Missing evidence remains live but not ready; a verified mismatch cannot be overridden.

The public endpoint has one authoritative configuration surface: `Briosa:Endpoint:Address` and `Briosa:Endpoint:Port`. The address must be an IPv4 or IPv6 loopback IP literal. Generic ASP.NET Core URL settings, configured Kestrel endpoints, hostnames, wildcards, LAN addresses, and public addresses are rejected at startup.

LAN, Internet, reverse-proxy, tunnel, shared-host, and other remotely reachable deployments are unsupported. Briosa currently has no client authentication, per-operation authorization, or TLS configuration. See the [public endpoint operator guide](endpoint-security.md) and [current threat model](../security/threat-model.md) before deployment.

The worker admission budget defaults to 32 MiB and can be set with `Briosa__Worker__MaxRetainedWorkMiB` (1–1024 MiB). Each accepted request reserves 16 KiB plus eight times its protobuf wire size, capped at 1 MiB, before MP command mapping. The reservation remains charged while queued or in flight; this is a conservative retained-work budget, not an exact CLR heap measurement. The existing queue count remains an independent limit. Worker response frames are limited separately to 64 KiB, with one active exchange per worker.

### Worker time bounds

The server chooses each operation's execution budget from that operation's reviewed duration class (`quick`, `long_running`, or `interactive`) in the target's classification table. The readiness probe and worker startup have their own bounds; duration classes never apply to them. Every value is a .NET `TimeSpan` (`hh:mm:ss`). The server validates them at startup and refuses to start, naming the configuration key, if a value is malformed, not positive, above its maximum, or a longer-class budget is shorter than the quick budget. The defaults are defined once, in the server; the packaged `appsettings.json` does not repeat them.

| Configuration key (environment variable form) | Bounds | Default | Maximum |
| --- | --- | --- | --- |
| `Briosa__Worker__ExecutionWatchdogTimeout` | Execution of `quick` operations | 30 seconds | 10 minutes |
| `Briosa__Worker__LongRunningExecutionWatchdogTimeout` | Execution of `long_running` operations (device actions, motion, scans, heavy document and cloud work) | 10 minutes | 2 hours; not shorter than the quick budget |
| `Briosa__Worker__InteractiveExecutionWatchdogTimeout` | Execution of `interactive` operations (prompts, runtime selection, pick, drag, manual guide) | 30 minutes | 8 hours; not shorter than the quick budget |
| `Briosa__Worker__ReadinessProbeTimeout` | The private execution-readiness probe after an explicit connect | 10 seconds | 10 minutes |
| `Briosa__Worker__StartupTimeout` | Worker launch, COM activation, the worker Ready message, and `ConnectEx` | 10 seconds | 5 minutes |

Every execution budget is enforced by the same watchdog. When it expires after dispatch, the supervisor terminates the worker, reports `OPERATION_FAILURE_KIND_WORKER_WATCHDOG_TIMEOUT` with `EXECUTION_DISPOSITION_STARTED_OUTCOME_UNKNOWN`, and requires explicit SDK recovery. The interrupted operation is never replayed. An operation without a reviewed duration class does not execute: the call is rejected as a policy denial before dispatch, with `EXECUTION_DISPOSITION_NOT_STARTED` and the diagnostic `operation-duration-unreviewed`.

The defaults are conservative starting points, not measurements. The quick budget keeps its 0.9 value. Ten minutes covers routine long operations, while a hung device or document call still releases the single serialized worker in bounded time. Thirty minutes gives an operator time to answer a prompt without leaving the worker blocked for a whole session. The readiness probe performs one trivial MP read and should answer in seconds, so 10 seconds detects a blocked SDK channel quickly. Startup keeps its earlier fixed 10 seconds. Raise a bound only when deployment evidence justifies it, and note that the queue is serialized: other requests wait, and a stop or recovery request waits for an in-flight call, for up to the longest budget in use.

A client deadline or cancellation stops that caller from waiting; it does not claim to cancel synchronous COM work already in flight.

Use standard gRPC health checks named `briosa.liveness` and `briosa.readiness`. See `HEALTH-AND-DISCOVERY.md` for discovery and response semantics.

If startup reports `OPERATOR_RECOVERY_REQUIRED`, do not repeatedly restart Briosa. Close affected SDK clients, establish a clean SpatialAnalyzer instance that owns the SDK ports, and then restart Briosa once to perform the explicit recovery cycle.

## Provenance

`manifest.json` records:

- Briosa version and full source revision;
- runtime identifier and self-contained/trimming choices;
- exact SpatialAnalyzer target;
- stable protocol package;
- canonical interop fingerprint; and
- explicit statements that SpatialAnalyzer is not bundled and requires a separate license.

Implemented operations are reported by `DiscoveryService/ListCapabilities` after
runtime policy is applied; they are not duplicated in package metadata.

`metadata/` retains the approved interop provenance used to build the distribution.

## Build locally

From a clean repository checkout:

```powershell
./eng/New-WindowsPackage.ps1 -Version 0.1.0
```

The script uses locked `win-x64` restores, clean self-contained publishes, deterministic ZIP ordering and timestamps, and writes the ZIP, external checksum, and external provenance manifest to `artifacts/`.

`Test-WindowsPackage.ps1` builds the archive twice and requires identical ZIP hashes. Its startup check explicitly selects the vendor-independent worker test host, so it never activates or connects to SpatialAnalyzer.

The Debug source-host composition is not a packaging input. The Release package script continues to publish server and worker independently, then merges their reviewed runtime cohorts. Package verification requires the production worker and approved interop assembly while rejecting `launchSettings.json`, `appsettings.Development.json`, and the Debug user-secrets project identity.

Run `./eng/Test-WindowsPackage.ps1 -Version 0.1.0-test` to build twice and verify identical archive hashes, all checksums, manifest/default configuration, offline diagnostics, and host launch without SpatialAnalyzer.

## Release production

Pushing a tag such as `v0.1.0` runs the verified package build and publishes its ZIP, checksum, and provenance manifest to the corresponding GitHub Release. The same release also publishes the runtime-neutral protocol ZIP, checksum, and provenance manifest described in the [protocol artifact guide](protocol-artifacts.md). Manually dispatching the release workflow performs both verified builds and retains one workflow artifact containing all release assets, but never creates a GitHub Release.
