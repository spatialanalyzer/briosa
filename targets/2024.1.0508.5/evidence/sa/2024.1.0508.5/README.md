# SA 2024.1.0508.5 binding facts

[`bindings.json`](bindings.json) records curated exact-target binding facts for
the operations registered by this target. It covers the implemented subset only,
as approved in [#247](https://github.com/spatialanalyzer/briosa/issues/247), and
is the 2024 input for the implemented-subset evidence cross-check.

The file is reference evidence. It does not register, select, or generate an
operation (AGENTS.md invariant 6), define defaults, or prove runtime behavior.
The handwritten registry, protobuf contracts, and runtime capability discovery
define the supported surface. The file must not be used to generate source.

## Boundary

- Curated facts only: step text, argument labels, direction, the published
  argument kind, and SDK method names. No vendor prose, samples, choice lists, or
  installed documentation are copied.
- No Hexagon binaries or raw command exports are committed.
- Public CI never reads private evidence. The portable test reads only this
  directory, the target's operation registry, and its committed interop surface.

## Provenance

| Source | Use | Recorded as |
| --- | --- | --- |
| `spatialanalyzer/briosa-docs` MP command catalog at `provenance.public_catalog.revision` | Primary source for every operation | `source.document` and `source.anchor`; `name_provenance: "briosa-docs"` |
| Private `spatialanalyzer/briosa-evidence` at `07acae44ad6cbed11391b38391317c4974910af8`, `2024.1.0508.5/mp-command-step-exports` | Only restores embedded double quotes that the catalog strips from labels | `name_provenance: "briosa-evidence@07acae4"` |
| `interop/SpatialAnalyzer/2024.1.0508.5` public API | Flags exporter helper names that the 2024 interface does not expose | argument `findings` |

Each catalog command section yields one of two source kinds:

- `captured_2024_signature`: the section's 2024 details contain a
  "Captured 2024 Argument Signature" block. Labels, direction, and argument kind
  come from it. Each published argument kind names one exporter call family; the
  curation script maps it to the exact method for the argument's direction
  (`method_basis: "captured_argument_kind"`). "Binding not emitted by the SDK
  exporter" is recorded as `unavailable` (an exporter `NOT_SUPPORTED` call).
- `reference_signature`: the 2024 details state that the reference signature
  applies to 2024, with any 2024 choice or sample adjustments described in prose
  (finding `reference_signature_with_2024_adjustments`). Labels, direction, and
  the MP type come from the reference table. MP types do not identify an SDK
  method, so methods are `unpublished`, except collection-object inputs whose
  section states that they use the 2024 collection/name binding
  (`method_basis: "collection_object_binding_statement"`,
  `SetCollectionObjectNameArg`). `SDK-unavailable MP argument` is recorded as
  `unavailable`. Rows without an exact backtick label are skipped and flagged
  `reference_row_without_exact_label`.

`none` and `no_public_2024_signature` are reserved for a registered operation
without a current 2024 catalog section.

## Schema

[`bindings.schema.json`](bindings.schema.json) defines the structure. Each
operation has `operation_id`, `mp_step` (the catalog heading, which is the SDK
step text), `source`, `arguments`, and `findings`. Each argument has:

- `ordinal`: position in the cited source table, not necessarily SDK call order.
- `sdk_name`: the exact label, including trailing spaces and embedded quotes.
- `direction`: `input`, `output`, or `input_output`.
- `argument_kind`: the published argument kind or reference MP type.
- `setter` and `getter`: `status` and `method`, using the 2026 inventory status
  vocabulary where it applies: `available`, `unavailable`, `not_observed`, plus
  `unpublished` when direction is known but no public source names the method.
- `method_basis`, `name_provenance`, and `findings`, described above.

The exporter setter for collection-object inputs is the three-argument
`SetCollectionObjectNameArg`. Briosa's reviewed adapter calls the four-argument
`SetCollectionObjectNameArg2`, which the 2024 interface also exposes; see the
[compatibility record](../../../../docs/development/sa2024-compatibility.md).
This file records the exporter fact and does not settle that mapping.

## Curation procedure

Run from the target directory with local clones. The script resolves the docs
revision to a commit and records it.

```powershell
./eng/Update-Sa2024BindingFacts.ps1 -DocsRepository <briosa-docs> `
  -EvidenceRepository <briosa-evidence>
```

- With `-EvidenceRepository`, it reads the private exports at the fixed revision
  only to find labels whose quote-stripped form equals a catalog label.
- Without it, it reuses the quote corrections already committed in
  `bindings.json`, so maintainers without private access can refresh public facts.
- `-DocsRevision <commit>` pins the catalog; `-Check` regenerates in memory and
  fails if the committed file differs.

Regenerate after the catalog changes or an operation is added. Review the diff;
an unmapped argument kind, unknown direction, or ambiguous catalog entry stops
the script rather than guessing. CI does not run this script.
