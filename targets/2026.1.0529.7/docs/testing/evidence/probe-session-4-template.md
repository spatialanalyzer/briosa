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
record. The SDK-ownership scenarios
(https://github.com/spatialanalyzer/briosa/pull/428) and the cold-start timings
(https://github.com/spatialanalyzer/briosa/pull/431) are not part of this session.

## Session

| Field | Value |
| --- | --- |
| Date (UTC) and runs | |
| Authorization | the maintainer's go-ahead for A1 and A2, each as given, or that A1 was not given |
| Agreed rules | the date the proposed rules and decisions D1 to D12 were agreed on #402, and any that differ from the runbook |
| Exclusive-use reservation (D2) | the date the maintainer confirmed that no other client would use a Briosa worker, a loopback Briosa server or SpatialAnalyzer on the machine during the session |
| R1 setup risk (D12) | accepted by authorizing R1, or not accepted (R1 not run: probe 34's public phase and probes 35 to 37 not run) |
| Merge SHA and harness revision | |
| Package and SHA-256 | `briosa-0.10.1-probe-s4-sa-2026.1.0529.7-win-x64.zip`, |
| Worker response budget | the bytes the records state (1 MiB since #411) |
| Activated SDK and connected SA identity | runtime-verified version; operator attestation alias (`sa-title-bar`, `sa-about-dialog`, `sa-installed-programs`, `sdk-file-version` or `installer-record`) |
| Operator | |

## Runs

| Run | Started–ended (UTC) | Result | Record |
| --- | --- | --- | --- |
| A1 R1 public 34 to 37 | | completed / stopped (reason) / not run (reason) | `probe-277-public-api` |
| A2 R2 worker 34 | | | `probe-277-worker` |

Server settings for R1: the admission arguments, the `InteractionMode` setting and
the execution watchdog it ran with.

A run that stopped is over (F1): list the probes it left not run. Nothing from it was
run again under the same authorization.

## 1. Result sizes (probe 34)

Fixtures: the large point group's and the cloud's presence (counts below), or
"not run: fixture missing".

| Step | Elements | Received frame bytes (R2) | Protobuf message bytes (R1) | List (R1 / R2) |
| --- | --- | --- | --- | --- |
| `p34-list-100` | | | | delivered / rejected, size unknown / unavailable (no list returned) |
| `p34-list-500` | | | | |
| `p34-list-2000` | | | | |
| `p34-list-10000` | | | | |
| `p34-list-20000` | | | | |
| `p34-objects-harness` | | | | |
| `p34-objects-job` | | | | |
| `p34-list-job-group` | | | | |
| `c34-count-job-cloud` | points (count only) | - | - | - |

Elements: the list's own count, the count step's "N (from its count)", or "unknown"
when neither a successful list nor a successful corresponding count supplies one. For
a rejected or unavailable list: Worker: record the received replacement or error
frame's bytes. Public RPC error: no result message was delivered; leave public message
bytes blank. Trailer size was not measured.

- Bytes per point name in the frame and in the message (from the rungs that were
  delivered):
- Bytes per object name (from the two object lists):
- The cloud's count as a name list, at the measured bytes per name (an estimate,
  labelled as such):
- The largest rung delivered, and the first whose encoding was rejected. A rejected
  result's size is unknown; write its count, or "unknown", never a size:
- `c34-count-N` hypotheses: did every group hold N points?
- Construct times of the larger rungs (`elapsed_ms`), and any dialog (F3: the step
  id only):

## 2. Corrected bindings (probes 35 to 37)

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

Any value the records give as `withheld` (a code, field name or enum value this
build does not list or define), with its step:

## 3. Tutorials

Not run in session 4 (D9): each language example starts its own Briosa server, and
this session has no residual check between programs, no empty-job preflight, and no
timeout or license-prompt rule for a running example.

## Postflight and process observations

Per run: what postflight step 1 stopped (after R1, the server R1's Phase 1 step 3
started, by Ctrl+C in its own console; after R2, nothing), and each state check's
lines (`Test-LicensedRunnerState.ps1` through the runbook's 64-bit PowerShell 7
wrapper, preflight and postflight: the rebuilt JSON line, if any, and the status
line). A run whose preflight failed was not started and had no postflight (F5); say
so. After a `failed` state check, the follow-up snapshot's one recording, the first
that applies: **observation inconclusive** (no JSON line; an incomplete or failed
snapshot; or a category the check counted with no label in the snapshot); **absent
when rechecked** (a JSON line, and a complete snapshot listing no SDK, worker or
server process); otherwise **listed on recheck: `<labels>`** (only labels in a
category the check counted, not asserted to be the processes it counted). Then the
`process-observation` record's final snapshot status,
`spatial_analyzer_listed_in_final_snapshot` and residual (the labels listed in the
final complete snapshot, each with its last-listed snapshot start and first-absent
snapshot end). No generic `stop-sdk` was run. A record whose last snapshot was
incomplete has not passed postflight.

- Whether a license prompt appeared, in which run and step (R1's Phase 1 step 4, a
  harness step, or postflight), whether the signal or a checkpoint answer raised it,
  and the record's `superseded_stop_reason`, if any:
- Every residual: unresolved, or stopped by the maintainer's decision between runs
  (label, time, decision):
- At the end: no Briosa or SDK process, or the list of unresolved residuals and of
  what was stopped:

## Not run

Every planned item that did not run, with its reason (fixture missing, declined, not
authorized, stopped, abandoned, refused by admission, license prompt), every probe
left not run when its run stopped (F1), and the tutorials (D9).

## Follow-up

- #245:
- #252: postflight observations only; the ownership scenarios are in https://github.com/spatialanalyzer/briosa/pull/428.
- #371: not measured in this session; the cold-start timings are in https://github.com/spatialanalyzer/briosa/pull/431.
- #402:

These are observations of one bounded session on one machine, not vendor guarantees.
