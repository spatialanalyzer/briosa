# GetNumberOfCollections

`GetNumberOfCollections` implements the SpatialAnalyzer MP command `Get Number of Collections` for exact target `2026.1.0529.7`. Its naming deliberately stays recognizable to developers who already program MPs.

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

The exact-target contract is based on inventory entry `documentation:AnalysisOperations/GetNumberOfCollections.htm` and the matching `AnalysisOperations.txt` View SDK Code observation. Pinned ObjectiveSA is secondary parity evidence for the same step and getter; exact-target evidence wins on conflict.

Portable tests cover the protobuf contract, handwritten command/result mapping, exact SDK call order, policy, discovery, reflection, and a standard generated-client workflow that obtains the count before reading collection index `0` through a fake worker.

The opt-in licensed workflow includes the same count-then-name sequence against a separately installed and licensed SpatialAnalyzer `2026.1.0529.7`. It reports structural success only and does not retain returned values. The scenario executed successfully on August 1, 2026, using separate operator attestations for the activated SDK and connected SpatialAnalyzer identities.
