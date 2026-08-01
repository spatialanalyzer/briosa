# Briosa

Briosa is an open-source gRPC bridge around the Hexagon SpatialAnalyzer SDK. This repository builds a separate, version-locked Briosa product for every supported SpatialAnalyzer release.

Briosa does not include SpatialAnalyzer, its SDK, or a license. SpatialAnalyzer must be installed, licensed, and running separately.

## Supported targets

| SpatialAnalyzer target | Product source | Public protobuf package | Generated C# namespace |
| --- | --- | --- | --- |
| `2026.1.0529.7` | [`targets/2026.1.0529.7`](targets/2026.1.0529.7) | `briosa` | `Briosa` |

Each target subtree owns its solution, protobuf contract, server, worker, tests, tools, dependency pins, interop boundary, reference evidence, packaging scripts, and target-specific documentation. Target projects must not reference projects or source from another target. A running server is built for exactly one SA release and controls one active SDK/SA instance; there is no runtime SA-version selector.

A Briosa semantic release such as `1.2.3` produces one server artifact and one protocol artifact per supported target. Artifact and future client-package names carry the SA release, while public RPC names and generated language namespaces remain stable to make ordinary SA upgrades straightforward.

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

## License

Briosa is licensed under Apache-2.0. SpatialAnalyzer, the SA SDK, their brands, proprietary binaries, and proprietary implementation remain Hexagon intellectual property.
