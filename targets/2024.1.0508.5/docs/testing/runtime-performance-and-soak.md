# Runtime performance and bounded-state evidence

`eng/Test-RuntimePerformance.ps1` exercises the supervised worker process through the private named-pipe control channel and a vendor-independent fake. It does not install, start, or connect to SpatialAnalyzer.

The harness warms up 64 requests and records 512 measured requests. It checks:

- every admitted request reaches a terminal state;
- the queue, admission waiters, and active-execution count drain to zero;
- no watchdog timeout or worker failure occurs in the normal scenario;
- dispatch, handwritten `GetWorkingDirectory` request/result mapping, and capability construction produce non-negative timing evidence; and
- retained managed memory measurement completes.

The JSON evidence is written under `artifacts/ci-metrics/runtime-performance`. It is diagnostic evidence, not a catalog/full-surface gate or a product latency guarantee.

The broader worker tests separately cover bounded queue saturation, cancellation before and after admission, shutdown wakeups, watchdog and crash recovery, lifecycle history bounds, uncertain completion, correlation, and value-free audit logging.

Real-SA soak testing remains an explicitly authorized licensed-environment task. It must avoid competing SDK clients and must not turn returned values, geometry, paths, or proprietary data into performance logs.

## Logging pipeline evidence

Run `./eng/Test-ObservabilityPerformance.ps1 -NoBuild` after a Release build.
It measures the shared OperationExecutor, fake named-pipe worker, supervisor
events, and configured log provider together. Four modes cover logging disabled,
normal JSONL files, a deliberately blocked sink, and a throwing sink.

Each mode warms 64 calls and measures 512. JSON records p50/p95/p99 latency,
throughput, process-wide allocations, retained managed-memory delta, queue depth
at the end of measurement, dropped events, and sink failures. The retained-memory
delta can be negative after collection. Normal mode checks successful flushing;
slow mode checks bounded dropping; failed mode checks containment. All modes
require completed fake operations with no watchdogs or worker failures.

These short runs include JIT, scheduling and filesystem-cache effects. Async
backlog is reported explicitly, so request throughput is not a claim about
sustained disk throughput. Use repeated controlled runs to set a regression
budget; this evidence is not an SA latency guarantee.

Run these measurements for this target and retain their output under `artifacts/ci-metrics`. Measurements from the 2026 product are not evidence for this product or for live SA latency.
