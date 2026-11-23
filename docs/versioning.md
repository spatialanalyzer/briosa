# Versioning and compatibility

Briosa server releases and first-party client libraries (.NET, JavaScript/TypeScript, and Python) are versioned independently. Each server and client package is also built for one exact SpatialAnalyzer release (`2026.1.0529.7` or `2024.1.0508.5`): a client for one SpatialAnalyzer target never connects to a server built for another target.

## Compatible server and client versions

Server `0.10.x` works with first-party clients `0.5.x`. Server [0.10.0](https://github.com/spatialanalyzer/briosa/releases/tag/v0.10.0) was published on 2026-11-21 alongside clients `0.5.0` for both supported SpatialAnalyzer targets.

Clients `0.4.x` and earlier do not work with Server `0.10.x`, and clients `0.5.x` do not work with Server `0.9.x` and earlier.

| Server line | Published server releases | Compatible first-party clients | Published client releases | Status |
| --- | --- | --- | --- | --- |
| `0.10.x` | `0.10.0` | `0.5.x` | `0.5.0` | Current release line |
| `0.9.x` | `0.9.0`, `0.9.1`, `0.9.2` | `0.4.x` | `0.4.0` | Server `0.9.2` remains available side by side for `0.4.x` clients |
| `0.6.1`, `0.7.0`, `0.8.0` | `0.6.1`, `0.7.0`, `0.8.0` | `0.2.0`, `0.3.0` | `0.2.0`, `0.3.0` | Earlier tested releases |

Incompatible server lines can stay installed side by side on the same machine. A first-party client selects only a compatible server for its exact SpatialAnalyzer target and rejects an incompatible installation before launch.

To move an application from Server `0.9.x` to Server `0.10.x`, upgrade to the matching `0.5.x` first-party client or regenerate direct gRPC bindings from the `0.10.x` exact-target protocol artifact (`briosa-protocol-<version>-sa-<target>.zip`).

## Version convention

Server `0.10.0` is the current published release. Starting with Server `0.10.1` (not yet released):

- A **patch release** (changing the third number, such as a later `0.10.x` release) stays compatible with the same clients (`0.5.x`).
- A **minor release** (changing the second number while Briosa is in `0.x`) may break compatibility and require an updated client release.

Direct gRPC callers that do not use a first-party client should generate their bindings from the exact-target protocol artifact published with the server release they target.
