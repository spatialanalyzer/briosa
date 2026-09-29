# Construction reference and name utilities

The `ConstructionOperations` RPCs below build or select typed SpatialAnalyzer references. Their request and result fields are defined in the [construction protobuf contract](../../proto/briosa/construction_operations.proto). Call `DiscoveryService/ListCapabilities` to see which RPCs the running server's policy admits.

| Purpose | RPCs |
| --- | --- |
| Instrument reference variables and selection | `GetCollectionInstrumentRefListVariable`, `SetCollectionInstrumentRefListVariable`, `AddCollectionInstrumentsToRefListWildcardSelection`, `MakeCollectionInstrumentIdRuntimeSelect`, `MakeCollectionInstrumentRefListRuntimeSelect` |
| Callout, relationship, event, picture, and report lists | `MakeCalloutViewRefListWildcardSelection`, `MakeRelationshipRefListRuntimeSelect`, `MakeRelationshipRefListWildcardSelection`, `MakeEventRefListWildcardSelection`, `MakePictureNameRefListRuntimeSelect`, `MakeReportRefListFromCollection`, `MakeReportRefListRuntimeSelect` |
| Vector group and vector names | `MakeCollectionVectorGroupNameRefListRuntimeSelect`, `MakeVectorNameRefListFromVectorGroup`, `MakeVectorNameRefListRuntimeSelect`, `MakeVectorNamesUniqueInVectorGroup`, `ConstructVectorGroupFromVectorNameRefList` |
| Other typed references | `ConstructSurfacesByDissectingSurfacesFromRefList`, `MakeSystemString` |

Required reference lists must contain at least one entry. The wildcard selectors default omitted criteria to `*`; an explicitly supplied empty string remains empty. `MakeVectorNameRefListRuntimeSelect` uses the MP prompt ` Select Vectors (ENTER when done) ` when its prompt is omitted. `MakeSystemString` requires a supported system-string choice and sends its format string only when supplied. Runtime-select commands may require user interaction in SpatialAnalyzer. Result lists preserve their typed object, item, instrument, or vector identities.

The ordinary build and focused tests use a fake worker, including a generated gRPC client call. These 19 mappings have **not** been validated against a licensed SpatialAnalyzer 2026.1.0529.7 instance in this migration. A successful `ConnectEx` alone is insufficient for MP readiness; see the [runtime lifecycle](../../../../docs/architecture/runtime-boundary-and-lifecycle.md). Caller deadline or cancellation after enqueue can leave completion unknown, so do not automatically replay a mutating command.
