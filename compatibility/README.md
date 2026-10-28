# Compatibility evidence

The behavioral declaration is target-owned in `compatibility.json`. The
architecture contract defines admission; `matrix.json` records tested pairs and
does not turn untested future releases into tested releases.

`declaredCompatibility` records the exact client releases, contract-major/revision
range, exact targets, architecture, default server-version limits, prerelease
policy, exclusions, and legacy exceptions. Empty exclusions mean no additional
known build exclusions are declared; applications can still narrow selection.
Only `testedPairs` entries carry execution evidence for exact package combinations.
Published pairs must match exactly one registry artifact in
`retained-clients.json`, either in `clients` or in `retiredClients`, by URL,
SHA-256, source, target, language, and version. Local package tests remain
explicitly unpublished.

`retained-clients.json` (schema 3) is the release gate's immutable package
inventory. Add each published contract-aware client version for both targets,
including the public registry URL, file name, SHA-256, and exact client source
revision that produced the package. Its required contract is recorded
separately. Never substitute a source project for a published package.

The gate follows the support window decided in #250. It covers the clients of
the server's current contract major. A release that changes the major also
keeps the previous major's clients, as negative checks that must be rejected
before launch. The policy enforces the bound: each entry in `clients` must
require the current or the previous major. Clients of older majors move to
`retiredClients`, and the previous major's clients may follow once the new
major has shipped. The gate never runs a retired client, but its record stays
unchanged, so the matrix can still verify the evidence published for it. A
current-major client can't be retired, and a client release appears in only
one list.

Public-API fixtures have their own immutable revision pins, so a fixture can
test an installed package without mislabeling the package's source.
`fixtureSources` pins the current committed fixture for each language, and
source candidates always use it. A published client may pin its own
`fixtureSourceRevision` instead, which must be one of its language's current or
historical fixture revisions. A previous-major client keeps the fixture that
tested it, so a newer fixture never has to build against an older package.
`historicalFixtureSources` retains older committed fixture revisions, so
previously recorded evidence and these pins remain verifiable after a fixture
advances.

The initial 0.7.0 bootstrap remains historical evidence; the empty-inventory
exception is no longer active. All twelve published 0.2.0 and 0.3.0 products are recorded with their
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

The released Server `0.9.0` (source `85ba108033fb5ddfcaf8a69434e037cea7deb57a`),
`0.9.1` (source `55d87626c96f4b8ff71990cf4b1229f33cfc41a6`), and `0.9.2`
(source `c72e02753fe7c6a0a9830282de9f9a5cff9e8c39`) release gates supplied the
2026-09-29, 2026-10-01, and 2026-10-04 reports: 54 released-package pairs, all
contract 2.0. All twelve published `0.2.0`/`0.3.0` packages rejected each
release before launch, and all six published `0.4.0` packages passed the twelve
fake-SDK scenarios per target against each release: 216 scenario runs. Every
report's server artifact hash matches the published conformance ZIP. The reports
are stored with the repository's LF line endings; their content is otherwise
unchanged from the CI artifacts. The matrix now declares contract 2.0 and the
published `0.4.0` clients. The 0.6.1 legacy exception applies only to contract
requirement 1.0, so it is no longer declared; `legacyBaseline` still verifies
the historical pairs. No licensed-SA coverage is implied.

For the developing major-3 server, the twelve `0.2.0`/`0.3.0` products, which
require contract major 1, moved to `retiredClients`. The six published `0.4.0`
packages stay in `clients` as negative checks: each must be rejected before
launch. Each runs with the fixture that tested it in the `0.9.0`, `0.9.1`, and
`0.9.2` release gates (`e6df2cab`, `d67ba80f`, and `6ee825e4`). The six
major-3 `0.5.0` source candidates are added once the client pull requests
exist. Their fixture commits then become `fixtureSources`, and the `0.4.0`-era
fixtures move to `historicalFixtureSources`, where the `0.4.0` pins still find
them. Until the candidates are added, no client accepts major 3, so the gate
fails by design with "Test a compatible client for every language and exact
target".

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
