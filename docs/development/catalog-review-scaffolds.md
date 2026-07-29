# Catalog review scaffolds

Issue [#59](https://github.com/spatialanalyzer/briosa/issues/59) adds a deterministic review boundary between a disposition-approved command and the supported public catalog. A scaffold is an incomplete review aid, not a catalog operation and not a generated public API.

## Safety boundary

The scaffold generator reads four committed sources for one exact SpatialAnalyzer target:

- the extracted command inventory;
- the reviewed disposition ledger;
- the exact-target value-family evidence catalog; and
- the current supported catalog.

It writes only to a separate output directory. The command rejects an output directory that contains, equals, or is contained by the supported `catalog` root. Existing supported catalog operations are traced back to their inventory keys and omitted from the scaffold set.

Every scaffold retains the inventory key, inventory and resolved MP-step claims, category path, delivery wave, evidence and decision references, source fingerprints, reviewed disposition summary, ordered command shape, exact SDK bindings, and resolved semantic family. Multi-domain SDK methods consume the reviewed assignment for the exact method, inventory key, and SDK order. A shared method name is never treated as one semantic domain.

Public API decisions remain explicit null blockers:

- operation, category partition, RPC, request, result, and argument names;
- explicit request/result field numbers;
- input requiredness, omission behavior, and default policy;
- effect, replay safety, risk flags, and execution scope;
- per-argument data classification; and
- public summaries, isolation guidance, argument descriptions, and catalog evidence.

The reviewed disposition and input-resolution values are preserved as evidence beside those blockers. Inventory key and SDK order are already exact evidence identities, but public category/name and field-number choices remain blocked. They are not inferred from sort order or copied automatically into the public catalog. A scaffold uses `scaffold.schema.json`, has `review_status: incomplete`, and cannot pass supported-catalog validation or generate protocol/server artifacts.

## Generate a review set

Run the generator from the repository root and keep its output under ignored `artifacts`:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- `
  catalog-scaffold-generate `
  inventory/sa/2026.1.0529.7/inventory.json `
  disposition/sa/2026.1.0529.7 `
  values/sa/2026.1.0529.7/catalog.json `
  catalog `
  artifacts/catalog-scaffolds/2026.1.0529.7
```

The manifest accounts for the complete reviewed candidate pool as existing catalog operations plus incomplete scaffolds. Candidate filenames are the SHA-256 of the exact inventory key; they do not invent or approve a public operation name.

Generation is incremental and conflict-reporting:

- an identical existing scaffold is left unchanged;
- a missing scaffold is created;
- an altered scaffold is reported as a conflict and is not overwritten;
- a previously generated scaffold that is no longer expected is reported for manual removal; and
- when any conflict exists, no scaffold or manifest is changed.

Review scaffolds as generated evidence. Do not edit them into a second source of truth. Place reviewer-owned decisions in a new supported catalog operation file.

## Promote one operation

Promote small, risk-ordered subsets rather than copying the complete scaffold set into `catalog`.

1. Confirm that the source fingerprints still match a freshly generated scaffold.
2. Review the resolved MP step, MP/documented ordinal, and distinct SDK order against the cited evidence.
3. Carry the exact `inventory_key`, register or reuse one stable category partition, and choose operation, RPC, request, result, argument, and explicit request/result field identities under [ADR 0021](../architecture/0021-exact-target-protobuf-partitions-and-identifiers.md).
4. Review input presence, omission, and default behavior; a retained candidate or prior disposition value is not automatic public policy.
5. Review effect, replay safety, execution scope, risk flags, data classifications, and isolation guidance under ADRs 0015, 0018, and 0019.
6. Write original public descriptions and select the exact catalog evidence references. Inventory traceability comes from the first-class `inventory_key`; evidence text no longer acts as an identity parser.
7. Add the completed operation file to the exact-target catalog manifest and increment the target-local catalog revision.
8. Run catalog validation, scaffold verification, generated-artifact verification, and the relevant portable command tests.

Removing a scaffold from an ignored local output does not promote or exclude a command. Only a complete operation listed by the supported catalog manifest can generate a public operation.

## Verification

Ordinary CI runs:

```powershell
./eng/Verify-CatalogScaffolds.ps1
```

The verifier produces two clean trees, compares their complete file and byte hashes, validates the manifest and every scaffold against the versioned schemas, verifies all manifest hashes, and checks that every approved candidate is accounted for. It requires neither SpatialAnalyzer, an SA license, proprietary binaries, nor the uncommitted vendor evidence corpus.
