# SA 2026.1.0529.7 device command review

Issue [#50](https://github.com/spatialanalyzer/briosa/issues/50) reviews all 243 exact-target commands in Instrument Operations, Robot Operations, and Robot Calibration Appliance Node Operations. The review replaces the remaining 242 unreviewed decisions and reassesses the one earlier issue #52 exclusion so every covered command has explicit risk, data, and value-family metadata.

## Results

| Disposition | Commands |
| --- | ---: |
| Approved candidate | 176 |
| Blocked | 35 |
| Intentional exclusion | 16 |
| SDK unavailable | 16 |

All 176 approved candidates are assigned to Wave 4. Of those, 85 carry `device_control`; none are promoted into the supported catalog by this review. The ordinary build and fake-worker test surface therefore remains hardware-independent, and the only currently advertised exact-target operation remains `Get Working Directory`.

## Review rules

- A coherent exact-target SDK shape may become a Wave 4 candidate and may be used to generate contracts, fake behavior, adapters, and portable tests before licensed execution is available. Candidate status does not advertise or enable the operation.
- A candidate carrying `device_control` cannot be promoted into the supported catalog until its protected real-SA fixture, cleanup sequence, command policy, deadline behavior, and failure recovery have passed review and conformance testing.
- A missing command occurrence or explicit unavailable required input binding produces `sdk_unavailable`.
- Direction, ordinal, getter, setter, or semantic conflicts remain command-scoped `blocked` decisions linked to issue #53. They are not guessed from nearby commands.
- Toolbar, dialog, manual-guide, inspection-window, and watch-window workflows that require operator-driven SpatialAnalyzer UI are intentional exclusions. Running a Crib Sheet remains client-owned control flow. `Watch Window Template 3D` is additionally excluded because it combines watch UI with SA-hosted UDP integration.
- File-backed operations carry explicit read or write risk and must use an isolated test directory. Device connection metadata is treated as network access where the exact operation can reach or configure an external endpoint.

## Protected fixture and cleanup gates

| Approved high-risk family | Required protected fixture before promotion | Required cleanup and recovery evidence |
| --- | --- | --- |
| Live instrument measurement, scanning, targeting, servo guide, projection, and hardware settings | Licensed exact-target SA; compatible dedicated instrument and driver; controlled work volume; representative targets/profiles; operator safety boundary | Stop active measurement, scan, guide, projection, or trapping mode; restore changed instrument settings; disconnect the interface; remove test observations and objects; prove worker replacement after a timeout or hang |
| Nikon laser-radar and APDIS calibration/self-test | Dedicated compatible laser-radar hardware, calibration artifacts, and vendor-required safe test setup | Stop the test or calibration; restore active calibration and laser settings; disconnect hardware; remove generated test data; record deterministic recovery behavior |
| Robot motion and robot interface | Dedicated protected robot cell or vendor-approved simulator; correct machine model and frames; speed/limit policy; exclusion zone and emergency-stop procedure | Stop motion and the robot interface; return to an approved safe state when the fixture permits; restore model parameters and frames; remove generated paths/calibrations; prove crash and timeout recovery cannot leave a second worker issuing commands |
| Calibration-appliance node and trapping | Dedicated appliance endpoint on an isolated network; compatible instrument, targets, profiles, frames, and point groups | Disable and clear the trap manager; stop trapping; disconnect and delete the test node; restore display/model state; remove captured measurements; prove repeated cleanup is safe |
| File-backed device configuration, cloud transfer, pose import, and simulation output | Isolated temporary directory and nonproduction representative files with no credentials or customer geometry | Restore the prior configuration; delete imported test objects and generated files; verify traversal and overwrite policy; retain no proprietary fixture data in logs or artifacts |
| Collection-only instrument and robot metadata | Licensed exact-target SA with an isolated disposable collection; hardware is unnecessary unless the command also carries `device_control` | Restore renamed/transformed/configured objects or discard the collection; delete created instruments, machines, fixtures, and calibrations; verify retries do not duplicate objects |

The command shards are the machine-readable source of truth. These family gates supplement, rather than replace, command-level risk flags and future operation-specific conformance tests.
