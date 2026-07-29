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

It prints JSON containing Briosa, protocol, catalog, target-SA, source, and interop identities plus booleans for required packaged files. It does not start the web host, activate COM, connect to SpatialAnalyzer, or expose paths, hostnames, ports, license information, or raw diagnostics. Exit code `0` means the static package layout and platform checks passed; exit code `2` means a required file or platform condition is missing.

## Start the server

The packaged default binds unencrypted HTTP/2 to loopback only at `127.0.0.1:50051`:

```powershell
./Briosa.Server.exe
```

The default SpatialAnalyzer SDK target is `localhost`. That configured target identifies where Briosa connects; it does not identify the activated SDK or connected application release. The current adapter has no reviewed runtime version query, so an ordinary deployment must supply the independent version/reference attestations documented in [the health and discovery guide](health-and-discovery.md). Missing evidence remains live but not ready; a verified mismatch cannot be overridden.

The public endpoint has one authoritative configuration surface: `Briosa:Endpoint:Address` and `Briosa:Endpoint:Port`. The address must be an IPv4 or IPv6 loopback IP literal. Generic ASP.NET Core URL settings, configured Kestrel endpoints, hostnames, wildcards, LAN addresses, and public addresses are rejected at startup.

LAN, Internet, reverse-proxy, tunnel, shared-host, and other remotely reachable deployments are unsupported. Briosa v0.1 has no client authentication, per-operation authorization, or TLS configuration. See the [public endpoint operator guide](endpoint-security.md) and [v0.1 threat model](../security/threat-model.md) before deployment.

The worker execution watchdog defaults to 30 seconds. Set `Briosa__Worker__ExecutionWatchdogTimeout` to a positive .NET `TimeSpan` no greater than ten minutes only when deployment evidence justifies an override. A client deadline or cancellation stops that caller from waiting; it does not claim to cancel synchronous COM work already in flight.

Use standard gRPC health checks named `briosa.liveness` and `briosa.readiness`. See `HEALTH-AND-DISCOVERY.md` for discovery and response semantics.

If startup reports `OPERATOR_RECOVERY_REQUIRED`, do not repeatedly restart Briosa. Close affected SDK clients, establish a clean SpatialAnalyzer instance that owns the SDK ports, and then restart Briosa once to perform the explicit recovery cycle.

## Provenance

`manifest.json` records:

- Briosa version and full source revision;
- runtime identifier and self-contained/trimming choices;
- catalog ID and revision;
- exact supported SpatialAnalyzer release set;
- core and target protocol packages;
- canonical interop fingerprint; and
- explicit statements that SpatialAnalyzer is not bundled and requires a separate license.

`metadata/` retains the generated catalog coverage manifest and approved interop provenance used to build the distribution.

## Build locally

From a clean repository checkout:

```powershell
./eng/New-WindowsPackage.ps1 -Version 0.1.0
```

The script uses locked `win-x64` restores, clean self-contained publishes, deterministic ZIP ordering and timestamps, and writes the ZIP, external checksum, and external provenance manifest to `artifacts/`.

Ordinary CI wraps the complete two-build package verification in the reviewed `package` duration budget. `Test-WindowsPackage.ps1` requires identical ZIP hashes before recording the verified archive against `package-size`; it then measures process start through the first accepted loopback connection against `startup` and records the host working set against `startup-working-set`. The startup checks intentionally point at a missing worker executable, so they never activate or connect to SpatialAnalyzer. Machine-readable reports are written below `artifacts/ci-metrics`. Thresholds and their adjustment evidence are defined in the [full-surface gate guide](../development/full-surface-gates.md) and [runtime performance guide](../testing/runtime-performance-and-soak.md); a slow run is not an implicit waiver.

Run `./eng/Test-WindowsPackage.ps1 -Version 0.1.0-test` to build twice and verify identical archive hashes, all checksums, manifest/default configuration, offline diagnostics, and host launch without SpatialAnalyzer.

## Release production

Pushing a tag such as `v0.1.0` runs the verified package build and publishes its ZIP, checksum, and provenance manifest to the corresponding GitHub Release. The same release also publishes the runtime-neutral protocol ZIP, checksum, and provenance manifest described in the [protocol artifact guide](protocol-artifacts.md). Manually dispatching the release workflow performs both verified builds and retains one workflow artifact containing all release assets, but never creates a GitHub Release.
