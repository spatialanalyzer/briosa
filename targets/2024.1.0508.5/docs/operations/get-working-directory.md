# GetWorkingDirectory

`GetWorkingDirectory` implements the SpatialAnalyzer MP command `Get Working Directory` for exact target `2024.1.0508.5`.

## gRPC contract

- Service: `briosa.FileOperations`
- RPC: `GetWorkingDirectory`
- Operation ID: `file_operations.get_working_directory`
- Request: no fields
- Result: optional string `directory` plus shared `execution` details

Example with a Development source host:

```powershell
grpcurl -plaintext -d '{}' 127.0.0.1:50051 `
  briosa.FileOperations/GetWorkingDirectory
```

The returned path is application data. Briosa returns it to the authorized caller but does not log it.

## SDK sequence

The worker performs the sequence on its single SDK-owning STA:

1. `SetStep("Get Working Directory")`
2. `ExecuteStep`
3. `GetMPStepResult`
4. require retrieved MP result code `2`
5. `GetStringArg("Directory", ...)`

If the MP result fails, the output getter is not called. If the getter fails, the RPC fails with `DataLoss` and never substitutes an empty directory.

## Runtime classification

- Effect: read only
- Execution scope: global-state read
- Replay safety: safe
- Risk flag: filesystem metadata

Runtime policy can still deny the operation. `DiscoveryService/ListCapabilities` advertises it only when the current process admits it.

## Validation

Portable tests cover the handwritten request/worker/result mapping, exact SDK call order, success, MP failure, getter failure, policy, cancellation, deadlines, watchdog replacement, readiness, discovery, and log redaction.

The opt-in licensed workflow covers this scenario. It has not been executed against SA 2024.1.0508.5.
