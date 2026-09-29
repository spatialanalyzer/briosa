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
produced the package. Its required contract is recorded separately. Public-API
fixture sources have their own immutable revision pins, so a newer fixture can
test an older installed package without mislabeling its source. Retain every such version until an
explicit support policy replaces this rule. Never substitute a source project
for a retained package.

`fixtureSources` selects the current committed development fixture for source
candidates. `historicalFixtureSources` retains older committed fixture revisions
so previously recorded evidence remains verifiable after a fixture advances.

The initial 0.7.0 bootstrap remains historical evidence; the empty-inventory
exception is no longer active. All twelve published 0.2.0 and 0.3.0 products are retained with their
actual registry artifacts. Each passed all 12 scenarios against released Server
0.6.1, 0.7.0, and 0.8.0 conformance bundles: 36 released-package pairs and 432
scenario runs. The 0.8.0 release gate supplied the six retained-client reports;
their server artifact hashes match the published conformance ZIPs. The matrix
also preserves 24 development-candidate pairs with their original identities.
No licensed-SA coverage is implied.

For the major-2 server, all twelve retained major-1 products must reject the
incompatible installation before launch. Six published 0.4.0 packages now cover
the three client languages and both exact targets. The release gate downloads
those registry artifacts by immutable URL and SHA-256 before exercising the
packaged server with a fake SDK. Earlier commit-pinned candidate results remain
historical development evidence, not evidence for the published package bytes.

The 2026-09-28 matrix entries use the frozen Server `0.9.0-dev.1` source
`89b59aa0ddd05ea868d94c4e4a4f89ee0fbcbccb` and its packaged conformance
artifacts. All twelve published `0.2.0`/`0.3.0` packages rejected that major-2
candidate before MP work, and all six then-unpublished `0.4.0` packages passed the
twelve fake-SDK scenarios per target. The earlier 2026-09-25 major-2 reports
remain byte-identical historical evidence for their own package hashes and
source checkpoint; the current matrix points to the new reports and hashes.

Each new tested pair records `expectedCompatibility`. An accepted pair proves
operation conformance; a rejected pair proves installation/startup rejection
only. Its success flag must never be described as operation compatibility.

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

The installed artifact's identity is `client.package`, including its version.
The enclosing `client.version` in existing reports identifies the fixture's
checkout version and can differ when that fixture tests a retained package.
Single-scenario reports may encode `scenarios` as a scalar; readers normalize it
to an array before checking the scenario identity and count.

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
  "sha256": "<64 lowercase hex characters>",
  "requiredContract": { "major": 1, "minimumRevision": 0 }
}
```
