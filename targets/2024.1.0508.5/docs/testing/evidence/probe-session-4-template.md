# SA 2024.1.0508.5 licensed probe session 4 — TEMPLATE

**This is a template, not evidence.** Copy it to
`probe-session-4-2024.1.0508.5-<yyyy-mm-dd>.md`, fill in every field from the
harness records and the session notes, and delete this paragraph. Follow the
[session 4 runbook](../licensed-probe-session.md#licensed-session-4-402).

Never write a returned value, a fixture name or value from the manifest, a file
path, a process id, a user or machine name, or license data. Write labels
(`sdk-1`), counts, byte sizes, times, enum names, MP codes and diagnostic codes.
Write "not run" with its reason for anything that did not run; a missing fixture
is never a pass. Nothing here is transferred from the SA 2026.1.0529.7 record.

## Session

| Field | Value |
| --- | --- |
| Date (UTC) and runs | |
| Authorization | the maintainer's go-ahead for A1 to A9, each as given |
| Agreed rules | the date the proposed rules and decisions D1 to D17 were agreed on #402, and any that differ from the runbook |
| Merge SHA and harness revision | |
| Package and SHA-256 | `briosa-0.10.1-probe-s4-sa-2024.1.0508.5-win-x64.zip`, |
| #411 in the build | yes or no; the worker response budget the records state (bytes) |
| Activated SDK and connected SA identity | runtime-verified version; operator attestation reference |
| `InteractionMode` registry value | before and after the session |
| Operator | |

## Runs

| Run | Started–ended (UTC) | Result | Record |
| --- | --- | --- | --- |
| A1 C1 fresh-install | | completed / stopped (reason) / not run (reason) | `ownership-release`, `startup-timing-fresh-install` |
| A2 R1 public 34 to 37 | | | `probe-277-public-api` |
| A3 R2 worker 34 | | | `probe-277-worker` |
| A4 O1 worker-crash | | | `ownership-worker-crash` |
| A5 O2 parent-death | | | `ownership-parent-death` |
| A6 O3 watchdog-kill | | | `ownership-watchdog-kill` |
| A7 O4 competing-client | | | `ownership-competing-client` |
| A8 C2 after-reboot | | | `ownership-release`, `startup-timing-after-reboot` |
| A9 tutorials | | not on this target (SA 2026 only) | |

Server settings per run: the admission arguments, the `InteractionMode` setting,
the execution watchdog and the startup bound each server ran with.

## 1. Result sizes (probe 34)

Fixtures: the large point group's and the cloud's presence (counts below), or
"not run: fixture missing".

| Step | Elements | Worker frame bytes (R2) | Protobuf message bytes (R1) | Over the worker budget (R1 / R2) |
| --- | --- | --- | --- | --- |
| `p34-list-100` | | | | |
| `p34-list-500` | | | | |
| `p34-list-2000` | | | | |
| `p34-list-10000` | | | | |
| `p34-list-20000` | | | | |
| `p34-objects-harness` | | | | |
| `p34-objects-job` | | | | |
| `p34-list-job-group` | | | | |
| `c34-count-job-cloud` | points (count only) | - | - | - |

- Bytes per point name in the frame and in the message (from the rungs that fit):
- Bytes per object name (from the two object lists):
- The cloud's count as a name list, at the measured bytes per name (an estimate,
  labelled as such):
- The largest rung that fit each budget, and the first that did not:
- `c34-count-N` hypotheses: did every group hold N points?
- Construct times of the larger rungs (`elapsed_ms`), and any dialog (F3):

## 2. Cold starts

| Condition | Endpoint listening (ms) | Start RPC (ms) | process-created | control-pipe-connected | ready-received | Connect RPC (ms) | Startup bound |
| --- | --- | --- | --- | --- | --- | --- | --- |
| fresh-install (C1 attempt 1) | | | | | | | |
| warm (C1 attempt 2) | | | | | | | |
| after-reboot (C2 attempt 1) | | | | | | | |
| warm (C2 attempt 2) | | | | | | | |

- How each condition was established: uptime, minutes since logon, SA idle time,
  and that the package folder was new (C1) or had run before the restart (C2):
- The published `start_sdk_milliseconds` and `startup_without_launch_milliseconds`
  (from the `ownership-release` record):
- Against the 10 s default: the margin, or the phase that would have exceeded it:

## 3. SDK ownership

For each scenario: the SDK process's label and parent role; whether it outlived
the worker, the server and SA, and for how long (`exited_ms`, then the `observe`
record); the lifecycle states the server reported; and the residual.

| Scenario | SDK process and parent | Worker / server outcome | SDK process after the event | After SA closed (`observe`) | Residual and who stopped it |
| --- | --- | --- | --- | --- | --- |
| identity and normal release (C1, C2) | | | | | |
| worker crash (O1) | | `await-faulted`: | | | |
| parent death (O2) | | worker exited after (ms): | | | |
| watchdog kill (O3) | | stall result: | dialog open: / dismissed: | | |
| competing client (O4) | second client's SDK process: | second `ConnectEx`: ; first client MP before / during / after: | | | |

- Operator checkpoint answers (O3):
- What the operator stopped, with the label, the time and the maintainer's agreement (D7):

## 4. Corrected bindings (probes 35 to 37)

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

Whether each callout showed the two harness lines (operator, at the checkpoints):

## 5. Tutorials

Not on this target: the `briosa-examples` tutorials target SA 2026 only.

## Postflight

Per run: the `Test-LicensedRunnerState.ps1 -Phase Postflight` line, the `observe`
residual, and what the operator stopped. At the end: no Briosa or SDK process, or
the list of what was stopped.

## Not run

Every planned item that did not run, with its reason (fixture missing, declined,
stopped, abandoned, refused by admission).

## Follow-up

- #245:
- #252:
- #371:
- #402:

These are observations of one bounded session on one machine, not vendor guarantees.
