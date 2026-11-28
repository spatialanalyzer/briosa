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
  request on its own, for example after the server refuses the HTTP/2 stream or
  closes the connection, so the CLI lets each request open exactly one HTTP/2
  stream and never writes a second one. A blocked resend ends the call with
  `request-resend-blocked` and exit code 10. The CLI sees the second attempt, not
  why the first one ended, so whether the server received or processed the first
  attempt is not known; see [outcomes and exit codes](#outcomes-and-exit-codes).

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
tier), `result_delivery` (`none`, `inline`, `file`, `file_unconfirmed`,
`file_unverified`, or `lost`; see [delivery states](#delivery-states)),
`result_message`, `result_json_bytes`, `inline_cap_bytes`, `result`,
`result_file` (the best file the call left on disk, with any others in its
`other_files`), `result_summary`, and `result_delivery_error`. It is null when the
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

The CLI uses a discovery listing, a typed error, or a result only when it agrees
with everything else that came back and with what the server's own code produces
on this target. The rules below hold for every operation, without a table of
operations; tests derive them from the server's outcome mapper and check them
against it.

### The operation and its method

Before it uses any row of a discovery listing, the CLI checks the whole listing:
each operation ID is well formed and appears once, each row names one method that
no other row names, that method is `/<grpc_service>/<rpc>`, and the operation ID
names that method. On this target an operation ID is its service and RPC names in
snake case: with the underscores removed, `category.operation` equals
`Service.Rpc`, ignoring ASCII case, where the service is named without the
`briosa.` package. For example, `file_operations.get_working_directory` names
`/briosa.FileOperations/GetWorkingDirectory`. This holds for all 1,018 operations
this target registers, with no exception, and no operation ID names another
registered method; a test pins both over the whole registry. A listing that breaks
any of this is refused whole (`capabilities-inconsistent`, exit code 5) before
anything else is sent, so no listing can send a call for one operation to another
operation's method.

### Typed errors

A typed error is evidence about the call only when all of the following hold:

1. It names the requested operation in `operation_id`, and its `replay_safety` is
   the one discovery reported for that operation; the server copies it into every
   error. Otherwise: `operation-error-unrelated`.
2. Its failure kind, disposition, recovery guidance, replay guidance, replay
   safety, and any policy-denial rule are values this CLI knows. An unknown failure
   kind is `operation-error-kind-unrecognized`; any other unknown value is
   `operation-error-enum-unrecognized`.
3. Its gRPC status, disposition, replay guidance, recovery guidance, diagnostic
   code where the server fixes it, and MP details are one row of this table.
   "Ambiguous" replay guidance is `MAY_REPLAY` for a `SAFE` operation and
   `RECONCILE_BEFORE_REPLAY` for any other. Recovery guidance is `NONE` unless the
   row says otherwise. A diagnostic code the table leaves open can be any code.

   | Failure kind | gRPC status | Disposition: replay guidance | Recovery guidance | Diagnostic code | MP details |
   | --- | --- | --- | --- | --- | --- |
   | `VALIDATION` | `INVALID_ARGUMENT` | not started: `DO_NOT_REPLAY` | | | none |
   | `UNSUPPORTED` | `UNIMPLEMENTED` | not started: `DO_NOT_REPLAY` | | | none |
   | `OVERLOADED` | `RESOURCE_EXHAUSTED` | not started: `MAY_REPLAY` | | | none |
   | `POLICY_DENIED` | `PERMISSION_DENIED` | not started: `DO_NOT_REPLAY` | | | none |
   | `CALLER_CANCELLED` | `CANCELLED` | not started: `MAY_REPLAY`; unknown: ambiguous | | | none |
   | `CALLER_DEADLINE_EXCEEDED` | `DEADLINE_EXCEEDED` | not started: `MAY_REPLAY`; unknown: ambiguous | | | none |
   | `WORKER_WATCHDOG_TIMEOUT` | `UNAVAILABLE` | unknown: ambiguous | `OPERATOR_INTERVENTION_REQUIRED` | `sa-modal-dialog-open` | none |
   | `WORKER_WATCHDOG_TIMEOUT` | `UNAVAILABLE` | unknown: ambiguous | `WORKER_REPLACEMENT` | any but `sa-modal-dialog-open` | none |
   | `WORKER_FAILURE` | `UNAVAILABLE` | unknown: ambiguous | `WORKER_REPLACEMENT` | | none |
   | `SPATIAL_ANALYZER_UNAVAILABLE` | `UNAVAILABLE` | not started: `MAY_REPLAY`; unknown: ambiguous | `WAIT_FOR_READINESS` or `OPERATOR_INTERVENTION_REQUIRED` | | none |
   | `WORKER_UNAVAILABLE` | `UNAVAILABLE` | not started: `MAY_REPLAY`; unknown: ambiguous | `WAIT_FOR_READINESS` | | none |
   | `SDK_ARGUMENT_REJECTED` | `FAILED_PRECONDITION` | not started: `DO_NOT_REPLAY` | | | `ARGUMENT_REJECTED`, no code, outputs not attempted |
   | `EXECUTE_STEP_REJECTED` | `FAILED_PRECONDITION` | unknown: ambiguous | | | `EXECUTE_STEP_REJECTED`, no code, outputs not attempted |
   | `MP_RESULT_RETRIEVAL_FAILURE` | `INTERNAL` | unknown: `RECONCILE_BEFORE_REPLAY` | | | `RESULT_UNAVAILABLE`, no code, outputs not attempted |
   | `MP_FAILURE` | `FAILED_PRECONDITION` | completed: `DO_NOT_REPLAY` | | | `FAILED`, a code other than 2, outputs not attempted |
   | `OUTPUT_RETRIEVAL_FAILURE` | `DATA_LOSS` | completed: `DO_NOT_REPLAY` | | | completed, outputs retrieved or failed, at least one failed unless the operation has no outputs |
   | `SDK_CALL_FAULTED` | `INTERNAL` | not started: `DO_NOT_REPLAY` | | | none |
   | `SDK_CALL_FAULTED` | `INTERNAL` | unknown: `RECONCILE_BEFORE_REPLAY` | | | `RESULT_UNAVAILABLE`, no code, outputs not attempted |
   | `SDK_CALL_FAULTED` | `DATA_LOSS` | completed: `DO_NOT_REPLAY` | | | completed, outputs retrieved or failed, at least one failed unless the operation has no outputs |
   | `INTERNAL` | `INTERNAL` | completed: `DO_NOT_REPLAY` | | `worker-output-shape-invalid` | none |
   | `INTERNAL` | `INTERNAL` | unknown: ambiguous | | `request-violation-after-mapping` | none |
   | `INTERNAL` | `DATA_LOSS` | completed: `DO_NOT_REPLAY` | | `result-mapping-failed` | completed, every output retrieved |

   "Completed" MP details are `SUCCEEDED` with code 2 or `PARTIAL_SUCCESS` with
   code 4. No disposition is ever unspecified. For an operation `call` serves, an
   `MP_FAILURE` never carries code 4 (see
   [what the CLI cannot tell apart](#what-the-cli-cannot-tell-apart)).
4. `worker_generation` is present and not negative. `request_validation` appears
   only on `VALIDATION`, with the diagnostic code `request-validation-failed`.
   `policy_denial` is present exactly on `POLICY_DENIED`, and carries exactly the
   fields its rule names, as the server writes them:

   | Rule | Required | Allowed | Value checked |
   | --- | --- | --- | --- |
   | `PROFILE` | `admission_profile` | `request_option_field` | not blank |
   | `FLAG` | `denied_flag` | `request_option_field` | not blank; a flag this CLI does not know is data |
   | `OVERRIDE`, `UNREVIEWED` | none | none | |
   | `EXCLUSIVE` | none | `request_option_field` | |
   | `CALLER_PROFILE` | `caller_admission_profile` | `request_option_field` | the profile this CLI's restriction names: `read-only` |
   | `CALLER_FLAG` | `caller_denied_flag` | `request_option_field` | a flag this CLI's restriction denies: `interactive_ui` |

   Any other field, and a blank `request_option_field`, does not fit. The server
   names a caller denial after the restriction the call carried: its profile, or the
   first of the request's flags that the restriction itself denies. So a caller rule
   naming anything else, or a server rule relabelled as a caller's, is not evidence
   of the CLI's own restriction. The warning `caller-restriction-denied` appears only
   for a caller denial that fits.
5. Its output rows name and cover the result, as described next.

Otherwise: `operation-error-inconsistent`. A diagnostic code that the table leaves
open is data: an unknown one alone is never a conflict. Where a row fixes the
code, as for `INTERNAL` and an operator-recovery watchdog timeout, a different code
alone makes the error conflicting evidence.

### Output rows

In every MP details it sends, the server writes one row per output of the
operation, named by that output's field name, all `NOT_ATTEMPTED` until an MP
result permits outputs, then `RETRIEVED` or `FAILED`. A field name is a field of
the result message, a dotted path into one, or the bare name of a field nested
inside one, and an operation that collects several outputs into one field repeats
its name. The CLI keeps no list of each operation's outputs, so it checks what
holds for every operation:

- Each row names something in this result: a top-level field other than
  `execution`, a dotted path that resolves through it, or the bare name of a field
  nested inside it. Otherwise: `result-output-unrelated` on an OK result,
  `operation-error-unrelated` on an error.
- Each top-level field of the result other than `execution` has a row of its own:
  the rows can be matched to the fields so that every field gets a different row
  that names it, by the field's name, a dotted path that starts with it, or a bare
  name found inside it. One row never answers for two fields, even when its bare
  name is nested inside both, as `collection_name` is inside each of the three datum
  fields of `gdt_operations.get_feature_check_datum_references`. Otherwise:
  `result-output-missing` on an OK result, `operation-error-inconsistent` on an
  error. Every result field of every operation on this target is written from its
  own outputs, so genuine replies always pass; a test checks the genuine replies of
  all 1,018 operations, and the 26 whose results nest one field name under several
  top-level fields each get a single-row case that must fail.

These rules cannot detect:

- a missing row for a nested field when another row already names the same
  top-level field, for example four of the five status fields of
  `relationship_operations.get_relationship_status`;
- extra, repeated, or reordered rows that name fields of this result, the wrong
  number of rows for an operation that repeats a name, or which of two fields a
  bare-named row belongs to (it is matched to one of them, never both);
- a reply that answers another call, or another operation with the same result
  message;
- whether a retrieved value is the one SpatialAnalyzer returned. A value left
  unset reads as its default.

Requiring a row for every nested field of a field named only through nested names
would be stronger, but it is false for `gdt_operations.get_gdt_options`, whose
result shares its `GdtOptions` message with a setter, so the CLI does not require
it.

### OK results

A gRPC OK result is evidence of success only when it carries no
`briosa-operation-error-bin` trailer, its `execution` is `SUCCEEDED` with code 2 or
`PARTIAL_SUCCESS` with code 4, every output row is `RETRIEVED` (the server answers
OK only when every output was retrieved), and its rows name and cover the result.
Otherwise: `ok-with-operation-error`, `result-execution-missing`,
`result-execution-contract-violation`, `result-output-unrelated`, or
`result-output-missing`.

### What the CLI cannot tell apart

The server reports MP code 4 as an MP failure for four reviewed operations whose
target documentation says code 4 leaves the work undone, and as partial success
for every other operation. None of the four is in the read-only tier, so for an
operation `call` serves, an MP failure with code 4 is something the server never
sends, and the CLI treats it as conflicting evidence. A test pins that none of the
four is in the read-only tier, so this rule fails loudly if that ever changes.

The CLI keeps no table of operations, so it cannot tell the four from the others
outside the read-only tier. A later tier that serves mutating operations must
accept code 4 in their MP failures again, and would then accept it for any of
them, and a partial success of those four.

### When a rule fails

The CLI relies on none of what came back: the outcome is
`completion_evidence_unavailable`, exit code 10, with a null disposition, no
recovery or replay guidance, the warning `operation-evidence-conflict`, and
guidance to check SpatialAnalyzer's state. The diagnostic code says which rule
failed. A conflicting answer never leads to advice to send the request again.

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
| 5 | `server_incompatible` | The server answered but is not one this command can use: the wrong target, package, or compatibility major; older than Server 0.10.2, for `call`; an inconsistent listing (`capabilities-inconsistent`: a duplicate operation ID or method, a row whose method is not its service and RPC, or an operation ID that does not [name its method](#the-operation-and-its-method)); a discovery reply that declares a message larger than the CLI accepts (`discovery-response-too-large`); or another discovery status, including a `RESOURCE_EXHAUSTED` status without that evidence (`transport-resource-exhausted`). No operation was sent. |
| 6 | `not_started` | The server reported `NOT_STARTED` for a reason other than validation: a policy denial (including the CLI's own restriction), readiness, capacity, or a similar reason. |
| 7 | `invalid_request` | The server reported `NOT_STARTED` because request validation failed. `failure.request_validation` names the first invalid field. |
| 8 | `mp_failed` | The server reported `COMPLETED` without success. Do not replay. |
| 9 | `partial_success` or `outputs_incomplete` | MP code 4, or the server reported that the operation completed but at least one output, or its whole result, was not delivered. Completed; do not replay to change the outcome or to recover an output. |
| 10 | `completion_evidence_unavailable` | The request was sent, and no trustworthy evidence of how it ended came back: cancellation, the `--deadline-seconds` deadline, a dropped connection, a missing or malformed error trailer, [conflicting evidence](#when-the-cli-relies-on-what-the-server-sent), an OK result whose MP details are missing or inconsistent (including an output that was not retrieved), or a blocked resend. The operation may have run. |
| 11 | `started_outcome_unknown` | The server reported `STARTED_OUTCOME_UNKNOWN`. The operation may have run. |
| 12 | `output_write_failed` | The operation completed, as its own evidence established, but its result could not be delivered as requested: the `--output` file failed, even if a spill copy holds the result, or the [delivery state](#delivery-states) is not `inline` or `file`. When completion is not established and the write also fails, the outcome stays `completion_evidence_unavailable` (exit code 10), and `data.result_delivery_error` records the failed write. |

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
  the envelope gets `data.result_file` (path, size, SHA-256, reason, what reading
  it back showed, and any other file the call left; see
  [delivery states](#delivery-states)) and `data.result_summary` (each top-level
  field with its type and, for a list, its count), never the data. The warning
  `result-spilled` says so.
- With `--output`, the result always goes to that file and never to standard
  output.

The spill directory is `%LOCALAPPDATA%\Briosa\cli\2026.1.0529.7\results`. The
CLI manages every directory below `%LOCALAPPDATA%`:

- It opens `%LOCALAPPDATA%` by path once, following a redirection of that folder,
  and holds it open. That directory is the root of the spill tree for this call.
- It checks that the volume keeps file permissions. On a volume that does not, such
  as FAT32 or exFAT, it creates nothing and writes nothing there (see below).
- It opens each directory below, or creates it, relative to the held handle of its
  parent, never by path, and as itself rather than what a reparse point leads to.
  It refuses one that is a reparse point, such as a junction, or not a directory.
  It holds each one open without delete sharing, so it cannot be renamed, until the
  file exists. It asks only to read each directory's attributes, to pass through
  it, and `SYNCHRONIZE`, which a synchronous handle needs. Where a directory is
  missing, the file system decides from the parent's own permissions whether it may
  be created; a directory that already exists needs no right to create anything.
- Only then, through the held handle of `results`, it makes the current user the
  owner and gives it a protected access list for the current user and SYSTEM only,
  with no inherited entries. This is written to `results` alone and is not passed
  on to anything already inside it, so existing files and folders in `results`,
  including earlier spill files, keep their owner and access list unchanged.
  Earlier spill files have their own protected list in any case.
- It creates the file relative to that handle, with its own protected access list
  for the same two, owned by the current user. It reports the path the file
  actually has, and checks that it is the spill tree's path.
- It writes the result, reads it back through the file's own handle, flushes it to
  the storage device, and closes it (see [delivery states](#delivery-states)). A
  spill file is removed through its own handle when it is known not to hold the
  whole result: the path check or any step of handing it over failed, or it read
  back shorter or different. Removal can itself fail, for example when the file has
  been made read-only; the envelope then names the incomplete file, including an
  empty one left at hand-over, at the path it was created at. A file that may hold
  the whole result is never removed. Directories it created are kept, as after a
  successful spill.

What this guarantees: whatever happens to any path during the call, including a
redirection of `%LOCALAPPDATA%` itself between two levels, the CLI changes owner or
permissions only on the `results` directory inside the tree that `%LOCALAPPDATA%`
led to when the CLI opened it, and on the new file, and writes only there. It
changes nothing that was already in `results`. Tests re-point the base after each
level and check that nothing outside that tree changed, and check that an ordinary
file, a folder, and an earlier spill file in `results` keep their owner and access
list byte for byte across two spills.

What it does not guarantee: that `%LOCALAPPDATA%` leads where the user expects when
the CLI opens it; whoever can redirect that folder chooses the tree. Another process
running as the same user can still read or change the file, or its permissions,
after the CLI has written it. A file named with `--output` is not a spill file: it
takes the permissions of the directory the caller chose.

On a volume that does not keep file permissions the CLI refuses to spill and
creates nothing there: the warning `spill-refused` says so. Without `--output`,
`data.result_delivery` is then `lost`, `data.result_delivery_error` is
`spill-permissions-unsupported`, and the warning `result-lost` says that no file
holds the result. If the call's own evidence established completion, the exit code
is 12; otherwise the call's outcome stands.

Spill file names hold a UTC timestamp, the requested operation ID, and random
characters, never text from the server or SpatialAnalyzer. The CLI does not
delete spill files; remove them when they are no longer needed.

A result is never fetched again by running the MP again. If an `--output` file
cannot be shown to hold the whole result, the CLI also writes the result to the
spill directory. An `--output` file that read back shorter or different is removed
through its handle, as a spill file is, because the call created it; one that
could not be read back is kept and named, because it may be the only copy.
`data.result_delivery_error` then names the `--output` file's failure whatever the
spill did, and the spill's own outcome is in its warnings and file entry.

### Delivery states

The CLI writes a result file in four steps, each through the handle it created the
file with:

1. It writes the result's bytes from the start of the file.
2. It reads them back and compares every byte with the result. This reads what the
   file system holds for the file, which can come from its cache rather than from
   the storage device.
3. It flushes the file to the storage device: it calls `FlushFileBuffers` itself
   and checks its result, so the operating system is asked to write the file's data
   and metadata to the device and must report that it did. (.NET's
   `RandomAccess.FlushToDisk` reports a flush that Windows refuses with
   `ERROR_ACCESS_DENIED` as a success, so the CLI does not use it.) Both kinds of
   result file are also opened write-through. A device that acknowledges writes it
   has not made durable, such as one whose volatile cache ignores flush requests, is
   beyond what the CLI can check.
4. It closes the handle. .NET does not report a failed close of a file handle, so in
   practice the flush is the last step that can report an error.

Before it writes any result bytes, the CLI formats the result, summarizes it, and
computes its SHA-256, so that nothing that runs once a file holds result bytes can
fail and lose it. (An `--output` file is already reserved, and empty, by then.) If
the SHA-256 cannot be computed, every file entry carries a null `sha256`, and the
warning `result-hash-unavailable` says so, whatever the delivery state. Reading a
file back does not use the SHA-256: it compares the file's bytes with the result,
when the file can be read back at all.

Each file the call wrote, or created and could not remove, is described by an
object with `path`, `bytes`, `sha256`, `reason`, and `verification`:

| `verification` | What it asserts | `bytes` and `sha256` |
| --- | --- | --- |
| `matched` | The file read back as exactly the result, and the write, the flush to disk, and the close reported no error. | The result's size and SHA-256. |
| `matched_unconfirmed` | The file read back as exactly the result, but the write, the flush to disk, or the close reported an error, so the CLI cannot confirm that it stays intact. It is kept. | The result's size and SHA-256, which the file held when read back. |
| `not_read_back` | The file could not be read back, so whether it holds the whole result is not known. It is kept, because it may be the only copy. | The result's size and SHA-256, to compare the file with. |
| `incomplete` | The file read back shorter or different, or nothing was written to it, and it could not be removed. It does not hold the result; delete it. | Its size on disk, or null when that could not be read, and no SHA-256. `reason` is `incomplete_not_removed`, or `reservation_not_removed` for an unused `--output` file. |

Before using a file labeled `not_read_back` or `matched_unconfirmed`, check that its
size and SHA-256 equal `bytes` and `sha256`: the first was never read back, and the
second may have changed after it was. When `sha256` is null, because the SHA-256
could not be computed, the size alone cannot verify such a file; do not use it
without another way to confirm its content. The warnings for these files say which
of the two applies.

A file that read back shorter or different is removed through its handle, on
either destination, because the call created it. A file that may hold the whole
result is never removed. The `reason` of any other file says why it was written:
`inline_cap_exceeded`, `output_requested`, or `output_write_failed` for a spill
that backs a failed `--output` file.

**Which file is named first.** `data.result_file` is the best of the files the call
left on disk: `matched` first, then `matched_unconfirmed`, `not_read_back`, and
`incomplete`; on a tie, the `--output` file. Its `other_files` lists every other
file the call left on disk, in the same order and form, and is empty when there is
none. `data.result_delivery` is
the state of `data.result_file`:

| `result_delivery` | What it asserts | `data.result_file` |
| --- | --- | --- |
| `none` | No result came back. | null, or an unused `--output` file that could not be removed |
| `inline` | `data.result` holds the whole result. | null |
| `file` | A file holds the result, read back and finished as `matched`. | `verification` is `matched` |
| `file_unconfirmed` | A file held the result when read back, but a later step reported an error. | `verification` is `matched_unconfirmed` |
| `file_unverified` | A file may hold the result; it could not be read back. | `verification` is `not_read_back` |
| `lost` | No file the call left holds the result in full, and it is not in the envelope. | null, or an `incomplete` file |

The CLI tries the `--output` file when one is given. It tries the spill directory
when there is no `--output` and the result is larger than the cap, or when the
`--output` file is neither `matched` nor `matched_unconfirmed`. The delivery
succeeded only when the file it had to go to is `matched`: the `--output` file when
one was given, otherwise the spill file. Then `data.result_delivery_error` is null.
Otherwise it names the first failure, the `--output` file's whenever there is one:

| `result_delivery_error` | The failure |
| --- | --- |
| `output-finalize-failed` | The `--output` file is `matched_unconfirmed`. Nothing is spilled. |
| `output-verify-failed` | The `--output` file could not be read back. It is kept, and the result is also spilled. |
| `output-write-failed` | The `--output` file read back shorter or different. It is removed, or named as `incomplete`, and the result is spilled. |
| `spill-finalize-failed` | The spill file is `matched_unconfirmed`. |
| `spill-verify-failed` | The spill file could not be read back. |
| `spill-write-failed` | No spill file could be created, a new one failed its checks before anything was written to it, or it read back shorter or different. |
| `spill-permissions-unsupported` | The volume keeps no file permissions, so nothing was spilled. |
| `result-format-failed` | The result could not be formatted as JSON or summarized. This is found before any file exists, so nothing was written. |
| `result-report-failed` | A defect in the CLI while it reported the delivery. Whatever `data.result_file` names is still on disk; check any other file the call may have written. |

Warnings name each event: `result-spilled` for a `matched` spill of a result over
the cap; `result-file-unconfirmed` and `result-file-unverified` for a file in those
states; `output-write-failed` and `spill-write-failed` for a failed write;
`spill-refused`; `incomplete-file-left` for an incomplete file left on disk;
`output-reservation-left`, with the file's path, for an unused `--output` file that
could not be removed, which calls it empty only when its size was read;
`result-hash-unavailable` whenever the SHA-256 could not be computed, in any
delivery state; and `result-lost` whenever the delivery state is `lost`, and only
then. Each warning states only what the CLI established for that file.

Any state other than `inline` and `file`, and any `--output` file that is not
`matched`, is a failed delivery: exit code 12 when the call's own evidence
established that the operation completed, and otherwise the call's outcome stands.

### Delivery invariants

These hold for every combination of failures, for every call that ends with an
envelope. A process that is killed while it writes a result, or that cannot write
its envelope to standard output, can leave a partial file that no envelope names:
nothing then reports it, so a file in the spill directory or at an `--output` path
that no envelope names must not be trusted.

1. **A possible copy is never lost.** If any file on disk may hold the whole
   result, the envelope names it, with its `verification` and the result's size
   and SHA-256 (null only when the SHA-256 could not be computed, which
   `result-hash-unavailable` says). It is never removed, and the result is never
   reported as absent.
2. **`file` is earned.** A file is `matched`, and the state is `file`, only when it
   was read back in full as exactly the result and the write, the flush to disk,
   and the close all succeeded.
3. **Nothing is left silently.** Every file the call created and left on disk,
   complete or not, is named in `data.result_file` or its `other_files`. An unused
   `--output` file that could not be removed is also named by the warning
   `output-reservation-left`, which gives its path. That warning is the only place
   it can be named when the call stopped before it found the operation, because
   `data` is then null.
4. **Incomplete is labeled.** A file known to be incomplete is `incomplete`, with
   no SHA-256 and an explicit reason, and never `matched` or
   `matched_unconfirmed`.
5. **Exit code 12 asserts completion.** It is used only when the operation's
   completion is established. Otherwise the outcome stays as the evidence dictates,
   and `data.result_delivery_error` records the failed delivery beside it.
6. **The envelope stays bounded.** Result data is in the envelope only for an
   `inline` delivery within the cap, whatever fails.

`DeliveryFaultMatrixTests` checks all six after each call. For every cell it first
predicts, from the injected faults alone, which files the CLI must write (including
the spill behind a failed `--output` file), what must be on disk afterwards and how
large, which file must be named first by the rule above, the delivery state, the
error, the exit code, and the main warnings. It then compares the files on disk,
a log of the real handle operations, and the envelope with that prediction, never
with the CLI's own account. It injects each fault point:

- for a spill: opening a directory, setting the permissions of `results`, a volume
  without permissions, creating the file, and the two hand-over checks;
- for either destination: a write that fails before any byte, after a prefix, or
  after the whole payload; the right length with different bytes; the read-back's
  length query and its read; the flush to disk; and the close.

It crosses them with the destination (inline, spill, or `--output`), the spill's
own fault when the `--output` file fails, removal that works or is refused, and
whether completion was established: 660 cells, and 4 more for an unused `--output`
file. Two fault points can only be injected, not produced on a real disk: a close
that reports an error, and a final path that differs from the path the file was
created at.

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
