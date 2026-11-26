# SA 2026.1.0529.7 licensed probe session 4 — 2026-11-26

This record summarizes licensed probe session 4 ([#402](https://github.com/spatialanalyzer/briosa/issues/402)) against one licensed SpatialAnalyzer `2026.1.0529.7` instance on 2026-11-26 (UTC), following the [session 4 runbook](../licensed-probe-session.md#licensed-session-4-402). Only this reviewed summary is committed; the raw harness records stay with the operator. It records counts, byte sizes, times, labels, enum names, MP codes, and listed diagnostic codes only—no returned values, raw arguments, fixture-manifest values, file or registry paths, process ids, job/collection/object names, license details, or dialog text.

Nothing here is transferred from the SA `2024.1.0508.5` record (`targets/2024.1.0508.5/docs/testing/evidence/probe-session-4-2024.1.0508.5-2026-11-26.md`). The SDK-ownership scenarios ([PR #428](https://github.com/spatialanalyzer/briosa/pull/428)) and the cold-start timings ([PR #431](https://github.com/spatialanalyzer/briosa/pull/431)) are not part of this session.

## Session

| Field | Value |
| --- | --- |
| Date (UTC) and runs | 2026-11-26: R1 attempt 1 (public API, probes 34 to 37; stopped at `p34-bspline`), R2 (worker, probe 34; completed), R1 attempt 2 (public API, probes 34 to 37, under a fresh go-ahead; completed). |
| Authorization | Separate maintainer go-ahead for each run: A1 for R1 attempt 1 ([amended agreement](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7180321589)), A2 for R2 after reviewing the R1 attempt 1 record, and a fresh A1 go-ahead for R1 attempt 2 after reviewing the R1 attempt 1 and R2 records. |
| Agreed rules | Decisions D1 to D12 and fallback rules F1 to F7 agreed on 2026-11-26 ([rules](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7179889851)) and amended on 2026-11-26 ([update](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7180321589)). Two updates from the runbook defaults: D5 settled that none of the five optional session 4 fixtures existed and none would be created (F4); D11 used a delegated operator for SpatialAnalyzer through computer use while the maintainer watched and pressed Ctrl+C in console windows when prompted. |
| Exclusive-use reservation (D2) | Confirmed by the maintainer on 2026-11-26 ([rules](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7179889851)): no other client used a Briosa worker, a loopback Briosa server, or SpatialAnalyzer on the machine during the session. Two separate Briosa Control Center processes were present outside the session. |
| R1 setup risk (D12) | Accepted by the maintainer on 2026-11-26 for both R1 authorizations ([rules](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7179889851)). |
| Merge SHA and harness revision | `d3919711eab2751bffebd49f5fc6ecd298b675d6` ([PR #427](https://github.com/spatialanalyzer/briosa/pull/427)); probe build `0.10.1-probe-s4`; preparation-safe harness unit tests passed 428 of 428 run (0 failed, 17 filtered out before execution). |
| Package and SHA-256 | `briosa-0.10.1-probe-s4-sa-2026.1.0529.7-win-x64.zip`, SHA-256 `D287374BD58D3BBD8D0F061928CB492ECCE1E030023282466D6934678832A80B` (all 854 extracted file hashes matched `files.sha256`). |
| Worker response budget | `1,048,576` bytes (1 MiB, since [PR #411](https://github.com/spatialanalyzer/briosa/pull/411)); published client receive limit `4,194,304` bytes (4 MiB). |
| Activated SDK and connected SA identity | R1 (attempts 1 and 2): connected SA `2026.1.0529.7` attested with `sa-installed-programs` (confirmed against the SpatialAnalyzer status bar and Windows installed-programs record); activated SDK `2026.1.0529.7` attested with `sdk-file-version` (registered SDK executable file version matched `2026.1.0529.7`); public-API harness records leave runtime identity fields null. R2: activated SDK `2026.1.0529.7` (`runtime-verified` by the worker; registered SDK file version also matched `sdk-file-version` before launch); connected SA `2026.1.0529.7` attested with `sa-installed-programs`. |
| Operator | The operator drove SpatialAnalyzer through computer use and ran non-console commands while the maintainer watched, opened the working job when prompted, and pressed Ctrl+C in the R1 server console at postflight step 1. |

## Runs

| Run | Started–ended (UTC) | Result | Record |
| --- | --- | --- | --- |
| A1 R1 attempt 1 (public 34 to 37) | Preflight 02:17:33Z; server 02:32:36Z–02:53:56Z; harness 02:33:47Z–02:35:49Z; postflight obs ended 03:00:19Z | **Stopped** (`outcome-unknown-do-not-replay` at `p34-bspline`, `WorkerWatchdogTimeout`, `sa-modal-dialog-open`, `StartedOutcomeUnknown`, F1). Exit code `1` inferred from pinned source. 37 of 37 guard/fixture steps ran; no ladder rung measured. | `probe-277-public-api-2026.1.0529.7` ([comment](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7193392858)) |
| A2 R2 (worker 34) | Preflight 13:19:36Z; harness 13:23:51Z–13:24:17Z; postflight obs settled 13:25:56Z | **Completed** (exit code `0`, `completed: true`, no stop reason). 26 of 26 guard/fixture steps succeeded; 120 s step timeout; no server. | `probe-277-worker-2026.1.0529.7` ([comment](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7193392858)) |
| Fresh A1 R1 attempt 2 (public 34 to 37) | Preflight 13:40:15Z; server 13:42:58Z–13:44:22Z; harness 13:43:33Z–13:43:50Z; postflight obs settled 13:45:21Z | **Completed** (exit code `0`, `completed: true`, no stop reason). 37 of 37 guard/fixture steps ran as planned. | `probe-277-public-api-2026.1.0529.7` ([comment](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7193876384)) |

Earlier preparation and preflight attempts before R1 attempt 1 stopped with no licensed step dispatched: once before preflight because computer use could not send keys to console windows (leading to the D11 amendment), once during preflight at the identity check because the title bar displayed no version, and once during renewed preflight when a provider error ended the operator session while waiting for the maintainer to open the working job (preflight then continued into R1 attempt 1).

**Server settings for R1 (both attempts):** `--Briosa:Security:Operations:Profile=device` (no per-operation admission overrides), `--Briosa:SpatialAnalyzer:InteractionMode=Unchanged`, `--Briosa:Worker:ExecutionWatchdogTimeout=00:02:00`, `--Briosa:Desktop:Mode=Disabled`, loopback `127.0.0.1:50051`. The lifecycle client (`external-connect`, 90 s bound) exited `0` with `ready_for_mp` in both attempts. The harness step timeout was 150 s.

**Stopped-run accounting (F1):** R1 attempt 1 stopped at `p34-bspline` (`outcome-unknown-do-not-replay`). All 17 remaining probe 34 ladder and object-list steps (`p34-points-100` through `p34-objects-job`) were not run under that authorization, and the three job-owned probe 34 steps and all nine steps of probes 35 to 37 were recorded as `fixture-missing`. Nothing from R1 attempt 1 was replayed under the same authorization. R1 attempt 2 ran only under a fresh maintainer authorization after review of the R1 attempt 1 and R2 records and a clean preflight.

## 1. Result sizes (probe 34)

Fixtures: `result_size_point_group` — not run: fixture missing (`c34-count-job-group`, `p34-list-job-group`); `result_size_cloud` — not run: fixture missing (`c34-count-job-cloud`).

R2 reports received worker-frame bytes against the 1,048,576-byte worker budget. R1 reports delivered protobuf message bytes against the 4,194,304-byte published client receive limit. Values in the summary table come from the two completed runs (R2 for worker frames; R1 attempt 2 for public protobuf messages). In R1 attempt 1, `p34-bspline` stopped before any list step ran, leaving all list measurements unavailable.

### List and count-only summary

| Step | Elements | Received frame bytes (R2) | Protobuf message bytes (R1 attempt 2) | List (R1 attempt 2 / R2) |
| --- | --- | ---: | ---: | --- |
| `p34-list-100` | 100 | 7,239 | 2,128 | delivered / delivered |
| `p34-list-500` | 500 | 34,039 | 10,928 | delivered / delivered |
| `p34-list-2000` | 2,000 | 137,540 | 46,928 | delivered / delivered |
| `p34-list-10000` | 10,000 | 699,540 | 248,928 | delivered / delivered |
| `p34-list-20000` | 20,000 (from its count) | 637 | | rejected, size unknown / rejected, size unknown |
| `p34-objects-harness` | 23 (R1 attempt 2) / 19 (R2) | 1,859 | 437 | delivered / delivered |
| `p34-objects-job` | 24 (R1 attempt 2) / 20 (R2) | 1,918 | 451 | delivered / delivered |
| `p34-list-job-group` | unknown | — | — | not run: fixture missing (`result_size_point_group`) |
| `c34-count-job-cloud` | unknown | — | — | not run: fixture missing (`result_size_cloud`; count only) |

The public-API setup runs 37 guard and fixture steps compared with 26 in the worker phase, which accounts for the four additional harness-created objects in `p34-objects-harness` and `p34-objects-job` during R1 attempt 2.

### Per-step probe 34 observations

In R1 attempt 1, `p34-bspline` ended with `grpc:Unavailable`, `WorkerWatchdogTimeout`, `sa-modal-dialog-open`, `StartedOutcomeUnknown`, and `OperatorInterventionRequired` at 120,085 ms; steps `p34-points-100` through `p34-objects-job` were not run, and the three job-owned steps were `fixture-missing`.

In R2 (worker run) and R1 attempt 2 (public-API run), every dispatched step had MP result code `2`:

| Step ID | R2 outcome (worker) | R2 MP code | R2 worker frame bytes | R2 elapsed ms | R1 attempt 2 gRPC status & outcome | R1 attempt 2 MP code | R1 attempt 2 protobuf bytes | R1 attempt 2 elapsed ms |
| --- | --- | ---: | ---: | ---: | --- | ---: | ---: | ---: |
| `p34-bspline` | `Succeeded` | 2 | 546 | 17,875 | `grpc:OK` / `Succeeded` | 2 | 7 | 10,259 |
| `p34-points-100` | `Succeeded` | 2 | 543 | 88 | `grpc:OK` / `Succeeded` | 2 | 7 | 96 |
| `c34-count-100` | `Succeeded` | 2 | 634 | 29 | `grpc:OK` / `Succeeded` | 2 | 26 | 45 |
| `p34-list-100` | `Succeeded` | 2 | 7,239 | 26 | `grpc:OK` / `Succeeded` | 2 | 2,128 | 26 |
| `p34-points-500` | `Succeeded` | 2 | 544 | 105 | `grpc:OK` / `Succeeded` | 2 | 7 | 109 |
| `c34-count-500` | `Succeeded` | 2 | 634 | 19 | `grpc:OK` / `Succeeded` | 2 | 27 | 17 |
| `p34-list-500` | `Succeeded` | 2 | 34,039 | 61 | `grpc:OK` / `Succeeded` | 2 | 10,928 | 61 |
| `p34-points-2000` | `Succeeded` | 2 | 544 | 228 | `grpc:OK` / `Succeeded` | 2 | 7 | 234 |
| `c34-count-2000` | `Succeeded` | 2 | 635 | 14 | `grpc:OK` / `Succeeded` | 2 | 27 | 45 |
| `p34-list-2000` | `Succeeded` | 2 | 137,540 | 204 | `grpc:OK` / `Succeeded` | 2 | 46,928 | 210 |
| `p34-points-10000` | `Succeeded` | 2 | 544 | 643 | `grpc:OK` / `Succeeded` | 2 | 7 | 640 |
| `c34-count-10000` | `Succeeded` | 2 | 636 | 35 | `grpc:OK` / `Succeeded` | 2 | 27 | 45 |
| `p34-list-10000` | `Succeeded` | 2 | 699,540 | 980 | `grpc:OK` / `Succeeded` | 2 | 248,928 | 965 |
| `p34-points-20000` | `Succeeded` | 2 | 545 | 1,143 | `grpc:OK` / `Succeeded` | 2 | 7 | 1,062 |
| `c34-count-20000` | `Succeeded` | 2 | 636 | 47 | `grpc:OK` / `Succeeded` | 2 | 28 | 55 |
| `p34-list-20000` | `OutputRetrievalFailed` (`worker-output-encoding-rejected`) | 2 | 637 (replacement) | 1,878 | `grpc:DataLoss` / `OutputRetrievalFailed` (`worker-output-encoding-rejected`, `OutputRetrievalFailure`) | 2 | | 1,966 |
| `p34-objects-harness` | `Succeeded` | 2 | 1,859 | 27 | `grpc:OK` / `Succeeded` | 2 | 437 | 31 |
| `p34-objects-job` | `Succeeded` | 2 | 1,918 | 6 | `grpc:OK` / `Succeeded` | 2 | 451 | 6 |
| `c34-count-job-group` | Not run (`fixture-missing`: `result_size_point_group`) | — | — | — | Not run (`fixture-missing`: `result_size_point_group`) | — | — | — |
| `p34-list-job-group` | Not run (`fixture-missing`: `result_size_point_group`) | — | — | — | Not run (`fixture-missing`: `result_size_point_group`) | — | — | — |
| `c34-count-job-cloud` | Not run (`fixture-missing`: `result_size_cloud`) | — | — | — | Not run (`fixture-missing`: `result_size_cloud`) | — | — | — |

For `p34-list-20000` in R1 attempt 2, the public error reported execution state `Succeeded`, execution disposition `Completed`, recovery guidance `None`, MP code `2`, and 0 of 1 outputs retrieved. No public protobuf result message was delivered (cell left blank). Trailer size was not measured. In both R2 and R1 attempt 2, the harness recorded `encoding_rejected: true` and `original_size: unknown`. The 637-byte frame in R2 is the bounded replacement frame. The original response size is unknown because the same diagnostic covers both an over-budget result and a result that cannot be encoded; this record does not state that it exceeded the 1 MiB budget as a measured fact.

- **Bytes per point name in the frame and in the message (from the rungs that were delivered):**
  - Worker frame (R2, including envelope): `72.39` (100), `68.08` (500), `68.77` (2,000), `69.95` (10,000) bytes per point.
  - Public protobuf message (R1 attempt 2, whole message): `21.28` (100), `21.86` (500), `23.46` (2,000), `24.89` (10,000) bytes per point.
- **Bytes per object name (from the two object lists):**
  - Worker frame (R2, including envelope): `97.84` bytes per object (`p34-objects-harness`, 19 elements); `95.90` bytes per object (`p34-objects-job`, 20 elements).
  - Public protobuf message (R1 attempt 2, whole message): `19.00` bytes per object (`p34-objects-harness`, 23 elements); `18.79` bytes per object (`p34-objects-job`, 24 elements).
- **The cloud's count as a name list (estimate):** Not available (`c34-count-job-cloud` not run: `result_size_cloud` missing).
- **The largest rung delivered, and the first whose encoding was rejected:** Largest rung delivered was `p34-list-10000` (`10,000` elements: `699,540` worker frame bytes in R2; `248,928` protobuf message bytes in R1 attempt 2). First rung whose encoding was rejected was `p34-list-20000` (`20,000` elements from `c34-count-20000`; original size unknown).
- **`c34-count-N` hypotheses:** Yes. In both R2 and R1 attempt 2, every constructed group held exactly `N` points (`100`, `500`, `2,000`, `10,000`, and `20,000`).
- **Construct times of the larger rungs (`elapsed_ms`), and any dialog (F3):**
  - Construct times: `p34-points-100` 88 ms (R2) / 96 ms (R1 attempt 2); `p34-points-500` 105 ms / 109 ms; `p34-points-2000` 228 ms / 234 ms; `p34-points-10000` 643 ms / 640 ms; `p34-points-20000` 1,143 ms / 1,062 ms.
  - Dialog (F3): `p34-bspline`.

## 2. Corrected bindings (probes 35 to 37)

| Binding | Step | Fixture | Result |
| --- | --- | --- | --- |
| Text callout `Text` (`SetEditTextArg`) | `p35-text` | `callout_view` | not run: fixture missing (`callout_view`) |
| Point callout `Notes` | `p35-point` | `callout_view` | not run: fixture missing (`callout_view`) |
| Point comparison callout `Additional Notes` | `p35-point-comparison` | `callout_view` | not run: fixture missing (`callout_view`) |
| Relationship callout `Additional Notes` | `p35-relationship` | `callout_view` | not run: fixture missing (`callout_view`) |
| Vector callout `Additional Notes` | `p35-vector` | `callout_view` | not run: fixture missing (`callout_view`) |
| Callout count before and after | `c35-callouts-before`, `c35-callouts-after` | `callout_view` | not run: fixture missing (`callout_view`) |
| TCP uncertainty `Result Notes` (`GetEditTextArg`) | `p36-notes` | `tcp_fixture` | not run: fixture missing (`tcp_fixture`) |
| Get Robot/Machine Parameter `Machine ID` (`SetColInstIdArg`) | `p37-parameter` | `robot_machine` | not run: fixture missing (`robot_machine`) |
| Start Robot/Machine Interface `Machine ID` | none | none | not run: refused by admission policy (exclusive workflow, D6) |
| Stop Robot/Machine Interface `Machine ID` | none | none | not run: refused by admission policy (exclusive workflow, D6) |

Whether each callout showed the two harness lines: not run (probe 35 was not dispatched; no checkpoints were requested or answered).

Any value the records give as `withheld`: none across all harness records for this target.

## 3. Tutorials

Not run in session 4 (D9): each language example starts its own Briosa server, and this session has no residual check between programs, no empty-job preflight, and no timeout or license-prompt rule for a running example.

## Postflight and process observations

No generic `stop-sdk` was run in any run. After every run, SpatialAnalyzer was closed without saving (File > Exit > No, answering `question` with No), and the working job copy matched the pristine copy byte for byte before execution and after closure.

### R1 attempt 1

- **Postflight step 1:** The maintainer stopped the R1 server with Ctrl+C in its own console window (exit code `0`).
- **Preflight state check:**
  ```text
  {"phase":"preflight","success":true,"exact_sa_instance_count":1,"briosa_process_count":0,"sdk_process_count":0,"issue_codes":[]}
  State check (Preflight): passed
  ```
  Preflight process observation: `settled`; final snapshot complete; `spatial_analyzer_listed_in_final_snapshot=1`; `residual_known=true`; `residual=none` (count 0).
- **Postflight state check (while SpatialAnalyzer was still open):**
  ```text
  {"phase":"postflight","success":false,"exact_sa_instance_count":1,"briosa_process_count":0,"sdk_process_count":1,"issue_codes":["RESIDUAL_SDK_PROCESS"]}
  State check (Postflight): failed
  ```
- **Follow-up snapshot after failed state check:** **listed on recheck: `sdk-1`** (`window-elapsed`; final snapshot complete; `spatial_analyzer_listed_in_final_snapshot=1`; `residual_known=true`; `residual=sdk-1`).
- **Postflight 300-second process observation (02:55:19Z–03:00:19Z):** `window-elapsed`; final snapshot complete; `spatial_analyzer_listed_in_final_snapshot=0`; `residual_known=true`; residual `sdk-1` (count 1; `last_seen_snapshot_start_ms=300379`, `first_absent_snapshot_end_ms=null`, started at `02:32:49Z`). Postflight did not pass due to this residual. The maintainer stopped the R1 server with Ctrl+C. No one stopped the residual SDK process. The `sdk-1` process had exited on its own before R2 preparation about ten hours later; its exact exit time in between was not observed ([issue #252 comment](https://github.com/spatialanalyzer/briosa/issues/252#issuecomment-7194865598)).

### R2

- **Postflight step 1:** Nothing stopped (no Briosa server ran in R2).
- **Preflight state check:**
  ```text
  {"phase":"preflight","success":true,"exact_sa_instance_count":1,"briosa_process_count":0,"sdk_process_count":0,"issue_codes":[]}
  State check (Preflight): passed
  ```
  Preflight process observation: `settled`; final snapshot complete; `spatial_analyzer_listed_in_final_snapshot=1`; `residual_known=true`; `residual=none` (count 0).
- **Postflight state check (while SpatialAnalyzer was still open):**
  ```text
  {"phase":"postflight","success":true,"exact_sa_instance_count":1,"briosa_process_count":0,"sdk_process_count":0,"issue_codes":[]}
  State check (Postflight): passed
  ```
- **Postflight 300-second process observation (13:25:56Z):** `settled` on the first complete snapshot; `spatial_analyzer_listed_in_final_snapshot=0`; `residual_known=true`; `residual=none` (count 0). Postflight passed.

### R1 attempt 2

- **Postflight step 1:** The maintainer stopped the R1 server with Ctrl+C in its own console window (exit code `0`).
- **Preflight state check:**
  ```text
  {"phase":"preflight","success":true,"exact_sa_instance_count":1,"briosa_process_count":0,"sdk_process_count":0,"issue_codes":[]}
  State check (Preflight): passed
  ```
  Preflight process observation: `settled`; final snapshot complete; `spatial_analyzer_listed_in_final_snapshot=1`; `residual_known=true`; `residual=none` (count 0).
- **Postflight state check (while SpatialAnalyzer was still open):**
  ```text
  {"phase":"postflight","success":true,"exact_sa_instance_count":1,"briosa_process_count":0,"sdk_process_count":0,"issue_codes":[]}
  State check (Postflight): passed
  ```
- **Postflight 300-second process observation (13:45:21Z):** `settled` on the first complete snapshot; `spatial_analyzer_listed_in_final_snapshot=0`; `residual_known=true`; `residual=none` (count 0). Postflight passed.

### Summary

- **License prompt:** None observed or reported in any run or step. `superseded_stop_reason` was null in all three runs.
- **Every residual:** `sdk-1` after R1 attempt 1 (`last_seen_snapshot_start_ms=300379`, `first_absent_snapshot_end_ms=null` at the end of the 300 s window). Unresolved at the end of R1 attempt 1. The maintainer stopped the R1 server with Ctrl+C. No one stopped the residual SDK process. It exited by itself before R2 preparation. No residual in R2 or R1 attempt 2. Two separate Briosa Control Center processes were present outside the session before and after runs.
- **At the end:** No Briosa server, worker, or SDK process remained, and no residual process was stopped.

## Deviations

- **R1 attempt 1:** The harness wrapper recorded its end time and both harness record files were written, but the wrapper did not write its exit-code marker; exit code `1` is inferred from the pinned source. Accessibility monitoring missed the `p34-bspline` `warning` dialog during dispatch; the dialog was dismissed with OK only after the 2-minute watchdog ended the step (`sa-modal-dialog-open`, 120,085 ms) and SpatialAnalyzer was brought to the foreground in postflight. Earlier pre-run stops occurred due to console-window input limits (leading to the D11 update), missing version in the title bar, and a provider error while waiting for job open.
- **R2:** An initial combined preparation and prompt-launch command was rejected before execution; separate preparation and launch commands succeeded before preflight, and the licensed harness command ran once. The harness started about 13 s after the maintainer pressed Enter in the ready prompt. The operator dismissed the `p34-bspline` `warning` dialog with OK about 18 s into the step (visible for about 16 s when dismissal was confirmed, compared with the approximately 10 s fallback target), and the step succeeded immediately afterwards (17,875 ms, within the 120 s step timeout). A transient progress window cleared on its own during the ladder.
- **R1 attempt 2:** The visible harness console briefly obscured part of SpatialAnalyzer at launch. The `p34-bspline` `warning` dialog was visible on the next screenshot; dismissal attribution (maintainer, inferred) and timing (~10 s) are inferred from the observed warning, the operator's lack of input, and step completion at 10,259 ms. The staged harness automatically included a collection name in its generated prose; the operator redacted those occurrences in both generated harness records while preserving all step IDs, outcomes, and measurements.

## Not run

- **R1 attempt 1 remaining probe 34 steps (`p34-points-100` through `p34-objects-job`):** Not run under the first A1 authorization because R1 attempt 1 stopped at `p34-bspline` (F1, `outcome-unknown-do-not-replay`). Those 17 steps ran later in R2 and R1 attempt 2 under separate authorizations.
- **Probe 34 job-owned objects:** `c34-count-job-group` and `p34-list-job-group` (`result_size_point_group` missing, F4); `c34-count-job-cloud` (`result_size_cloud` missing, F4).
- **Probe 35 (`c35-callouts-before`, `p35-text`, `p35-point`, `p35-point-comparison`, `p35-relationship`, `p35-vector`, `c35-callouts-after`):** `callout_view` missing (F4).
- **Probe 36 (`p36-notes`):** `tcp_fixture` missing (F4).
- **Probe 37 (`p37-parameter`):** `robot_machine` missing (F4).
- **Start Robot/Machine Interface and Stop Robot/Machine Interface:** Refused by admission policy (exclusive workflow, D6).
- **`briosa-examples` tutorials:** Not run in session 4 (D9).

## Follow-up

- [#245](https://github.com/spatialanalyzer/briosa/issues/245): live probe 34 point-list sizes ([point lists](https://github.com/spatialanalyzer/briosa/issues/245#issuecomment-7194857802)) and object-list sizes ([object lists](https://github.com/spatialanalyzer/briosa/issues/245#issuecomment-7195517623)) posted; 10,000 points delivered in 699,540 worker frame bytes (248,928 protobuf bytes) under the 1 MiB budget, and 20,000 points rejected with `worker-output-encoding-rejected` (original size unknown). Job-owned point groups and clouds remain unmeasured.
- [#252](https://github.com/spatialanalyzer/briosa/issues/252): postflight observations only ([comment](https://github.com/spatialanalyzer/briosa/issues/252#issuecomment-7194865598)); `sdk-1` outlived SpatialAnalyzer after the watchdog-stopped R1 attempt 1 and exited on its own later, while R2 and R1 attempt 2 left no residual. The ownership scenarios are in https://github.com/spatialanalyzer/briosa/pull/428.
- [#371](https://github.com/spatialanalyzer/briosa/issues/371) / [#438](https://github.com/spatialanalyzer/briosa/issues/438): not measured in this session. https://github.com/spatialanalyzer/briosa/issues/438 tracks the outstanding measurements. Draft https://github.com/spatialanalyzer/briosa/pull/431 preserves the proposed harness and runbook.
- [#402](https://github.com/spatialanalyzer/briosa/issues/402): public run records posted ([R1/R2](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7193392858), [R1 attempt 2](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7193876384), [session close](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7194857335)); probes 35 to 37 and job-owned probe 34 steps remain unexecuted pending fixtures.
- [#448](https://github.com/spatialanalyzer/briosa/issues/448): filed for the `p34-bspline` warning dialog that blocks unattended probe 34 runs until dismissed.

These are observations of one bounded session on one machine, not vendor guarantees.
