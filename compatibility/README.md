# Compatibility evidence

The behavioral declaration is target-owned in `compatibility.json`. The
architecture contract defines admission; `matrix.json` records tested pairs and
does not turn untested future releases into tested releases.

`retained-clients.json` is the release gate's immutable package inventory. Add
each published contract-aware client version for both targets, including the
public registry URL, file name, SHA-256, and exact client source revision that
owns its public-API conformance fixture. Retain every such version until an
explicit support policy replaces this rule. Never substitute a source project
for a retained package.

The first contract-aware server, **0.7.0 only**, must precede client publication
because clients consume its immutable protocol artifact. Its empty-inventory
bootstrap is explicit in CI evidence. No later release can sign with an empty
inventory. After publishing all six 0.2.0 products, add their actual registry
artifacts before any subsequent server release. Do not invent URLs or digests.

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

