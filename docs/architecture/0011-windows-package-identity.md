# ADR 0011: Windows package identity and release artifacts

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-22
- Issue: [#21](https://github.com/spatialanalyzer/briosa/issues/21)

## Context

Operators need a Windows distribution that can be installed without a machine-wide .NET runtime and identified unambiguously before it connects to SpatialAnalyzer. Briosa has three independent compatibility coordinates: its own semantic version, the exact SpatialAnalyzer release represented by generated MP contracts, and the Windows runtime architecture.

A package must preserve the server/worker isolation boundary, remain buildable on an ordinary GitHub-hosted Windows runner, and avoid redistributing SpatialAnalyzer binaries, license material, or vendor documentation. Rebuilding the same source and version should not create unexplained artifact drift.

## Decision

Each release package supports exactly one SpatialAnalyzer release. The initial artifact is named:

```text
briosa-<briosa-semver>-sa-2026.1.0529.7-win-x64.zip
```

Briosa Git tags use `v<briosa-semver>`. A tag identifies the Briosa source release; the package filename and manifest add the exact SpatialAnalyzer and runtime coordinates. No version range, nearest-match behavior, or cross-release MP normalization is implied.

The archive is a self-contained, non-trimmed, non-single-file `win-x64` directory publish. It contains separate `Briosa.Server.exe` and `Briosa.Worker.exe` processes plus the approved generated interop assembly. It does not contain SpatialAnalyzer, its SDK executable, original type library, license material, or copied vendor documentation.

The package includes:

- `manifest.json` with Briosa version, source revision, runtime, protocol, catalog, exact target, and interop coordinates;
- `files.sha256` for every other file inside the package;
- catalog-coverage and interop-provenance metadata used by the build;
- an operator guide, health/discovery guide, and Apache-2.0 license.

The build also emits an external ZIP checksum and a copy of the provenance manifest. Publish outputs are produced independently and merged only when duplicate paths have identical content. ZIP entries are sorted and carry a fixed timestamp. CI builds the package twice and requires identical SHA-256 hashes.

Pushing a valid `v*` tag builds, verifies, uploads, and publishes these files to a GitHub Release. A manual workflow dispatch performs the same verified build and retains a workflow artifact without creating a GitHub Release.

## Runtime defaults and diagnostics

The packaged server binds unencrypted HTTP/2 to `127.0.0.1:50051` and targets SpatialAnalyzer at `localhost` by default. Remote exposure remains outside this decision.

`Briosa.Server.exe diagnostics` is an offline static-layout check. It reports safe build and compatibility coordinates and the presence of the adjacent worker and interop assembly. It does not start the web host, activate COM, connect to SpatialAnalyzer, inspect licenses, or disclose local paths, target hostnames, ports, MP arguments, or raw diagnostic details.

## Testing

Portable CI, without SpatialAnalyzer, verifies:

- locked `win-x64` restore and self-contained publish;
- byte-reproducible ZIP output from two builds;
- external and internal SHA-256 checksums;
- manifest and exact-target metadata;
- successful offline diagnostics; and
- public-host startup on a temporary loopback endpoint while worker startup is intentionally unavailable.

Real SpatialAnalyzer connection and operation smoke tests remain protected integration-runner work in issues #18 and #20.

## Consequences

- Operators can select and audit a package from its filename and manifest without running SpatialAnalyzer.
- Server and worker remain independently diagnosable and replaceable; trimming and single-file extraction do not obscure the process boundary.
- Generic Windows CI proves release construction without a SpatialAnalyzer installation or license.
- Code signing, richer standards-oriented SBOM output, installer formats, and multi-target release aggregation can be layered on later without changing the initial archive identity.
