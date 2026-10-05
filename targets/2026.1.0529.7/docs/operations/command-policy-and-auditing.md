# Command policy and auditing

Briosa has two command-exposure boundaries:

1. the handwritten operations compiled and registered in `SpatialAnalyzerApi.Operations`; and
2. runtime admission policy, which can only reduce that set.

Runtime admission comes from the reviewed per-target classification table,
[`OperationClassification.cs`](../../src/Briosa.Server/Security/OperationClassification.cs).
It holds one row per registered operation: risk flags, duration class,
validation status, and isolation class. The operator selects a named profile
and can adjust it with per-flag settings and per-operation overrides. The design
was decided on [#242](https://github.com/spatialanalyzer/briosa/issues/242) and is
implemented by [#293](https://github.com/spatialanalyzer/briosa/issues/293).

## Configuration

```jsonc
"Briosa": { "Security": { "Operations": {
  "Profile": "standard",                         // required: read-only, standard, device, or full
  "Flags": { "filesystem_write": "deny" },       // optional: <risk_flag> = allow | deny
  "Overrides": {                                 // optional: <service>:<operation> = allow | deny
    "mp_subroutines": { "run_subroutine": "allow" },
    "file_operations": { "get_working_directory": "deny" }
  }
}}}
```

The packaged [`appsettings.json`](../../src/Briosa.Server/appsettings.json) sets
only `"Profile": "standard"`. Each key holds one scalar value, so the last
configuration provider wins and any single decision can be narrowed or widened
without editing the package. An operation ID maps to an override key by
replacing `.` with `:`. Environment variables use `__`:

```powershell
$env:Briosa__Security__Operations__Profile = "read-only"
$env:Briosa__Security__Operations__Flags__interactive_ui = "allow"
$env:Briosa__Security__Operations__Overrides__file_operations__get_working_directory = "deny"
```

Startup fails, rather than ignoring the value, when:

- `Profile` is missing or names an unknown profile;
- a flag, service, or operation name is unknown, or a section name other than
  `Profile`, `Flags`, or `Overrides` appears;
- a value is anything other than `allow` or `deny`; or
- the retired indexed `Allow` or `Deny` arrays are present. There is no
  compatibility shim; see the [migration note](../../../../docs/development/admission-profile-migration.md).

Profile, flag, service, and operation names and the `allow`/`deny` values are
exact and case-sensitive. Policy is fixed for the process lifetime; restart the
server after changing it.

## Profiles

A profile admits an operation when every risk flag on its row is in the
profile's set and its effect filter passes. Unflagged operations are admitted by
every profile except where the `read-only` effect filter excludes them.

| Profile | Admitted risk flags | Effect filter | Intended use |
| --- | --- | --- | --- |
| `read-only` | `filesystem_metadata` | read-only operations only | Dashboards, inspection, CI probes |
| `standard` (packaged default) | + `filesystem_read`, `filesystem_write`, `destructive` | none | Document automation: construct, analyze, relate, report, import/export, save, delete model objects |
| `device` | + `device_session`, `device_config`, `physical_motion` | none | Measurement, robot, and appliance automation on a station whose session the operator started in SA |
| `full` | + `external_io`, `code_execution`, `filesystem_delete` | none | Everything except interactive operations and exclusive workflows |

`interactive_ui` is in no profile. Operations that block on, or open, operator
UI are admitted only by an explicit opt-in: `Flags:interactive_ui=allow`, or a
per-operation `allow` override.

Operations whose isolation class is `ExclusiveWorkflow` are multi-call device,
trapping, watch, robot-interface, and appliance session lifecycles. They are
denied under every profile, flag, and override until an accepted lease design
exists (AGENTS.md invariant 13). Their effective execution scope is
`exclusive_workflow`, and they are never advertised.

Validation status (`no_recorded_gap`, `fixture_pending`, `at_risk_unvalidated`)
is recorded for review and audit. It never affects admission.

`OperationClassificationTests` in this target pins the exact number of
operations each profile admits. The default-profile contract in
`ImplementedOperationRegistryTests` requires `standard` to admit
`file_operations.get_working_directory`, the eight smoke-client reads, the five
briosa-examples tutorial operations, and
`utility_operations.get_working_frame_properties`.

## Precedence

The first matching rule decides:

| Step | Rule | Result | Configurable |
| --- | --- | --- | --- |
| 1 | Operation not registered, or its MP binding does not match | `Unsupported` (`operation-unsupported`, `operation-binding-mismatch`) | no |
| 2 | Unreviewed metadata: no complete classification row, unknown effect, unspecified replay safety, or an unreviewed execution scope | `Denied` (`operation-risk-unreviewed`, `operation-isolation-unreviewed`) | no |
| 3 | `ExclusiveWorkflow` in the current `single_tenant` mode | `Denied` (`operation-isolation-unsupported`) | **no**; an `allow` override cannot bypass it |
| 4 | `Overrides:<service>:<operation>=deny` | `Denied` (`operation-policy-denied`) | yes |
| 5 | `Overrides:<service>:<operation>=allow` | `Allowed` | yes |
| 6 | Any of the operation's flags set to `Flags:<flag>=deny` | `Denied` (`operation-policy-denied`) | yes |
| 7 | Every flag is in the profile set or set to `allow`, and the effect filter passes | `Allowed`, otherwise `Denied` (`operation-policy-denied`) | yes |

Specific settings beat general ones, and across an operation's flags any single
`deny` wins. Policy denial happens before request mapping, worker enqueue, or
SDK execution and returns a typed, value-free `PermissionDenied` outcome with a
`NotStarted` disposition.

Configuration cannot create an operation that is absent from handwritten source.
`DiscoveryService/ListCapabilities` reports exactly the admitted operations. It is
the correct way for a client to learn what the current process admits.

## Audit events

See [server observability](server-observability.md) for persistent log location,
startup configuration, event levels, metrics, and optional tracing. These records
are operational evidence with bounded best-effort delivery. Caller completion
and eventual worker execution resolution are separate correlated events.

At startup Briosa records the resolved admission profile, the numbers of
admitted operations, flag settings, and overrides, the target isolation mode,
and a value-free policy fingerprint. The fingerprint is a SHA-256 over the
resolved profile, flags, overrides, and the classification rows of every
registered operation, so any configuration or table change produces a new
fingerprint.

For each admitted or rejected request, Briosa records structural metadata:

- correlation ID;
- exact operation ID and gRPC method;
- actor category;
- execution scope;
- the policy rule that decided (`override`, `flag.<flag>`, `profile.<profile>`,
  or one of the fixed `registry`, `classification`, and `isolation` steps);
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

Each operation change assigns its exact ID, effect, replay safety, execution
scope, and any author risk flags in its handwritten descriptor, and adds exactly
one reviewed row to `OperationClassification.cs`. The classification tests fail
when a registered operation has no complete row, and an operation without one is
denied at runtime. The row's risk flags decide which profiles admit the
operation; there is no allowlist to edit. Inventory or historical catalog
membership cannot expand policy.
