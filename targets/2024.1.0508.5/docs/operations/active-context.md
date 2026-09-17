# Active-context read operations

This workflow adds three strongly typed, read-only MP commands for inspecting the current SpatialAnalyzer context on exact target `2024.1.0508.5`:

| Service/RPC | MP step | Result fields |
| --- | --- | --- |
| `ConstructionOperations/GetActiveCollectionName` | `Get Active Collection Name` | `currently_active_collection_name` |
| `UtilityOperations/GetActiveUnits` | `Get Active Units` | `length`, `angular`, `temperature` |
| `UtilityOperations/GetWorkingFrameProperties` | `Get Working Frame Properties` | `frame_name`, `collection_name`, `working_frame` |

All requests are empty. Every returned scalar has explicit protobuf presence, and `working_frame` is a typed `CollectionObjectName` with `collection_name`, `object_name`, and the exact 26-choice object-type domain. The values describe live application state; Briosa returns them to the caller but does not log them.

## SDK sequences

The worker executes each sequence atomically on its single SDK-owning STA. Output getters run only after `ExecuteStep` succeeds and `GetMPStepResult` retrieves success code `2`.

`GetActiveCollectionName` then calls:

1. `GetStringArg("Currently Active Collection Name", ...)`

`GetActiveUnits` then calls, in order:

1. `GetStringArg("Length", ...)`
2. `GetStringArg("Angular", ...)`
3. `GetStringArg("Temperature", ...)`

`GetWorkingFrameProperties` then calls, in order:

1. `GetStringArg("Frame Name", ...)`
2. `GetStringArg("Collection Name", ...)`
3. `GetCollectionObjectNameArg("Working Frame", ...)`

The collection-object getter normally carries an SA object-type literal with the object name. The 2026 implementation observed a missing literal for this command. The 2024 port retains the same operation-specific fallback as a compatibility policy; it has not been observed against a live 2024 installation. Because the documented output is specifically a working frame, this operation supplies `Frame` only when that literal is omitted. An embedded literal still takes precedence, and Briosa still fails closed on an unknown embedded type rather than exposing an unreviewed value. This fallback is operation metadata, not a global parser assumption.

## Runtime classification

All three operations are read only, use the `global-state read` execution scope, are safe to replay, and have no additional risk flags. Runtime policy can still deny them; `DiscoveryService/ListCapabilities` reports only the admitted subset.

## Evidence boundary

The [2024 compatibility review](../development/sa2024-compatibility.md) establishes matching MP arguments and SDK methods for these operations. The exact 2024 capture uses `GetStringArg` for `Get Active Collection Name`. No 2026 runtime observation is counted as 2024 validation.

## Validation

Focused portable tests cover the protobuf contracts, handwritten command/result mappings, exact SDK getter order, typed object mapping, and failure on an unknown object type. Registry, discovery, policy, reflection, and shared lifecycle/failure suites cover the same cross-cutting behavior as existing operations. The standard generated-client ready scenario calls all three operations and validates only response structure and retrieval outcomes.

The opt-in licensed runner covers this workflow, but it has not been executed against SA 2024.1.0508.5.
