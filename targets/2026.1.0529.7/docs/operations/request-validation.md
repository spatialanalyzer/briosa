# Request validation

Before Briosa queues an MP command, it reads the request field by field and
checks what the operation needs: required fields, non-blank names, non-empty
lists, exact list lengths, number ranges, and supported choices. A request that
fails one of these checks never reaches SpatialAnalyzer.

## The failure

A validation failure returns:

| Part | Value |
| --- | --- |
| gRPC status | `INVALID_ARGUMENT` |
| Status text | `The request is invalid.` |
| `OperationError.kind` | `OPERATION_FAILURE_KIND_VALIDATION` |
| `OperationError.diagnostic_code` | `request-validation-failed` |
| `OperationError.execution_disposition` | `EXECUTION_DISPOSITION_NOT_STARTED` |
| `OperationError.recovery_guidance` | `RECOVERY_GUIDANCE_NONE` |
| `OperationError.replay_guidance` | `REPLAY_GUIDANCE_DO_NOT_REPLAY` |

The `OperationError` travels in the `briosa-operation-error-bin` trailer, as
for every other typed failure. Nothing ran, so correcting the request and
sending it again is safe. Sending the same request again fails the same way.

The checks run after admission. A request that the active policy denies, or that
arrives while the worker is not ready or the queue is full, fails for that
reason first, before any of its fields are read.

## Which field

Server 0.10.2 and later also set `OperationError.request_validation`, a
`RequestValidationDetails` that names the first field the server found invalid
and the check it failed. Everything else about the failure is unchanged from
Server 0.10.0 and 0.10.1. Earlier servers do not send it, and clients built for
them ignore it.

`field_path` is the field's protobuf name, with the names of the enclosing
message fields before it, separated by dots, from the request message down:

| Request | `field_path` | `constraint` |
| --- | --- | --- |
| `ScaleObjectsRequest` without `scale_factor` | `scale_factor` | `REQUIRED` |
| `ScaleObjectsRequest` with no `objects` | `objects` | `NOT_EMPTY` |
| `GetPointNotesRequest` whose `point.target_name` is `"  "` | `point.target_name` | `NOT_BLANK` |
| `ConstructFrameRequest` whose transform has 15 values | `transform_in_working_coordinates.values` | `ELEMENT_COUNT` |
| `ScaleObjectsRequest` with an `objects` entry whose `object_type` is unknown | `objects.object_type` | `SUPPORTED_CHOICE` |

A path through a repeated message field names the element's field without an
index, so `objects.object_type` means some entry of `objects`. The constraints
are:

| `constraint` | The field must |
| --- | --- |
| `REQUIRED` | be set. It is a message, or a field with explicit presence, that this operation needs. Some fields are needed only when another field turns on the behavior that uses them, such as `reference_object` when cloud clipping is enabled. |
| `NOT_BLANK` | contain a character other than whitespace. An unset string reads as empty, so it can fail this check instead of `REQUIRED`. |
| `NOT_EMPTY` | have at least one element. |
| `ELEMENT_COUNT` | have the exact number of elements its value needs, such as 16 for a transform. |
| `RANGE` | be a number the target accepts, such as 0 to 255 for a color channel or a font size. |
| `SUPPORTED_CHOICE` | hold a choice this operation supports on this target. For an enum, `UNSPECIFIED`, a number the schema does not define, and a value the operation does not accept all fail. A string that names a target choice fails for a name the target does not define. |
| `ONEOF_REQUIRED` | set one field of its oneof. The path names the message that holds the oneof, such as `report_output_options` without `external_path` or `embedded_file`. |

A newer server can send a constraint that an older client does not know. The
path still names the field.

The server stops at the first invalid field it finds, in the order it reads the
request, which is not necessarily field-number order. A request can have more
than one invalid field, so a corrected request can fail again on another one.

`request_validation` is absent when the failed check does not belong to one
field. For example, a request that cannot be encoded for the worker fails with
diagnostic `request-encoding-rejected` and no detail. On Server 0.10.2 and
later, a malformed `briosa-caller-restriction` header fails with diagnostic
`caller-restriction-invalid` and no detail, because the header is not a request
field; see [caller restriction](command-policy-and-auditing.md#caller-restriction).
A client must treat an absent detail as normal.

## No request values

The detail holds field names from the protobuf schema and a constraint, never
the value the request sent. The server publishes a path only when it is a dotted
list of lowercase protobuf field names. The status text stays fixed, and the
audit records and telemetry of a failed request are unchanged: they carry the
diagnostic code, not the field or its value. See
[server observability](server-observability.md) and
[command policy and auditing](command-policy-and-auditing.md#audit-events).
