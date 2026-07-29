# Full-surface generation and CI gates

Issues [#62](https://github.com/spatialanalyzer/briosa/issues/62) and [#68](https://github.com/spatialanalyzer/briosa/issues/68) make the complete reviewed surface and its generated portable conformance contract reproducible and measurable on an ordinary Windows runner. The gate does not install, launch, or connect to SpatialAnalyzer and does not require an SA license or proprietary source evidence.

## Repository policy

[`eng/full-surface-policy.json`](../../eng/full-surface-policy.json) is the reviewed, schema-validated source of truth for:

- exact SpatialAnalyzer targets included in complete-surface generation;
- generation-surface order and evidence paths;
- mappings from clean generated output to committed artifacts;
- explicit released protocol baselines;
- deterministic sharding; and
- CI duration and size budgets.

The policy schema is [`eng/schemas/full-surface-policy.schema.json`](../../eng/schemas/full-surface-policy.schema.json). Do not put runner-specific paths, discovered processor counts, wall-clock timestamps, or default-branch names in the policy.

## Two-clean-generation contract

Run the complete gate after building `Briosa.Generator`:

```powershell
./eng/Verify-FullSurface.ps1 -NoBuild
```

Without `-NoBuild`, the verifier builds the generator first. For every configured exact target it generates these surfaces twice in new temporary roots:

1. the complete disposition ledger;
2. the value-family binding review, queue, manifest, and reference report;
3. the SDK binding registry and report;
4. the incomplete catalog review-scaffold tree; and
5. every path emitted by catalog generation; and
6. the evidence-derived portable-conformance manifest.

The verifier discovers catalog artifact paths recursively from the clean generator output. It does not assume one protocol file, one service partition, or fixed generated filenames. The clean catalog context mirrors the repository's complete `proto` tree beside the isolated `catalog` input so generator validation can distinguish fixed protocol definitions from catalog-owned partitions without filename rules. The conformance context also uses the freshly synchronized binding review and registry, so upstream evidence drift cannot be hidden by a stale committed intermediate. It compares ordinal path lists, file lengths, and SHA-256 values across both runs. It then checks every committed mapping, runs the existing schema and semantic validators, and writes `artifacts/full-surface/manifest.json`. The manifest contains no timestamp or machine path; each unit records its evidence paths and fingerprints plus every affected generated file and fingerprint.

An error names the exact target, logical surface, changed member or file, source evidence fingerprint, and affected generated surface. The value-family verifier additionally reports exact enum symbol/number drift, worker enum members, SDK literals, structured public and worker fields, and command-assignment keys of the form `method|inventory_key|sdk_order`.

The combined gate retains the existing fail-closed checks:

- an inventory command without one reviewed disposition fails;
- stale disposition evidence, manifests, shards, or reports fail;
- a missing or uncovered SDK binding fails;
- an enum member, SDK literal, structured field, or exact command-family assignment missing from either evidence or implementation fails;
- a supported operation without an exact portable scenario set, executable generated binding, or reviewed shared-method assignment fails;
- missing, stale, extra, or nondeterministic generated artifacts fail; and
- protocol formatting, lint, compilation, and applicable released-baseline breaking checks fail.

Raw installed documentation, View SDK Code, ObjectiveSA source, and `Instrument.lst` remain outside this workflow. Their reviewed aggregate fingerprints in the committed inventory and value-family catalog are the portable evidence boundary.

## Released protocol baselines

Briosa has no public release at present, so `released_protocol_baselines` is deliberately empty. Unreleased `main` is mutable and is never an inferred compatibility baseline.

Immediately after publishing the first release, add an entry in a dedicated reviewed pull request:

```json
{
  "ref": "refs/tags/v0.1.0",
  "commit": "<the tag's 40-character commit SHA>",
  "packages": [
    "briosa.core.v1alpha1",
    "briosa.sa.v2026_1_0529_7.v1alpha1"
  ]
}
```

Only an immutable `refs/tags/v...` ref is accepted, and it must resolve to the pinned commit. CI runs Buf's strict `FILE` comparison against every listed baseline. Do not replace a baseline with a newer tag merely to make a breaking check pass. A necessary published semantic break requires a new protocol package line while preserving the released files; changing or removing the baseline requires a separately recorded governance decision.

## CI budgets

`Measure-CiBudget.ps1` writes one small JSON report per metric and fails after recording a value above the configured maximum. The comparison uses the raw measurement; rounding to three decimal places is display/report behavior only. `Test-CiBudgetPolicy.ps1` protects the exact-limit and just-over-limit boundary. Ordinary CI uploads the reports even when a later step fails.

Ordinary CI has one validation path for each proposed change. The `pull_request` trigger covers draft and ready pull requests targeting `main`; the `push` trigger is restricted to `main` for post-merge validation. The workflow does not run a second branch-push copy of the same jobs for an open pull request. Concurrency is grouped by pull-request number or branch ref, and a newer commit cancels an obsolete in-progress run in the same group. `Verify-CiWorkflow.ps1` fails if this trigger, cancellation, or read-only permission policy drifts.

| Metric | Unit | Maximum | Measurement boundary |
| --- | --- | ---: | --- |
| Restore | seconds | 300 | locked solution restore |
| Generation | seconds | 360 | complete two-clean-generation and semantic gate |
| Compile | seconds | 300 | Release solution build without restore |
| Test | seconds | 600 | Release solution tests without build or restore |
| Package | seconds | 900 | two deterministic Windows package builds and package verification |
| Startup | seconds | 30 | packaged host process start through accepting a loopback connection |
| Descriptor size | bytes | 4,194,304 | verified protocol artifact descriptor set |
| Package size | bytes | 268,435,456 | verified deterministic Windows package ZIP |
| Startup working set | bytes | 536,870,912 | packaged host immediately after accepting a loopback connection without a worker or SA |
| Dispatch p95 | milliseconds | 250 | 512 sequential named-pipe fake-worker calls after 64 warmups |
| Request-mapping p95 | milliseconds | 50 | 512 generated request/outcome/response mappings after 64 warmups |
| Discovery p95 | milliseconds | 50 | 512 capability responses over all currently allowed generated operations after 64 warmups |
| Retained managed memory | bytes | 33,554,432 | non-negative managed-heap increase across the 512-call fake-worker sample |

Budget changes require a pull request that includes the uploaded metric reports from at least three representative successful runs, explains the surface growth or runner change, and selects the smallest practical threshold with explicit headroom. A transient slow runner should be rerun; it is not sufficient evidence to raise a budget. Lowering a budget after an optimization follows the same review path. Never add an inline bypass or per-branch exception.

For a local duration measurement, pass an executable and its argument array:

```powershell
./eng/Measure-CiBudget.ps1 `
  -Metric compile `
  -Executable dotnet `
  -ArgumentList @("build", "Briosa.slnx", "-c", "Release", "--no-restore")
```

Startup, startup-working-set, and package-size measurements are recorded inside `Test-WindowsPackage.ps1`; descriptor size is recorded by `Test-ProtocolArtifact.ps1`. `Test-RuntimePerformance.ps1` records fake-worker dispatch, request-mapping, discovery p95, and retained managed memory. See the [runtime performance and soak guide](../testing/runtime-performance-and-soak.md) for the sample and state boundaries; none of these portable gates starts or connects to SpatialAnalyzer.

## Deterministic sharding

Generation units are ordered by the policy's target order and then its surface order. The repository reserves `target-then-surface-ordinal-modulo` as the deterministic ownership rule, but the policy schema and verifier currently require `shard_count: 1` and shard index zero. Multi-shard execution is deferred until a checked-in CI matrix can prove every configured shard ran; changing only the JSON policy fails closed. A future sharding change must add that matrix and coverage check in the same reviewed pull request and must never derive shard count or ownership from runner capacity, file count, timings, or unordered filesystem enumeration.

When adding an exact target, add all evidence and catalog inputs first, then add the target to the policy in its reviewed order. The full-surface manifest makes the new unit and file counts visible in the pull request.
