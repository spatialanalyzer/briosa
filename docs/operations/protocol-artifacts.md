# Protocol artifacts and client conformance

Briosa publishes a runtime-neutral protocol ZIP beside each Windows server release:

```text
briosa-protocol-<briosa-version>-sa-2026.1.0529.7-catalog-<revision>.zip
```

The protocol ZIP is the only supported generation input for `briosa-dotnet`, `briosa-js`, and `briosa-py`. Client repositories must not copy protocol or catalog policy from a branch, documentation page, or another client.

## Contents

The archive contains one top-level directory with:

- `buf.yaml` and `proto/`: canonical protobuf sources;
- `descriptor/briosa.protoset`: a pure `google.protobuf.FileDescriptorSet`;
- `catalog/coverage.json`: exact-target generated operation coverage;
- `conformance/v1/`: live packaged-host scenarios and typed-error/replay cases;
- `manifest.json`: release, protocol, catalog, target, fixture, and hash coordinates;
- `files.sha256`: hashes for every other file in the archive; and
- this guide and the Apache-2.0 license.

Verify the adjacent ZIP checksum before extraction, then verify `files.sha256`. A client release records the ZIP hash plus the manifest's protocol schema hash, descriptor hash, exact target, protocol packages, catalog ID/revision, and conformance fixture IDs.

Archive entries use a stored, uncompressed representation with ordinal path ordering and a fixed timestamp. This is intentional: Deflate output can change between PowerShell/.NET runtime implementations even when every input byte is identical. The stored representation makes the outer ZIP checksum portable across supported Windows build environments while the manifest and `files.sha256` continue to protect every bundled file.

## Generate a client

Use either the `.proto` tree or descriptor set as supported by the language generator. Generate repetitive messages, services, and transport stubs. Keep hand-written code limited to idiomatic adapters and package integration.

Client adapters must:

- preserve protobuf presence rather than replacing absence with a language default;
- use discovery to compare the exact SA target, target protocol package, catalog identity, target isolation mode, readiness, and enabled capabilities;
- decode binary metadata key `briosa-operation-error-bin` as `briosa.core.v1alpha1.OperationError`;
- expose execution disposition independently from recovery and replay guidance;
- never parse gRPC status text for policy; and
- never automatically replay an ambiguous unsafe or unknown operation.

The live scenario file is the common packaged-host test matrix. The typed-error file supplies additional offline cases, including unsafe/unknown ambiguous completion that the initial read-only live operation cannot produce. Client-specific test runners may differ, but fixture IDs and expected public semantics do not.

## Versioning and drift

The Briosa version selects a release. The exact SA target, target protocol package, and catalog revision identify what that release implements. The client package version is independent and does not imply compatibility with other Briosa or SpatialAnalyzer releases.

Regeneration is an explicit dependency update. Download the chosen release artifact, verify its checksum, regenerate, run all shared fixtures, and update the recorded coordinates atomically. CI fails when recorded hashes or coordinates differ from the artifact; it must never regenerate from a moving `main` branch.

## Build locally

Buf 1.72.0 and the repository's .NET SDK are the only protocol-artifact build prerequisites:

```powershell
./eng/New-ProtocolArtifact.ps1 -Version 0.2.0-test
./eng/Test-ProtocolArtifact.ps1 -Version 0.2.0-test
```

The test builds twice in the current PowerShell runtime and, when Windows PowerShell is available, rebuilds the identical bundle there and requires the same outer ZIP SHA-256.

See [ADR 0020](../architecture/0020-protocol-artifacts-and-client-conformance.md) for the authoritative decision.
