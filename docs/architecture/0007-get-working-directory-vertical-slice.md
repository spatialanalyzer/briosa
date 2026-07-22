# ADR 0007: Get Working Directory reference vertical slice

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-22

## Context

`Get Working Directory` is Briosa's first production-shaped public MP operation for SpatialAnalyzer 2026.1.0529.7. It has no inputs and one result-only string argument. The installed MP command reference and generated SDK sample agree that the MP step is `Get Working Directory` and the SDK getter argument is `Directory`.

The command is intentionally small, but it must exercise the same boundaries required by larger commands: exact-release catalog metadata, deterministic public generation, the gRPC host, the supervised worker process, the serialized SDK-owning STA, MP-result inspection, and result-only argument retrieval.

## Decision

The reviewed catalog remains the source of truth for repetitive operation artifacts.

- `Briosa.Generator catalog-generate` generates the exact-target `operations.proto` contract and an immutable worker-command binding. CI regenerates both in a temporary directory and fails when committed artifacts differ or stale generated files remain.
- The generated binding creates a command with operation ID `file_operations.get_working_directory`, MP step `Get Working Directory`, no input setters, and one requested text output named `Directory`.
- The hand-written target service submits that command through `IWorkerCommandExecutor`. The production implementation is the existing `WorkerProcessSupervisor`; the public host never owns SDK or COM state.
- The worker executes `SetStep("Get Working Directory")`, `ExecuteStep`, `GetMPStepResult`, and, only after MP success, `GetStringArg("Directory", ...)` on its single SDK-owning STA.
- A successful getter produces a present `directory` field. An MP failure suppresses output retrieval. A failed getter produces a gRPC failure and never creates a response containing an empty or default directory.
- Diagnostics contain only curated codes, generation, duration, and the numeric MP result. The retrieved path is neither logged nor placed in error status text.

## Initial transport mapping

The vertical slice originally used the deliberately small mapping below. ADR 0008 now supersedes it with the shared typed outcome and error-detail contract while preserving these canonical gRPC statuses.

| Internal outcome | gRPC status | Diagnostic detail |
| --- | --- | --- |
| Completed with retrieved output | OK | Typed result with explicit string presence |
| `ExecuteStep` rejected or MP failed | `FailedPrecondition` | Curated diagnostic; MP code in metadata when available |
| Output getter failed | `DataLoss` | `sdk-output-retrieval-failed` |
| Worker unavailable, crashed, or watchdog expired | `Unavailable` | Curated worker diagnostic |
| Caller stopped waiting | `Cancelled` | `client-wait-cancelled` |

A worker watchdog expiration is not reported as the caller's gRPC deadline. ADR 0008 defines the complete cross-operation mapping, retry guidance, caller-deadline behavior, and shared typed error details.

## Generation boundary

This issue generates the contract and no-input command binding needed to prove one vertical slice. Generation rejects cataloged input operations until issue #16 adds reviewed input presence/default mapping, general response adapters, reference documentation, and completeness manifests. Policy, logging, worker supervision, and gRPC outcome mapping remain hand-written review points.

## Testing

Ordinary tests use the generated client and a fake worker executor to verify success, MP failure, output-getter failure, and watchdog behavior. A focused SDK-adapter test verifies the exact call order through `GetStringArg("Directory", ...)`. These tests do not activate SpatialAnalyzer and remain suitable for generic Windows CI.

Validation against a real SpatialAnalyzer installation is a separately authorized integration level. It must use SA 2026.1.0529.7, avoid attaching competing SDK clients, and confirm that the generated client receives the application's current working directory.

## Consequences

- The first public operation travels through the same worker boundary future commands will use.
- Catalog and generated artifacts cannot drift silently.
- Getter failure cannot be mistaken for a successful empty path.
- Issues #13 and #16 retain their broader design and generation responsibilities rather than being silently settled by this single operation.
