# MP Argument Name Refactoring Validation

This records portable validation for [the naming migration](mp-argument-name-migration.md), tracked by [Task #217](https://github.com/spatialanalyzer/briosa/issues/217). It covers both supported exact targets, SA 2024.1.0508.5 and SA 2026.1.0529.7. These results do not claim licensed SpatialAnalyzer execution.

## Product Coverage

| Product | Change and Validation |
| --- | --- |
| Server, worker, protocol, and Control Center distribution | 168 fields in 83 commands for 2024; 172 fields in 86 commands for 2026. Both target solutions build and their server, worker, and protocol suites pass. Added tests preserve exact SDK labels and the zero sentinel, binary field tags, explicit presence, JSON names, and unrelated `timeout_seconds` fields. Host/worker execution, SDK registration, readiness, and Control Center behavior remain unchanged. |
| Protocol compatibility tooling | Buf format, lint, build, and WIRE checks pass. A descriptor baseline hash restricts the source/JSON naming exception to the reviewed ledger. After restoring only listed names in a temporary descriptor, full FILE compatibility passes. Tests reject six unapproved mutations per target: an unlisted rename, type, presence, tag, collision-method rename, and a rename against an already-migrated baseline. |
| .NET clients | Version 0.3.0 candidate for each target. Generated drift, formatting, 56/39 tests (2024/2026), actual NuGet packages, and isolated consumers pass. Named-argument and generated transport checks use `angleTolerance`. |
| JavaScript/TypeScript clients | Version 0.3.0 candidate for each target. Generated drift, lint, formatting, 69/63 tests, package metadata, installed JS consumers, and TypeScript compilation pass. |
| Python clients | Version 0.3.0 candidate for each target. Generated drift, Ruff, mypy, 52/35 tests, wheels, metadata, and isolated consumers pass. Keyword-argument checks use `angle_tolerance`. |
| Cross-generation compatibility | 24 additional packaged client/server pairs pass 12 scenarios each, using fake SDK workers. All six retained published 0.2.0 packages work with the candidate server. All six new 0.3.0 packages work with candidate 0.8.0-dev.1, released 0.7.0, and legacy 0.6.1 servers. New client fixtures also call a renamed-input MP operation. Old package fixtures remain at their retained source revisions. See the [evidence-backed matrix](../../compatibility/matrix.json). |
| Installer and launcher | No application or manifest-schema change is needed. Locked build, 177 core/CLI tests, and the windowless WPF harness pass. An unsigned self-contained validation distribution passes packaged CLI/launcher acquisition, repair, update-selection, settings preservation, and cleanup checks. Both real candidate server ZIPs pass disposable signed-catalog verification, schema-3 admission, side-by-side installation, receipt checks, verification, repair, and removal. No server or SDK is launched by these installer tests. |
| Documentation site | Current working references and migration guidance cover all renamed members and preserve qualifier meaning. Changed references identify unpublished candidates and exact source commits. Historical snapshots remain unchanged. The site build, search, route/navigation/SEO tests, and a temporary-snapshot rendering test cover both targets and all four API reference families. |
| Standalone examples | No affected identifiers occur in the existing examples. Their published 0.2.0 pins remain reproducible; all 47 existing fake-server scenarios pass. New installed-package consumer probes in client repositories exercise the clean names. |
| Evidence, brand, governance | No product changes are necessary. Exact-target SDK evidence, vendor MP labels, historical observations, artwork, licenses, and organization policy remain intact. |

## Candidate Identity and Release Follow-Through

Protocol and server candidates were produced from clean source revision `3306d43253a1e4e41b75b83360ad4f6f2b7f60b7`, with version `0.8.0-dev.1`. Client 0.3.0 package evidence records each clean client source revision and artifact digest. Candidate package evidence is explicitly unpublished; published 0.2.0 evidence is matched to the retained registry artifacts. The behavioral contract remains major 1, revision 0. Binary compatibility does not remove the documented source/JSON migration requirement.

This work prepares reviewed source and local candidate packages, not a public release. The coordinated release sequence is:

1. Merge the server change and produce the approved immutable server/protocol release through its normal signing and release gates.
2. Reimport that released protocol artifact into each client, replacing the explicit `source_commit_bootstrap` candidate pins. Recheck drift, packages, and retained-client compatibility before publishing the six client packages.
3. Update the release catalog through the normal signed publication workflow. The existing installer consumes it without an installer application upgrade.
4. Cut new documentation snapshots from the updated working references, with final package identities and release links. Keep old snapshots and published example pins until a separate coordinated dependency update.

No production signing credentials, public package registries, public catalog, real SDK registration, or licensed SA process were used or changed for this validation.
