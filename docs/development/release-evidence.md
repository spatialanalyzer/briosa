# Generated support and release evidence

Issue [#72](https://github.com/spatialanalyzer/briosa/issues/72) owns the exact-target support matrix and release audit. The committed outputs reconcile Briosa's complete command disposition ledger with the smaller supported catalog and portable conformance manifest. They do not install, launch, or connect to SpatialAnalyzer.

## Evidence boundary

`release/sa/<target>/audit-policy.json` is the reviewed source for evidence that cannot be derived from the catalog. Its versioned schema currently permits only the truthful `pending` state for protected SpatialAnalyzer conformance and runtime-identity validation. A later issue must define and validate the protected evidence contract before either state can become passing; changing a word in the policy cannot assert completion.

`release-evidence-generate` joins each catalog operation to exactly one reviewed `approved_candidate` disposition using `inventory_key`. MP identity and risk effect/flags must agree, and the portable conformance operation set must equal the catalog operation set. Every one of the 1,412 disposition entries then receives one classification:

- `cataloged_portable_only`: generated into the public server surface and covered by Briosa's vendor-independent portable contract, but not represented as protected licensed-SA validation;
- `approved_not_cataloged`: a reviewed candidate that is not part of the supported public surface;
- `blocked`, `intentional_exclusion`, or `sdk_unavailable`: the corresponding final disposition.

The generator enumerates every catalog manifest and its `operation_files`; it has no fixed operation list. Later Wave 1 or Wave 2 catalog additions therefore change the matrix automatically. Adding a mutating or device-control operation also changes the issue #47 risk-fixture criterion from `not_applicable` to `blocked` until the protected evidence owner supplies a reviewed contract.

## Operator interpretation

Catalog membership describes what the exact-target binary can express. Runtime capability discovery remains the intersection of that catalog with the deployment allowlist after deny rules and isolation checks. A cataloged operation requires:

- a separately installed and licensed SpatialAnalyzer matching the exact package target;
- exact-match activated-SDK and connected-SA identity evidence;
- an execution-channel readiness proof for the current worker generation;
- explicit runtime allowlisting; and
- single-tenant coordination for application-global state.

Risk flags add corresponding `risk_policy_*` prerequisites. Cancellation never proves cancellation or rollback of a synchronous COM call, and worker recovery never authorizes replay of an ambiguously completed operation. Returned paths, geometry, identifiers, measurements, credentials, license data, and proprietary values retain their reviewed data-handling requirements.

## Regenerate and verify

Regenerate all exact targets after changing a disposition, catalog operation, portable conformance manifest, audit policy, operator guide, CI policy, or release workflow:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- `
  release-evidence-generate . .
```

Generated JSON and Markdown files are committed below `generated/release/sa/<target>` and `docs/reference/generated/sa/<target>`. Do not edit them by hand.

The focused verifier schema-checks the source and outputs, reconciles counts and identities, validates every evidence fingerprint, generates twice, and compares both generations with the committed bytes:

```powershell
./eng/Verify-ReleaseEvidence.ps1
```

`Verify-FullSurface.ps1` includes the same surface in ordinary CI. A truthful audit may be current while `release_ready` is false. `Assert-ReleaseReady.ps1`, used only by the release workflow, first performs the complete verification and then fails publication if any audit criterion is blocked. This separation lets pull requests preserve an explicit blocker without allowing a tag or manual release to bypass it.

An empty `released_protocol_baselines` list is explicitly `not_applicable` before the first public release; mutable `main` is never treated as a baseline. After that release, immutable tag and commit entries remain governed by the [full-surface gate](full-surface-gates.md#released-protocol-baselines).
