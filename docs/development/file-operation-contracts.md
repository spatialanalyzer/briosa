# SA 2026.1.0529.7 file-operation contracts

Issue [#80](https://github.com/spatialanalyzer/briosa/issues/80) reviewed 11 commands whose path, overwrite, append, merge, embedded-file, or interactive behavior was not safe to infer during command-shape reconciliation. The final decisions combine committed exact-target inventory and value-family evidence with controlled licensed validation against SpatialAnalyzer 2026.1.0529.7 on July 30, 2026.

Four commands are constrained Wave 3 candidates and seven are intentional exclusions. No issue #80 command remains blocked. Candidate status is not public support: the candidates remain deny-by-default and require operation-specific catalog, protocol, policy, adapter, fake-worker, and conformance review in [Wave 3 issue #65](https://github.com/spatialanalyzer/briosa/issues/65). A timeout, cancellation, lost response, or worker replacement never authorizes automatic replay.

## Evidence boundary

Licensed checks used one exact-target SA generation and one SDK client at a time, a fresh disposable collection, temporary noncustomer files, and bounded watchdogs. The committed evidence records behavioral classifications only. It does not contain the probe harness, filesystem paths, file contents, geometry, measurements, collection-object names, process identifiers, or proprietary artifacts.

`validation_status: performed` means the named exact-target behavior was exercised inside that boundary. It is not a general compatibility claim. `not_performed` is retained where a probe was unnecessary for an exclusion or no sanctioned disposable format fixture existed.

## Machine-readable boundary

The disposition shard's optional `operation_contract` preserves reviewed constraints for later catalog scaffolding:

- `decision` agrees with the disposition: `constrained_candidate` or `intentional_exclusion` for these final rows;
- `validation_status` distinguishes `not_performed` from `performed`;
- `constraints` records stable fail-closed requirements; and
- `evidence_limitations` records the remaining scope boundary without retaining an issue blocker.

The schema, semantic validator, deterministic report, binding registry, and scaffold generator enforce and preserve this metadata. A later probe may narrow an evidence limitation, but it must not silently weaken a constraint.

## Cross-operation policy

For every retained candidate:

- require every MP input explicitly; a generated sample or prior-release wrapper value is not a public default;
- require a non-empty absolute external path, reject embedded-file references initially, and authorize the canonical path before enqueue;
- require an existing writable parent before enqueue so an invalid destination cannot enter an ambiguously modal SDK call;
- require explicit replacement or append intent as applicable;
- keep the operation disabled until an operation-specific policy enables it;
- redact paths, file contents, geometry, measurements, and object names from default logs and test artifacts; and
- classify replay as prohibited. Worker recovery restores availability only.

## Exact-target observations

- `Export ASCII Points` created a nonempty file for an absent destination in both append modes. With an existing destination, append mode preserved the existing prefix and appended data, while replace mode replaced the prior data.
- `Export ASCII Points` with a missing parent neither created the parent nor the file, but `ExecuteStep` did not return within 55 seconds and exposed a modal condition. Briosa must reject a missing or non-writable parent before enqueue and treat a post-enqueue timeout as unknown completion that is never retried automatically.
- `Save As` created a nonempty job, its explicitly selected serial-number mode produced exactly one nonempty output, and an existing destination was silently replaced.
- `Save As Read-Only Template` created a nonempty template and silently replaced an existing destination.
- `Output SA Report to PDF`, using a freshly created disposable empty report with `Show PDF?` false, created a nonempty PDF and silently replaced an existing destination. Viewer launch remains prohibited.
- `Export ASCII Point Set` rejected a point-group container and created no file, confirming the typed point-set requirement. No safe non-device disposable point-set fixture exists to establish a successful export contract.
- Event wildcard discovery in a fresh disposable collection did not return within 45 seconds and was watchdog-terminated. Completion was ambiguous, and no safe event fixture exists.

## Final outcomes

| Inventory key | Final outcome | Fail-closed contract and evidence boundary |
| --- | --- | --- |
| `documentation:FileOperations/Save.htm` | Intentional exclusion | The MP has no destination input, depends on the current job's hidden file identity, and may open Save As for an unnamed job. The unattended service cannot obtain destination or UI consent; no live probe was needed. |
| `documentation:FileOperations/SaveAs.htm` | Constrained Wave 3 candidate; performed | Require an absolute destination, every serial-number input, explicit replacement consent, and no replay. New, serial-number, and existing-destination behavior was observed; atomic replacement and filename formatting are not public guarantees. |
| `documentation:FileOperations/SaveAsReadOnlyTemplate.htm` | Constrained Wave 3 candidate; performed | Require an absolute destination and explicit replacement consent. Do not expose the serial-number behavior mentioned in prose because the exact argument table and SDK occurrence contain no such binding. |
| `sdk:FileOperations_FileExport.txt#1` | Constrained Wave 3 candidate; performed | Require all 18 inputs, a non-empty group list, Space or Comma delimiter, existing writable parent, and explicit replace-or-create or append-or-create mode. Missing-parent completion is unknown after watchdog termination. |
| `documentation:ProcessFlowOperations/OutputSAReportToPDF.htm` | Constrained Wave 3 candidate; performed | Require a typed report, absolute destination, explicit replacement consent, and `Show PDF?` false. Viewer launch is prohibited; the probe used only a fresh disposable empty report. |
| `sdk:FileOperations_FileExport.txt#2` | Intentional exclusion; performed | A wrong collection-object type was rejected, but no sanctioned non-device point-set fixture exists. Reconsideration requires a typed point-set fixture and successful disposable-file conformance. |
| `sdk:EventOperations.txt#4` | Intentional exclusion; performed | Safe event discovery itself had an ambiguous watchdog outcome and no sanctioned disposable event fixture exists. Reconsideration requires bounded discovery and a safe typed event-list fixture. |
| `documentation:FileOperations/XML/ImportNominalsFromXMLFile.htm` | Intentional exclusion; not performed | No sanctioned exact-target XML schema or fixture exists to prove group/point collision, partial mutation, rollback, or UI behavior. |
| `documentation:FileOperations/XML/MergeMeasurementsintoXML.htm` | Intentional exclusion; not performed | The command mutates XML in place, but no sanctioned exact-target schema or fixture exists to prove preservation, collision, failure, and atomicity behavior. |
| `sdk:FileOperations_FileImport.txt#16` | Intentional exclusion; not performed | No sanctioned exact-target VSTARS fixture or format guidance exists to prove naming, duplicate, partial-import, rollback, or UI behavior. |
| `sdk:FileOperations_FileImport.txt#19` | Intentional exclusion; not performed | No sanctioned exact-target Polyworks fixture or format guidance exists to prove target-cloud mutation, partial-import, rollback, type, or UI behavior. |

All 11 commands have an observed SDK step or binding. None is `sdk_unavailable`, and none retains a blocker reference.

## Evidence identity

The exact-target value catalog records installed documentation aggregate SHA-256 `21d20f9cc79c37ca3515d184a5de3d820b8ecabff4a2da4f24977628d79b8d3a` and View SDK Code aggregate SHA-256 `cc12ba5bd8ded0e9af45eecb59c7894b1f19d0e45aa961cebb60c877cc72ef86`. ObjectiveSA remains pinned only as prior-release secondary evidence at commit `324c73b8e172868b4ccb4a0121e3bd1cbc520c5c`, fingerprint `d6107f1e10d2c957198c3cb082368033117e7e2ed2907eafb9eadc40607d295b`.

The ASCII SDK commands reconcile positionally to installed pages whose titles differ: `ExportASIIPoints.htm` has page SHA-256 `82685888be75ed26da7826a5d19abadfc964496221530021496e0753046b0c1b`, and `ExportASCIIPointSets.htm` has `9df859b0baa67091e983a9db1dbafead7ecda888ec3d074fc34a75cd39c86748`. The live `Export ASCII Points` matrix resolves the prior-release overwrite conflict for the exact target without generalizing to other releases.
