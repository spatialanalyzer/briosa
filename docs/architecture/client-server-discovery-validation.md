# Installer discovery validation

- Date: 2026-09-18
- Work item: [briosa #191](https://github.com/spatialanalyzer/briosa/issues/191)
- Contract: [installed server discovery](installed-package-store.md#client-server-discovery)

## Scope and result

The .NET, Node.js, and Python clients discovered Briosa Server 0.6.0 for both
SA 2024.1.0508.5 and SA 2026.1.0529.7 in the canonical current-user Installer store.
All six clients started their selected server, verified runtime compatibility,
completed the `control-plane-only` conformance scenario, and stopped their owned
server. `BRIOSA_SERVER_PATH` was absent and client-local distributions were absent.

The products were acquired and verified by released Briosa Installer 0.2.0 from
the published catalog. Complete receipts and payloads were temporarily copied
into the canonical store under its exclusive lock, committed from a staging
directory, and removed after the smoke checks. The original acquired products
were retained. No temporary server or worker processes remained.

Both server manifests identify source revision
`a2825dee76cd817ba3fa697449d35bbd3a38eaeb`. The catalog used for acquisition had
SHA-256 `b483fad89a13df3b898ecffc8fda77340ca71d021ab30596a207cdf969718e0f`.
The published artifacts are available in the
[Briosa v0.6.0 release](https://github.com/spatialanalyzer/briosa/releases/tag/v0.6.0).

## Portable regressions

| Client | SA 2024 tests passed | SA 2026 tests passed |
| --- | ---: | ---: |
| .NET | 69 | 52 |
| Node.js | 52 | 46 |
| Python | 69 | 52 |

The 340 passing tests include discovery regressions for explicit/local/user/machine/
legacy precedence, custom explicit paths, unavailable roots, exact product identity,
receipt and manifest schema/shape, missing required files, and transaction-directory
exclusion. Each target owns its independent implementation and tests.

## Limits

The live acquisition-to-discovery smoke checks exercised the current-user store.
Machine-store selection was exercised with isolated portable fixtures; these checks
do not establish enterprise policy, protected ACL, or Artifactory acceptance.
Discovery checks required metadata and entry points; full integrity verification
remains an Installer operation.

SpatialAnalyzer and SDK startup were disabled, as were the server desktop companion
and file logging. These checks do not exercise MP execution, change SDK registration,
or establish licensed SA runtime validation. The existing v1.0 validation gates
remain separate from this v0.x installation integration fix.
