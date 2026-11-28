# Briosa CLI

`Briosa.Cli.exe` lets an AI agent or a script inspect a live SpatialAnalyzer
session through a Briosa server that is already running on the same machine,
without writing a program. It writes one JSON document per command to standard
output.

This page describes the CLI as it stands in this release. It is built from source
only so far; see [what is not here yet](#what-is-not-here-yet).

```powershell
dotnet run --project src/Briosa.Cli -c Release -- status
```

## What it does and does not do

- **Attach only.** The CLI talks to a server that the engineer started, for
  example with the Control Center. It never starts or stops a server, never
  starts, connects, reconnects, or recovers the SDK or SpatialAnalyzer, and sends
  no lifecycle RPC. If the server is not running, the command reports that and
  stops.
- **Local only.** The endpoint is `http://127.0.0.1:50051` unless `--endpoint`
  names another. It must be `http://`, an IP loopback address (`127.0.0.0/8` or
  `[::1]`), and an explicit port. Hostnames, including `localhost`, are rejected,
  as the server rejects them. The CLI connects to that address directly, through
  no proxy.
- **One verified connection.** Each command opens one TCP connection, checks the
  server on it, and never opens another. If the connection drops, a server that
  took over the port, for example an older one, never receives the call.
- **Read-only execution.** `call` runs only operations in the
  [read-only tier](#bridge-tiers), and the server enforces that promise (see
  [what the read-only promise rests on](#what-the-read-only-promise-rests-on)).
- **No hidden MP calls, one submission.** The CLI sends exactly the one MP call
  that `call` asks for, once. It never retries, replays, or repeats a call to fetch
  a result again. This holds below the gRPC client too: the HTTP client may resend a
  request on its own when the server refuses the HTTP/2 stream or closes the
  connection before processing it, so the CLI lets each request open exactly one
  HTTP/2 stream and never writes a second one. A blocked resend ends the call with
  `request-resend-blocked` and exit code 10; see
  [outcomes and exit codes](#outcomes-and-exit-codes).

## Server versions

Every command that talks to the server first calls `GetServerInfo` and checks
that the server serves SpatialAnalyzer `2026.1.0529.7` with the `briosa`
protocol package and reports compatibility major 3 in `compatibility.major`.
Any other server is refused before anything else is sent (`server_incompatible`).

| Server | `status` and `ops` | `call` |
| --- | --- | --- |
| 0.10.2 and later | Full detail, including `mp_step` | Yes |
| 0.10.1 | Works; no `mp_step`, so searches cannot match MP command names | Refused |
| 0.10.0 | Works; also no request options, typed denial detail, lifecycle bounds, or configured interaction mode | Refused |

`call` needs Server 0.10.2 or later because older servers ignore the caller
restriction header. The CLI never sends a call to a server that would ignore its
restriction; it fails closed with exit code 5 instead. The envelope carries a
warning naming what an older server does not report.

## Commands

| Command | What it does |
| --- | --- |
| `status` | The server's identity, target, and compatibility; worker, connection, and execution-readiness states; the activated-SDK and connected-SpatialAnalyzer identity evidence; the configured interaction mode; the admission profile and policy fingerprint. It warns when the configured interaction mode is not `SILENT`, because an operator dialog can then block an MP call. |
| `ops categories` | The MP service categories with the number of operations each has, how many the server admits and denies, and how many fall in each bridge tier. |
| `ops search` | A page of operations matching the filters. |
| `ops describe OPERATION_ID` | One operation: its classification, bridge tier, reviewed request options, and a JSON Schema for its request. |
| `call OPERATION_ID` | Runs one read-only operation. |
| `version` | This build's version and target. Sends nothing. |
| `help` | The usage lines. Sends nothing. |

Every command except `version` and `help` accepts `--endpoint URL`.

`ops search` options:

| Option | Meaning |
| --- | --- |
| `--query TEXT` | Case-insensitive substring of the operation ID, MP step, RPC, or gRPC service, 1 to 200 characters. |
| `--category NAME` | The category, which is the operation ID's first part, for example `file_operations`. |
| `--effect read_only\|mutating\|unknown` | The reviewed effect. |
| `--risk-flag FLAG` | Repeatable. Every named flag must be on the operation's row, for example `filesystem_read`. |
| `--tier TIER` | A [bridge tier](#bridge-tiers). |
| `--include-denied` | Also list the operations the server denies, each with its typed denial detail. Without it, only admitted operations are listed. |
| `--validated-only` | Only operations whose `validation_status` is `NO_RECORDED_GAP`. Validation status is informational; it never affects admission. |
| `--offset N`, `--limit N` | Paging. The limit defaults to 25 and is at most 100. The response gives `total_matches` and `next_offset`, which is null on the last page. |

`call` options:

| Option | Meaning |
| --- | --- |
| `--json TEXT` or `--input FILE` | The request, exactly one of the two. A leading UTF-8 byte order mark in a file is ignored. |
| `--output FILE` | Write the full result to this file instead of standard output. The file must not exist yet. The CLI creates it before any RPC, so a path it cannot write is refused before anything is sent, and holds it exclusively until the result is written. If the call ends without a result, the empty file is removed through the CLI's own handle, so a file someone else put at that path is never removed. |
| `--max-inline-bytes N` | The inline cap, 0 to 4,194,304 bytes. Defaults to 65,536. |
| `--deadline-seconds N` | A deadline for the call, 1 to 86,400 seconds. Without it the CLI imposes none, and the server's own execution budget applies. |

Options are written `--name value`; the next argument is always the value. There
is no `--name=value` form, and an option given twice, other than `--risk-flag`,
is a usage error. Shells differ in how they pass quotes to a program; `--input
FILE` avoids that for a request.

### Describing a request

`ops describe` returns `data.request_schema`, a JSON Schema (draft 2020-12) built
from the compiled protocol descriptors, with the exact protobuf field names. It
describes the JSON form of each value and nothing more. **It does not describe
which fields the server requires.** Protobuf `optional` (explicit presence)
does not mean that a field may be omitted, and a field without it can still be
required. The schema therefore has no `required` list. When the server rejects
a request, the failure names the first offending field and the constraint it did
not meet; see [request validation](request-validation.md).

`data.request_options` lists the operation's reviewed request options as the
server describes them: the field, its effect (operator UI, or work left running),
the condition that turns it on, and what the server decides for it alone. An
option that turns on its effect when the field is omitted has
`activates_when_omitted: true`, and the envelope carries the warning
`option-activates-when-omitted`. On Server 0.10.0 the options are not described
(`described: false`), which is not the same as "none apply".

## Input rules

Before the protobuf JSON parser runs, the CLI checks the request against the
request message's descriptor and accepts exactly one form of each value:

| Rule | Accepted | Rejected with |
| --- | --- | --- |
| Field names | Exact protobuf names, such as `group_name` | `input-field-name-not-exact` for a camelCase JSON name (with `suggested_field_name`), `input-unknown-field` otherwise |
| Null | Never; omit the field instead | `input-null` |
| Duplicate keys | Never, at any depth | `input-duplicate-field` |
| Oneofs | At most one field of each oneof | `input-oneof-conflict` (the path names the oneof) |
| 32-bit integers | A JSON integer without a fraction or exponent, such as `7` | `input-number-form`, `input-out-of-range` |
| 64-bit integers | A decimal JSON string, such as `"18446744073709551615"`, as the protobuf JSON mapping writes them | `input-number-form`, `input-out-of-range` |
| Doubles | A finite JSON number, or one of the strings `"NaN"`, `"Infinity"`, `"-Infinity"` | `input-number-form`, `input-out-of-range` |
| Enums | The exact value name as a JSON string, such as `"OBJECT_TYPE_POINT_GROUP"` | `input-enum-name-required` for a number, `input-unknown-enum-name` |
| Booleans, strings, messages, lists | `true`/`false`; a JSON string; a JSON object; a JSON array | `input-type-mismatch`, `input-invalid-string` |
| Document | One JSON object, UTF-8, no comments or trailing commas | `input-root-not-object`, `input-malformed-json` |
| Size | At most 262,144 bytes of JSON | `input-too-large` |
| Nesting | At most 32 levels of objects and arrays | `input-too-deep` |
| Encoded request | At most 65,536 bytes, the server's receive limit | `request-too-large` |

Enum numbers are refused because similar enums number their values differently;
a name cannot be confused. The registered requests of this target use no other
field kinds; a field of any other kind is refused with
`input-unsupported-field-type`.

A local rejection is `rejected_locally` with exit code 3. Nothing was sent, so
`execution.disposition` is `EXECUTION_DISPOSITION_NOT_STARTED` with
`disposition_source: "cli"`. `failure.input` gives the field path (protobuf
names joined by dots, as the server writes them, or null at the top level), the
byte offset in the input as given, counting a leading byte order mark, and the
line and column. It never repeats the offending text, not even an unknown key.
Every `call` refused before any RPC, including a usage error or a rejected
endpoint, carries this not-sent `execution` block.

## The result envelope

Every command writes one envelope: one line of UTF-8 JSON without a byte order
mark, ending in a newline. Every top-level member is always present, null when it
does not apply. Standard error carries at most one value-free line, such as
`briosa-cli: invalid_request (request-validation-failed)`, and only when the exit
code is not 0. It names a diagnostic code only when the code is one of the CLI's
own or one of the server's documented codes; any other code the server sent is
shown as `server-diagnostic` there, and stays in `failure.diagnostic_code` as
data.

| Member | Meaning |
| --- | --- |
| `envelope_version` | `1`. It changes when a member is added, removed, or changes meaning. |
| `command` | For example `call` or `ops search`. |
| `outcome` | The [overall outcome](#outcomes-and-exit-codes). It summarizes the members below; it never replaces them. |
| `exit_code` | The process exit code. |
| `endpoint` | The normalized endpoint, once one was accepted. |
| `server` | The server's claims (`briosa_version`, `source_revision`, `protocol_package`, `spatial_analyzer_target`, `interop_fingerprint`, `compatibility`), what this build expects, `verified`, `verification_problems`, and `execution_supported`. |
| `transport` | The gRPC `method` and canonical `status` name of the last RPC the command depended on. |
| `failure` | Why the command did not succeed: `source` (`server` for a typed error the CLI accepted as evidence, `transport` for a gRPC failure without one, `cli` for the CLI's own finding, including conflicting or malformed evidence), `kind` (the server's `OperationFailureKind`), `diagnostic_code`, `policy_denial`, `request_validation`, and `input`. Null on success. |
| `execution` | For `call` only; see below. |
| `data` | The command's payload. |
| `warnings` | Fixed warnings, each with a `code` and a `message`. |
| `guidance` | Fixed sentences on what to do next. |

`execution` keeps every dimension of the MP call separate, always with these
members in this order:

| Member | Meaning |
| --- | --- |
| `request_sent` | Whether the MP request was handed to the transport. |
| `caller_restriction` | The restriction header the call carried. |
| `disposition` | `EXECUTION_DISPOSITION_NOT_STARTED`, `..._STARTED_OUTCOME_UNKNOWN`, or `..._COMPLETED`, exactly as reported, or null when nothing trustworthy reported one, including when the evidence conflicts. A missing disposition is never read as "not started". |
| `disposition_source` | `operation_error` (the server's typed error), `grpc_ok` (a gRPC OK with consistent MP details, which the server documents as completed), `cli` (the CLI never sent the request), or null. |
| `completion_evidence` | `available`, or `unavailable` when nothing trustworthy says how the call ended. |
| `recovery_guidance`, `replay_guidance`, `replay_safety`, `worker_generation` | From the server's typed error. Null on gRPC OK, which carries none. |
| `mp_execution` | The `MpExecutionDetails`: `state`, `mp_result_code`, and each output's retrieval state. |

`data` for `call` has, in this order: `operation` (the discovery row and its
tier), `result_delivery` (`inline`, `file`, `none`, or `lost`),
`result_message`, `result_json_bytes`, `inline_cap_bytes`, `result`,
`result_file`, `result_summary`, and `result_delivery_error`. It is null when the
command stopped before it found the operation.

Result data is the protobuf JSON of the result message with the exact field
names. A field without explicit presence is written even at its default value,
so a written field always means a value; a field with presence is written only
when it is set. Floating-point values keep the formatter's round-trip text and
are never rounded. An enum value this build does not know is written as its
number.

For example, a partial success, shortened:

```json
{"envelope_version":1,"command":"call","outcome":"partial_success","exit_code":9,
 "endpoint":"http://127.0.0.1:50051","server":{"briosa_version":"0.10.2","verified":true,"execution_supported":true},
 "transport":{"method":"/briosa.AnalysisOperations/GetIthPointFromGroup","status":"OK"},"failure":null,
 "execution":{"request_sent":true,"caller_restriction":"profile=read-only; deny=interactive_ui",
  "disposition":"EXECUTION_DISPOSITION_COMPLETED","disposition_source":"grpc_ok","completion_evidence":"available",
  "recovery_guidance":null,"replay_guidance":null,"replay_safety":null,"worker_generation":null,
  "mp_execution":{"state":"MP_EXECUTION_STATE_PARTIAL_SUCCESS","mp_result_code":4,"output_retrievals":[]}},
 "data":{"operation":{"operation_id":"analysis_operations.get_ith_point_from_group","tier":"read_only"},
  "result_delivery":"inline","result":{"point_name_only":"P1"}},
 "warnings":[{"code":"partial-success","message":"SpatialAnalyzer returned MP code 4 (partial success): ..."}],
 "guidance":[]}
```

## When the CLI relies on what the server sent

A typed error or a result counts as evidence about the call only when it is about
this call and agrees with everything else that came back. The CLI applies these
rules, and they are exactly the shapes the server's outcome mapper produces on this
target:

1. A typed error names the requested operation in `operation_id`.
2. Every output in its `mp_execution`, and every output of an OK result, names a
   field of this operation's result message.
3. Its failure kind is one the CLI knows, and the gRPC status, the disposition,
   and the MP details are one of the combinations the server produces for that
   kind:

   | Failure kind | gRPC status | Disposition | MP details |
   | --- | --- | --- | --- |
   | `VALIDATION` | `INVALID_ARGUMENT` | not started | none |
   | `UNSUPPORTED` | `UNIMPLEMENTED` | not started | none |
   | `OVERLOADED` | `RESOURCE_EXHAUSTED` | not started | none |
   | `POLICY_DENIED` | `PERMISSION_DENIED` | not started | none |
   | `CALLER_CANCELLED` | `CANCELLED` | not started or unknown | none |
   | `CALLER_DEADLINE_EXCEEDED` | `DEADLINE_EXCEEDED` | not started or unknown | none |
   | `WORKER_WATCHDOG_TIMEOUT`, `WORKER_FAILURE` | `UNAVAILABLE` | unknown | none |
   | `SPATIAL_ANALYZER_UNAVAILABLE`, `WORKER_UNAVAILABLE` | `UNAVAILABLE` | not started or unknown | none |
   | `SDK_ARGUMENT_REJECTED` | `FAILED_PRECONDITION` | not started | `ARGUMENT_REJECTED` |
   | `EXECUTE_STEP_REJECTED` | `FAILED_PRECONDITION` | unknown | `EXECUTE_STEP_REJECTED` |
   | `MP_RESULT_RETRIEVAL_FAILURE` | `INTERNAL` | unknown | `RESULT_UNAVAILABLE` |
   | `MP_FAILURE` | `FAILED_PRECONDITION` | completed | `FAILED` |
   | `OUTPUT_RETRIEVAL_FAILURE` | `DATA_LOSS` | completed | `SUCCEEDED` or `PARTIAL_SUCCESS`, at least one output failed |
   | `SDK_CALL_FAULTED` | `INTERNAL` | not started | none |
   | `SDK_CALL_FAULTED` | `INTERNAL` | unknown | `RESULT_UNAVAILABLE` |
   | `SDK_CALL_FAULTED` | `DATA_LOSS` | completed | `SUCCEEDED` or `PARTIAL_SUCCESS`, at least one output failed |
   | `INTERNAL` | `INTERNAL` | any, including unspecified | none |
   | `INTERNAL` | `DATA_LOSS` | completed | `SUCCEEDED` or `PARTIAL_SUCCESS` |

4. Within the MP details, the state agrees with the code: `SUCCEEDED` has code 2,
   `PARTIAL_SUCCESS` code 4, `FAILED` any code but 2, and the other states none.
   Outputs were retrieved or failed only after code 2 or 4; otherwise none was
   attempted.
5. A gRPC OK carries no `briosa-operation-error-bin` trailer, and its `execution`
   is `SUCCEEDED` with code 2 or `PARTIAL_SUCCESS` with code 4.

When any rule fails, the CLI relies on none of it: the outcome is
`completion_evidence_unavailable`, exit code 10, with a null disposition, no
recovery or replay guidance, the warning `operation-evidence-conflict`, and
guidance to check SpatialAnalyzer's state. The diagnostic code says which rule
failed: `operation-error-unrelated`, `operation-error-inconsistent`,
`operation-error-kind-unrecognized`, `ok-with-operation-error`,
`result-output-unrelated`, `result-execution-missing`, or
`result-execution-contract-violation`. A
conflicting answer never leads to advice to send the request again.

A completed failure other than `MP_FAILURE` follows a terminal MP result whose
outputs, or whole result, the server could not deliver, for example
`worker-output-shape-invalid` or `result-mapping-failed`. It is
`outputs_incomplete`, exit code 9, never an MP failure.

## Outcomes and exit codes

| Exit code | `outcome` | Meaning |
| ---: | --- | --- |
| 0 | `succeeded` | The command did what it was asked. For `call`: MP code 2 with every output retrieved. |
| 1 | `internal_error` | The CLI failed unexpectedly before sending any operation. |
| 2 | `usage_error` | The command line could not be read. Nothing was sent. |
| 3 | `rejected_locally` | The CLI refused before sending the operation: invalid input, unknown operation, a tier `call` does not serve, a non-loopback endpoint, an output file it cannot create, or cancellation before sending. Not started. |
| 4 | `server_unreachable` | The server could not be reached, or discovery failed with `UNAVAILABLE`, `DEADLINE_EXCEEDED`, `INTERNAL`, or `UNKNOWN`. No operation was sent. |
| 5 | `server_incompatible` | The server answered but is not one this command can use: the wrong target, package, or compatibility major; older than Server 0.10.2, for `call`; an inconsistent listing (`capabilities-inconsistent`: a duplicate operation ID or method, or a row whose method is not its service and RPC); a discovery reply larger than the CLI accepts (`discovery-response-too-large`); or another discovery status. No operation was sent. |
| 6 | `not_started` | The server reported `NOT_STARTED` for a reason other than validation: a policy denial (including the CLI's own restriction), readiness, capacity, or a similar reason. |
| 7 | `invalid_request` | The server reported `NOT_STARTED` because request validation failed. `failure.request_validation` names the first invalid field. |
| 8 | `mp_failed` | The server reported `COMPLETED` without success. Do not replay. |
| 9 | `partial_success` or `outputs_incomplete` | MP code 4, or the operation completed but at least one output, or its whole result, was not delivered. Completed; do not replay to change the outcome or to recover an output. |
| 10 | `completion_evidence_unavailable` | The request was sent, and no trustworthy evidence of how it ended came back: cancellation, the `--deadline-seconds` deadline, a dropped connection, a missing or malformed error trailer, [conflicting evidence](#when-the-cli-relies-on-what-the-server-sent), an OK result whose MP details are missing or inconsistent, or a blocked resend. The operation may have run. |
| 11 | `started_outcome_unknown` | The server reported `STARTED_OUTCOME_UNKNOWN`. The operation may have run. |
| 12 | `output_write_failed` | The operation completed, as its own evidence established, but its result could not be written to the `--output` file or the spill directory. When completion is not established and the write also fails, the outcome stays `completion_evidence_unavailable` (exit code 10), and `data.result_delivery_error` records the failed write. |

After exit codes 10 and 11, check SpatialAnalyzer's state before trying again.
The CLI never retries, and running the call again is a new call, not a recovery.
A `Ctrl+C` while a call is waiting stops the wait; the call itself may still run
to completion on the server, so it ends with exit code 10.

## Result size and spill files

The CLI keeps the gRPC receive allowance as large as the server can send (4 MiB).
What it prints is bounded instead:

- A result whose JSON is at most the inline cap (65,536 bytes by default) is
  printed in `data.result`.
- A larger result is written in full to a new file in the spill directory, and
  the envelope gets `data.result_file` (path, size, SHA-256, and reason) and
  `data.result_summary` (each top-level field with its type and, for a list, its
  count), never the data. The warning `result-spilled` says so.
- With `--output`, the result always goes to that file and never to standard
  output.

The spill directory is `%LOCALAPPDATA%\Briosa\cli\2026.1.0529.7\results`. The
CLI manages every directory below `%LOCALAPPDATA%`:

- It creates each one if needed and opens it as itself, without following a
  reparse point. It refuses one that is a reparse point, such as a junction.
- It holds each one open, so it cannot be renamed or replaced, until the file
  exists.
- It gives `results` access for the current user and SYSTEM only.
- It creates the file through those directories and checks that the new file
  resolves to exactly that path. A file that does not resolve there is removed
  through its own handle.

Spill file names hold a UTC timestamp, the requested operation ID, and random
characters, never text from the server or SpatialAnalyzer. The CLI does not
delete spill files; remove them when they are no longer needed.

A result is never fetched again by running the MP again. If an `--output` write
fails after the operation completed, the CLI writes the result to the spill
directory instead and reports exit code 12 with the spill file in
`data.result_file`; the `--output` file may then be incomplete. If no file can be
written, `data.result_delivery` is `lost`.

## Bridge tiers

Every operation belongs to exactly one bridge tier, decided from its discovery
metadata alone: effect, risk flags, execution scope, and whether its row is
reviewed. The first matching rule decides:

| Tier | Rule | `call` | Operations of this target |
| --- | --- | --- | ---: |
| `excluded` | A risk flag `physical_motion`, `device_session`, `device_config`, `code_execution`, or `external_io`; an exclusive-workflow, unknown, or unrecognized execution scope; an unknown or unrecognized effect or risk flag; or an unreviewed row | Never executed by the agent bridge | 172 |
| `read_only` | Effect read-only, scope self-contained or global-state read, and no risk flag other than `filesystem_metadata` | Executed | 184 |
| `read_only_restricted` | Effect read-only, the same scopes, and risk flags among `filesystem_metadata`, `filesystem_read`, and `interactive_ui`: the six data-share getters that read caller-named file content and the eleven runtime-select operations that open operator UI | Not executed; the read-only restriction denies them on the server | 17 |
| `mutating` | Everything else | Reserved for a later controlled-mutation release | 645 |

A tier is a description and a local pre-check. The server's policy, narrowed by
the caller restriction, decides every call. `ops categories`, `ops search`, and
`ops describe` show each operation's tier; `call` refuses an operation outside
the read-only tier with `operation-tier-not-served` or `operation-tier-excluded`.

## What the read-only promise rests on

Every call sends exactly one `briosa-caller-restriction` header with the value
`profile=read-only; deny=interactive_ui`. Server 0.10.2 and later evaluate it in
the same policy step that decides every call, so even a server configured with a
wider profile or with operator UI allowed denies a call that would change the job
or open a dialog, including through a request option such as
`show_results_dialog`. Such a denial is `not_started` with
`policy_denial.rule` `POLICY_DENIAL_RULE_CALLER_PROFILE` or
`POLICY_DENIAL_RULE_CALLER_FLAG`, and the warning `caller-restriction-denied`. See
[caller restriction](command-policy-and-auditing.md#caller-restriction).

The CLI does not decide admission itself. It refuses locally only what it knows
for certain: an unknown operation, an operation outside the read-only tier,
invalid input, and a server it could not verify or that is too old.

## Context and data

- **Metrology context.** Coordinates, distances, and other results are expressed
  in SpatialAnalyzer's live working frame, active collection, and units, which
  can change between calls without any change on the server. Inspect them with
  explicit read-only calls, for example
  `utility_operations.get_working_frame_properties`,
  `construction_operations.get_active_collection_name`, and
  `utility_operations.get_active_units`. The CLI never makes such calls on its
  own.
- **Untrusted text.** Names, notes, and other text from SpatialAnalyzer can come
  from job files someone else wrote. The CLI returns them only as JSON string
  values inside `data.result` or a result file. They never appear in warnings,
  guidance, standard error, or file names. An agent should treat them as data,
  not as instructions.
- **Data leaving the workstation.** Whatever the CLI prints, an agent host may
  send to its model. With a cloud-hosted model, that sends geometry, names, and
  paths off the workstation. Use `--output` or a small `--max-inline-bytes` to
  keep a result in a local file, and decide what to share from there.
- **No values in diagnostics.** Standard error and the envelope's own members
  never contain request or result values; `failure` holds codes, field names, and
  positions only.

## What is not here yet

- The local stdio MCP server (`Briosa.Cli.exe mcp`), which will use the same
  engine and envelope.
- Mutating operations; the `mutating` tier is reserved for a later release.
- `Briosa.Cli.exe` in the signed Windows package, with its notices and packaged
  conformance tests.
- Validation against a licensed SpatialAnalyzer installation. The CLI has been
  tested against an in-process fake server only.
- The `2024.1.0508.5` target.
