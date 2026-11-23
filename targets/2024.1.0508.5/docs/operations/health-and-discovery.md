# Health and discovery

Briosa exposes the standard gRPC health service and a read-only core discovery service. Reading either service does not invoke another SpatialAnalyzer MP command. A freshly started server is live but not MP-ready; readiness reflects verification performed only after explicit SDK startup and connection for the current generation.

## Development-only reflection

A Debug build exposes standard gRPC server reflection only when the ASP.NET Core environment is `Development`. Registration and endpoint mapping both enforce the runtime environment check, while the server's Debug compilation controls whether the reflection implementation exists at all. Production and other runtime environments do not map the service, and the Release package excludes both the reflection host and protocol assemblies from its dependency closure.

Reflection describes every mapped health, discovery, and exact-target service. A reflected method is not an enabled capability: reflection never invokes a worker, changes readiness, supplies identity evidence, or bypasses request validation and the admission policy. Treat `ListCapabilities` as the authority for the current process's admitted operation set.

## Health checks

Use a standard `grpc.health.v1.Health` client with one of these service names:

| Service name | Meaning |
| --- | --- |
| `briosa.liveness` | The public Briosa host is serving. SpatialAnalyzer and worker state do not affect it. |
| `briosa.readiness` | The worker is control-ready, attached to SpatialAnalyzer, has completed the bounded execution-channel probe for its current generation, and has exact-match evidence for both runtime identities. |

The standard empty service name returns the aggregate health state. Deployment probes should use the explicit names so a SpatialAnalyzer outage does not restart an otherwise healthy public host.

### Execution-channel verification

Live SA 2024.1.0508.5 experiments showed that a second SDK client can report a successful `ConnectEx` while blocking indefinitely in `ExecuteStep`. Briosa therefore treats successful attachment as `Unverified` and does not admit ordinary MP work.

After an explicit connect request and exact-match identity gating, the server sends a dedicated private verification request. The worker performs Get Working Directory through the normal SDK sequence on its owning STA, validates MP result code `2` and the expected output shape, then discards the path before replying. If either identity is unavailable or mismatched, Briosa does not issue the probe. `Briosa:Worker:ReadinessProbeTimeout` (default 30 seconds) bounds an issued exchange, independently of the operation execution budgets; see [the Windows package guide](windows-package.md#worker-time-bounds). A probe timeout, worker exit, SDK-engine exit, or lost response closes admission and records a faulted generation. After any required operator action, call `RecoverSpatialAnalyzerSdk` with that generation and `REPLACE_WITHOUT_REPLAY`, then explicitly connect the disconnected replacement. Recovery never replays the interrupted operation. Caller cancellation or a gRPC deadline does not interrupt an issued probe: it finishes under its own bound, and the caller refreshes state with `GetSpatialAnalyzerSdkState`.

## Server information

`briosa.DiscoveryService/GetServerInfo` returns:

- Briosa and protocol build coordinates;
- the configured exact SpatialAnalyzer target;
- stable protocol package, exact SA target, and interop fingerprint;
- safe worker, SDK connection, and execution-readiness states;
- the target-isolation mode (`single_tenant` for the current release); and
- whether MP requests are currently ready.

The response carries separate evidence objects for the activated SDK engine/type library and connected SpatialAnalyzer application. Each contains an optional version plus:

- a source: `UNAVAILABLE`, `RUNTIME_VERIFICATION`, or `OPERATOR_ATTESTATION`; and
- a match state: `UNAVAILABLE`, `EXACT_MATCH`, or `MISMATCH`.

The configured target is never substituted for an unobserved runtime version. Runtime verification takes precedence for its own claim, so a configured attestation cannot hide a runtime mismatch. The legacy connected-version fields mirror the effective connected-SA claim for older clients and retain distinct runtime-verified and operator-attested states.

### Operator attestation when runtime evidence is unavailable

The production adapter observes the file/product version of the SDK process uniquely created during activation. It does not infer that version from COM registration or the configured target, and it has no reviewed runtime query for the connected SA application's version. If either effective identity is missing, Briosa remains live but fails readiness and rejects MP admission with a not-started unavailable outcome. An operator may attest either missing claim independently; omit the SDK pair when its runtime version is already observed:

```json
{
  "Briosa": {
    "SpatialAnalyzer": {
      "Identity": {
        "ActivatedSdk": {
          "OperatorAttestation": {
            "Version": "2024.1.0508.5",
            "Reference": "change-record:SDK-identity-review"
          }
        },
        "ConnectedSpatialAnalyzer": {
          "OperatorAttestation": {
            "Version": "2024.1.0508.5",
            "Reference": "change-record:SA-install-review"
          }
        }
      }
    }
  }
}
```

Each configured claim requires both `Version` and `Reference`; a partial pair fails startup. The reference identifies separately retained evidence and must not contain a path, credential, license value, or sensitive host detail. It is validated for presence but never returned by discovery or written to default logs. Record the version actually supported by the evidence rather than copying the package target as an assumption. See the [runtime boundary](../../../../docs/architecture/runtime-boundary-and-lifecycle.md) for precedence and release-gate limits.

Debug source hosting enables the standard .NET user-secrets provider for these same four keys. User-secrets keeps the local values out of tracked settings and launch profiles, but it is not an evidence vault; retain the independently established evidence elsewhere. Release builds do not carry the user-secrets project identity. The complete source command and configuration procedure is tracked in issue #120.

## Capabilities

`briosa.DiscoveryService/ListCapabilities` lists only handwritten operations registered in the current build, supported by the current isolation mode, and enabled by runtime policy. The packaged `standard` admission profile, narrowed or widened by configured flags and overrides, decides the list; a missing profile fails startup. Unknown and `exclusive_workflow` scopes are not advertised in the current `single_tenant` mode. Entries are ordered by operation ID.

The response reports the active policy:

| Field | Meaning |
| --- | --- |
| `admission_profile` | The resolved profile name: `read-only`, `standard`, `device`, or `full`. |
| `policy_fingerprint` | The value-free `sha256:<hex>` policy fingerprint that startup also records in the audit log. It covers every admission input: the policy semantics version, the profile and its definition, the isolation mode, flag settings, overrides, and for every registered operation its descriptor metadata (MP step, effect, replay safety, execution scope, and author risk flags), reviewed classification, and request options. Any change to an input changes the fingerprint, and two servers with equal fingerprints make the same static admission decisions. It never reveals a configured value. |

Each entry includes its stable operation ID, gRPC service and RPC, fully qualified method, reviewed read-only/mutating/unknown effect classification, replay safety, and execution scope, plus its reviewed classification row from [`OperationClassification.cs`](../../src/Briosa.Server/Security/OperationClassification.cs):

| Field | Values |
| --- | --- |
| `risk_flags` | Zero or more of the closed vocabulary, in declaration order: `FILESYSTEM_METADATA`, `FILESYSTEM_READ`, `FILESYSTEM_WRITE`, `FILESYSTEM_DELETE`, `DESTRUCTIVE`, `CODE_EXECUTION`, `PHYSICAL_MOTION`, `DEVICE_SESSION`, `DEVICE_CONFIG`, `INTERACTIVE_UI`, `EXTERNAL_IO`. An empty list means no reviewed risk applies. |
| `duration_class` | `QUICK`, `LONG_RUNNING`, or `INTERACTIVE`. It selects the server execution budget; see [worker time bounds](windows-package.md#worker-time-bounds). |
| `validation_status` | `NO_RECORDED_GAP`, `FIXTURE_PENDING`, or `AT_RISK_UNVALIDATED`. It is informational and never affects admission. |
| `admission` | `ADMITTED` for every entry of the default response. |
| `policy_denial` | Only on a denied entry; see the denial table below. Server 0.10.1 and later send it. |
| `request_options` | The operation's reviewed request options; see [request options in discovery](#request-options-in-discovery). Server 0.10.1 and later send it on every entry, with an empty list when no option applies. |

Server 0.10.0 sends neither `policy_denial` nor `request_options`, and clients built for Server 0.10.0, such as the 0.5.0 clients, ignore both. A client that reads them treats an absent `request_options` as "not described", never as "no options".

Discovery advertises the static row. A request option can still turn on operator UI or leave device work running at call time; the server then decides that request with the added `interactive_ui` flag or as an exclusive workflow, and can deny an advertised operation. See [request options](command-policy-and-auditing.md#request-options).

Set `include_denied` to `true` to list every registered operation instead, so a client can explain why one is unavailable. Admitted entries are unchanged. Each denied entry carries its classification row and the precedence step that denied it:

| `admission` | Denied by |
| --- | --- |
| `DENIED_UNREVIEWED` | Unreviewed classification metadata. Its `risk_flags` are empty and its classes are `UNSPECIFIED`. |
| `DENIED_EXCLUSIVE` | An exclusive workflow, which no setting admits in the `single_tenant` mode. Its execution scope is `EXCLUSIVE_WORKFLOW`. |
| `DENIED_OVERRIDE` | A per-operation `deny` override. |
| `DENIED_FLAG` | A risk flag set to `deny`. |
| `DENIED_PROFILE` | The profile, widened by flags set to `allow`, does not admit every risk flag, or the `read-only` effect filter excludes the operation. |

A denied entry is not callable; the call returns the typed `PermissionDenied` outcome. The denial reason names a precedence step, never a configured value.

On Server 0.10.1 and later, each denied entry also carries `policy_denial`, the same detail that a denied call carries in its typed error (see [typed denial detail](command-policy-and-auditing.md#typed-denial-detail)):

| `policy_denial` field | Meaning |
| --- | --- |
| `rule` | `PROFILE`, `FLAG`, `OVERRIDE`, `EXCLUSIVE`, or `UNREVIEWED`: the same step as the denied `admission` value with the same suffix. Match the two by name, because their numbers differ. |
| `denied_flag` | Only for `FLAG`: the flag set to `deny`, as the lowercase suffix of its `risk_flags` value, for example `filesystem_write` for `FILESYSTEM_WRITE`. When several of the operation's flags are set to `deny`, it names the first in `risk_flags` order. A newer server can name a flag that an older client does not know. |
| `admission_profile` | Only for `PROFILE`: the active profile. A profile denial names no flag, because the profile or its `read-only` effect filter can exclude an operation for more than one reason. |
| `request_option_field` | The request field that decided, when a request option did. An entry's own row decision never names one. |

### Request options in discovery

On Server 0.10.1 and later, `request_options` lists the operation's reviewed request options, in no particular order. Each option has:

| Field | Meaning |
| --- | --- |
| `field_name` | The request field, by its protobuf name. |
| `effect` | `INTERACTIVE_UI`: the request opens or blocks on operator UI, so it gains `interactive_ui` and the `INTERACTIVE` duration class. `BACKGROUND_WORK`: the call returns while work keeps running, so the request is an exclusive workflow. |
| `condition` | `WHEN_TRUE`, `WHEN_FALSE`, `WHEN_PRESENT`, `WHEN_NON_EMPTY`, or `WHEN_ONE_OF`. For `WHEN_ONE_OF`, `enabling_enum_values` lists the enum value names that turn the effect on, and any enum number the server does not recognize turns it on too. |
| `omission` | `DOES_NOT_ACTIVATE`; `ACTIVATES`, when omitting the field turns the effect on, as for `display_view_control`; or `REQUIRED`, when omitting the field fails request validation once policy admits the request. |
| `admission`, `policy_denial` | What the active policy decides for a request that turns on this option alone, with every other option inactive. |

The options of an admitted operation appear in the default response too. For example, under `standard` the default response lists `QueryPointsToObjects` as `ADMITTED`, and its `show_results_dialog` option as `DENIED_PROFILE` with `request_option_field` set. The per-option decision answers that one question only. The server decides a request that turns on several options with all of them together, and then names the first option of the deciding effect in its reviewed order. When the operation's own row is denied, an option can keep that decision without naming a field, or change it when a step of higher precedence applies: a `BACKGROUND_WORK` option is `DENIED_EXCLUSIVE` even when an override denies the operation. Clients do not pre-validate requests from this description; the server still decides each request.

Serialization covers one MP sequence, not a workflow spanning RPCs. See the [workflow-isolation guide](workflow-isolation.md) before coordinating multiple operations through one target.

Discovery does not expose hostnames, ports, process IDs, status codes, raw diagnostics, license information, credentials, MP arguments, returned values, or the complete installed SpatialAnalyzer command inventory.
