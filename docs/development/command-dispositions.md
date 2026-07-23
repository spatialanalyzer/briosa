# Command disposition review

The `disposition` tree is the complete, exact-target review ledger between the extracted SpatialAnalyzer MP command inventory and Briosa's supported command catalog. It answers why every inventoried command is a candidate, excluded, unavailable through the SDK, or still blocked without treating inventory presence as public API approval.

The initial target is SpatialAnalyzer `2026.1.0529.7`. Its ledger covers all 1,412 inventory keys. A new command fails closed: synchronization creates it as `blocked` and `unreviewed`, and runtime capability discovery remains limited to operations separately promoted into the reviewed catalog.

## Files and ownership

Each exact target contains:

- `manifest.json`, which pins the inventory path, inventory SHA-256, command count, and every category shard;
- `categories/*.json`, which groups decisions by the inventory's top-level category;
- `report.md`, a deterministic summary containing every disposition and review-state count.

When intentional exclusions exist, `report.md` also publishes an exact command-level table with each exclusion's Briosa-authored rationale and decision reference.

The generator owns inventory identity, MP step, category path, evidence references, per-command evidence fingerprint, manifest hashes, shard placement, and the report. Reviewers own disposition, review state, rationale, reason codes, decision references, blocker references, risk assessment, data classifications, value families, and delivery wave. Do not edit `manifest.json` or `report.md` by hand.

Reviewers may update decision fields in category shards and then run synchronization. Synchronization preserves reviewed decision fields when the command evidence is unchanged. If the per-command inventory fingerprint changes, it sets the entry to `needs_re_review`, adds `evidence_changed`, and restores the review blocker. This is intentionally command-scoped; unrelated inventory changes do not invalidate already reviewed decisions.

## Dispositions

| Disposition | Meaning |
| --- | --- |
| `approved_candidate` | Eligible to be promoted into a delivery wave and the supported catalog after review. |
| `intentional_exclusion` | Deliberately outside Briosa's public gRPC purpose or policy. |
| `sdk_unavailable` | The exact-target evidence shows no viable SDK execution binding. |
| `blocked` | A named dependency prevents a final decision or implementation. |

Review state is independent:

- `unreviewed` is the fail-closed initial state.
- `reviewed` means a maintainer-reviewed pull request accepted the exact-target decision and its evidence.
- `needs_re_review` means the decision's source evidence changed after review.

A maintainer-reviewed pull request is sufficient decision approval. Escalate to Hexagon only when the available exact-target evidence cannot establish a necessary fact. The complete-support milestone cannot close while an apparently SDK-callable, in-scope command remains `blocked`.

| Current disposition | Promotion or reopening rule |
| --- | --- |
| `approved_candidate` | Promote only after `reviewed`, assessed risk and value families, a delivery wave, and no blockers. Reopen when evidence changes, implementation proves the binding unsuitable, or the public policy changes. |
| `intentional_exclusion` | Remains absent from the catalog. Reopen through a reviewed PR when Briosa's scope or command-risk policy changes. |
| `sdk_unavailable` | Remains absent from the catalog. Reopen when new exact-target SDK evidence or vendor clarification identifies a viable binding. |
| `blocked` | Resolve or replace every named blocker, then use a reviewed PR to choose one of the other dispositions or retain a documented external block. Evidence changes also reopen the entry. |

## Required decision metadata

Use concise Briosa-authored rationale; do not copy vendor documentation prose, SDK source, sample default values, geometry, paths, or other proprietary content into the ledger.

Reason codes and value families are stable lowercase identifiers such as `read_only_operation`, `office_integration`, `path`, or `geometry`. Keep lists ordinally sorted and prefer an existing code when it has the same meaning. Risk effects, risk flags, and data classifications reuse the supported-catalog vocabulary. An empty `risk_flags` array means the review found no special command-level risk flag; `unknown` remains an explicit unresolved state. Decision and blocker references must be canonical `https://github.com/spatialanalyzer/.../issues/<number>` or pull-request URLs. Do not record reviewer names, timestamps, workstation paths, or local source locations.

The semantic rules are:

- every entry needs at least one reason code and non-empty rationale;
- a `reviewed` entry needs a decision reference;
- a reviewed `approved_candidate` needs one delivery wave and no blockers;
- a reviewed `approved_candidate` must replace `unknown` risk, data-classification, and value-family metadata with an assessment;
- a reviewed `blocked` entry needs at least one blocker and no delivery wave;
- reviewed `intentional_exclusion` and `sdk_unavailable` entries have neither blockers nor a delivery wave;
- `unreviewed` and `needs_re_review` entries remain blocked from promotion;
- `needs_re_review` retains the prior decision context but requires `evidence_changed` and a blocker.

Delivery waves are `wave_1` through `wave_4`, followed by `final`. A wave is scheduling metadata, not public support by itself. Only a reviewed `approved_candidate` may be promoted into the supported command catalog, whose separate validation and generation process defines the runtime surface.

## Workflow

After extracting or updating an exact-target inventory, synchronize its ledger:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- `
  disposition-sync `
  inventory/sa/2026.1.0529.7/inventory.json `
  disposition/sa/2026.1.0529.7
```

Review category shards in a focused pull request, update only reviewer-owned fields, and synchronize again so hashes and the report reflect the decisions. Then run:

```powershell
./eng/Verify-Disposition.ps1
```

The verifier applies both JSON Schemas, checks exact inventory coverage and hashes, enforces state combinations and reference formats, regenerates a temporary copy, and fails on stale or nondeterministic output. It requires neither SpatialAnalyzer, an SA license, proprietary binaries, nor the local documentation and SDK-code evidence corpus.

When a new exact SpatialAnalyzer release is added, create its inventory first and run `disposition-sync` against a new exact-target directory. Never reuse a prior release's decisions without review: MP argument shapes and meanings may change even when names and primitive types appear unchanged.

The first domain-scale review and its fail-closed decision rules are summarized in [SA 2026.1.0529.7 geometry command review](geometry-command-review.md).
