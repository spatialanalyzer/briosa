# Compatibility evidence

The behavioral declaration is target-owned in `compatibility.json`. The
architecture contract defines admission; `matrix.json` records tested pairs and
does not turn untested future releases into tested releases.

`declaredCompatibility` records the exact client releases, contract-major/revision
range, exact targets, architecture, default server-version limits, prerelease
policy, exclusions, and legacy exceptions. Empty exclusions mean no additional
known build exclusions are declared; applications can still narrow selection.
Only `testedPairs` entries carry execution evidence for exact package combinations.
Published pairs must match a retained registry artifact by URL, SHA-256, source,
target, language, and version. Local package tests remain explicitly unpublished.

`retained-clients.json` is the release gate's immutable package inventory. Add
each published contract-aware client version for both targets, including the
public registry URL, file name, SHA-256, and exact client source revision that
owns its public-API conformance fixture. Retain every such version until an
explicit support policy replaces this rule. Never substitute a source project
for a retained package.

The first contract-aware server, **0.7.0 only**, must precede client publication
because clients consume its immutable protocol artifact. Its empty-inventory
bootstrap is explicit in CI evidence. No later release can sign with an empty
inventory. All twelve published 0.2.0 and 0.3.0 products are retained with their
actual registry artifacts. Each passed all 12 scenarios against released Server
0.6.1, 0.7.0, and 0.8.0 conformance bundles: 36 released-package pairs and 432
scenario runs. The 0.8.0 release gate supplied the six retained-client reports;
their server artifact hashes match the published conformance ZIPs. The matrix
also preserves 24 development-candidate pairs with their original identities.
No licensed-SA coverage is implied.

The earlier 0.2.0 NuGet payloads were compared with their successful release
builds while allowing the registry signature; npm and PyPI artifacts matched
those release builds byte for byte. The 0.3.0 downloads likewise match their successful publishing artifacts
(byte-identical npm/wheels; NuGet archive payloads identical except for the
registry signature). The checks install these actual registry downloads into
isolated consumers. See the [package comparison](evidence/2026-09-24/client-0.3.0-registry-comparison.json).

The reusable release gate installs the verified NuGet, npm, or wheel artifact in
an isolated consumer and exercises the packaged server with a fake SDK. The
client-owned fixture reports identities, artifact hashes, exact source
revisions, scenario IDs, and whether its source was clean. Evidence is retained
as a CI artifact. These checks never activate SpatialAnalyzer or its real SDK;
licensed coverage is recorded separately.

Record example data (replace every placeholder with release evidence):

```json
{
  "language": "dotnet",
  "repository": "spatialanalyzer/briosa-dotnet",
  "target": "2026.1.0529.7",
  "version": "0.2.0",
  "sourceRevision": "<40 lowercase hex characters>",
  "url": "https://api.nuget.org/v3-flatcontainer/<package>/<version>/<package>.<version>.nupkg",
  "fileName": "<package>.<version>.nupkg",
  "sha256": "<64 lowercase hex characters>"
}
```
