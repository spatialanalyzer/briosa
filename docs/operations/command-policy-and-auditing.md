# Command policy and auditing

Briosa has two command-exposure boundaries. The exact-target generated catalog defines every operation the binary can express. Runtime policy can only reduce that set.

## Configure allowed operations

The packaged v0.1 configuration enables only the reviewed vertical-slice operation:

```json
{
  "Briosa": {
    "Security": {
      "Operations": {
        "Allow": [
          "file_operations.get_working_directory"
        ],
        "Deny": []
      }
    }
  }
}
```

Use indexed environment variables when deployment tooling cannot edit JSON:

```powershell
$env:Briosa__Security__Operations__Allow__0 = 'file_operations.get_working_directory'
$env:Briosa__Security__Operations__Deny__0 = 'file_operations.get_working_directory'
./Briosa.Server.exe
```

The denylist overrides the allowlist. Omitting the allowlist denies every operation. Unknown, empty, duplicate, or non-array values fail startup instead of being ignored. Restart the server after changing policy; policy is not reloaded in place.

`DiscoveryService/ListCapabilities` reports the intersection of the generated catalog and the runtime allowlist after deny rules. It is the correct way for a client to learn what this server instance currently exposes.

## Interpret audit events

The operation event sequence is normally:

1. `2001` request received;
2. `2002` policy allowed or `2003` policy rejected; and
3. `2004` operation completed or `2005` operation failed.

All request events carry the same correlation ID. Worker lifecycle event `1201` identifies generation changes and value-free connection state. Event `2000` records allow/deny counts and a SHA-256 fingerprint of the effective policy without listing operation IDs.

The actor category is `local-unauthenticated` for the supported loopback deployment. It is not a user or service identity. A non-loopback peer is recorded only as `unverified-unauthenticated`; such public binding remains unsupported and is rejected by endpoint configuration.

Completion and failure events separate:

- total request duration from SDK execution duration;
- MP success, MP failure, and rejected `ExecuteStep`;
- successful, failed, and not-attempted output retrieval; and
- gRPC status from the curated diagnostic code.

## Data handling and retention

Briosa audit APIs never accept raw arguments or results. Do not add middleware that logs gRPC bodies, protobuf messages, COM values, peer strings, target hosts, or exception objects. Debug and trace levels are not permission to log them.

Treat paths, geometry, object identifiers, measurements, credentials, license data, and proprietary values as protected in both directions. Apply restrictive filesystem or collector access to logs, forward them only to an approved destination, and retain them only as long as operational or compliance needs require. Client applications remain responsible for protecting returned values after they leave Briosa.

Policy denial is reported as gRPC `PERMISSION_DENIED` with a typed `POLICY_DENIED` error and `DO_NOT_RETRY` guidance. It indicates deployment policy, not that an authenticated identity was evaluated.

See [ADR 0015](../architecture/0015-command-policy-and-audit-events.md), the [public endpoint guide](endpoint-security.md), and the [v0.1 threat model](../security/threat-model.md) for the complete security boundary.
