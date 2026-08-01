# Command policy and auditing

Briosa has two command-exposure boundaries:

1. the handwritten operations compiled and registered in `SpatialAnalyzerApi.Operations`; and
2. runtime exact-ID allow and deny policy, which can only reduce that set.

The packaged configuration enables only:

```json
{
  "Briosa": {
    "Security": {
      "Operations": {
        "Allow": [
          "analysis_operations.get_i_th_collection_name",
          "file_operations.get_working_directory"
        ],
        "Deny": []
      }
    }
  }
}
```

The denylist overrides the allowlist. Omitting the allowlist denies every operation. Unknown, empty, duplicate, or non-array values fail startup instead of being ignored. Restart the server after changing policy; policy is not reloaded in place.

An allowlist cannot create an operation that is absent from handwritten source. `DiscoveryService/ListCapabilities` reports the intersection of implemented operations and runtime policy after isolation checks. It is the correct way for a client to learn what the current process admits.

## Audit events

For each admitted or rejected request, Briosa records structural metadata:

- correlation ID;
- exact operation ID and gRPC method;
- actor category;
- execution scope;
- worker generation;
- request and SDK duration where available;
- execution disposition;
- MP and output-retrieval outcome;
- numeric MP result code when retrieved;
- gRPC status; and
- curated diagnostic code.

Audit APIs do not accept raw request arguments or returned values. Paths, geometry, identifiers, notes, credentials, hostnames, proprietary data, and raw exception text are excluded even when verbose logging is enabled.

Correlation does not imply safe replay. A cancelled, timed-out, crashed, or lost request may have started and completed in SpatialAnalyzer. Follow the typed execution disposition, replay guidance, and operation-specific evidence.

## Adding an operation

The operation pull request assigns its exact ID, effect, replay safety, execution scope, and risk flags in its handwritten descriptor. It also decides whether packaged defaults should admit it. Inventory or historical catalog membership cannot expand policy.
