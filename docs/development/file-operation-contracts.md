# SA 2026.1.0529.7 file-operation contracts

Issue [#80](https://github.com/spatialanalyzer/briosa/issues/80) reviews 11 commands whose path, overwrite, append, merge, embedded-file, or interactive behavior was not safe to infer during command-shape reconciliation. All 11 are retained as at-risk Wave 3 candidates. None is intentionally excluded merely because a valid local fixture, third-party application, or license is unavailable.

Candidate status is not public support. Promotion remains a separate catalog, protocol, policy, adapter, fake-worker, and conformance review in [Wave 3 issue #65](https://github.com/spatialanalyzer/briosa/issues/65). Every candidate is deny-by-default, all request inputs remain explicit, and ambiguous completion never authorizes automatic replay.

## ObjectiveSA parity rule

The pinned ObjectiveSA commit `324c73b8e172868b4ccb4a0121e3bd1cbc520c5c` implements every issue #80 MP step with the same complete setter shape found in the committed exact-target inventory. That exact name-and-shape match authorizes Briosa to retain these operations without a separate command-by-command permission request.

ObjectiveSA targets an earlier SA release and remains secondary evidence. The exact SA 2026.1 inventory, interop API, and controlled live results win on conflict. ObjectiveSA cannot add an input, output, choice, default, or compatibility claim absent from the exact target.

An unavailable fixture is an at-risk validation gap, not evidence that the MP command is unsupported. If a future command lacks exact ObjectiveSA parity and its disposition is uncertain, maintainers must be asked before it is excluded or promoted.

## Repeatable licensed-test boundary

The committed matrix at `tests/Briosa.SpatialAnalyzer.IntegrationTests/file-operation-matrix.json` contains one scenario for each command. `eng/Test-LicensedFileOperations.ps1` builds the pinned ObjectiveSA checkout, verifies and starts exactly SA 2026.1.0529.7, admits one SDK client, runs one scenario under a watchdog, closes the disposable SA generation, and emits structural status only. Object-dependent scenarios open a disposable copy of a supplied SA job; the XML merge scenario also copies its input and verifies that the supplied source remains unchanged.

The runner is deliberately excluded from `Briosa.slnx`. Ordinary builds and pull-request tests must not require SpatialAnalyzer, a desktop session, a license, ObjectiveSA, or third-party fixtures. Local fixture descriptors, imported files, object names, paths, geometry, file contents, process identifiers, and proprietary artifacts must never be committed or copied into test reports.

The matrix distinguishes:

- `generated_by_test`: the scenario can create its disposable prerequisites;
- a named local SA-object or file fixture: the runner exists, but successful execution still needs that safe fixture; and
- `licensed_third_party_fixture_required`: the exact command is retained at risk even though the current machine cannot legally or practically generate a valid VSTARS or Polyworks source.

No unexecuted scenario is a passing test. `validation_status: performed` records controlled exact-target behavior already observed; `not_performed` remains explicit until the committed runner succeeds with a valid fixture.

## Cross-operation policy

For every candidate:

- require every MP input explicitly; ObjectiveSA convenience values are not public defaults;
- require and authorize non-empty absolute external paths before enqueue;
- reject embedded-file references initially;
- require an existing readable input or writable output parent as appropriate;
- require explicit append, replacement, merge, or current-job consent;
- keep the operation disabled until an operation-specific policy enables it;
- redact paths, contents, geometry, measurements, and object names from default logs and retained evidence; and
- prohibit automatic replay after timeout, cancellation, crash, or response loss.

`Save` additionally requires a named current job before enqueue. Briosa must reject the unnamed-job case rather than allowing the zero-argument MP step to open a modal Save As dialog.

## Exact-target observations

- `Export ASCII Points` created a nonempty file for an absent destination in both append modes. With an existing destination, append preserved the existing prefix and added data, while replace mode replaced the prior data.
- `Export ASCII Points` with a missing parent neither created the parent nor the file, but `ExecuteStep` did not return within 55 seconds and exposed a modal condition. Briosa must reject a missing or non-writable parent before enqueue and treat a later timeout as unknown completion.
- `Save` completed against a disposable named job and left a nonempty saved job after a subsequent mutation.
- `Save As` created a nonempty job, its serial-number mode produced exactly one nonempty output, and an existing destination was silently replaced.
- `Save As Read-Only Template` created a nonempty template and silently replaced an existing destination.
- `Output SA Report to PDF`, with a disposable empty report and `Show PDF?` false, created a nonempty PDF and silently replaced an existing destination. Viewer launch remains prohibited.
- `Export ASCII Point Set` rejected a generated point-group substitute and created no file. The committed negative scenario reproduces that result; it proves the typed point-set requirement, not successful export behavior.
- Event wildcard discovery did not return within 45 seconds and was watchdog-terminated. That ambiguous discovery attempt provides no successful Event export evidence.

## Candidate outcomes

| Inventory key | Validation | At-risk contract or remaining fixture |
| --- | --- | --- |
| `documentation:FileOperations/Save.htm` | Performed | Require a named current job; reject unnamed jobs to prevent modal Save As; no replay. |
| `documentation:FileOperations/SaveAs.htm` | Performed | Require all three inputs, existing writable parent, explicit replacement consent, and no replay. Atomicity and filename formatting are not public guarantees. |
| `documentation:FileOperations/SaveAsReadOnlyTemplate.htm` | Performed | Require an explicit destination and replacement consent. Do not expose the unbound serial-number prose. |
| `sdk:FileOperations_FileExport.txt#1` | Performed | Require all 18 inputs, nonempty group list, supported delimiter, writable parent, and explicit append or replacement consent. |
| `sdk:FileOperations_FileExport.txt#2` | Wrong-type path performed | The committed generated-negative scenario requires MP failure and no output. A valid typed point-set fixture is still required before claiming successful export. |
| `sdk:EventOperations.txt#4` | No valid fixture | Require a nonempty typed event-list fixture and explicit replacement consent. The discovery timeout is not a passing export test. |
| `documentation:FileOperations/XML/ImportNominalsFromXMLFile.htm` | Not performed | Require a valid exact-target XML fixture; collision, partial mutation, rollback, and UI behavior remain at risk. |
| `documentation:FileOperations/XML/MergeMeasurementsintoXML.htm` | Not performed | Require valid exact-target XML plus a typed point group and explicit in-place mutation consent. |
| `documentation:ProcessFlowOperations/OutputSAReportToPDF.htm` | Performed | Require a typed report, writable destination, explicit replacement consent, and `Show PDF?` false. |
| `sdk:FileOperations_FileImport.txt#16` | Not performed | Require a valid VSTARS camera fixture. Naming, duplicates, partial import, rollback, and UI behavior remain at risk. |
| `sdk:FileOperations_FileImport.txt#19` | Not performed | Require a valid Polyworks fixture and typed cloud target. Target mutation, partial import, rollback, and UI behavior remain at risk. |

All 11 commands have an exact SDK occurrence, complete required input shape, matching ObjectiveSA implementation, resolved command shape, and no issue #80 blocker reference. They are candidates only; none is automatically cataloged or enabled.

## Evidence identity

The exact-target value catalog records installed documentation aggregate SHA-256 `21d20f9cc79c37ca3515d184a5de3d820b8ecabff4a2da4f24977628d79b8d3a` and View SDK Code aggregate SHA-256 `cc12ba5bd8ded0e9af45eecb59c7894b1f19d0e45aa961cebb60c877cc72ef86`. The pinned ObjectiveSA baseline has fingerprint `d6107f1e10d2c957198c3cb082368033117e7e2ed2907eafb9eadc40607d295b`.

The ASCII SDK commands reconcile positionally to installed pages whose titles differ: `ExportASIIPoints.htm` has page SHA-256 `82685888be75ed26da7826a5d19abadfc964496221530021496e0753046b0c1b`, and `ExportASCIIPointSets.htm` has `9df859b0baa67091e983a9db1dbafead7ecda888ec3d074fc34a75cd39c86748`. Exact-target live behavior overrides the prior-release overwrite description without generalizing to other releases.
