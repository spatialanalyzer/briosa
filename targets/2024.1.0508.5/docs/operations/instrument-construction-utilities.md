# Instrument construction utilities

The [instrument protobuf contract](../../proto/briosa/instrument_operations.proto) defines eight typed `InstrumentOperations` RPCs in this family:

| Task | RPCs |
| --- | --- |
| Build and combine geometry | `BuildTarget`, `CombinePointGroups`, `DissectPointGroup` |
| Construct mirror and perimeter geometry | `ConstructMirrorFromPlane`, `ConstructMirrorFromTwoPoints`, `ConstructPerimetersFromSurfaceFaceList` |
| Set up instrument references | `CreateNewDynamicReference`, `CreateTemplatedInstrumentUsmn` |

`BuildTarget` requires an instrument, output and nominal points, and tolerance options. Its HTML prompt file is omitted when absent. `ConstructMirrorFromTwoPoints` sends the mirror to the instrument by default and returns the mirror plane. `ConstructPerimetersFromSurfaceFaceList` returns separate scan and exclusion perimeter lists. `CreateTemplatedInstrumentUsmn` preserves the MP enable flags and weight defaults; callers can explicitly override each optional value.

These commands mutate SpatialAnalyzer state and require an MP-ready, single-tenant server. They are unsafe to replay after an ambiguous completion. Portable fake-worker and generated-client tests cover bindings, defaults, required inputs, and output mapping. They have **not** been executed against a licensed SpatialAnalyzer 2024.1.0508.5 instance.
