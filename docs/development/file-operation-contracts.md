# SA 2026.1.0529.7 file-operation contracts

Issue [#80](https://github.com/spatialanalyzer/briosa/issues/80) reviews 11 commands whose path, overwrite, append, merge, embedded-file, or interactive behavior was not safe to infer during command-shape reconciliation. This review uses committed exact-target inventory and value-family facts plus the installed-evidence fingerprints recorded below. It did not launch or connect to SpatialAnalyzer and did not perform a disposable-file experiment.

An approved candidate is not a supported Briosa operation. The four candidates below are assigned to [Wave 3 issue #65](https://github.com/spatialanalyzer/briosa/issues/65), remain deny-by-default, and must satisfy their recorded conformance prerequisites before catalog promotion. A timeout, cancellation, lost response, or worker replacement never authorizes automatic replay of one of these operations.

## Machine-readable boundary

The disposition shard's optional `operation_contract` preserves reviewed constraints for later catalog scaffolding:

- `decision` must agree with the disposition: `constrained_candidate`, `intentional_exclusion`, or `blocked_pending_exact_target_evidence`;
- `validation_status` distinguishes `not_performed` from `performed` instead of presenting documentation review as live proof;
- `constraints` records stable fail-closed requirements; and
- `evidence_limitations` records unresolved behavior. `not_performed` requires `live_validation_not_performed`; `performed` rejects that limitation.

The schema, semantic validator, deterministic report, and scaffold generator enforce and preserve this metadata. A later authorized exact-target probe must change `validation_status` and its limitations together. It must not silently weaken the constraints.

## Cross-operation policy

For every candidate in this review:

- require every MP input explicitly; a generated sample or prior-release wrapper value is not a public default;
- require a non-empty absolute external path and reject embedded-file references initially;
- canonicalize and authorize paths under the normal filesystem policy before enqueue;
- require explicit merge, replacement, creation, or append intent where applicable;
- keep the operation disabled until an operation-specific policy enables it;
- redact paths, file contents, geometry, measurements, and object names from default logs and test artifacts; and
- classify replay as prohibited until exact-operation evidence proves otherwise. Worker recovery restores availability only.

## Reviewed outcomes

| Inventory key | Outcome | Fail-closed contract | Evidence still required |
| --- | --- | --- | --- |
| `documentation:FileOperations/Save.htm` | Intentional exclusion | The MP has no destination input, depends on the current job's hidden file identity, and may open Save As for an unnamed job. The unattended service cannot obtain destination or UI consent. | No probe is needed for the exclusion. Reopening requires a vendor-supported bounded destination and UI-suppression mechanism. |
| `documentation:FileOperations/XML/MergeMeasurementsintoXML.htm` | Constrained Wave 3 candidate | Require an absolute external XML path, typed point group, all inputs, explicit in-place merge consent, and no automatic replay. Matching point records update or gain their actual value; the file is mutated in place. | Disposable conformance must prove unmatched-content preservation, update/create behavior, collision handling, and source preservation on failure. |
| `sdk:EventOperations.txt#4` | Constrained Wave 3 candidate | Require a non-empty typed event list, absolute external path, non-negative precision, and an explicit create-new or replace mode mapped to the overwrite Boolean. Do not offer append. | Exact-target conformance must prove false preserves an existing destination, true replaces it without prompting, ordering, and negative-path preservation. |
| `sdk:FileOperations_FileExport.txt#1` | Constrained Wave 3 candidate | Require all 18 inputs. Restrict delimiter to Space or Comma initially. Offer only explicit replace-or-create and append-or-create modes; do not promise create-only. | Exact-target conformance must resolve the prior-release conflict, absent/existing destination matrix, append boundary, preservation on failure, and modal behavior. |
| `sdk:FileOperations_FileExport.txt#2` | Constrained Wave 3 candidate | Require all 13 inputs and a typed point-set container. Apply the same delimiter and explicit replace-or-create/append-or-create policy; do not promise create-only. | The same disposable-file matrix is required, including point-set type rejection and modal behavior. |
| `documentation:FileOperations/SaveAs.htm` | Blocked | If reconsidered, require an absolute external destination and explicit serial-number choice; reject embedded paths and inferred overwrite consent. | Existing-file, atomicity, and modal-prompt behavior remain unresolved. |
| `documentation:FileOperations/SaveAsReadOnlyTemplate.htm` | Blocked | Require an absolute external destination. Do not expose the serial-number behavior mentioned in prose because the exact argument table and SDK observation contain no such binding. | Existing-file and modal-prompt behavior plus clarification of the prose/table conflict. |
| `documentation:FileOperations/XML/ImportNominalsFromXMLFile.htm` | Blocked | Require an absolute external XML source and reject automatic replay. | Same-named group and point collisions, partial mutation, rollback, and UI behavior. |
| `documentation:ProcessFlowOperations/OutputSAReportToPDF.htm` | Blocked | Require a typed report and absolute external PDF destination; force `Show PDF?` false and prohibit viewer launch. | Existing-file and modal-prompt behavior with the viewer disabled. |
| `sdk:FileOperations_FileImport.txt#16` | Blocked | Require an absolute external source; reject embedded paths and automatic replay. | Exact-target documentation or sanctioned sample guidance for camera naming, duplicate handling, partial import, rollback, and UI behavior. |
| `sdk:FileOperations_FileImport.txt#19` | Blocked | Require a typed cloud and absolute external source; reject embedded paths and automatic replay. | Target-cloud creation/append/replacement semantics, partial import, rollback, type rejection, and UI behavior. |

All 11 commands have an observed SDK step or binding. None is classified `sdk_unavailable`.

## Evidence identity and conflicts

The exact-target value catalog records installed documentation aggregate SHA-256 `21d20f9cc79c37ca3515d184a5de3d820b8ecabff4a2da4f24977628d79b8d3a` and View SDK Code aggregate SHA-256 `cc12ba5bd8ded0e9af45eecb59c7894b1f19d0e45aa961cebb60c877cc72ef86`. ObjectiveSA is pinned only as prior-release secondary evidence at commit `324c73b8e172868b4ccb4a0121e3bd1cbc520c5c`, fingerprint `d6107f1e10d2c957198c3cb082368033117e7e2ed2907eafb9eadc40607d295b`.

The ASCII SDK commands reconcile positionally to installed pages whose titles differ: `ExportASIIPoints.htm` has page SHA-256 `82685888be75ed26da7826a5d19abadfc964496221530021496e0753046b0c1b`, and `ExportASCIIPointSets.htm` has `9df859b0baa67091e983a9db1dbafead7ecda888ec3d074fc34a75cd39c86748`. Both exact pages describe false as replacement and true as append, while ObjectiveSA describes false as preserving an existing file. Explicit replace-or-create consent is safe under either behavior; a create-only promise is not.

The XML merge page is fingerprinted as `d2e5926a0659c5f9796e8b51b12f7be19e02a7169920aff8f1b967950ae7420b`; the additional installed product-help record is `374cf99d6526ea87ffb185dd618b482eb5ebbcefa1d75b820dcb8470d7fa644a`. These fingerprints identify reviewed source inputs without committing or reproducing vendor documentation.

## Remaining issue work

Issue #80 remains open. Six commands still require exact-target behavior evidence, and the four candidates still carry `promotion_requires_disposable_file_conformance` or equivalent limitations. Any live probe requires fresh authorization, a controlled licensed SA instance, disposable noncustomer fixtures, and a cleanup/recovery plan. `Show PDF? = true` is unnecessary and must not be tested as part of this contract.
