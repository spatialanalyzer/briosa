# Crib sheets and laser projection

These three `briosa.InstrumentOperations` RPCs are implemented for SA
2024.1.0508.5. They have portable mapping and fake SDK coverage, but no licensed
2024 runtime or hardware validation. They are unsafe to replay automatically.

| RPC | Required inputs | Behavior |
| --- | --- | --- |
| `RunCribSheet` | `collection`, `crib_sheet_name`, `instrument` | Starts the named crib sheet. The instrument must be in the active collection with its interface running. |
| `ProjectObjects` | `instrument`, nonempty `objects_to_project` | Requests projection of the supplied objects with the selected projector. |
| `StopProjection` | `instrument` | Independent stop request. No preceding call or caller-owned projection session is required. |

All three return `MpExecutionDetails` on success. Briosa retrieves the MP result
after `ExecuteStep` returns true and reports success only for code `2`. Other
codes are preserved in `OperationError.mp_execution` in the binary gRPC error
trailer. A non-success projection result does not prove that nothing was
projected; inspect the raw result and reconcile the projector state. A deadline
or cancellation does not prove a crib sheet or projector stopped. There is no
automatic replay or automatic stop after an ambiguous outcome.

## Opt-in licensed scenarios — not yet executed

Obtain explicit maintainer permission, an exact licensed 2024 installation,
reviewed SDK/application identity evidence, and a safe instrument fixture before
running these scenarios. Start and connect the matching Briosa server using the
[lifecycle workflow](../development/local-grpc-server.md). The probe does not start
SA, attach another experimental SDK client, or configure identity attestations.

Use one operation per probe invocation. Supply a JSON request file containing
the approved fixture names and IDs. For example, a crib-sheet request has this
shape (replace the fixture values):

```json
{
  "collection": { "name": "fixture" },
  "cribSheetName": "reviewed-crib",
  "instrument": { "collectionName": "fixture", "instrumentId": 1 }
}
```

From the target directory:

```powershell
dotnet run --project tools/Briosa.SmokeClient -c Release -- `
  --licensed-instrument run-crib-sheet `
  --confirm-licensed-sa2024 `
  --address http://127.0.0.1:50051 `
  --timeout-seconds 120 `
  --request-file crib-fixture.json
```

For projection, choose `project-objects` and supply `instrument` plus
`objectsToProject`, a list of collection/object names with their exact object
types. For the independent stop scenario, choose `stop-projection` and supply
only `instrument`. Only loopback HTTP endpoints are accepted by this probe.

Validate a known-good crib sheet, a known-good projection, a deliberately reviewed
missing-object projection case, and an independent stop. Record structural
outcomes and raw MP result codes together with human confirmation of instrument
behavior. The probe prints no request values, fixture paths, or raw exception
text. It never retries. Configure the server watchdog and probe deadline for the
approved fixture duration; on an unknown outcome, reconcile the instrument state
before deciding whether another request is appropriate.

The protected read-only CI workflow does not automatically run these hardware
actions. An unexecuted scenario remains an at-risk validation gap.
