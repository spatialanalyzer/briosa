# ADR 0016: Command-argument semantic families for shared SDK bindings

- Status: Accepted
- Date: 2026-07-27
- Issue: [#86](https://github.com/spatialanalyzer/briosa/issues/86)
- Amends: [ADR 0006](0006-versioned-command-catalog.md), [ADR 0009](0009-catalog-derived-operation-artifacts.md)

## Context

SpatialAnalyzer SDK method names do not uniquely identify the semantic type of an MP argument. In SA 2026.1.0529.7, `SetCollectionObjectNameArg2`, `GetCollectionObjectNameArg`, and their reference-list counterparts carry both collection objects and the broader collection-item domain.

The domains are not interchangeable. `ObjectType` has 26 exact choices. `ItemType` has 42 choices and additionally includes non-geometric entities such as annotations, callout views, charts, pictures, feature checks, reports, tables, and events. Treating the method name as the type would either discard valid item values or incorrectly admit item-only values where a command requires an object.

The unavailable standalone `SetItemTypeArg` is a separate binding fact. It does not make `ItemType` unavailable when an exact composite setter or getter carries that type as one component.

## Decision

A semantic value family belongs to an exact command argument, not to an SDK method name. One SDK method may therefore serve multiple reviewed families.

For SA 2026.1.0529.7:

- `CollectionObjectName` contains a collection name, object name, and the exact `ObjectType` enum.
- `CollectionItemName` contains a collection name, item name, and the exact `ItemType` enum.
- scalar and reference-list forms remain distinct public and worker value kinds;
- both families may dispatch through the same exact composite SDK methods;
- returned SDK type literals are parsed against the selected command-argument family, and an unknown or out-of-family literal makes retrieval fail;
- no generic string, integer, or object-type fallback is permitted.

Protocol and worker plumbing may be implemented before catalog promotion. A generated operation may use either family only after the binding review assigns that exact inventory command argument to one family. Issue [#87](https://github.com/spatialanalyzer/briosa/issues/87) owns the evidence-backed assignment catalog and completeness checks for all shared-method observations.

Exact-target evidence has precedence. The current ObjectiveSA wrapper is useful secondary evidence for calling and marshaling conventions, but its earlier-release types cannot establish the SA 2026.1.0529.7 member set or command assignment.

## Consequences

- Shared SDK method names no longer collapse distinct public semantics.
- The same native call path remains direct and allocation-light; family selection happens in generated code, not through runtime reflection or catalog lookup.
- Unknown returned type strings fail closed rather than leaking as raw public strings.
- `SetItemTypeArg` remains blocked under issue #79 while composite collection-item values remain implementable.
- Adding another exact SA target requires independent object/item member evidence and command-argument assignments.
