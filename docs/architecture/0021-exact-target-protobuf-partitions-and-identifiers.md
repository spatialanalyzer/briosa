# ADR 0021: Exact-target protobuf partitions and stable identifiers

- Status: Accepted for the v0.2 command surface
- Date: 2026-07-28
- Issue: [#60](https://github.com/spatialanalyzer/briosa/issues/60)
- Amends: [ADR 0005](0005-exact-sa-target-protocols.md), [ADR 0006](0006-versioned-command-catalog.md), and [ADR 0009](0009-catalog-derived-operation-artifacts.md)

## Context

The reviewed exact-target surface may contain hundreds of operations. One target-wide `operations.proto` file would make unrelated categories share a review and breaking-change boundary. Deriving identifiers or field numbers from the current set of commands would also make a later addition capable of renaming, renumbering, or moving an already published operation.

Raw MP step names are not unique. Category spelling can normalize to the same protobuf identifier, the same RPC name can occur in several categories, and request and result messages share the package-level protobuf symbol namespace. A collision policy based on enumeration order, numeric suffixes, or hashes would be deterministic for one snapshot but unstable as that snapshot grows.

The catalog also needs a stable join back to the exact inventory occurrence and its SDK ordering. MP documentation ordinal, SDK order, and protobuf field number are independent claims and must not be conflated.

## Decision

Each exact-target catalog manifest owns an ordinally sorted `protocol_partitions` registry. A partition records the reviewed SA/Briosa category, its immutable lower-snake-case alias, the PascalCase service, and the lower-snake-case `.proto` filename. The service and filename are the exact deterministic transforms of the alias. Every operation's category, operation-ID prefix, and service must resolve to exactly one partition.

The generator emits one protobuf file and one service per partition while retaining the target's single package, for example:

```text
proto/briosa/sa/v2026_1_0529_7/v1alpha1/
  file_operations.proto  # service FileOperations
  values.proto
  specialized_values.proto
```

Adding a category adds a file and service. It never moves an existing service. A category rename or repartition after publication is a breaking API change and requires a new package line unless the published symbols remain in their original file.

Every supported operation records these independent identities:

- `inventory_key`: the exact inventory occurrence; unique within the target catalog;
- `operation_id`: `<partition_alias>.<operation_alias>` in canonical lower snake case;
- exact `mp_step`: allowed to duplicate another operation's raw step text;
- protocol service, RPC, request, and result names;
- exact fully qualified method derived as `/<package>.<service>/<rpc>`;
- each argument's MP/documented `ordinal`, distinct `sdk_order`, stable field name, and explicit request/result field numbers.

RPC names are unique across the exact-target package, not merely within one service. Request and result type names must be unique across the package and may not collide with a service or each other. Generated binding type names and fully qualified methods must also be unique. Category aliases, services, and protobuf filenames are unique.

The validator rejects every unresolved collision. It reserves the filenames and top-level package symbols declared by the target's fixed, non-catalog protobuf files, including `values.proto`, `specialized_values.proto`, and types such as `PointName`; a category cannot replace them. It never chooses a winner or appends an order-dependent number, category name, or hash. A new command that collides must receive a reviewed, semantically meaningful operation alias while every published identity remains unchanged. Duplicate raw MP step strings remain valid only because distinct explicit inventory and Briosa operation identities disambiguate them.

## Field allocation

Each catalog argument explicitly stores nullable `request` and `result` field numbers. An input requires only a request number, an output requires only a result number, and an input/output argument requires both. Numbers are unique within their message, use the protobuf legal range, exclude protobuf's reserved 19000–19999 range, and may not use 1000. Result field 1000 remains the shared `MpExecutionDetails execution` field; reserving it in both message directions prevents later reinterpretation.

The initial reviewed number normally follows the MP ordinal when that produces a legal free number, but generation never recalculates it. Reordering documentation, adding another argument, or inserting another operation cannot change a committed field number. MP ordinal, SDK order, and request/result field numbers remain separately reviewable in the catalog, coverage manifest, and generated reference documentation.

Removed published field names and numbers must be reserved in the owning protobuf message. That future catalog representation is intentionally deferred until a removal is proposed; deletion without a reservation cannot pass the released-baseline breaking check.

## Compatibility and validation

`CommandCatalogGenerator` first runs semantic catalog validation, so fixed-file, package-symbol, operation-derived, or field-allocation collisions fail before files are written. Validation reads the fixed target protocol from the repository-layout sibling `proto` tree; an isolated copied catalog must carry that sibling protocol context and fails closed when it is absent. As a second boundary, the generator refuses to overwrite an existing destination unless the file carries the catalog generator's marker. Buf then formats, lints, and compiles the complete package and its imports.

After the first public release, `eng/Verify-Protocol.ps1 -AgainstRef <released-ref>` applies Buf's `FILE` policy. The explicit released ref—not `main`—guards service/file ownership, fully qualified methods, type identity, field names, field numbers, and wire types. Adding a new partition or operation is compatible; moving a published symbol between category files is not.

All checks are deterministic and require neither SpatialAnalyzer, an SDK connection, proprietary evidence, nor a license.

## Consequences

- Reviews and generated diffs stay category-scoped while clients keep one exact-target package.
- Public identities are selected once and never depend on future catalog membership or sort order.
- Common command names require explicit semantic disambiguation before promotion.
- The first-class inventory key and SDK order give later adapter generation an exact assignment key without changing the public wire contract.
- Catalog promotion requires deliberate field allocation in addition to semantic, risk, and binding review.
