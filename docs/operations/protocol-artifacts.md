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
- `conformance/v1/`: vertical-slice and Wave 1 packaged-host scenarios plus typed-error/replay cases;
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

The vertical-slice and Wave 1 read-only scenario files are common packaged-host test matrices. The Wave 1 matrix covers generated success, required-input validation, policy denial, and MP failure without carrying or reporting returned values. The typed-error file supplies additional offline cases, including unsafe/unknown ambiguous completion that the initial read-only live operation cannot produce. Client-specific test runners may differ, but fixture IDs and expected public semantics do not.

## Versioning and drift

The Briosa version selects a release. The exact SA target, target protocol package, and catalog revision identify what that release implements. The client package version is independent and does not imply compatibility with other Briosa or SpatialAnalyzer releases.

Regeneration is an explicit dependency update. Download the chosen release artifact, verify its checksum, regenerate, run all shared fixtures, and update the recorded coordinates atomically. CI fails when recorded hashes or coordinates differ from the artifact; it must never regenerate from a moving `main` branch.

## Build prerequisites and entrypoints

Build from the repository root with PowerShell 7 and Buf 1.72.0. Git is required only when `-SourceRevision` is omitted; the script then records the current `HEAD`. The protocol artifact scripts do not install, launch, or connect to SpatialAnalyzer and require neither an SA license nor proprietary SDK binaries. The repository's .NET SDK remains necessary for the wider build and client-conformance workflow, but the protocol ZIP producer itself invokes Buf rather than `dotnet`.

`Test-ProtocolArtifact.ps1` records the verified descriptor-set byte count against the repository-owned `descriptor-size` CI budget. The JSON metric is written below `artifacts/ci-metrics` and uploaded by ordinary CI. See the [full-surface gate guide](../development/full-surface-gates.md) for the threshold and required review evidence before adjustment.

Create one artifact and its sidecars with explicit release coordinates:

```powershell
$sourceRevision = (git rev-parse HEAD).Trim()
./eng/New-ProtocolArtifact.ps1 `
  -Version 0.2.0-test `
  -SourceRevision $sourceRevision `
  -OutputDirectory artifacts/protocol-test `
  -BufPath buf
```

The output directory receives the protocol ZIP, its adjacent `.zip.sha256`, and an external `.provenance.json` copy of the bundled manifest. `-OutputDirectory` defaults to `artifacts`, `-BufPath` defaults to `buf` on `PATH`, and `-SourceRevision` defaults to the current Git commit for local builds.

Verify the full publication contract with:

```powershell
./eng/Test-ProtocolArtifact.ps1 `
  -Version 0.2.0-test `
  -OutputDirectory artifacts/protocol-test `
  -BufPath buf
```

The test builds twice in the current PowerShell runtime; verifies the canonical stored representation, ordinal entry order, fixed timestamps, descriptor reconstruction, manifests, checksums, provenance, and fixture coverage; and, when Windows PowerShell is available, rebuilds the identical bundle there and requires the same outer ZIP SHA-256. `eng/New-DeterministicZip.ps1` is the producer's internal canonical-container helper and is not a substitute release entrypoint.

See [ADR 0020](../architecture/0020-protocol-artifacts-and-client-conformance.md) for the authoritative decision.
