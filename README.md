# Briosa

[Documentation](https://briosa.dev/) | [API Reference](https://briosa.dev/api/grpc) | [First MP Command](https://briosa.dev/docs/getting-started/first-request)

Briosa is an open-source gRPC bridge around the Hexagon SpatialAnalyzer SDK. This repository builds a separate, version-locked Briosa product for every supported SpatialAnalyzer release.

Briosa does not include SpatialAnalyzer, its SDK, or a license. SpatialAnalyzer must be installed, licensed, and running separately.

## Supported targets

| SpatialAnalyzer target | Product source | Public protobuf package | Generated C# namespace |
| --- | --- | --- | --- |
| `2026.1.0529.7` | [`targets/2026.1.0529.7`](targets/2026.1.0529.7) | `briosa` | `Briosa` |
| `2024.1.0508.5` | [`targets/2024.1.0508.5`](targets/2024.1.0508.5) | `briosa` | `Briosa` |

Server [0.10.0](https://github.com/spatialanalyzer/briosa/releases/tag/v0.10.0) was published on 2026-11-21 for both targets and requires first-party clients 0.5.x (published with 0.5.0). Server 0.10.x works with clients 0.5.x; clients 0.4.x and earlier do not work with Server 0.10.x, and clients 0.5.x do not work with Server 0.9.x and earlier. It replaces allow/deny lists with named admission profiles and a narrower `standard` default, requires some previously defaulted inputs and permits omission of selected optional arguments, removes nine operations, and adds typed partial-success and SDK-fault outcomes alongside explicit lifecycle bounds. Applications moving from Server 0.9.x must upgrade to the matching 0.5.x first-party client or regenerate direct gRPC bindings from the exact-target protocol artifact, while Server [0.9.2](https://github.com/spatialanalyzer/briosa/releases/tag/v0.9.2) remains available side by side for 0.4.x clients; see [versioning and compatibility](docs/versioning.md), the [Server 0.10.0 release record](docs/development/contract-major-3.md), and its [admission](docs/development/admission-profile-migration.md), [argument](docs/development/argument-defaults-migration.md), and [retirement](docs/development/operation-retirement.md) guides. For the 2024 target, six read-only operations and basic lifecycle behavior passed local licensed checks; broader runtime coverage remains outstanding. See its [compatibility and validation record](targets/2024.1.0508.5/docs/development/sa2024-compatibility.md).

Each target subtree owns its solution, protobuf contract, server, worker, tests, tools, dependency pins, interop boundary, reference evidence, packaging scripts, and target-specific documentation. Target projects must not reference projects or source from another target. A running server is built for exactly one SA release and controls one active SDK/SA instance; there is no runtime SA-version selector.

A Briosa semantic release such as `1.2.3` produces one server artifact and one protocol artifact per supported target. Artifact and client-package names carry the SA release, while public RPC names and generated language namespaces remain stable to make ordinary SA upgrades straightforward.

## Build a target

Requirements are Windows x64, the .NET 10 SDK selected by [`global.json`](global.json), and Buf.

```powershell
cd targets/2026.1.0529.7
dotnet restore Briosa.slnx --locked-mode
dotnet build Briosa.slnx -c Release --no-restore
dotnet test Briosa.slnx -c Release --no-build --no-restore
./eng/Verify-Protocol.ps1
./eng/Verify-InteropArtifacts.ps1 -NoBuild
```

Ordinary builds and tests do not require SpatialAnalyzer or a license. See the [SA 2026.1.0529.7 target guide](targets/2026.1.0529.7/README.md) for its API, package, smoke-test, and licensed-development workflows.

## Add another SpatialAnalyzer target

Adding a target is an explicit product fork, not a shared-project extension:

1. Create `targets/<exact-sa-release>/` as a complete copy of the closest reviewed target.
2. Change the exact SA identity, interop input/provenance, dependency pins, evidence, and target documentation inside the new subtree.
3. Review every retained MP operation against the new release; do not assume compatibility merely because the public `briosa` package is stable.
4. Keep all project references and source includes inside the new target subtree.
5. Add the target to the explicit matrices in CI and release workflows and add or update its protected licensed workflow.
6. Produce target-qualified server and protocol artifacts and validate exact-version mismatch rejection before MP execution.

Repository-wide governance, current architecture, workflow policy, and release orchestration remain at the root. See the [exact-target product model](docs/architecture/exact-target-product-model.md).

## Maintainer Infrastructure

See the [maintainer runbooks](docs/maintainers/README.md) for Azure release signing
and the `briosa.dev` domain, documentation, and download hosting setup.

## License

Briosa is licensed under Apache-2.0. SpatialAnalyzer, the SA SDK, their brands, proprietary binaries, and proprietary implementation remain Hexagon intellectual property.
