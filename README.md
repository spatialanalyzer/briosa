# Briosa

Briosa is an open-source gRPC bridge around the Hexagon SpatialAnalyzer SDK. SpatialAnalyzer must be installed separately before Briosa can perform useful work.

## Current target

The initial vertical slice targets .NET 10 on Windows x64 and SpatialAnalyzer 2026.1.0529.7. The repository is currently establishing its process, COM interop, protocol, generation, and test boundaries.

## Build

Install the SDK selected by `global.json`, then run:

```powershell
dotnet restore Briosa.slnx --locked-mode
dotnet build Briosa.slnx -c Release --no-restore
dotnet test Briosa.slnx -c Release --no-build --no-restore
```

The committed managed interop metadata allows these commands to run on an ordinary Windows x64 machine without installing or starting SpatialAnalyzer.

## Public protocol

Briosa separates the stable `briosa.core.v1alpha1` package from MP contracts generated for one exact SpatialAnalyzer release, beginning with `briosa.sa.v2026_1_0529_7.v1alpha1`. Target packages are independent, version-faithful APIs; matching command shapes never imply matching semantics across SA releases.

Install Buf 1.72.0 and run `./eng/Verify-Protocol.ps1` to check formatting, lint rules, and compatibility with the current `main` baseline. The .NET build compiles the reviewed protobuf sources directly.

See [the exact-SA-target protocol decision](docs/architecture/0005-exact-sa-target-protocols.md) for package layout, version coordinates, compatibility, presence, target isolation, and review rules.

## SpatialAnalyzer interop

Only the worker boundary references the generated COM metadata. Original SpatialAnalyzer binaries and type libraries are not part of this repository.

See [the interop generation guide](docs/development/interop-generation.md) and [the COM boundary architecture decision](docs/architecture/0001-spatialanalyzer-com-boundary.md) for generation, provenance, redistribution, architecture, and STA rules.

## Portable SDK tests

The [fake SDK and contract-test harness](docs/testing/fake-sdk-harness.md) verifies Briosa''s lifecycle, serialization, result handling, and recovery seams without installing or licensing SpatialAnalyzer. The scripted fake tests Briosa contracts and is not a SpatialAnalyzer emulator.

## Worker process lifecycle

The gRPC host supervises SpatialAnalyzer automation in a disposable child worker over a private named pipe. It reports explicit lifecycle snapshots, replaces hung or crashed workers within a bounded restart window, and escalates failed graceful shutdown to process-tree termination.

The host expects `Briosa.Worker.exe` beside the server by default. Development or packaged layouts can set `Briosa__Worker__ExecutablePath` to an explicit worker path. A missing worker degrades SDK readiness without terminating the public host.

Each worker owns one SDK client and reports `Disconnected`, `Connecting`, `Connected`, `Faulted`, or `Stopping` independently from process readiness. The SpatialAnalyzer target defaults to `localhost`; set `Briosa__SpatialAnalyzer__Host` to an explicit hostname or IP address. A connection cycle is bounded to three `ConnectEx` attempts one second apart, and MP work is rejected with a stable unavailable outcome unless the SDK state is `Connected`.

See [the worker process lifecycle decision](docs/architecture/0002-worker-process-lifecycle.md) and [the SDK connection lifecycle decision](docs/architecture/0003-sdk-connection-lifecycle.md), and [the MP execution pipeline decision](docs/architecture/0004-mp-execution-pipeline.md) for protocol, connection ownership, serialization, deadlines, recovery, security, and STA details.

## License

Briosa is licensed under Apache-2.0. SpatialAnalyzer, the SA SDK, their brands, and their proprietary implementation remain Hexagon intellectual property. This project does not imply Hexagon affiliation, endorsement, or support.
