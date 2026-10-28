# Protocol artifacts

The protocol artifact is a deterministic snapshot of Briosa's handwritten protobuf source for ordinary ecosystem client generation. It contains:

- `buf.yaml`;
- the complete `proto` tree;
- a Buf `FileDescriptorSet`;
- this guide and the Apache-2.0 license;
- a manifest with Briosa version, source revision, exact SA target, stable protocol package, source fingerprint, descriptor fingerprint, and file hashes; and
- internal and external SHA-256 checksum files.

It does not contain a command catalog, disposition ledger, generated conformance manifest, client-language template, or Briosa-specific operation generator.

Create an artifact:

```powershell
./eng/New-ProtocolArtifact.ps1 `
  -Version 0.1.0 `
  -SourceRevision <40-character-commit> `
  -OutputDirectory artifacts/protocol
```

The archive name is:

```text
briosa-protocol-<briosa-version>-sa-2026.1.0529.7.zip
```

Verify two independent builds, descriptor equivalence, manifests, and checksums:

```powershell
./eng/Test-ProtocolArtifact.ps1 -Version 0.1.0
```

Client repositories should verify the published checksum, generate their transport types with standard protobuf/gRPC tools, and add any language-idiomatic convenience layer as reviewed source in that client repository. The protobuf RPC and fields remain the shared API authority.

Use the artifact for the exact SA target that the server reports in discovery. Both targets use the `briosa` package and mostly the same message names, but a same-named message can differ on the wire where the SA releases differ. For example, `ConstructObjectsFromSurfaceFacesRuntimeSelectRequest` takes an object-type choice and a point offset for SA 2026.1.0529.7, and seven per-shape Booleans for SA 2024.1.0508.5, on the same field numbers. A request built from the other target's artifact can be rejected or misread. The first-party clients check the exact target during installation selection, before any call, and a raw client must do the same. [eng/cross-target-protocol-allowlist.json](../../../../eng/cross-target-protocol-allowlist.json) lists every reviewed case, and CI fails on any new one.

A Briosa release may add or change an operation only through the normal protobuf compatibility policy and the handwritten vertical-slice review described by the [operation and protocol model](../../../../docs/architecture/operation-and-protocol-model.md).
