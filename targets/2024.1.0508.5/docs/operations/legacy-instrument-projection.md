# Legacy instrument projection and crib sheet commands

SpatialAnalyzer 2024.1.0508.5 exposes three typed `InstrumentOperations` RPCs in the [instrument protobuf contract](../../proto/briosa/instrument_operations.proto):

| RPC | Purpose |
| --- | --- |
| `RunCribSheet` | Run a named crib sheet for an instrument in a collection. |
| `ProjectObjects` | Project a required list of collection objects with an instrument. |
| `StopProjection` | Stop projection for an instrument. |

All three commands control an instrument and mutate SpatialAnalyzer state. They are unsafe to replay after an ambiguous completion. `RunCribSheet` may run for an extended period. The server must be MP-ready and single-tenant.

Portable fake-worker tests cover typed bindings, required inputs, and a generated-client route. These commands have **not** been executed against a licensed SpatialAnalyzer 2024.1.0508.5 instance; they remain at-risk pending protected licensed validation.
