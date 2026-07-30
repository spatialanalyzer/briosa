# Engineering scripts

Run the scripts in this directory from the repository root. Most scripts require PowerShell 7 and the repository's documented .NET SDK. Protocol schema and artifact work also requires Buf 1.72.0. Interop generation requires Visual Studio Developer PowerShell. The licensed-SA scripts are the only scripts in this directory that may connect to SpatialAnalyzer; follow their explicit opt-in guidance.

## Local SpatialAnalyzer source-host verification

`Test-LocalSpatialAnalyzerHost.ps1` verifies the conventional Debug source-host composition without activating the SDK or connecting to SpatialAnalyzer. It enforces the first/default `SpatialAnalyzer` launch profile, builds `Briosa.Server` in Debug, compares every file in the real worker output cohort with its colocated server-output copy, and runs the production worker control lifecycle with SDK activation explicitly disabled. The lifecycle check also proves bounded graceful worker cleanup.

```powershell
./eng/Test-LocalSpatialAnalyzerHost.ps1
```

Pass `-NoRestore` only after a locked restore. Ordinary CI runs this check separately from Release build and package verification. `Test-WindowsPackage.ps1` continues to build the Release server and worker independently and rejects the Debug launch profile, Development settings, and Debug user-secrets identity in the archive. Neither check supplies SpatialAnalyzer identity evidence or produces licensed-SA validation.

For the developer success path that starts the real source server and uses standard reflection-aware clients against an already-running licensed exact-target SpatialAnalyzer instance, follow the [local gRPC server guide](../docs/development/local-grpc-server.md). That manual workflow is distinct from this portable script and from protected licensed-SA release validation.

## Complete-surface and CI-budget verification

`Verify-FullSurface.ps1` is the ordinary-CI umbrella for disposition, value-family, binding-registry, scaffold, catalog-artifact, portable-conformance, release-evidence, and interop validation. It generates every configured surface twice in clean temporary roots, discovers all emitted paths, compares bytes and committed freshness, runs the existing semantic validators, and writes a fingerprinted manifest under `artifacts/full-surface`:

```powershell
./eng/Verify-FullSurface.ps1
```

The schema-validated `full-surface-policy.json` owns exact targets, evidence paths, committed-output mappings, explicit released protocol baselines, deterministic sharding, and budgets. The released-baseline list is empty while Briosa is unreleased; `main` is never treated as published. Catalog output discovery is recursive and does not assume a single operation-protocol file or fixed partition names. Release evidence enumerates catalog manifests and operation files dynamically, so later delivery waves do not require a second support list.

## Release evidence verification

`Verify-ReleaseEvidence.ps1` schema-checks and regenerates the exact-target support matrix and release audit. It joins all disposition entries to the supported catalog and portable conformance manifest, verifies evidence hashes and one-to-one inventory accounting, generates twice, and rejects stale JSON or Markdown:

```powershell
./eng/Verify-ReleaseEvidence.ps1
```

The audit policy below `release/sa/<target>` remains fail-closed for protected conformance and runtime-identity evidence. Ordinary CI verifies that pending state without contacting SpatialAnalyzer. `Assert-ReleaseReady.ps1` repeats the verifier and then fails if any audit criterion is blocked; `.github/workflows/release.yml` runs it before uploading or publishing release assets. See [the release-evidence guide](../docs/development/release-evidence.md).

`Measure-CiBudget.ps1` measures a command duration or validates an observed size, duration, latency, or memory value; it writes a JSON metric and fails when the raw value exceeds the reviewed threshold, while only display values are rounded. `Test-CiBudgetPolicy.ps1` verifies exact and just-over boundaries. CI measures locked restore, full generation, compile, tests, Windows packaging, packaged-host startup, protocol descriptor size, Windows package size, startup working set, fake-worker dispatch, request-mapping and discovery p95, and retained managed memory, then uploads the reports. Multi-shard execution is fail-closed until CI has a checked matrix. See [the full-surface gate guide](../docs/development/full-surface-gates.md) before changing a baseline, budget, target, mapping, or sharding policy.

`Test-RuntimePerformance.ps1` runs 64 warmups plus 512 samples for named-pipe fake-worker dispatch, generated request mapping, and catalog discovery, and records retained-memory observations without activating SpatialAnalyzer:

```powershell
./eng/Test-RuntimePerformance.ps1 -NoBuild
```

It also verifies admitted/terminal accounting is drained. Queue saturation, admission cancellation, stop races, repeated watchdog and crash replacement, bounded lifecycle history, and sustained audit correlation are deterministic server tests. See [the runtime performance and soak guide](../docs/testing/runtime-performance-and-soak.md) for exact boundaries and the deferred licensed soak.

`Verify-CiWorkflow.ps1` protects the ordinary workflow trigger and concurrency policy. Feature branches are validated by one `pull_request` run when they target `main`; `push` validation is restricted to `main`, and a newer commit cancels an obsolete run for the same pull request or branch. This avoids running the same Windows jobs for both `push` and `pull_request` on every open branch while retaining post-merge validation of `main`.

## Protocol verification

`Verify-Protocol.ps1` requires Buf 1.72.0. It verifies canonical formatting, lint rules, and schema compilation:

```powershell
./eng/Verify-Protocol.ps1
```

Briosa is currently unreleased, so ordinary validation does not treat `main`
as a published compatibility baseline. After the first public release, record
its immutable tag and commit in `full-surface-policy.json`; the full-surface
gate runs the strict FILE-level comparison against every recorded release.
For a focused local comparison, run:

```powershell
./eng/Verify-Protocol.ps1 -AgainstRef <released-ref>
```

## Command catalog verification

`Verify-Catalog.ps1` applies the versioned JSON Schemas and semantic release rules to every exact-target supported-command catalog and release membership. It rejects unlisted files, target or naming drift, stale release coordinates, duplicate or unknown release members, category/service/file collisions, unresolved RPC or message identities, missing or duplicate field numbers, conflated inventory/MP/SDK identities, unresolved metadata, unsafe default inference, and missing SDK bindings:

```powershell
./eng/Verify-Catalog.ps1
```

Validation reserves every fixed filename and top-level symbol already declared in the exact-target `proto` package. The repository layout is therefore part of the validation input: a synthetic or copied `<workspace>/catalog` must be accompanied by the matching `<workspace>/proto` tree. Missing fixed-protocol context fails closed rather than allowing a category to claim `values.proto`, `specialized_values.proto`, or an existing package symbol.

Regenerate the complete catalog-derived protobuf, request/worker/result adapter, gRPC service/registration, capability, reference, and coverage surface with:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- catalog-generate catalog .
```

Generated artifacts are committed but must not be hand-edited. Each manifest `protocol_partitions` entry owns one stable exact-target category `.proto` file and generated service in the same package; one generated aggregate extension registers every category service. Listed `release_membership_files` must live under the target's `release-memberships` directory, match the target catalog coordinates, and contain sorted exact operation IDs. The generator embeds those memberships in coverage and generated reference documentation without treating them as runtime authorization. Argument `field_numbers` are explicit API identities; do not recalculate them from MP `ordinal` or `sdk_order`. Generated request validation uses only reviewed presence/default policy, and generated result mapping uses the exact reviewed semantic family. `CatalogOperationExecutor` remains the hand-written audit/outcome seam. The generator refuses to overwrite any existing destination that does not carry its catalog-artifact marker. Verify a clean generation and reject stale or extra generated files—including registrations or category files left after a partition change—with:

```powershell
./eng/Verify-CatalogArtifacts.ps1
```

Pass `-NoBuild` only after `Briosa.Generator` has already been built in the selected configuration.

## Portable conformance verification

Generate the exact-target portable scenario inventory from the supported catalog, binding registry/review, and value-family evidence with:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- `
  portable-conformance-generate . .
```

The committed `generated/conformance/sa/<target>/manifest.json` fingerprints every input and records stable positive and negative identities for each supported operation, usable method/family row, value family, enum member, structured shape, and exact multi-family assignment. Generated operation bindings let server tests execute request validation, immutable worker-command mapping, typed results, capability/policy behavior, readiness, uncertain completion, replay guidance, and fake worker failures without maintaining a second operation list.

Verify schema, evidence fingerprints, exact operation and case identities, two-run determinism, and committed freshness with:

```powershell
./eng/Verify-PortableConformance.ps1
```

This is an ordinary-runner Briosa contract and never activates SpatialAnalyzer. See [the portable conformance guide](../docs/testing/portable-conformance.md) for the executable test boundary and regeneration workflow.

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

After promoting an operation, add its exact ID to a reviewed release membership only when it belongs to that delivery subset. Keep membership IDs sorted and catalog/target/revision coordinates exact, then regenerate. The [release-membership guide](../docs/development/release-membership.md) explains why membership is additive and does not change runtime allow/deny policy.

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

Optional disposition `operation_contract` metadata is also semantic input. Its decision must match the disposition; constraint and evidence-limitation codes must be sorted; and validation status must truthfully distinguish documentation review from an authorized live probe. The generated catalog scaffolds preserve candidate contracts. Issue #80's final ledger records `performed` only for the six commands exercised by controlled exact-target probes and retains `not_performed` for the five exclusions that did not need or lacked a sanctioned fixture; see [the file-operation contract review](../docs/development/file-operation-contracts.md).

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
./eng/Test-GeneratedClientScenarios.ps1 `
  -PackagePath <path-to-briosa-zip> `
  -FixturePath conformance/v1/wave1-read-only-scenarios.json
./eng/Test-GeneratedClientScenarios.ps1 `
  -PackagePath <path-to-briosa-zip> `
  -FixturePath conformance/v1/wave2-point-lifecycle-scenarios.json
```

The Wave 1 matrix covers generated-client success, missing-required-input validation, deny-overrides-allow policy, and MP failure. The Wave 2 matrix covers the exact construct/rename/delete point lifecycle, authoritative `vector3` mapping, required-input rejection, reviewed-default omission, deny-overrides-allow, MP failure, and unknown mutation replay safety. Reports contain only identity, state, status, booleans, and failure classification; raw arguments and returned values are never printed.

The portable harness supplies explicitly labeled fake-worker attestations for both runtime identities. Production defaults remain fail-closed: the activated SDK and connected SA each require runtime-verified exact evidence or their own complete `Version`/`Reference` operator-attestation pair. Runtime evidence takes precedence and cannot be masked by attestation. Evidence references are kept out of discovery and default logs; see [ADR 0022](../docs/architecture/0022-runtime-identity-and-attestation.md).

`Test-LicensedSpatialAnalyzer.ps1` is an explicit opt-in check for one already-running, separately licensed SA 2026.1.0529.7 instance. It requires independent attested version and evidence-reference arguments for the activated SDK and connected application; these arguments never convert the configured package target into an observed fact.

`Test-LicensedRunnerState.ps1` verifies safe preflight or postflight process state without logging process IDs, paths, license data, or returned values:

```powershell
./eng/Test-LicensedRunnerState.ps1 -Phase Preflight
```

`Verify-LicensedRunnerWorkflow.ps1` is an ordinary-CI policy check that rejects untrusted triggers, missing identity-reference inputs, mutable action references, a licensed-runner checkout, runner-only contexts used before job steps begin, an unsafe or missing step-scoped run-directory initializer, or drift from the exact runner group and environment:

```powershell
./eng/Verify-LicensedRunnerWorkflow.ps1
```

See [the generated-client smoke guide](../docs/testing/generated-client-smoke.md) for package creation, prerequisites, safety boundaries, and scenario coverage.
See [the licensed runner operations guide](../docs/operations/licensed-sa-runner.md) before provisioning or operating the protected machine.
