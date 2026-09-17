# GetNumberOfCollections

`GetNumberOfCollections` implements the SpatialAnalyzer MP command `Get Number of Collections` for exact target `2024.1.0508.5`. Its naming deliberately stays recognizable to developers who already program MPs.

## gRPC contract

- Service: `briosa.AnalysisOperations`
- RPC: `GetNumberOfCollections`
- Operation ID: `analysis_operations.get_number_of_collections`
- Request fields: none
- Result: optional `int32 total_count` plus shared `execution` details

Example with a Development source host:

```powershell
grpcurl -plaintext -d '{}' 127.0.0.1:50051 `
  briosa.AnalysisOperations/GetNumberOfCollections
```

## SDK sequence

The worker performs the sequence on its single SDK-owning STA:

1. `SetStep("Get Number of Collections")`
2. `ExecuteStep`
3. `GetMPStepResult`
4. require retrieved MP result code `2`
5. `GetIntegerArg("Total Count", ...)`

If the MP result fails, the output getter is not called. If the getter fails, the RPC fails with `DataLoss` and never substitutes a count.

## Runtime classification

- Effect: read only
- Execution scope: global-state read
- Replay safety: safe
- Risk flags: none

Runtime policy can still deny the operation. `DiscoveryService/ListCapabilities` advertises it only when the current process admits it.

## Evidence and validation

The [2024 compatibility review](../development/sa2024-compatibility.md) records matching exact-target MP arguments and SDK methods.

Portable tests cover the protobuf contract, handwritten command/result mapping, exact SDK call order, policy, discovery, reflection, and a standard generated-client workflow that obtains the count before reading collection index `0` through a fake worker.

The opt-in licensed workflow covers this scenario. It has not been executed against SA 2024.1.0508.5.
