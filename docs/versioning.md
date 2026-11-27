# Versioning and compatibility

Briosa server releases and first-party client libraries (.NET, JavaScript/TypeScript, and Python) are versioned independently. Each server and client package is also built for one exact SpatialAnalyzer release (`2026.1.0529.7` or `2024.1.0508.5`). Choose the client package and server package for the same SpatialAnalyzer release.

## Compatible server and client versions

The `0.10.x` server line is paired with the `0.5.x` first-party client line. For both SpatialAnalyzer targets, the published releases are Server [0.10.0](https://github.com/spatialanalyzer/briosa/releases/tag/v0.10.0) and [0.10.1](https://github.com/spatialanalyzer/briosa/releases/tag/v0.10.1), and clients `0.5.0` and `0.5.1` for .NET, JavaScript/TypeScript, and Python. JavaScript/TypeScript also published client [`0.5.2`](https://github.com/spatialanalyzer/briosa-js/releases/tag/v0.5.2), the recommended JavaScript client because of a [known issue](https://github.com/spatialanalyzer/briosa/issues/450) in which a call can occasionally receive no reply.

Clients `0.4.x` and earlier do not work with Server `0.10.x`, and clients `0.5.x` do not work with Server `0.9.x` and earlier.

| Server line | Published server releases | Compatible first-party clients | Published client releases | Status |
| --- | --- | --- | --- | --- |
| `0.10.x` | `0.10.0`, `0.10.1` | `0.5.x` | `0.5.0`, `0.5.1`; JavaScript/TypeScript also `0.5.2` | Current release line |
| `0.9.x` | `0.9.0`, `0.9.1`, `0.9.2` | `0.4.x` | `0.4.0` | Server `0.9.2` remains available side by side for `0.4.x` clients |
| `0.7.0`, `0.8.0` | `0.7.0`, `0.8.0` | `0.2.0`, `0.3.0` | `0.2.0`, `0.3.0` | Earlier tested releases |
| `0.6.1` | `0.6.1` | `0.1.1`, `0.2.0`, `0.3.0` | `0.1.1`, `0.2.0`, `0.3.0` | Client `0.1.1` requires exactly Server `0.6.1` |
| `0.6.0` | `0.6.0` | `0.1.0` | `0.1.0` | Client `0.1.0` requires exactly Server `0.6.0` |

You can install Server `0.9.2` and Server `0.10.x` side by side. For the same SpatialAnalyzer target, client `0.4.0` selects `0.9.2` and clients `0.5.x` select `0.10.x`. Both reject the other installation before launch. When `0.10.0` and `0.10.1` are both installed for the same target, clients `0.5.x` select `0.10.1` unless the application selects a server explicitly.

To move an application from Server `0.9.x` to Server `0.10.x`, upgrade to the matching `0.5.x` first-party client or regenerate direct gRPC bindings from the `0.10.x` exact-target protocol artifact (`briosa-protocol-<version>-sa-<target>.zip`).

## Version convention

Server `0.10.1` is the current published release. Starting with Server `0.10.1`:

- A **server patch release** changes the third number. A client that works with the preceding server release on the same minor line continues to work without an upgrade. For example, client `0.5.0` needed no upgrade for Server `0.10.1`. This does not mean an older server provides features introduced by a newer one.
- A **minor release** (changing the second number while Briosa is in `0.x`) may break compatibility and require an updated client release.

Direct gRPC callers that do not use a first-party client should generate their bindings from the exact-target protocol artifact published with the server release they target.
