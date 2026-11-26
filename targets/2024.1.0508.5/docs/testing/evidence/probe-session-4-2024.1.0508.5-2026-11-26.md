# SA 2024.1.0508.5 licensed probe session 4 — 2026-11-26

This record summarizes licensed probe session 4 ([#402](https://github.com/spatialanalyzer/briosa/issues/402)) against one licensed SpatialAnalyzer `2024.1.0508.5` instance on 2026-11-26 (UTC), following the [session 4 runbook](../licensed-probe-session.md#licensed-session-4-402). Only this reviewed summary is committed; the raw harness records stay with the operator. It records counts, byte sizes, times, labels, enum names, MP codes, and listed diagnostic codes only—no returned values, raw arguments, fixture-manifest values, file or registry paths, process ids, job/collection/object names, license details, or dialog text.

Nothing here is transferred from the SA `2026.1.0529.7` record (`targets/2026.1.0529.7/docs/testing/evidence/probe-session-4-2026.1.0529.7-2026-11-26.md`). The SDK-ownership scenarios ([PR #428](https://github.com/spatialanalyzer/briosa/pull/428)) and the cold-start timings ([PR #431](https://github.com/spatialanalyzer/briosa/pull/431)) are not part of this session.

## Session

| Field | Value |
| --- | --- |
| Date (UTC) and runs | 2026-11-26: R2 attempt 1 (worker, probe 34; stopped at preflight under F5), R1 (public API, probes 34 to 37, after approved SDK registration switch; completed), R2 attempt 2 (worker, probe 34, followed by approved SDK registration restore; completed). |
| Authorization | Separate maintainer go-ahead after reviewing preceding records: A2 for R2 attempt 1, then approval for the vendor SDK registration switch and A1 for R1, then A2 for R2 attempt 2 and the post-session SDK registration restore. |
| Agreed rules | Decisions D1 to D12 and fallback rules F1 to F7 agreed on 2026-11-26 ([rules](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7179889851)) and amended on 2026-11-26 ([update](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7180321589)). Two updates from the runbook defaults: D5 settled that none of the five optional session 4 fixtures existed and none would be created (F4); D11 used a delegated operator for SpatialAnalyzer through computer use while the maintainer watched and pressed Ctrl+C in console windows when prompted. |
| Exclusive-use reservation (D2) | Confirmed by the maintainer on 2026-11-26 ([rules](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7179889851)): no other client used a Briosa worker, a loopback Briosa server, or SpatialAnalyzer on the machine during the session. Two separate Briosa Control Center processes were present outside the session. |
| R1 setup risk (D12) | Accepted by the maintainer on 2026-11-26 for R1 ([rules](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7179889851)). |
| Merge SHA and harness revision | `d3919711eab2751bffebd49f5fc6ecd298b675d6` ([PR #427](https://github.com/spatialanalyzer/briosa/pull/427)); probe build `0.10.1-probe-s4`; preparation-safe harness unit tests passed 428 of 428 run (0 failed, 17 filtered out before execution). |
| Package and SHA-256 | `briosa-0.10.1-probe-s4-sa-2024.1.0508.5-win-x64.zip`, SHA-256 `7BC21DCF627BB094386D7685F1BCB1B1BD26B0E8317ADDE439B6E93D2D9583F7` (all 854 extracted file hashes matched `files.sha256`). |
| Worker response budget | `1,048,576` bytes (1 MiB, since [PR #411](https://github.com/spatialanalyzer/briosa/pull/411)); published client receive limit `4,194,304` bytes (4 MiB). |
| Activated SDK and connected SA identity | R2 attempt 1: registered SDK executable in the 32-bit COM view reported `2026.1.0529.7` (`sdk-file-version`), mismatching target `2024.1.0508.5`; no SDK was activated and no SA instance ran (no SA attestation issued). R1: after the approved registration switch, registered SDK file version matched `2024.1.0508.5` (`sdk-file-version`), and at connection the server reported a runtime-verified activated SDK exact match; connected SA `2024.1.0508.5` attested with `sa-installed-programs` (confirmed against the SpatialAnalyzer status bar and Windows installed-programs record); public-API harness records leave runtime identity fields null. R2 attempt 2: registered SDK file version matched `2024.1.0508.5` during setup (`sdk-file-version`), and the worker recorded activated SDK `2024.1.0508.5` as `runtime-verified`; connected SA `2024.1.0508.5` attested with `sa-installed-programs`. |
| Operator | The operator drove SpatialAnalyzer through computer use and ran non-console commands while the maintainer watched, approved UAC for the SDK registration switch and restore, opened the working job when prompted, and pressed Ctrl+C in the R1 server console at postflight step 1. |

## Runs

| Run | Started–ended (UTC) | Result | Record |
| --- | --- | --- | --- |
| A2 R2 attempt 1 (worker 34) | Preflight 13:35:30Z–13:35:31Z; harness never started | **Stopped at preflight (F5)**: registered SDK version was `2026.1.0529.7`, not `2024.1.0508.5`. No SA launched, no harness run, no postflight. | preflight state check & recheck ([comment](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7193876384)) |
| A1 R1 (public 34 to 37) | Preflight 13:59:09Z; server 14:00:14Z–14:10:49Z; harness 14:09:42Z–14:10:21Z; postflight obs settled 14:11:44Z | **Completed** (exit code `0`, `completed: true`, `stop_reason: null`). 37 of 37 guard/fixture steps ran as planned. | `probe-277-public-api-2024.1.0508.5` ([comment](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7194535080)) |
| A2 R2 attempt 2 (worker 34) | Preflight 14:19:21Z; harness 14:20:56Z–14:21:27Z; postflight obs settled 14:22:29Z | **Completed** (exit code `0`, `completed: true`, `stop_reason: null`). 26 of 26 guard/fixture steps succeeded; 120 s step timeout; no server. | `probe-277-worker-2024.1.0508.5` ([comment](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7194857335)) |

**Server settings for R1:** `--Briosa:Security:Operations:Profile=device` (no per-operation admission overrides), `--Briosa:SpatialAnalyzer:InteractionMode=Unchanged`, `--Briosa:Worker:ExecutionWatchdogTimeout=00:02:00`, `--Briosa:Desktop:Mode=Disabled`, loopback `127.0.0.1:50051`. The lifecycle client (`external-connect`, 90 s bound) exited `0` with `ready_for_mp`. The harness step timeout was 150 s.

**Stopped-run accounting (F5):** R2 attempt 1 stopped during preflight before SpatialAnalyzer or the harness started, leaving every step of probe 34 undispatched under that authorization. Registration, installation, and settings were left unchanged until the maintainer approved the registration switch and subsequent runs.

## SDK registration switch and restore

Because the machine-wide COM registration pointed to SA `2026.1.0529.7` before SA `2024.1.0508.5` testing, the operator switched and later restored SDK registration as explicit operator actions approved by the maintainer:

- **Switch to SA `2024.1.0508.5` (before R1):** Approved by the maintainer after reviewing earlier records. With zero SpatialAnalyzer, SDK, Briosa server, and Briosa worker processes running (two Control Center processes outside the session), three before-switch registry exports (the application CLSID key and two ProgID keys) were taken. The maintainer acknowledged the prompt (`13:58:38Z`) and approved UAC, and the vendor registration command ran (`13:58:38Z–13:58:41Z`). Immediately afterwards (`13:58:41Z`), the 32-bit COM registration resolved to the installed SA `2024.1.0508.5` SDK executable with matching file version, three after-switch exports were taken, and process counts remained zero for SpatialAnalyzer, SDK, server, and worker.
- **Restore to SA `2026.1.0529.7` (after R2 attempt 2 postflight):** Approved by the maintainer as part of A2. With zero SpatialAnalyzer, SDK, server, and worker processes running, the maintainer acknowledged the prompt (`14:24:31Z`) and approved UAC, and the vendor registration command ran once (`14:24:31Z–14:24:35Z`). Immediately afterwards (`14:24:35Z`) and on final recheck (`14:25:58Z`), the 32-bit COM registration resolved to the original SDK executable with file version normalized to `2026.1.0529.7`. After completing the remaining two read-only ProgID exports in a follow-up read-only command, all three restored registry exports were verified **byte-for-byte identical** to the before-switch exports, and process counts remained zero for SpatialAnalyzer, SDK, server, and worker.

## 1. Result sizes (probe 34)

Fixtures: `result_size_point_group` — not run: fixture missing (`c34-count-job-group`, `p34-list-job-group`); `result_size_cloud` — not run: fixture missing (`c34-count-job-cloud`).

R2 reports received worker-frame bytes against the 1,048,576-byte worker budget. R1 reports delivered protobuf message bytes against the 4,194,304-byte published client receive limit. Values in the summary table come from the two completed runs (R2 attempt 2 for worker frames; R1 for public protobuf messages). R2 attempt 1 stopped at preflight under F5 and produced no measurements.

### List and count-only summary

| Step | Elements | Received frame bytes (R2 attempt 2) | Protobuf message bytes (R1) | List (R1 / R2 attempt 2) |
| --- | --- | ---: | ---: | --- |
| `p34-list-100` | 100 | 7,239 | 2,128 | delivered / delivered |
| `p34-list-500` | 500 | 34,039 | 10,928 | delivered / delivered |
| `p34-list-2000` | 2,000 | 137,540 | 46,928 | delivered / delivered |
| `p34-list-10000` | 10,000 | 699,540 | 248,928 | delivered / delivered |
| `p34-list-20000` | 20,000 (from its count) | 637 | | rejected, size unknown / rejected, size unknown |
| `p34-objects-harness` | 23 (R1) / 19 (R2 attempt 2) | 1,859 | 437 | delivered / delivered |
| `p34-objects-job` | 24 (R1) / 20 (R2 attempt 2) | 1,918 | 451 | delivered / delivered |
| `p34-list-job-group` | unknown | — | — | not run: fixture missing (`result_size_point_group`) |
| `c34-count-job-cloud` | unknown | — | — | not run: fixture missing (`result_size_cloud`; count only) |

The public-API setup runs 37 guard and fixture steps compared with 26 in the worker phase, which accounts for the four additional harness-created objects in `p34-objects-harness` and `p34-objects-job` during R1.

### Per-step probe 34 observations

In R2 attempt 1, all 21 probe 34 steps were not run (stopped at preflight under F5).

In R1 (public-API run) and R2 attempt 2 (worker run), every dispatched step had MP result code `2`:

| Step ID | R2 attempt 2 outcome (worker) | R2 attempt 2 MP code | R2 attempt 2 worker frame bytes | R2 attempt 2 elapsed ms | R1 gRPC status & outcome | R1 MP code | R1 protobuf bytes | R1 elapsed ms |
| --- | --- | ---: | ---: | ---: | --- | ---: | ---: | ---: |
| `p34-bspline` | `Succeeded` | 2 | 546 | 23,240 | `grpc:OK` / `Succeeded` | 2 | 7 | 31,201 |
| `p34-points-100` | `Succeeded` | 2 | 544 | 133 | `grpc:OK` / `Succeeded` | 2 | 7 | 96 |
| `c34-count-100` | `Succeeded` | 2 | 634 | 48 | `grpc:OK` / `Succeeded` | 2 | 26 | 43 |
| `p34-list-100` | `Succeeded` | 2 | 7,239 | 25 | `grpc:OK` / `Succeeded` | 2 | 2,128 | 26 |
| `p34-points-500` | `Succeeded` | 2 | 544 | 103 | `grpc:OK` / `Succeeded` | 2 | 7 | 103 |
| `c34-count-500` | `Succeeded` | 2 | 634 | 16 | `grpc:OK` / `Succeeded` | 2 | 27 | 18 |
| `p34-list-500` | `Succeeded` | 2 | 34,039 | 61 | `grpc:OK` / `Succeeded` | 2 | 10,928 | 67 |
| `p34-points-2000` | `Succeeded` | 2 | 544 | 262 | `grpc:OK` / `Succeeded` | 2 | 7 | 232 |
| `c34-count-2000` | `Succeeded` | 2 | 635 | 19 | `grpc:OK` / `Succeeded` | 2 | 27 | 48 |
| `p34-list-2000` | `Succeeded` | 2 | 137,540 | 238 | `grpc:OK` / `Succeeded` | 2 | 46,928 | 203 |
| `p34-points-10000` | `Succeeded` | 2 | 544 | 623 | `grpc:OK` / `Succeeded` | 2 | 7 | 631 |
| `c34-count-10000` | `Succeeded` | 2 | 636 | 56 | `grpc:OK` / `Succeeded` | 2 | 27 | 49 |
| `p34-list-10000` | `Succeeded` | 2 | 699,540 | 939 | `grpc:OK` / `Succeeded` | 2 | 248,928 | 1,029 |
| `p34-points-20000` | `Succeeded` | 2 | 545 | 1,080 | `grpc:OK` / `Succeeded` | 2 | 7 | 1,078 |
| `c34-count-20000` | `Succeeded` | 2 | 636 | 15 | `grpc:OK` / `Succeeded` | 2 | 28 | 62 |
| `p34-list-20000` | `OutputRetrievalFailed` (`worker-output-encoding-rejected`) | 2 | 637 (replacement) | 1,920 | `grpc:DataLoss` / `OutputRetrievalFailed` (`worker-output-encoding-rejected`, `OutputRetrievalFailure`) | 2 | | 1,954 |
| `p34-objects-harness` | `Succeeded` | 2 | 1,859 | 28 | `grpc:OK` / `Succeeded` | 2 | 437 | 34 |
| `p34-objects-job` | `Succeeded` | 2 | 1,918 | 5 | `grpc:OK` / `Succeeded` | 2 | 451 | 7 |
| `c34-count-job-group` | Not run (`fixture-missing`: `result_size_point_group`) | — | — | — | Not run (`fixture-missing`: `result_size_point_group`) | — | — | — |
| `p34-list-job-group` | Not run (`fixture-missing`: `result_size_point_group`) | — | — | — | Not run (`fixture-missing`: `result_size_point_group`) | — | — | — |
| `c34-count-job-cloud` | Not run (`fixture-missing`: `result_size_cloud`) | — | — | — | Not run (`fixture-missing`: `result_size_cloud`) | — | — | — |

For `p34-list-20000` in R1, the public error reported execution state `Succeeded`, execution disposition `Completed`, recovery guidance `None`, MP code `2`, `ExecuteStep` returned true, MP result retrieval returned true, and 0 of 1 outputs retrieved. No public protobuf result message was delivered (cell left blank). Trailer size was not measured. In both R1 and R2 attempt 2, the harness recorded `encoding_rejected: true` and `original_size: unknown`. The 637-byte frame in R2 attempt 2 is the bounded replacement frame. The original response size is unknown because the same diagnostic covers both an over-budget result and a result that cannot be encoded; this record does not state that it exceeded the 1 MiB budget as a measured fact.

- **Bytes per point name in the frame and in the message (from the rungs that were delivered):**
  - Worker frame (R2 attempt 2, including envelope): `72.39` (100), `68.08` (500), `68.77` (2,000), `69.95` (10,000) bytes per point.
  - Public protobuf message (R1, whole message): `21.28` (100), `21.86` (500), `23.46` (2,000), `24.89` (10,000) bytes per point.
- **Bytes per object name (from the two object lists):**
  - Worker frame (R2 attempt 2, including envelope): `97.84` bytes per object (`p34-objects-harness`, 19 elements); `95.90` bytes per object (`p34-objects-job`, 20 elements).
  - Public protobuf message (R1, whole message): `19.00` bytes per object (`p34-objects-harness`, 23 elements); `18.79` bytes per object (`p34-objects-job`, 24 elements).
- **The cloud's count as a name list (estimate):** Not available (`c34-count-job-cloud` not run: `result_size_cloud` missing).
- **The largest rung delivered, and the first whose encoding was rejected:** Largest rung delivered was `p34-list-10000` (`10,000` elements: `699,540` worker frame bytes in R2 attempt 2; `248,928` protobuf message bytes in R1). First rung whose encoding was rejected was `p34-list-20000` (`20,000` elements from `c34-count-20000`; original size unknown).
- **`c34-count-N` hypotheses:** Yes. In both R1 and R2 attempt 2, every constructed group held exactly `N` points (`100`, `500`, `2,000`, `10,000`, and `20,000`).
- **Construct times of the larger rungs (`elapsed_ms`), and any dialog (F3):**
  - Construct times: `p34-points-100` 133 ms (R2 attempt 2) / 96 ms (R1); `p34-points-500` 103 ms / 103 ms; `p34-points-2000` 262 ms / 232 ms; `p34-points-10000` 623 ms / 631 ms; `p34-points-20000` 1,080 ms / 1,078 ms.
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

Not run in session 4 (D9): each language example starts its own Briosa server, and this session has no residual check between programs, no empty-job preflight, and no timeout or license-prompt rule for a running example. The tutorials also target SA 2026 only.

## Postflight and process observations

No generic `stop-sdk` was run in any run. After every started run (R1 and R2 attempt 2), SpatialAnalyzer was closed without saving (File > Exit > No, answering `question` with No), and the working job copy matched the pristine copy byte for byte before execution and after closure.

### R2 attempt 1 (stopped at preflight)

- **Preflight state check:**
  ```text
  {"phase":"preflight","success":false,"exact_sa_instance_count":0,"briosa_process_count":0,"sdk_process_count":0,"issue_codes":["EXACT_SA_INSTANCE_COUNT_INVALID"]}
  State check (Preflight): failed
  ```
  The zero SpatialAnalyzer count reflects stopping before launching SpatialAnalyzer because of the SDK registration mismatch.
- **Follow-up snapshot after failed state check (13:35:31Z):** **absent when rechecked** (`settled`; final snapshot complete; `spatial_analyzer_listed_in_final_snapshot=0`; `residual_known=true`; `residual=none`, count 0).
- **Postflight:** Not applicable — the run was not started and had no postflight (F5). The pristine job and working copy were left untouched.

### R1

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
- **Postflight 300-second process observation (14:11:44Z):** `settled` early on a complete final snapshot; `spatial_analyzer_listed_in_final_snapshot=0`; `residual_known=true`; `residual=none` (count 0). Postflight passed.

### R2 attempt 2

- **Postflight step 1:** Nothing stopped (no Briosa server ran in R2 attempt 2).
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
- **Postflight 300-second process observation (14:22:29Z):** `settled` on the first complete snapshot; `spatial_analyzer_listed_in_final_snapshot=0`; `residual_known=true`; `residual=none` (count 0). Postflight passed.

### Summary

- **License prompt:** None observed or reported in any run or step. `superseded_stop_reason` was null in both completed runs.
- **Every residual:** None in any run on this target. Two separate Briosa Control Center processes were present outside the session before and after runs.
- **At the end:** No Briosa server, worker, or SDK process remained, and no residual process was stopped.

## Deviations

- **R2 attempt 1:** The operator checked SDK registration before copying the working job or prompting the maintainer to start SpatialAnalyzer, which allowed the F5 stop before either action. Only the pinned state wrapper and immediate read-only recheck ran after the mismatch; registration, installation, and settings were left as found.
- **R1:** The first screenshot follow-up after harness launch occurred about 20 s later (outside the requested every-few-seconds cadence). The operator dismissed the `p34-bspline` `warning` dialog with OK at about `14:10:15Z`, roughly 31 s into the step (~13 s after first sight at `14:10:02Z`), and the step succeeded in 31,201 ms. A transient progress window (`other`) cleared between screenshots, leaving its exact rung and timing unverified. The ready prompt launched a minimized harness console to keep SpatialAnalyzer visible. The generated harness JSON and Markdown records included a collection name and the JSON purpose descriptions contained fixture references; the operator redacted 30 occurrences in JSON and 1 in Markdown while preserving all step IDs, outcomes, and measurements.
- **R2 attempt 2:** The first screenshot returned about 9 s after harness start (~4 s beyond the 5 s target). The operator dismissed the `p34-bspline` `warning` dialog with OK about 23 s into the step (first sighted by `14:21:05Z`, click returned at `14:21:21Z`, about 16 s after first sight and ~6 s beyond the 10 s fallback target), and the step succeeded in 23,240 ms. A transient progress window (`other`) cleared on its own; its exact step and duration were unverified. The generated harness records included a collection name once in JSON and once in Markdown, and 19 JSON point-setup purpose descriptions contained fixture references; the operator redacted all of them while preserving step IDs, timings, outcomes, MP codes, and frame measurements. During registration restore, the restore wrapper verified registration and wrote the CLSID export before ending prior to the comparison and the two ProgID exports; a follow-up read-only command completed the remaining two exports and byte-for-byte comparison, with the vendor registration command run once.

## Not run

- **R2 attempt 1 (all 21 probe 34 steps and setup steps):** Not run under the first A2 authorization because preflight stopped on the SDK registration mismatch (F5). The 18 non-fixture-missing probe 34 steps later ran in R1 and R2 attempt 2 after the approved registration switch.
- **Probe 34 job-owned objects:** `c34-count-job-group` and `p34-list-job-group` (`result_size_point_group` missing, F4); `c34-count-job-cloud` (`result_size_cloud` missing, F4).
- **Probe 35 (`c35-callouts-before`, `p35-text`, `p35-point`, `p35-point-comparison`, `p35-relationship`, `p35-vector`, `c35-callouts-after`):** `callout_view` missing (F4).
- **Probe 36 (`p36-notes`):** `tcp_fixture` missing (F4).
- **Probe 37 (`p37-parameter`):** `robot_machine` missing (F4).
- **Start Robot/Machine Interface and Stop Robot/Machine Interface:** Refused by admission policy (exclusive workflow, D6).
- **`briosa-examples` tutorials:** Not run in session 4 (D9).

## Follow-up

- [#245](https://github.com/spatialanalyzer/briosa/issues/245): live probe 34 point-list sizes ([point lists](https://github.com/spatialanalyzer/briosa/issues/245#issuecomment-7194857802)) and object-list sizes ([object lists](https://github.com/spatialanalyzer/briosa/issues/245#issuecomment-7195517623)) posted; 10,000 points delivered in 699,540 worker frame bytes (248,928 protobuf bytes) under the 1 MiB budget, and 20,000 points rejected with `worker-output-encoding-rejected` (original size unknown). Job-owned point groups and clouds remain unmeasured.
- [#252](https://github.com/spatialanalyzer/briosa/issues/252): postflight observations only ([comment](https://github.com/spatialanalyzer/briosa/issues/252#issuecomment-7194865598)); no residual process was observed on this target in either completed run. The ownership scenarios are in https://github.com/spatialanalyzer/briosa/pull/428.
- [#371](https://github.com/spatialanalyzer/briosa/issues/371) / [#438](https://github.com/spatialanalyzer/briosa/issues/438): not measured in this session. https://github.com/spatialanalyzer/briosa/issues/438 tracks the outstanding measurements. Draft https://github.com/spatialanalyzer/briosa/pull/431 preserves the proposed harness and runbook.
- [#402](https://github.com/spatialanalyzer/briosa/issues/402): public run records posted ([R2 attempt 1 stop](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7193876384), [R1](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7194535080), [R2 attempt 2 and session close](https://github.com/spatialanalyzer/briosa/issues/402#issuecomment-7194857335)); probes 35 to 37 and job-owned probe 34 steps remain unexecuted pending fixtures.
- [#448](https://github.com/spatialanalyzer/briosa/issues/448): filed for the `p34-bspline` warning dialog that blocks unattended probe 34 runs until dismissed.

These are observations of one bounded session on one machine, not vendor guarantees.
