# Generated-client smoke testing

Issue #18 validates the v0.1 vertical slice through a separate process that uses the generated .NET gRPC client. The smoke client crosses the packaged server's real loopback HTTP/2 boundary; it does not call server services in memory.

The probe reports only compatibility coordinates, state enums, booleans, and stable failure classifications. It intentionally does not print the working directory or any other returned SpatialAnalyzer value.

## Portable packaged-host scenarios

Build a package and run all generated-client scenarios on an ordinary Windows x64 machine:

```powershell
./eng/New-WindowsPackage.ps1 `
  -Version 0.1.0-test `
  -OutputDirectory artifacts/generated-client

./eng/Test-GeneratedClientScenarios.ps1 `
  -PackagePath artifacts/generated-client/briosa-0.1.0-test-sa-2026.1.0529.7-win-x64.zip
```

These tests substitute the separate `Briosa.SmokeWorker.exe` process for the real SDK worker. They require neither SpatialAnalyzer nor a license and cover:

| Client scenario | Expected public behavior |
| --- | --- |
| Ready | Generated `GetWorkingDirectory` client receives a successful MP/result-retrieval shape |
| Unavailable | Disconnected SDK state maps to `Unavailable` with a typed availability failure |
| MP failure | MP failure maps to `FailedPrecondition`, preserves the result, and marks output retrieval not attempted |
| Output failure | A successful MP followed by getter failure maps to `DataLoss` without returning a substitute value |
| Deadline | An expired client deadline remains distinct and a later call succeeds |
| Cancellation | Caller cancellation remains distinct and a later call succeeds |
| Watchdog recovery | A hung worker is terminated, replaced, and followed by a successful call without restarting the public host |
| Unsupported version | An RPC for an unavailable exact-target service returns `Unimplemented` |

The fake worker's results, codes, delays, failures, and hangs are invented Briosa test inputs. They are not a SpatialAnalyzer emulator.

Adapter tests separately prove that a failed MP suppresses all result-only SDK getters and that a successful MP followed by a failed getter is preserved. Error-mapper tests cover validation, unsupported operation, disconnected SA, unavailable worker, cancellation, deadline, watchdog, worker failure, rejected `ExecuteStep`, MP failure, getter failure, and malformed result shapes.

## Licensed SpatialAnalyzer smoke test

The licensed test is an explicit local or protected-runner action. Before running it:

- Install and separately license SpatialAnalyzer 2026.1.0529.7 x64.
- Start exactly one instance and allow it to become ready.
- Ensure that instance is the first eligible process owning the SDK ports. If ownership is uncertain, close all SA instances and start one clean instance.
- Close other Briosa servers, workers, and standalone SDK clients.
- Use a Briosa package built for SA 2026.1.0529.7.
- Run from a trusted checkout. Do not run untrusted pull-request code on a licensed machine.

Then run:

```powershell
./eng/Test-LicensedSpatialAnalyzer.ps1 `
  -PackagePath artifacts/release/briosa-0.1.0-sa-2026.1.0529.7-win-x64.zip `
  -ConfirmLicensedSpatialAnalyzerTest
```

The script:

1. verifies the exact running SA executable and that no competing Briosa/SDK client is present;
2. runs package diagnostics;
3. launches the packaged server and real worker on loopback;
4. uses the external generated client to verify discovery and `GetWorkingDirectory`;
5. requires successful MP execution and result-only retrieval without logging the returned directory; and
6. stops only the Briosa processes it created and reports a residual SDK process.

The connected SA version remains reported as unverified because the approved SDK interface has no reviewed version query. The exact executable prerequisite and package target provide the controlled test assumption; they do not change runtime discovery semantics.

Do not inject MP failures, hangs, crashes, or getter failures into a real production SA session. Those behaviors remain portable fake-worker tests. Protected runner trust boundaries, scheduling, credentials, and recovery policy remain issue #20.
