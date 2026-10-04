# Operation classification seed

This directory records where the per-target D1 operation classification tables
came from and how to reproduce that seed. The D1 decision is on
[#242](https://github.com/spatialanalyzer/briosa/issues/242); the implementation
is [#293](https://github.com/spatialanalyzer/briosa/issues/293).

**The tables are the source of truth**, in each target's
`src/Briosa.Server/Security/OperationClassification.cs`. Each one holds one
hand-reviewed row per registered operation: risk flags, duration class,
validation status, and intended isolation class. Change a classification by
editing the table, and add a row with every new operation. The target's
`OperationClassificationTests` fail when a row is missing, extra, or incomplete.
The files here are review artifacts and are not read by any build or test.

At step 1 of #293 the tables are not consumed by `OperationPolicy`. The
registered descriptors still govern admission, scope, and replay safety.

## Files

| File | Purpose |
|---|---|
| `operation-classification-<target>.csv` | The reviewed seed, one row per operation. The tables were transcribed from these files. |
| `classify.py` | The rules used for the decision, plus the corrections made in review. It writes the CSVs from a descriptor dump. |
| `Export-OperationDescriptors.ps1` | Dumps a built target's compiled registry (`SpatialAnalyzerApi.Operations`) as `descriptors-<target>.json`, the input of `classify.py`. |
| `target-differences.json` | Reviewed shared operations whose rows may differ between targets. It is read by `eng/Test-OperationClassificationTwins.ps1`. |

The CSV columns are the operation id, its registered effect, a primary family
(a review aid only), the snake_case risk flags, the duration class, the
validation status, the intended isolation class, the legacy descriptor flags,
and admission under each D1 profile. Admission is computed from the proposal's
profile definitions as amended by the decision. A profile admits an operation
when all of its flags are in the profile's set and its effect filter passes.
`interactive_ui` is in no profile's set, and `exclusive_workflow` is never
admitted.

## Provenance

The decision classified the registries of `origin/main` at `5d2c2cb` (Server
0.9.1 line). `classify.py` uses those name, service and per-operation rules,
which were checked against the registry but not against licensed SA runs. The
#242 decision then changed two things, and both are applied here:

- The interactive operations (47 per target) are not exclusive. They carry
  `interactive_ui` and the `interactive` duration class, and their intended
  isolation is `admissible`.
- Only the multi-call session lifecycles are `exclusive_workflow`: 43 on 2026
  and 46 on 2024.

The existing descriptor flags map as decided. `fixture_validation_pending` and
the 2024 `at-risk-no-runtime-validation` become validation statuses
(`fixture_pending`, `at_risk_unvalidated`). The 2024 `long-running` becomes the
`long_running` duration class. The 2024 `instrument-control` becomes
`device_session`. `destructive` and `filesystem_metadata` stay flags.

These corrections were made while reviewing the seed for #293 step 1. They are
encoded as `REVIEW_ADD_FLAGS` and `REVIEW_REMOVE_FLAGS` in `classify.py`, and
none of them changes a profile admission count:

| Operation | Change | Reason |
|---|---|---|
| `instrument_operations.measure_existing_single_point_manual_guide` | + `device_session` | It measures with a live instrument, like the other `measure_*` device actions. |
| `file_operations.load_html_form`, `file_operations.load_html_form_in_edge_browser` | + `filesystem_write` | The form writes its results to the caller-named output data-share file. |
| `file_operations.make_embedded_file_name_list` | − `filesystem_read` | `file_name_pattern` filters files embedded in the SA job; no filesystem path is read. |

## Reviewed counts

| | 2026.1.0529.7 | 2024.1.0508.5 |
|---|---:|---:|
| Operations | 1,027 | 996 |
| `exclusive_workflow` | 43 | 46 |
| `interactive_ui` | 47 | 47 |
| `read-only` / `standard` / `device` / `full` | 185 / 831 / 927 / 937 | 181 / 805 / 893 / 903 |

## Regenerate the seed

Run from the repository root with the .NET SDK from `global.json` and Python 3.12:

```powershell
foreach ($target in '2026.1.0529.7', '2024.1.0508.5') {
    dotnet build "targets/$target/src/Briosa.Server/Briosa.Server.csproj" -c Release
    ./eng/classification/Export-OperationDescriptors.ps1 -TargetDirectory "targets/$target" -OutputDirectory artifacts/classification
}
python eng/classification/classify.py --descriptors-dir artifacts/classification --output-dir artifacts/classification
```

Then compare the output with the committed CSVs and the tables. Once a table
has been edited, the CSV no longer has to match it. Regenerating is a review
aid for an SA-version migration or a broad reclassification, not a build step.
Do not generate table rows or operations from it.
