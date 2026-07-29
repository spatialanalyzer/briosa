# Runtime performance, backpressure, and soak evidence

Issue [#71](https://github.com/spatialanalyzer/briosa/issues/71) defines a vendor-independent gate for the full generated surface. It measures Briosa-owned work and proves bounded runtime state without installing, launching, or connecting to SpatialAnalyzer.

Run the focused performance evidence after a Release build:

```powershell
./eng/Test-RuntimePerformance.ps1 -NoBuild
```

Omit `-NoBuild` when the server tests have not already been built. The script runs only `RuntimePerformanceEvidenceTests`, writes the raw evidence and standard budget reports below `artifacts/ci-metrics/runtime-performance`, and fails when a repository-owned threshold is exceeded. Ordinary CI runs the script and uploads the reports.

## Deterministic bounded-state contract

`WorkerProcessSupervisor.ExecutionSnapshot` contains no operation names, arguments, results, paths, or process identities. It reports:

- configured queue capacity, current admitted queue depth, callers waiting for admission, active executions, and peak admitted depth;
- cumulative admitted and terminal requests;
- caller cancellations before and after admission; and
- watchdog timeouts and worker failures.

Admission is generation-scoped. A caller waiting because the bounded queue is full is cancelled by either its own token or runtime-loop shutdown. Caller cancellation before admission is `NotStarted`; cancellation after admission remains `StartedOutcomeUnknown` while the single consumer drains the request to a terminal internal outcome. Stopping the supervisor closes admission, wakes capacity waiters, and produces one terminal result for every admitted item. The admission depth cannot exceed the reviewed queue capacity.

Lifecycle transition history is capped by `WorkerRestartPolicy.LifecycleHistoryCapacity`, which defaults to 256. `Current` remains available independently, so bounding historical diagnostics cannot remove the current state. Process tests repeatedly saturate and drain the queue, stop while admission is blocked, replace fake workers across six consecutive watchdog timeouts and eight consecutive crashes, and cycle worker generations to prove both execution accounting and lifecycle history remain bounded.

The audit soak writes 512 request-start and 512 completion events and verifies exactly one start/terminal pair per correlation ID. The events remain value-free. This is an audit-contract test, not a logging-throughput benchmark.

## Reviewed budgets

The schema-validated source of truth is [`eng/full-surface-policy.json`](../../eng/full-surface-policy.json). Measurements use raw values for enforcement; displayed JSON budget values are rounded only by the shared reporter.

| Metric | Maximum | Measurement boundary |
| --- | ---: | --- |
| Windows package size | 268,435,456 bytes | byte length of the first verified deterministic `win-x64` ZIP after its hash matches a second clean package |
| Startup working set | 536,870,912 bytes | packaged server working set immediately after the first accepted loopback connection, with the worker path intentionally missing and no SpatialAnalyzer connection |
| Fake-worker dispatch p95 | 250 milliseconds | 95th percentile of 512 sequential `WorkerProcessSupervisor.ExecuteAsync` calls after 64 warmups, crossing the private named-pipe JSON channel and a separate fake worker process but no SDK call |
| Request-mapping p95 | 50 milliseconds | 95th percentile of 512 generated `CreateCommand` → `RequireSuccess` → generated `CreateResult` mappings after 64 warmups with a fixed completed outcome; no service implementation or executor is involved |
| Discovery p95 | 50 milliseconds | 95th percentile of 512 `ListCapabilities` response constructions after 64 warmups, including every operation allowed from the current generated catalog |
| Retained managed memory | 33,554,432 bytes | non-negative managed-heap increase across the same 512-request sample, with full collections immediately before and after the sample |

The thresholds are deliberately conservative portability tripwires, not product latency or capacity claims. The three latency metrics use a percentile after warmup so one runner pause does not fail the gate. Retained memory detects unbounded request retention; it is not a promise about total process memory. The package startup working set covers the full generated server binary, the fake-worker measurement isolates queue/serialization/control-channel overhead, and the mapping/discovery samples cover catalog-derived host work without an SDK call.

The existing full-surface budgets continue to cover generation, restore, build, test, package duration, startup duration, descriptor size, and deterministic artifact generation. `Test-WindowsPackage.ps1` builds two complete packages and requires byte-identical ZIP SHA-256 values before recording package size or startup memory.

Budget changes require the evidence and review process in the [full-surface gate guide](../development/full-surface-gates.md#ci-budgets). Do not tune a threshold to a single slow or fast run.

## Licensed soak remains conditional

No licensed SpatialAnalyzer soak was run for issue #71. A conservative real-SA read-only soak remains deferred until issue [#20](https://github.com/spatialanalyzer/briosa/issues/20) is operational **and** Hexagon licensing guidance permits the proposed sustained automation. Those are external prerequisites, not evidence that the fake worker can supply.

When both prerequisites are satisfied, the soak must use the protected runner, one reviewed read-only operation, one worker/SA target, bounded iterations, value-free evidence, and the recovery procedure in the [licensed runner guide](../operations/licensed-sa-runner.md). It must not be added to an untrusted pull-request workflow or run on a personal workstation.
