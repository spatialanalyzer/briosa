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

A fixture source may also list `optionalScenarios`: the optional conformance
scenarios its scripts request whenever the packaged runner and scenario contract
offer them. Each entry must be a distinct scenario ID that
`targets/<target>/conformance/client/v1/scenarios.json` offers for both targets.
Every accepted job that runs on that fixture must then show each listed scenario
passed exactly once, whichever client version it tests. A rejection job requests
no optional scenario, so it requires none. A fixture keeps its list when it moves to
`historicalFixtureSources`, so a client still pinned to it keeps the
requirement.

The data cannot lower one requirement. `eng/RetainedClientPolicy.psm1` sets a
floor: from client 0.5.0 on, every accepted job must pass `large-result`. The
policy rejects an inventory whose current fixture sources do not list it, or
that runs an accepted 0.5.0 or later client on a fixture that does not. The
workflow gate also checks each job's list against the floor before it counts
pass lines, so a list lost on the way to the job still fails.
`eng/Test-RetainedClientPolicy.ps1` also checks the jobs that the committed
inventory plans for the current release. Update its expected table with every
change to `retained-clients.json`.

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
unchanged from the CI artifacts. For major 2, the matrix declared contract 2.0
and the published `0.4.0` clients. The 0.6.1 legacy exception applies only to
contract requirement 1.0, so it is no longer declared; `legacyBaseline` still
verifies the historical pairs. No licensed-SA coverage is implied.

For the major-3 server, the twelve `0.2.0`/`0.3.0` products, which require
contract major 1, moved to `retiredClients`. The six published `0.4.0` packages
stay in `clients` as negative checks: each must be rejected before launch. Each
runs with the fixture that tested it in the `0.9.0`, `0.9.1`, and `0.9.2`
release gates (`e6df2cab`, `d67ba80f`, and `6ee825e4`). Those fixtures moved to
`historicalFixtureSources` when the major-3 fixtures replaced them in
`fixtureSources`. Six published `0.5.0` packages now cover the three client
languages and both exact targets. They require contract 3.0, were generated
from the `0.10.0-dev.1` protocol checkpoint, and were published from merged
source: briosa-dotnet `bc722444`, briosa-js `b94fce3f`, and briosa-py
`cae6e535`. The Server 0.10.0 gate used the fixtures from the major-3 client
pull requests (`38233690`, `feda30f3`, and `6a04ff93`), now kept as historical
fixture pins. The gate runs twelve jobs, six rejections and six published
accepts, and downloads every package by immutable URL and SHA-256. The earlier
source-candidate results remain development evidence, not evidence for the
published package bytes.

The released Server `0.10.0` (source `ee16784d9fdaf67ce7d7bb0b68609a98fe403e1c`)
release gate supplied the 2026-11-20 reports: 12 released-package pairs, all
contract 3.0. All six published `0.4.0` packages rejected the release before
launch, proving installation/startup rejection only, and all six published
`0.5.0` packages passed all fifteen fake-SDK scenarios per target: 90 scenario
runs. The twelve published `0.2.0`/`0.3.0` packages remain retired from the live
gate. Every report's server artifact hash matches the published conformance ZIP.
The reports are stored with the repository's LF line endings; their content is
otherwise unchanged from the CI artifacts. The matrix then declared contract 3.0
and the published `0.5.0` clients, bringing the recorded total to 144
evidence-backed pairs: 102 released-package pairs (36 major-1, 54 major-2, and
12 major-3; 738 accepted scenario runs) and 42 development-candidate pairs. No
licensed-SA coverage is implied.

For Server 0.10.1, each target's `compatibility.json` records `major` 3 and
`revision` 1 ([#397](https://github.com/spatialanalyzer/briosa/issues/397)),
and the published `0.5.0` and `0.5.1` clients, whose `requiredContract` is 3.0,
work with it. Its release gate tested the published packages with the fixtures
from the merged .NET, JavaScript, and Python 0.5.1 pull requests (`f03d2c8c`,
`4e5ab1ce`, and `f101fcdf`). Their scripts request `large-result` when the
packaged runner and scenario contract offer it, and the fixtures report the
exact UTF-8 byte length and SHA-256 of the value received. The six `0.5.1`
packages were published from those same commits, the `v0.5.1` tags of
briosa-dotnet, briosa-js, and briosa-py, so they ran on those fixtures without a
pin of their own. The six 0.4.0 packages kept their explicit fixture pins and
ran only to verify pre-launch rejection.

The released Server `0.10.1` (source `d947d95ca6e7ff1ecece94cdcb218724f151a807`)
release gate supplied the 2026-11-27 reports from
[run 46039672611](https://github.com/spatialanalyzer/briosa/actions/runs/46039672611),
in which no job was rerun: 18 released-package pairs. All six published `0.4.0`
packages rejected the release before launch, proving installation/startup
rejection only. On each target, the six published `0.5.0` and `0.5.1` packages
passed all 15 required fake-SDK scenarios and `large-result`, for 192 scenario
runs across both targets.
The twelve published `0.2.0`/`0.3.0` packages remain retired from the live gate.
Every report's server artifact hash matches the published conformance ZIP, and
the server manifest inside both ZIPs records compatibility 3.1. The reports are
stored with the repository's LF line endings; their content is otherwise
unchanged from the CI artifacts. The matrix now records 3.1 in `contract` and
declares the published `0.5.0` and `0.5.1` clients. That brings the recorded
total to 162 evidence-backed pairs: 120 released-package pairs (36 with Server
0.6.1 to 0.8.0, 54 with Server 0.9.x, and 30 with Server 0.10.x; 930 accepted
scenario runs) and 42 development-candidate pairs. No licensed-SA coverage is
implied.

The two published JavaScript `0.5.2` packages, one per exact target, now join
the gate beside `0.5.1`. Client 0.5.2 has the same API as 0.5.1 and the same
`requiredContract`, and raises the HTTP/2 receive window for the known issue in
[#450](https://github.com/spatialanalyzer/briosa/issues/450). The packages were
published from the briosa-js `v0.5.2` tag `a4c3af89`, which is now the current
JavaScript fixture source. Since `4e5ab1ce`, the fixture's `eng/`, `tools/`,
`conformance.lock.json` and `tests/fixtures/` files are unchanged and the
package manifests changed only their version. `4e5ab1ce` moves to
`historicalFixtureSources` with its `large-result` list, so the 2026-11-27
JavaScript reports stay verifiable. Like the `0.5.1` packages before them, the
`0.5.2` packages run on the current fixture without a pin of their own, and so
do the JavaScript `0.5.0` and `0.5.1` packages. The
`large-result` floor applies from client 0.5.0 on, so an accepted `0.5.2` job
fails unless the packaged runner reports `large-result` as passed exactly once.
The gate runs twenty jobs: six rejections and fourteen published accepts. The
Server 0.10.1 release gate did not run client 0.5.2, so the matrix records no
`0.5.2` pair and does not declare it until a release gate's reports for it are
recorded.

For the planned Server 0.10.2, each target's `compatibility.json` records
`major` 3 and `revision` 2, for the request validation detail
([#442](https://github.com/spatialanalyzer/briosa/issues/442)), `mp_step` in
discovery and the bundled descriptor set
([#444](https://github.com/spatialanalyzer/briosa/issues/444)), and the caller
restriction header ([#443](https://github.com/spatialanalyzer/briosa/issues/443)).
The published `0.5.0` and `0.5.1` clients and the JavaScript `0.5.2` clients,
whose `requiredContract` is 3.0, work with it, so the gate keeps running them
as compatible clients, and the `0.4.0` packages still verify pre-launch
rejection. The matrix keeps recording 3.1 in `contract` until the 0.10.2
release gate's reports are recorded.

Each new tested pair records `expectedCompatibility`. An accepted pair passed
the fake-SDK scenarios listed in its report; a rejected pair proves
installation/startup rejection only. Its success flag must never be described as operation compatibility.

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

## Soak runs

The `Client conformance soak` workflow (`.github/workflows/conformance-soak.yml`)
repeats one retained-client job on hosted runners until an intermittent failure
shows itself, with every diagnostic on (#450). It is dispatched by hand and runs
nothing else. It builds the client conformance package for one target from the
dispatched commit and uploads it as the workflow's input package,
`compatibility-server-<target>`, for the iterations to download. It then calls
this directory's retained-client workflow in soak mode. That takes the job from
the same plan as the gate (the same pinned package, fixture revision and checks)
and runs it once per iteration, each iteration on its own runner. Each run also
takes heap dumps of the .NET processes at a fixture timeout
(`BRIOSA_CONFORMANCE_DUMP_ON_TIMEOUT=1`). The conformance runner always records
the fixture's process tree at a timeout and turns on the server's Debug unary
call records. It traces no client, for any language: a gRPC library's trace and a
fixture's own output share one stream and cannot be separated reliably. The
server records show which calls arrived and were answered, and the process tree
shows which process was idle or busy.

A soak run is not compatibility evidence. It uploads none of the per-client
`compatibility-<language>-<target>-<version>` evidence the gate uploads. When it
repeats one scenario it builds a soak package, whose runner runs only that
scenario and ends every run with an error naming it, so the client's own test
script never writes a compatibility report for a run that skipped the other
scenarios; the iteration passes only with exactly one validated pass of that
scenario and that error. It publishes, signs and releases nothing, and it uses
no secrets and no environment. The release workflow policy check
(`eng/Verify-ReleaseWorkflowPolicy.ps1`) reads both workflows as parsed YAML. It
fails, with the file and line, on any construct outside the plain block style
they are written in, such as a quoted key, a flow collection or an anchor. It
also fails if the soak could run from anything but a dispatch or call anything
but the retained-client workflow, or if either could reach secrets, an
environment, a write permission or an OIDC token.

Inputs:

| Input | Default | Meaning |
| --- | --- | --- |
| `language` | `js` | `js`, `dotnet` or `py` |
| `target` | `2024.1.0508.5` | the exact SpatialAnalyzer target |
| `client-version` | `0.5.1` | a version `retained-clients.json` lists for that language and target; the gate must accept it |
| `iterations` | `20` | a whole number from 1 to 50 |
| `scenario` | empty | one scenario the job runs, such as `capability-denied`; empty runs every scenario of the job |

The workflow checks these inputs before it builds the package, and it plans
the job exactly as the gate will.

To dispatch it from the command line:

```powershell
gh workflow run conformance-soak.yml --repo spatialanalyzer/briosa --ref main `
  -f language=js -f target=2024.1.0508.5 -f client-version=0.5.1 `
  -f iterations=20 -f scenario=capability-denied
```

Each iteration records the job's status after every check, the digest
vocabulary check included, and the scenarios that failed in it. The
`Summarise the soak` job lists each iteration's status and failed scenarios,
marking those that timed out. The run fails unless there is one successful
result for every requested iteration: a missing, unreadable or unsuccessful
result fails it, and results that could not be downloaded are reported as such.
A failing iteration keeps its diagnostics as the artifact
`conformance-diagnostics-<language>-<target>-<version>-iteration-<n>`: for each
failed scenario, the failure, the fixture's stderr and stdout, the server log
digest and log files, the process tree at a timeout (`timeout-processes.txt`)
and any heap dumps.
