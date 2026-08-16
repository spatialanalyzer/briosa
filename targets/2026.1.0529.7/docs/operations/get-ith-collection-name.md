# GetIthCollectionName

`GetIthCollectionName` implements the SpatialAnalyzer MP command `Get i-th Collection Name` for exact target `2026.1.0529.7`. Its naming deliberately stays recognizable to developers who already program MPs.

## gRPC contract

- Service: `briosa.AnalysisOperations`
- RPC: `GetIthCollectionName`
- Operation ID: `analysis_operations.get_ith_collection_name`
- Required request field: optional-presence `int32 collection_index`
- Result: optional string `resultant_name` plus shared `execution` details

`collection_index` uses the MP command's zero-based indexing. The protobuf field has explicit presence so index `0` is transmitted as a valid value while omission fails with `InvalidArgument`. Briosa does not impose an invented upper bound; SpatialAnalyzer reports an out-of-range index through the normal MP outcome.

Example with a Development source host:

```powershell
grpcurl -plaintext -d '{"collectionIndex":0}' 127.0.0.1:50051 `
  briosa.AnalysisOperations/GetIthCollectionName
```

The returned collection name is application data. Briosa returns it to the caller but does not log it.

## SDK sequence

The worker performs the sequence on its single SDK-owning STA:

1. `SetStep("Get i-th Collection Name")`
2. `SetIntegerArg("Collection Index", value)`
3. `ExecuteStep`
4. `GetMPStepResult`
5. require retrieved MP result code `2`
6. `GetCollectionNameArg("Resultant Name", ...)`

If the MP result fails, the output getter is not called. If the getter fails, the RPC fails with `DataLoss` and never substitutes an empty collection name.

## Runtime classification

- Effect: read only
- Execution scope: global-state read
- Replay safety: safe
- Risk flags: none

Runtime policy can still deny the operation. `DiscoveryService/ListCapabilities` advertises it only when the current process admits it.

## Evidence and validation

The exact-target contract is based on inventory entry `documentation:AnalysisOperations/GetI-thCollectionName.htm` and the matching `AnalysisOperations.txt` View SDK Code observation. Pinned ObjectiveSA is secondary parity evidence for the same step, setter, getter, and zero-based usage; exact-target evidence wins on conflict.

Portable tests cover protobuf presence, index `0`, request validation, worker mapping, exact SDK call order, success, MP failure, getter failure, policy, discovery, reflection, cancellation, and deadlines. The standard generated-client package smoke invokes this RPC through a fake worker.

The opt-in licensed workflow validates a zero-based lookup against a separately installed and licensed SpatialAnalyzer `2026.1.0529.7`. It reports structural success only and does not retain the returned collection name.
