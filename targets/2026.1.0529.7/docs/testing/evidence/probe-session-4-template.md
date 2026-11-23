# SA 2026.1.0529.7 licensed probe session 4 — TEMPLATE

**This is a template, not evidence.** Copy it to
`probe-session-4-2026.1.0529.7-<yyyy-mm-dd>.md`, fill in every field from the
harness records and the session notes, and delete this paragraph. Follow the
[session 4 runbook](../licensed-probe-session.md#licensed-session-4-402).

Never write a returned value, a fixture name or value from the manifest, a file
path, a process id, a user or machine name, license data, or any text copied from
a dialog. Write labels (`sdk-1`), counts, byte sizes, times, enum names, MP codes,
listed diagnostic codes (`withheld` where the record says so) and the closed
checkpoint words. Write "not run" with its reason for anything that did not run; a
missing fixture is never a pass. Nothing here is transferred from the SA 2024.1.0508.5
record. The SDK-ownership scenarios are not part of this session
(https://github.com/spatialanalyzer/briosa/pull/428).

## Session

| Field | Value |
| --- | --- |
| Date (UTC) and runs | |
| Authorization | the maintainer's go-ahead for A1 to A5, each as given |
| Agreed rules | the date the proposed rules and decisions D1 to D14 were agreed on #402, and any that differ from the runbook |
| Operating precondition | how the clean state established exclusive use of the server and SA for each run |
| Merge SHA and harness revision | |
| Package and SHA-256 | `briosa-0.10.1-probe-s4-sa-2026.1.0529.7-win-x64.zip`, |
| Worker response budget | the bytes the records state (1 MiB since #411) |
| Activated SDK and connected SA identity | runtime-verified version; operator attestation alias (`sa-title-bar`, `sa-about-dialog`, `sa-installed-programs`, `sdk-file-version` or `installer-record`) |
| Operator | |

## Runs

| Run | Started–ended (UTC) | Result | Record |
| --- | --- | --- | --- |
| A1 C1 fresh-install | | completed / stopped (reason) / not run (reason) | `startup-timing-fresh-install` |
| A2 R1 public 34 to 37 | | | `probe-277-public-api` |
| A3 R2 worker 34 | | | `probe-277-worker` |
| A4 C2 after-reboot | | | `startup-timing-after-reboot` |
| A5 tutorials | | per program: exit code, as expected or not | |

Server settings per run: the admission arguments, the `InteractionMode` setting,
the execution watchdog and the startup bound each server ran with. For every rerun:
the `--rerun-of` record it named and the probes it selected (F1).

## 1. Result sizes (probe 34)

Fixtures: the large point group's and the cloud's presence (counts below), or
"not run: fixture missing".

| Step | Elements | Received frame bytes (R2) | Protobuf message bytes (R1) | Encoding (R1 / R2) |
| --- | --- | --- | --- | --- |
| `p34-list-100` | | | | delivered / rejected, size unknown |
| `p34-list-500` | | | | |
| `p34-list-2000` | | | | |
| `p34-list-10000` | | | | |
| `p34-list-20000` | | | | |
| `p34-objects-harness` | | | | |
| `p34-objects-job` | | | | |
| `p34-list-job-group` | | | | |
| `c34-count-job-cloud` | points (count only) | - | - | - |

- Bytes per point name in the frame and in the message (from the rungs that were
  delivered):
- Bytes per object name (from the two object lists):
- The cloud's count as a name list, at the measured bytes per name (an estimate,
  labelled as such):
- The largest rung delivered, and the first whose encoding was rejected. A rejected
  result's size is unknown; write its count, never a size:
- `c34-count-N` hypotheses: did every group hold N points?
- Construct times of the larger rungs (`elapsed_ms`), and any dialog (F3: the step
  id only):

## 2. Cold starts

| Condition | Configuration | Label | Correlation | Endpoint listening (ms) | Start RPC (ms) | process-created | control-pipe-connected | ready-received | Connect RPC (ms) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| fresh-install (C1 call 1) | changed configuration (60 s) / stock | | | | | | | | |
| warm (C1 call 2) | | repeat | | | | | | | |
| after-reboot (C2 call 1) | | | | | | | | | |
| warm (C2 call 2) | | repeat | | | | | | | |

- How each condition was established: uptime, minutes since logon, SA idle time,
  and that the package folder was new (C1) or had run before the restart (C2):
- Each `Briosa.LifecycleClient --scenario external` call's result (started,
  connected, stopped its own generation), or its typed failure:
- An attempt labelled `first-attempt-unproven`, or not `unambiguous`, is not a cold
  measurement; say so. An `overlapping-requests` attempt has no RPC time
  (`withheld-overlapping`) and no phases. The record's `unreadable_lines`:
- Against the stock 10 s default: the margin, or the phase that would have exceeded
  it. A raised bound is a changed configuration, not a stock start:

## 3. Corrected bindings (probes 35 to 37)

| Binding | Step | Fixture | Result |
| --- | --- | --- | --- |
| Text callout `Text` (`SetEditTextArg`) | `p35-text` | `callout_view` | |
| Point callout `Notes` | `p35-point` | `callout_view` | |
| Point comparison callout `Additional Notes` | `p35-point-comparison` | `callout_view` | |
| Relationship callout `Additional Notes` | `p35-relationship` | `callout_view` | |
| Vector callout `Additional Notes` | `p35-vector` | `callout_view` | |
| Callout count before and after | `c35-callouts-*` | `callout_view` | |
| TCP uncertainty `Result Notes` (`GetEditTextArg`) | `p36-notes` | `tcp_fixture` | outputs retrieved; note lines |
| Get Robot/Machine Parameter `Machine ID` (`SetColInstIdArg`) | `p37-parameter` | `robot_machine` | |
| Start Robot/Machine Interface `Machine ID` | none | none | not run: refused by admission policy (exclusive workflow) |
| Stop Robot/Machine Interface `Machine ID` | none | none | not run: refused by admission policy (exclusive workflow) |

Whether each callout showed the two harness lines (`lines-shown` or
`lines-not-shown` at the checkpoints):

## 4. Tutorials

Installed server version, client pins, and per program (.NET, TypeScript, Python, direct gRPC): the exit code and whether the output was as expected; for a failure, its error type and listed diagnostic code. Or "not run" with its reason (D14).

## Postflight and process observations

Per run: the `Test-LicensedRunnerState.ps1 -Phase Postflight` line, and the
`process-observation` record's final snapshot status, `spatial_analyzer_alive` and
residual (labels, with last-seen and first-absent bounds). No generic `stop-sdk` was
run. A record whose last snapshot was incomplete has not passed postflight.

- Whether a license prompt appeared, in which run and step, whether the signal or a
  checkpoint answer raised it, and the record's `superseded_stop_reason`, if any:
- Every residual: unresolved, or stopped by the maintainer's decision between runs
  (label, time, decision):
- At the end: no Briosa or SDK process, or the list of unresolved residuals and of
  what was stopped:

## Not run

Every planned item that did not run, with its reason (fixture missing, declined,
stopped, abandoned, refused by admission, refused by the rerun rule, license
prompt), and every probe not rerun under F1 because one of its steps had been sent
or a shared step's completion was unknown.

## Follow-up

- #245:
- #252: postflight observations only; the ownership scenarios are in https://github.com/spatialanalyzer/briosa/pull/428.
- #371:
- #402:

These are observations of one bounded session on one machine, not vendor guarantees.
