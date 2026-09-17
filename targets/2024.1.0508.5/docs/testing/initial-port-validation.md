# Initial SA 2024 port validation

Local Windows x64 validation on September 17, 2026, for issue
[#186](https://github.com/spatialanalyzer/briosa/issues/186).

The implementation starts from Briosa commit
`7a04881063f7efb3dccd3fdc1cc0417929b52739` and is reviewed as the change in this
issue's pull request. The `0.1.0-sa2024-review` artifacts were local test builds,
not published releases. Their base-commit provenance is not a release attestation
for the uncommitted port; release CI must rebuild from the reviewed commit.

## Portable results

| Check | Result |
| --- | --- |
| Release protocol, server, and worker tests | Passed: 25 protocol, 237 server, 177 worker cases in the final full-suite run. |
| Existing 2026 target regression | Passed: 400 portable cases. |
| Debug source-host composition | Passed with SDK activation disabled. |
| Development reflection | Passed: three Debug cases. |
| Buf formatting, lint, descriptor build | Passed. |
| Interop verification | Passed: assembly identity, canonical API, hashes, and approved tracked-binary boundary. |
| Staged provenance | Brand and interop blobs match the recorded fingerprints; original license bytes are preserved. |
| Windows package | Two builds matched; checksums, offline diagnostics, loopback launch, unsafe-bind rejection, and Control Center ownership/lifecycle passed with fake workers. |
| Protocol artifact | Two builds matched; target identity, descriptors, manifest, and checksums passed. |
| Generated-client package smoke | All nine readiness, policy, result-failure, deadline, cancellation, recovery, and unsupported-service scenarios passed. |
| Client conformance artifact | Two builds matched; all 12 lifecycle, compatibility, failure, and cleanup scenarios passed. |
| Runtime and logging measurements | Completed using fake operations; all four logging modes passed. No SA latency claim. |
| Repository policy | Ordinary CI, both protected licensed workflows, target isolation, and release catalog checks passed. |

The default allowlist is checked against the compiled operation registry. This
caught and corrected a copied 2026-only policy entry before handoff. No exported
command inventory is used as a build completeness gate.

## Installer consumption

The current local Briosa Installer CLI consumed the real 2024 review package and
an existing 2026 Control Center review package from a signed local test feed.
Both installed and verified in one disposable workspace store. Removing the
2024 product preserved the 2026 product and its integrity; both test products
were then removed. The signing key was disposable and deleted after signing.
Normal installer settings, installed products, and SDK registration were not
changed. This checks package compatibility, not enterprise Artifactory behavior.

## Not executed

No live SpatialAnalyzer process was controlled, no SDK COM instance was activated,
and no licensed integration or physical-instrument scenario was run. The
protected 2024 runner and environment still need provisioning. All 996 operations
retain an unvalidated 2024 runtime status; the three new crib-sheet/projection
operations have additional hardware-fixture gaps. Enterprise Artifactory
verification also remains outstanding before the currently planned v1.0 gate.
