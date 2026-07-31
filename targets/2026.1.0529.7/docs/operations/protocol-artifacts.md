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

A Briosa release may add or change an operation only through the normal protobuf compatibility policy and the handwritten vertical-slice review described by [ADR 0024](../../../../docs/architecture/0024-handwritten-mp-operation-vertical-slices.md).
