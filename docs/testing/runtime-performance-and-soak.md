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
