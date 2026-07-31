# MP command inventory reference

`inventory/sa/2026.1.0529.7` is a retained, non-authoritative snapshot of curated facts derived from installed SpatialAnalyzer MP documentation and View SDK Code observations.

The snapshot combines:

- 1,302 structured documentation records;
- 1,360 View SDK Code observations; and
- 1,412 reconciled command identities.

The JSON intentionally avoids copying vendor prose or raw generated SDK samples. Its report records aggregate fingerprints, coverage, and unresolved extraction findings.

Inventory membership does not mean that Briosa supports, plans, approves, or has validated a command. The supported surface is the handwritten protobuf and operation source registered in `SpatialAnalyzerApi.Operations`.

Issue #132 retired the inventory extractor and completeness workflow from ordinary development. Git history preserves the exact extractor used for this snapshot. A future evidence-refresh issue may restore or replace extraction tooling after reviewing provenance, redistribution safety, and whether the result is still useful.

When implementing an operation:

1. use the inventory only to locate exact-target evidence;
2. compare it with the maintainer-provided MP command text and current interop surface;
3. resolve discrepancies in the operation issue;
4. hand-author the operation vertical slice; and
5. record real-SA validation status honestly.

See [ADR 0024](../../../../docs/architecture/0024-handwritten-mp-operation-vertical-slices.md).
