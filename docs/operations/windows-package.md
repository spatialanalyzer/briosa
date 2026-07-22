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

SpatialAnalyzer, its installer, SDK executable, original type library, license material, and vendor documentation are not included. Briosa can start and report liveness without SpatialAnalyzer, but readiness remains not serving until its worker connects.

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

The default SpatialAnalyzer SDK target is `localhost`. ASP.NET Core and Briosa configuration can be overridden with environment variables or command-line settings. Keep the endpoint on loopback unless the security, TLS, authentication, and authorization requirements tracked by the project have been implemented for your deployment.

Use standard gRPC health checks named `briosa.liveness` and `briosa.readiness`. See `HEALTH-AND-DISCOVERY.md` for discovery and response semantics.

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

Run `./eng/Test-WindowsPackage.ps1 -Version 0.1.0-test` to build twice and verify identical archive hashes, all checksums, manifest/default configuration, offline diagnostics, and host launch without SpatialAnalyzer.

## Release production

Pushing a tag such as `v0.1.0` runs the verified package build and publishes its ZIP, checksum, and provenance manifest to the corresponding GitHub Release. Manually dispatching the release workflow performs the same build and retains a workflow artifact, but never creates a GitHub Release.
