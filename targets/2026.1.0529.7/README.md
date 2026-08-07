# Briosa for SpatialAnalyzer 2026.1.0529.7

Briosa is an open-source gRPC bridge around the Hexagon SpatialAnalyzer SDK. It exposes SpatialAnalyzer MP commands as strongly typed, language-neutral RPCs while keeping COM and SDK state inside a separately supervised Windows worker process.

Briosa does not include SpatialAnalyzer, its SDK, or a license. SpatialAnalyzer must be installed, licensed, and running separately.

## Current API

The current exact target is SpatialAnalyzer `2026.1.0529.7`. The handwritten [protobuf contracts](proto/briosa) and [operation registry](src/Briosa.Server/Operations/SpatialAnalyzerApi.cs) define the compiled MP surface; `DiscoveryService/ListCapabilities` reports the runtime-policy-admitted subset. Inventory entries, retained evidence, former catalog entries, prose lists, and code present only in Git history are not supported operations.

The wire package is `briosa` and the generated C# namespace is `Briosa`; the exact SA release belongs to this product and its artifact identity, not to public RPC or type names. The handwritten server mappings are under [src/Briosa.Server/Operations](src/Briosa.Server/Operations). Operation and workflow details are under [docs/operations](docs/operations), including the [active-context read workflow](docs/operations/active-context.md).

## Operation strategy

Briosa delivers complete handwritten MP-operation vertical slices, either individually or in coherent batches. Each operation includes:

1. an MP-compatible strongly typed protobuf RPC;
2. handwritten host, worker-command, result, and SDK mapping;
3. capability and policy registration;
4. portable success and failure tests;
5. an explicit real-SA validation status; and
6. user documentation.

Related operations may share service, workflow, test, and documentation context when every command remains independently reviewable. There is no fixed batch maximum. Standard protobuf and gRPC tools still generate transport plumbing and clients. Briosa has no custom operation generator and no generic public `ExecuteCommand` RPC. See the [operation and protocol model](../../docs/architecture/operation-and-protocol-model.md).

Generative-AI tools may draft a vertical slice from maintainer-provided MP and SDK evidence. Committed source, tests, observations, and engineering review are authoritative.

## Runtime architecture

- The public ASP.NET Core host never owns COM state.
- One supervised worker process owns one SDK connection.
- One worker-owned STA serializes the full `SetStep` → setters → `ExecuteStep` → MP result → getters sequence.
- A timeout or caller cancellation does not imply that an in-flight COM operation stopped.
- Worker replacement restores availability but does not make ambiguous automatic replay safe.
- Readiness requires exact-match SDK and connected-SA identity evidence plus a bounded execution-channel probe.
- The initial target is single-tenant; queue serialization is not cross-client workflow isolation.
- The endpoint binds to loopback by default.
- Audit events contain operation identity and structural outcomes, not command arguments or returned values.

The repository-wide [current architecture](../../docs/architecture/README.md) describes these constraints in detail.

## Build and test

Requirements:

- Windows x64;
- .NET 10 SDK selected by the repository [`global.json`](../../global.json); and
- Buf for protobuf formatting, linting, descriptors, and compatibility checks.

From this target directory:

```powershell
dotnet restore Briosa.slnx --locked-mode
dotnet build Briosa.slnx -c Release --no-restore
dotnet test Briosa.slnx -c Release --no-build --no-restore
./eng/Verify-Protocol.ps1
./eng/Verify-InteropArtifacts.ps1 -NoBuild
```

These commands do not require SpatialAnalyzer, a license, or proprietary SDK installation beyond the approved committed interop boundary.

Package and standard generated-client smoke validation:

```powershell
./eng/Test-WindowsPackage.ps1 -Version 0.1.0-local
./eng/Test-ProtocolArtifact.ps1 -Version 0.1.0-local
./eng/Test-ClientScenarios.ps1 `
  -PackagePath artifacts/package-smoke/briosa-0.1.0-local-sa-2026.1.0529.7-win-x64.zip
```

The smoke scenarios use a separate fake worker and cover readiness, MP failure, output retrieval failure, policy rejection, caller deadline, cancellation, watchdog replacement, and unsupported services without starting SpatialAnalyzer.

## Local licensed SpatialAnalyzer workflow

The conventional source workflow is documented in [Local gRPC server development](docs/development/local-grpc-server.md). It requires a separately installed and licensed exact-target SpatialAnalyzer and independently established activated-SDK and connected-SA identity evidence.

After configuring the documented user secrets and starting exactly one eligible SpatialAnalyzer instance:

```powershell
dotnet run --project src/Briosa.Server --launch-profile SpatialAnalyzer
```

In a second shell:

```powershell
grpcurl -plaintext -d '{}' 127.0.0.1:50051 `
  briosa.FileOperations/GetWorkingDirectory

grpcurl -plaintext -d '{}' 127.0.0.1:50051 `
  briosa.AnalysisOperations/GetNumberOfCollections

grpcurl -plaintext -d '{"collectionIndex":0}' 127.0.0.1:50051 `
  briosa.AnalysisOperations/GetIThCollectionName

grpcurl -plaintext -d '{}' 127.0.0.1:50051 `
  briosa.ConstructionOperations/GetActiveCollectionName

grpcurl -plaintext -d '{}' 127.0.0.1:50051 `
  briosa.UtilityOperations/GetActiveUnits

grpcurl -plaintext -d '{}' 127.0.0.1:50051 `
  briosa.UtilityOperations/GetWorkingFrameProperties
```

Do not attach competing SDK clients. The protected licensed workflow and local runner intentionally report only structural success and never print returned SpatialAnalyzer values.

## Adding an MP operation

Start with a focused GitHub issue for one command or a coherent command batch. Preserve each MP command's established names wherever protobuf and the implementation language permit it. A developer familiar with MP programming should recognize the RPC and fields directly.

The [v1 command-surface planning guide](docs/development/v1-command-surface.md)
defines how an operation is proposed, selected, classified, delivered through
v0.x releases, and frozen for v1. Inventory or historical-candidate membership
does not select an operation.

A typical slice changes:

- `proto/briosa/...`;
- `src/Briosa.Server/Operations/...`;
- the worker SDK seam only when a new exact binding or value codec is needed;
- `SpatialAnalyzerApi.Operations`;
- portable protocol, server, worker, and smoke tests as appropriate; and
- operation documentation and real-SA validation status.

The follow-up must build without editing or extending a Briosa-specific generator.

## Reference evidence

The retained [inventory](inventory), [bindings](bindings), and [values](values) trees are non-authoritative reference snapshots derived from exact-target observations and pinned secondary ObjectiveSA review. They can accelerate implementation review, but they do not define support, approve commands, generate source, or participate in ordinary build completeness gates.

Raw installed documentation, raw View SDK Code, ObjectiveSA source, proprietary binaries, paths, credentials, and licensed data are not copied into the repository. Removed catalogs, dispositions, generated conformance manifests, and historical generated operation artifacts remain recoverable from Git history.

## Packaging and security

The Windows package is self-contained for Briosa but does not bundle SpatialAnalyzer. It contains safe build coordinates and the approved interop provenance; the runtime discovery service reports the admitted operation surface. See [Windows package](docs/operations/windows-package.md), [health and discovery](docs/operations/health-and-discovery.md), and [protocol artifacts](docs/operations/protocol-artifacts.md).

Remote authentication, authorization, TLS, and command-risk policy remain unresolved. Keep production bindings loopback-only.

## License

Briosa is licensed under Apache-2.0. SpatialAnalyzer, the SA SDK, their brands, proprietary binaries, and proprietary implementation remain Hexagon intellectual property.
