# Local SA 2024 functional checks — 2026-09-17

Maintainer-authorized checks on a Windows x64 workstation with separately
installed and licensed SpatialAnalyzer. This record contains no returned
application values, machine paths, license details, or credentials.

## Tested product and environment

- Briosa source: `d269015d4a634a78997e396ea24e3c8b3f0de368` (PR #187).
- Local package: `0.1.0-sa2024-local`, exact target `2024.1.0508.5`.
- Package ZIP SHA-256:
  `8BA1FE83952F4D111BAE41C9A58120EF19BD11B4B0AE188E733B3718F29C3B86`.
- The package's offline diagnostics identified the correct target, source
  revision, x64 architecture, worker, and approved interop metadata.
- No SA application, SDK engine, Briosa server, or worker was running before
  testing. One new, empty SA 2024 session was opened for each test phase.
- The application's file and product versions both identified 2024.1.0508.5.
  That process owned the observed local TCP 901, 902, and 903 listeners.
  Port ownership is local evidence, not a vendor protocol guarantee.
- Connected-SA operator evidence reference:
  `sa2024-local-functional-2026-09-17`. This identifies the observed local
  session only. No SDK-version fallback attestation was configured.

## Mismatch protection

The initial COM registration selected the installed 2026.1.0529.7 SDK. With the
2024 application open, the packaged Control Center started Briosa and activated
that SDK. Briosa reported the SDK's 2026.1.0529.7 version as runtime-verified,
displayed **Version mismatch**, disabled Connect, and left execution unverified.
The generated client's `unavailable` scenario passed: an MP request was rejected
with gRPC `UNAVAILABLE` and a structured `SpatialAnalyzerUnavailable` error.
No MP command ran during this phase. SDK and server shutdown preserved the
external SA application and left no residual SDK engine or worker.

## Matching SDK and read-only operations

The maintainer explicitly approved temporarily changing machine-wide SDK
registration. The installed 2024 SDK's vendor `/Regserver` command ran with
administrator elevation; the resulting registration selected the exact 2024
executable. A fresh SA 2024 session owned the local SDK listeners.

Through the packaged Control Center, server startup left the SDK stopped.
Starting the SDK reported **2024.1.0508.5 · Runtime verification · Exact match**.
The connected-SA claim was independently labeled operator attestation and exact
match. Connect completed the bounded execution-channel probe and displayed
**Ready for commands**.

The standard generated client's `ready` scenario passed these operations through
the same server and worker:

| Operation | Observed result |
| --- | --- |
| Get Working Directory | Successful MP result and retrieved directory output. |
| Get Number of Collections | Successful MP result and positive collection count. |
| Get i-th Collection Name | Successful MP result and retrieved name for the first collection. |
| Get Active Collection Name | Active collection output present. |
| Get Active Units | Successful MP result and retrieved, typed unit outputs. |
| Get Working Frame Properties | Successful MP result and retrieved frame properties, including a typed frame reference. |

Only structural success and retrieval assertions were retained. Returned values
were not logged in the test report. The working-frame check did not separately
capture the raw SDK object-type literal, so it does not establish when the
operation-specific omitted-type fallback was used.

## Restart and cleanup

Control Center's graceful restart preserved the same external SA application
and started the replacement server with its SDK stopped. The public lifecycle
client's `external-connect` scenario then explicitly started and connected a
new worker/SDK. All six read-only operations passed again.

The public `stop-sdk` scenario succeeded, reported no active SDK generation,
and preserved the external application. Control Center then stopped the server.
The test Control Center and empty SA session were closed.

The installed 2026.1.0529.7 SDK's vendor registration command restored the
original COM activation path. Its file version was verified after restoration.
Final process counts were zero for the SA application, SDK engine, Briosa
server, worker, and test Control Center. The temporary package's saved identity
attestation was removed after the session ended.

## Coverage limits

This is a small interactive smoke test. It does not validate the other 990
operations, physical instruments, the three new crib-sheet/projection commands,
nonempty geometry fixtures, destructive operations, ambiguous-outcome recovery,
or long-running behavior. No protected licensed CI runner or enterprise
Artifactory environment was exercised. Those validation gaps and the currently
planned v1.0 gates remain open.
