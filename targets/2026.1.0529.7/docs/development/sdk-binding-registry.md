# SDK binding reference snapshot

`bindings/sa/2026.1.0529.7` is retained evidence that reconciles View SDK Code method observations with the committed exact-target interop API and reviewed semantic value families.

It is not runtime registration, proof of command support, or an operation-completeness gate. Historical coverage fields for protocol, worker, adapter, fake, and generator belonged to the retired catalog system and must not be treated as current claims.

For current source:

- `ISpatialAnalyzerSdkCalls` defines the private SDK seam;
- `SpatialAnalyzerSdkAdapter` and its codecs perform exact CLR/COM marshaling;
- worker tests verify reusable binding families directly; and
- each handwritten operation defines only the setter/getter calls it actually uses.

The snapshot remains useful for identifying exact method names, interop-only methods, missing interop observations, and semantic ambiguities. A `usable` historical registry row still does not approve any MP command.

Issue #132 retired registry synchronization and freshness enforcement. Git history preserves the producing code. Refreshing the snapshot is a separate evidence task and must not block an unrelated operation build.

See the [operation and protocol model](../../../../docs/architecture/operation-and-protocol-model.md).
