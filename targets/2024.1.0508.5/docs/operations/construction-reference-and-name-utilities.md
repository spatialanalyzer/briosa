# Construction reference and name utilities

The `ConstructionOperations` RPCs below build or select typed SpatialAnalyzer references. Their request and result fields are defined in the [construction protobuf contract](../../proto/briosa/construction_operations.proto). Call `DiscoveryService/ListCapabilities` to see which RPCs the running server's policy admits.

| Purpose | RPCs |
| --- | --- |
| Instrument reference variables and selection | `GetCollectionInstrumentRefListVariable`, `SetCollectionInstrumentRefListVariable`, `MakeCollectionInstrumentIdRuntimeSelect`, `MakeCollectionInstrumentRefListRuntimeSelect` |
| Relationship, event, picture, and report lists | `MakeRelationshipRefListRuntimeSelect`, `MakeRelationshipRefListWildcardSelection`, `MakeEventRefListWildcardSelection`, `MakePictureNameRefListRuntimeSelect`, `MakeReportRefListFromCollection`, `MakeReportRefListRuntimeSelect` |
| Vector group and vector names | `MakeCollectionVectorGroupNameRefListRuntimeSelect`, `MakeVectorNameRefListFromVectorGroup`, `MakeVectorNameRefListRuntimeSelect`, `MakeVectorNamesUniqueInVectorGroup`, `ConstructVectorGroupFromVectorNameRefList` |
| Other typed references | `ConstructSurfacesByDissectingSurfacesFromRefList`, `MakeSystemString` |

Required reference lists must contain at least one entry. The wildcard selectors default omitted criteria to `*`; an explicitly supplied empty string remains empty. `MakeVectorNameRefListRuntimeSelect` uses the MP prompt ` Select Vectors (ENTER when done) ` when its prompt is omitted. `MakeSystemString` requires a supported system-string choice and sends its format string only when supplied. Runtime-select commands may require user interaction in SpatialAnalyzer. Result lists preserve their typed object, item, instrument, or vector identities.

The ordinary build and focused tests use a fake worker, including a generated gRPC client call. These 17 mappings have **not** been validated against a licensed SpatialAnalyzer 2024.1.0508.5 instance in this migration. A successful `ConnectEx` alone is insufficient for MP readiness; see the [runtime lifecycle](../../../../docs/architecture/runtime-boundary-and-lifecycle.md). Caller deadline or cancellation after enqueue can leave completion unknown, so do not automatically replay a mutating command.

`MakeCalloutViewRefListWildcardSelection` and `AddCollectionInstrumentsToRefListWildcardSelection` were removed for contract major 3. Both returned MP code 2 on this target, but their list output was never retrievable (probes 7 and 14 of the [licensed probe record](../testing/evidence/probe-277-2024.1.0508.5-2026-10-07.md)), and the exact-target SDK has no getter for it. `MakeCollectionItemNameRefListWildcardSelection` with the callout-view item type selects callout views through a different MP step; that item type has not been observed on a licensed run. See [operation retirement](../../../../docs/development/operation-retirement.md).
