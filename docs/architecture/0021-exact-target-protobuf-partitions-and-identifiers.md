# ADR 0021: Exact-target protobuf partitions and stable identifiers

- Status: Accepted; generation mechanics superseded by [ADR 0024](0024-handwritten-mp-operation-vertical-slices.md)
- Date: 2026-07-28
- Revised: 2026-07-31
- Issue: [#60](https://github.com/spatialanalyzer/briosa/issues/60)
- Amends: [ADR 0005](0005-exact-sa-target-protocols.md)

## Context

An exact-target API may eventually contain many operations. One target-wide `operations.proto` file would make unrelated categories share a review and breaking-change boundary. Identifiers or field numbers derived from the current command set would also let a later addition rename, renumber, or move an already published operation.

Raw MP step names are not guaranteed unique. MP documentation ordinal, SDK setter/getter order, and protobuf field number are independent claims and must not be conflated.

## Decision

Handwritten exact-target protobuf contracts are partitioned by stable MP/Briosa category. The initial file is:

```text
proto/briosa/sa/v2026_1_0529_7/v1alpha1/
  file_operations.proto  # service FileOperations
```

Adding a category normally adds a file and service. It never moves a published service merely to rebalance file sizes. A category rename or repartition after publication is a breaking API change unless the published symbols remain in their original file.

Every supported operation selects these identities explicitly in reviewed source:

- stable operation ID, normally `<category_alias>.<operation_alias>`;
- exact MP step;
- protobuf service, RPC, request, and result names;
- exact fully qualified method `/<package>.<service>/<rpc>`;
- MP argument name and direction;
- SDK setter/getter order and method; and
- stable request and result field numbers.

Names remain mechanically recognizable to MP programmers wherever protobuf permits it. When two commands would collide, the operation issue selects a meaningful disambiguation; tooling must not append a sort-order number or hash.

## Field allocation

Input and output field numbers are assigned explicitly in the handwritten `.proto` message. Result field `1000` is reserved for shared `MpExecutionDetails execution`. MP ordinal, SDK order, and protobuf field number remain separately reviewable.

Removed published field names and numbers must be reserved in the owning message. Reordering documentation, adding an argument, or inserting another operation cannot renumber a committed field.

## Compatibility and validation

Buf formats, lints, and compiles the complete package. After the first public release, `eng/Verify-Protocol.ps1 -AgainstRef <released-ref>` applies the `FILE` breaking policy against an explicit released ref.

The handwritten operation tests additionally verify that the descriptor, gRPC route, worker command, MP argument names, and SDK bindings agree. No catalog or Briosa-specific operation generator participates.

## Consequences

- Reviews stay category-scoped while clients keep one exact-target package.
- Public identities are selected once and never depend on future inventory membership or sort order.
- Common command names require explicit semantic disambiguation.
- A pull request can review MP identity, SDK ordering, and public field allocation together.
