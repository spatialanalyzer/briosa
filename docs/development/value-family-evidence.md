# Exact-target value-family evidence

The `values` tree is the reviewed, machine-readable source of truth for semantic value families in one exact SpatialAnalyzer release. It records provenance separately from implementation and command support. A value being present in this catalog does not approve an MP command for Briosa's public API.

For SA `2026.1.0529.7`, `values/sa/2026.1.0529.7/catalog.json` traces:

- all 115 binding-registry families;
- all 42 public enum types, 470 non-sentinel members, worker symbols, and exact SDK literals;
- all 35 structured public/worker types and their 108 public and 108 worker fields;
- all 995 exact inventory observations that use one of six multi-domain SDK methods.

Assignments are keyed by SDK method, exact inventory key, and observed SDK execution order. Documentation ordinals are retained as a list because one SDK occurrence can reconcile duplicate or missing documentation rows. Do not use a documentation ordinal as the stable SDK identity.

## Evidence and conflict policy

Exact SA `2026.1.0529.7` evidence wins. Installed MP documentation, View SDK Code observations, and the committed exact-target interop API establish release-specific facts. ObjectiveSA is pinned secondary evidence from an older SA release; it may corroborate behavior, but it cannot add a choice or default that the exact target does not support. Conflicts remain inactive and require review.

The pinned ObjectiveSA baseline is:

- repository: `https://github.com/spatialanalyzer/ObjectiveSA`;
- commit: `324c73b8e172868b4ccb4a0121e3bd1cbc520c5c`;
- declared version: `2024.1.5.1`;
- layout: colocated interfaces and implementations under `ObjectiveSA/Methods`;
- 133 C# source files with aggregate manifest SHA-256 `d6107f1e10d2c957198c3cb082368033117e7e2ed2907eafb9eadc40607d295b`.

The current importer also accepts the legacy `ObjectiveSA/Interfaces/Methods` split when it exists. The current colocated layout is the reviewed baseline.

`InstrumentType` comes from the installed target's `Instrument Models/Instrument.lst`. The reviewed input SHA-256 is `0e0e31124355c5b3ec02f8510e2de1d22fd993471024d6210178114264b490f7`. The parser finds 195 records, excludes the five category-10 stand/mount graphics, and preserves the exact source order and text of 190 model names. Their canonical newline-delimited SHA-256 is `826471c04a0ae46f422e42486ce857d2725ad060f01eb5aed410c0971666d569`.

Raw vendor documentation, View SDK Code, `Instrument.lst`, and ObjectiveSA source are not copied into this repository. The catalog retains only reviewed facts, source coordinates, fingerprints, and small evidence references required for drift detection.

## Shared-method review

The following method names do not determine one semantic domain:

- `SetAsciiFileFormatArg` carries ASCII import and frame-set formats;
- `SetAxisNameArg` carries signed axes and WCF axes;
- `GetCollectionObjectNameArg` and `SetCollectionObjectNameArg2` carry collection objects or broader collection items;
- `GetCollectionObjectNameRefListArg` and `SetCollectionObjectNameRefListArg` carry the corresponding object or item lists.

Every exact inventory observation for these methods must have one explicit assignment in the evidence catalog. Generation fails when an observation is missing, duplicated, stale, or assigned outside the method's reviewed domains. Reviewers must inspect the command argument's documented type, MP name, and exact SDK evidence; they must not infer a family from the method name.

## Regeneration and review

On a machine containing the pinned ObjectiveSA checkout and licensed SA installation, rebuild the candidate catalog:

```powershell
./eng/New-ValueFamilyEvidence.ps1 `
  -ObjectiveSARoot C:\git\objectivesa `
  -InstrumentListPath "C:\Program Files (x86)\New River Kinematics\SpatialAnalyzer 2026.1.0529.7\Instrument Models\Instrument.lst"
```

Review every catalog diff. In particular, confirm that new or changed shared-method assignments reflect exact command semantics. Then synchronize the binding review, issue #82 default-review queue, manifest, and generated report:

```powershell
./eng/Sync-ValueFamilyEvidence.ps1
dotnet run --project tools/Briosa.Generator -c Release -- `
  binding-registry-sync `
  inventory/sa/2026.1.0529.7/inventory.json `
  disposition/sa/2026.1.0529.7 `
  interop/SpatialAnalyzer/2026.1.0529.7 `
  bindings/sa/2026.1.0529.7
```

Ordinary CI uses only committed inputs:

```powershell
./eng/Verify-ValueFamilyEvidence.ps1
./eng/Verify-BindingRegistry.ps1
```

The first command validates JSON schemas, fingerprints, code/catalog completeness in both directions, exact shared assignments, a byte-identical two-run generation, and committed-artifact freshness. Maintainers with the local evidence inputs can additionally reproduce the bootstrap catalog:

```powershell
./eng/Verify-ValueFamilyEvidence.ps1 `
  -ObjectiveSARoot C:\git\objectivesa `
  -InstrumentListPath "C:\Program Files (x86)\New River Kinematics\SpatialAnalyzer 2026.1.0529.7\Instrument Models\Instrument.lst"
```

Changing a source fingerprint, enum literal, structured field, family mapping, or exact shared-method observation requires regenerating and reviewing the affected artifacts. Never hand-edit generated files below `generated/values` or `docs/reference/generated`.

## Adding another exact SA release

Treat each release as an independent evidence target; a matching CLR signature or MP label is not compatibility evidence. For a new release:

1. Create the release-owned protocol value files, inventory, interop metadata, disposition ledger, binding review, and `values/sa/<release>/catalog.json` path.
2. Pin and fingerprint the installed documentation, complete View SDK Code export, exact interop API, `Instrument.lst`, and any ObjectiveSA revision used as secondary evidence. Record new reviewed Instrument selection counts rather than carrying the current release's counts forward.
3. Extract a candidate catalog with `New-ValueFamilyEvidence.ps1 -SpatialAnalyzerTarget <release>`, updating its reviewed release-specific bootstrap constants when the new evidence differs.
4. Review every enum literal, structured field, family, and shared-method assignment against that release. Copying the previous target is only a review starting point and cannot establish correctness.
5. Synchronize the target's binding review and generated value artifacts, add the target to CI discovery where required, and run both evidence and binding-registry verifiers.
6. Keep unresolved source conflicts and unknown assignments blocked. Escalate redistribution questions instead of committing raw vendor inputs.

## Reproducing the issue #82 queue

The ObjectiveSA importer supports the current source layout and no longer requires the raw View SDK Code directory when the exact-target candidates already committed to the disposition ledger are being recalculated:

```powershell
./eng/Review-CommandDefaults.ps1 `
  -ObjectiveSARoot C:\git\objectivesa `
  -InventoryPath inventory\sa\2026.1.0529.7\inventory.json `
  -DispositionDirectory disposition\sa\2026.1.0529.7 `
  -Apply
dotnet run --project tools/Briosa.Generator -c Release -- `
  disposition-sync `
  inventory/sa/2026.1.0529.7/inventory.json `
  disposition/sa/2026.1.0529.7
./eng/Sync-ValueFamilyEvidence.ps1
```

The reviewed baseline produces 1,187 ObjectiveSA mappings, 719 exact-target setter samples, 421 corroborated defaults, 314 entries requiring issue #82 review, and 1,271 inputs with no candidate. The committed queue is `generated/values/sa/2026.1.0529.7/default-review-queue.json`; candidates remain inactive until maintainers review them command by command.
