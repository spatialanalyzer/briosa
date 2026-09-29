# Transform utility commands

The [construction protobuf contract](../../proto/briosa/construction_operations.proto) defines these typed `ConstructionOperations` RPCs:

| Task | RPCs |
| --- | --- |
| Read or calculate a transform | `GetWorkingTransformOfObjectFixedXyz`, `InvertTransform`, `MakeTransformFromDoublesFixedXyz`, `MakeTransformFromDoublesEulerParameters` |
| Decompose a transform into doubles | `DecomposeTransformIntoDoublesFixedXyz`, `DecomposeTransformIntoDoublesEulerXyz`, `DecomposeTransformIntoDoublesEulerZyx`, `DecomposeTransformIntoDoublesEulerZxz`, `DecomposeTransformIntoDoublesEulerZyz` |
| Decompose a transform into vectors | `DecomposeTransformIntoVectorsFixedXyz`, `DecomposeTransformIntoVectorsOriginAndAxes` |
| Decompose a world transform operator | `DecomposeWorldTransformOperatorIntoDoublesFixedXyzInWorld`, `DecomposeWorldTransformOperatorIntoVectorsFixedXyzInWorld` |

A transform contains exactly 16 values. The world-transform operator requires its transform and defaults an omitted scale factor to 1. Decomposition results expose the MP output arguments in their documented order, including the Euler convention in each RPC name.

The read/calculation commands are registered as read-only. The decomposition commands retain the exact-target MP state-mutation classification, so treat them as unsafe to replay. All commands require an MP-ready, single-tenant server. Portable fake-worker and generated-client tests cover typed bindings and result mapping. This migration has **not** executed these commands against a licensed SpatialAnalyzer 2026.1.0529.7 instance. Deadline or cancellation after enqueue can leave completion unknown; inspect the execution disposition before considering replay.
