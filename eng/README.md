# Engineering scripts

Run the scripts in this directory from the repository root. Most scripts require PowerShell 7 and the repository's documented .NET SDK. Protocol schema and artifact work also requires Buf 1.72.0. Interop generation requires Visual Studio Developer PowerShell. The licensed-SA scripts are the only scripts in this directory that may connect to SpatialAnalyzer; follow their explicit opt-in guidance.

## Protocol verification

`Verify-Protocol.ps1` requires Buf 1.72.0. It verifies canonical formatting, lint rules, and schema compilation:

```powershell
./eng/Verify-Protocol.ps1
```

Briosa is currently unreleased, so ordinary validation does not treat `main`
as a published compatibility baseline. After the first public release, run the
strict FILE-level comparison against its explicit Git ref:

```powershell
./eng/Verify-Protocol.ps1 -AgainstRef <released-ref>
```

## Command catalog verification

`Verify-Catalog.ps1` applies the versioned JSON Schemas and semantic release rules to every exact-target supported-command catalog. It rejects unlisted files, target or naming drift, category/service/file collisions, unresolved RPC or message identities, missing or duplicate field numbers, conflated inventory/MP/SDK identities, unresolved metadata, unsafe default inference, and missing SDK bindings:

```powershell
./eng/Verify-Catalog.ps1
```

Validation reserves every fixed filename and top-level symbol already declared in the exact-target `proto` package. The repository layout is therefore part of the validation input: a synthetic or copied `<workspace>/catalog` must be accompanied by the matching `<workspace>/proto` tree. Missing fixed-protocol context fails closed rather than allowing a category to claim `values.proto`, `specialized_values.proto`, or an existing package symbol.

Regenerate the catalog-derived protobuf contracts and worker bindings with:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- catalog-generate catalog .
```

Generated artifacts are committed but must not be hand-edited. Each manifest `protocol_partitions` entry owns one stable exact-target category `.proto` file and service in the same package. Argument `field_numbers` are explicit API identities; do not recalculate them from MP `ordinal` or `sdk_order`. The generator refuses to overwrite any existing destination that does not carry its catalog-artifact marker. Verify a clean generation and reject stale or extra generated files with:

```powershell
./eng/Verify-CatalogArtifacts.ps1
```

Pass `-NoBuild` only after `Briosa.Generator` has already been built in the selected configuration.

After a public release, pair catalog verification with `Verify-Protocol.ps1 -AgainstRef <released-ref>`. The repository's FILE-level Buf policy rejects moving a published service or message between category files as well as incompatible field changes. See [ADR 0021](../docs/architecture/0021-exact-target-protobuf-partitions-and-identifiers.md) before allocating a new category alias, RPC, request/result type, or field number.

Generate incomplete, evidence-traceable review scaffolds for every reviewed approved candidate that is not already in the supported catalog with `catalog-scaffold-generate`. Output must remain separate from `catalog`; use an ignored `artifacts` directory:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- `
  catalog-scaffold-generate `
  inventory/sa/2026.1.0529.7/inventory.json `
  disposition/sa/2026.1.0529.7 `
  values/sa/2026.1.0529.7/catalog.json `
  catalog `
  artifacts/catalog-scaffolds/2026.1.0529.7
```

`Verify-CatalogScaffolds.ps1` runs that workflow twice in clean temporary directories, validates both versioned scaffold schemas and every hash, and rejects nondeterminism or incomplete candidate accounting:

```powershell
./eng/Verify-CatalogScaffolds.ps1
```

Scaffolds deliberately retain null public-policy blockers and cannot generate a public operation. Existing changed or stale scaffolds produce conflicts instead of being overwritten. See [the catalog review-scaffold guide](../docs/development/catalog-review-scaffolds.md) before promoting a candidate.

## Command disposition verification

`Verify-Disposition.ps1` validates the complete exact-target command review ledger against its pinned inventory. It applies the versioned schemas, requires exact inventory-key coverage, enforces fail-closed review semantics, and rejects stale shards, hashes, or reports:

```powershell
./eng/Verify-Disposition.ps1
```

Synchronize a ledger after inventory or reviewed decision changes with:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- disposition-sync <inventory-path> <target-directory>
```

See [the command disposition review guide](../docs/development/command-dispositions.md) before editing category decisions.

## SDK binding registry verification

`Verify-BindingRegistry.ps1` reconciles the inventory-observed setter/getter names, command dispositions, committed exact-target interop signatures, semantic value families, and protocol/worker/adapter/fake/generator coverage. It rejects stale generated artifacts, unknown families, uncovered methods, and sample-only methods that are not blocked explicitly:

```powershell
./eng/Verify-BindingRegistry.ps1
```

Regenerate the exact-target registry and report after reviewing `bindings/sa/<target>/review.json`:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- binding-registry-sync <inventory-path> <disposition-directory> <interop-directory> <target-directory>
```

See [the SDK binding registry guide](../docs/development/sdk-binding-registry.md) before changing semantic-family mappings or adapter coverage.

The binding verifier is paired with `BindingFamilyAdapterCompletenessTests` in the ordinary worker test suite. Those evidence-driven tests require all five implemented-coverage sets to equal the usable registry, expand every method/family row, round-trip every implemented private value kind through `WorkerControlChannel`, and exercise the real adapter against a dispatch fake on the owned STA. Setter rejection, MP-result suppression, getter failure, exact method identity, `VariantWrapper` list marshalling, every reviewed enum literal, and shared collection object/item decoding are part of the gate. Run the focused contract with:

```powershell
dotnet test tests/Briosa.Worker.Tests/Briosa.Worker.Tests.csproj -c Release --filter FullyQualifiedName~BindingFamilyAdapterCompletenessTests
```

This is a vendor-independent ordinary-runner test. It never activates or connects to SpatialAnalyzer.

## Value-family evidence verification

`Verify-ValueFamilyEvidence.ps1` validates the exact-target evidence catalog, source fingerprints, public/worker enum and structured definitions, exact SDK literal mappings, and every multi-domain method assignment. It also synchronizes twice into clean temporary directories and rejects nondeterministic or stale generated artifacts:

```powershell
./eng/Verify-ValueFamilyEvidence.ps1
```

Regenerate the reviewed binding assignments, issue #82 default-review queue, manifest, and reference report with:

```powershell
./eng/Sync-ValueFamilyEvidence.ps1
```

`Review-CommandDefaults.ps1` recalculates default candidates from the pinned ObjectiveSA source and committed exact-target evidence. Issue #82 resolutions use `reviewed_no_default`: candidate evidence and reasons remain auditable while the input stays required and omission remains `reject_request`. The script accepts the reviewed proposal through `-DecisionProposalPath` for initial application and preserves an existing resolution only while its recalculated candidates match exactly. Run disposition synchronization before value-family synchronization after any accepted decision change.

On a maintainer machine containing the pinned ObjectiveSA checkout and installed exact-target `Instrument.lst`, use `New-ValueFamilyEvidence.ps1` to rebuild the candidate source catalog. Raw vendor evidence remains local. See [the value-family evidence guide](../docs/development/value-family-evidence.md) for the exact paths, fingerprints, conflict policy, and review workflow.

## Protocol artifact production and verification

`New-ProtocolArtifact.ps1` is the release-asset producer for the protobuf, descriptor, exact-target catalog identity, and shared client-conformance fixtures. A release build should supply the exact semantic version and source commit explicitly:

```powershell
$sourceRevision = (git rev-parse HEAD).Trim()
./eng/New-ProtocolArtifact.ps1 `
  -Version 0.2.0-test `
  -SourceRevision $sourceRevision `
  -OutputDirectory artifacts/protocol-test `
  -BufPath buf
```

`-SourceRevision` defaults to the current Git `HEAD` for local builds. `-OutputDirectory` defaults to `artifacts`, and `-BufPath` defaults to the `buf` executable on `PATH`. The producer writes three files whose base name also includes the exact SpatialAnalyzer target and catalog revision:

- `*.zip`, the runtime-neutral client generation bundle;
- `*.zip.sha256`, the checksum for the exact ZIP bytes; and
- `*.provenance.json`, a copy of the archive manifest for inspection before extraction.

`New-DeterministicZip.ps1` is an internal producer building block, not a separate release entrypoint. It writes canonical ZIP32 stored entries with ordinal paths, UTF-8 names, fixed headers and timestamps, and CRC-32 values. Do not replace it with `Compress-Archive` or `ZipArchive`: those APIs can choose different container bytes across PowerShell/.NET runtime implementations. The helper intentionally rejects ZIP64-sized inputs, an existing destination, and a destination inside the source tree.

Build and verify the complete artifact contract with:

```powershell
./eng/Test-ProtocolArtifact.ps1 `
  -Version 0.2.0-test `
  -OutputDirectory artifacts/protocol-test `
  -BufPath buf
```

The test performs two byte-reproducible builds; verifies stored entries, ordinal ordering, fixed timestamps, the external checksum, manifest, internal file checksums, provenance, descriptor reconstruction, and fixture coverage; and requires the same ZIP bytes from Windows PowerShell when it is available. Temporary verification files are removed, while the requested output directory retains the first verified ZIP and its two sidecars.

No protocol artifact command installs, launches, or connects to SpatialAnalyzer, and none requires an SA license or proprietary SDK binary. CI runs this verification in `.github/workflows/ci.yml`; `.github/workflows/release.yml` publishes the verified files together with the Windows package. See the [protocol artifact operator guide](../docs/operations/protocol-artifacts.md) and [ADR 0020](../docs/architecture/0020-protocol-artifacts-and-client-conformance.md) for contents, client consumption, version coordinates, and release policy.

## Generated-client smoke tests

Run portable packaged-host success and failure scenarios without SpatialAnalyzer:

```powershell
./eng/Test-GeneratedClientScenarios.ps1 -PackagePath <path-to-briosa-zip>
```

`Test-LicensedSpatialAnalyzer.ps1` is an explicit opt-in check for one already-running, separately licensed SA 2026.1.0529.7 instance.

`Test-LicensedRunnerState.ps1` verifies safe preflight or postflight process state without logging process IDs, paths, license data, or returned values:

```powershell
./eng/Test-LicensedRunnerState.ps1 -Phase Preflight
```

`Verify-LicensedRunnerWorkflow.ps1` is an ordinary-CI policy check that rejects untrusted triggers, mutable action references, a licensed-runner checkout, or drift from the exact runner group and environment:

```powershell
./eng/Verify-LicensedRunnerWorkflow.ps1
```

See [the generated-client smoke guide](../docs/testing/generated-client-smoke.md) for package creation, prerequisites, safety boundaries, and scenario coverage.
See [the licensed runner operations guide](../docs/operations/licensed-sa-runner.md) before provisioning or operating the protected machine.
