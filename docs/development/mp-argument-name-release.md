# MP Argument Name Release Record

This completes the portable release validation for [Task #217](https://github.com/spatialanalyzer/briosa/issues/217) and the [MP argument name migration](mp-argument-name-migration.md). Server 0.8.0 and client 0.3.0 cover both exact targets, SA 2024.1.0508.5 and SA 2026.1.0529.7.

## Released Products

| Product | Release | Source Revision | Release Gate |
| --- | --- | --- | --- |
| Server, worker, Control Center, protocol, conformance | [0.8.0](https://github.com/spatialanalyzer/briosa/releases/tag/v0.8.0) | `e986a3ba91cb501416126eb5f3ecaeb7f9d97c05` | [Build, retained clients, signing, publication](https://github.com/spatialanalyzer/briosa/actions/runs/36055880292) |
| .NET clients | [0.3.0](https://github.com/spatialanalyzer/briosa-dotnet/releases/tag/v0.3.0) | `34f79fe722d353a31d563360a5551b3cf5407a07` | [Publishing checks](https://github.com/spatialanalyzer/briosa-dotnet/actions/runs/36059548895) |
| JavaScript/TypeScript clients | [0.3.0](https://github.com/spatialanalyzer/briosa-js/releases/tag/v0.3.0) | `a861004f93a0dc1548939739e7a16d0d70341750` | [Publishing checks](https://github.com/spatialanalyzer/briosa-js/actions/runs/36059418728) |
| Python clients | [0.3.0](https://github.com/spatialanalyzer/briosa-py/releases/tag/v0.3.0) | `08f5db6ab36fb3ad5fbfc269d4576bc4e3225ccd` | [Publishing checks](https://github.com/spatialanalyzer/briosa-py/actions/runs/36059223959) |

Each client imports the published target-specific 0.8.0 protocol with `github_release` provenance. Both target packages were downloaded from each actual registry and installed into isolated consumers. Each new client package passes all 12 fake-SDK scenarios against released Servers 0.6.1, 0.7.0, and 0.8.0, including a call through a renamed MP input. The six retained client 0.2.0 packages also pass against 0.8.0 in the server release gate. Their conformance artifact hashes match the published release bundles.

The [compatibility matrix](../../compatibility/matrix.json) now records 36 released-package pairs with 432 passing scenario runs, plus the original 24 development-candidate pairs. Candidate reports retain their original sources, hashes, and unpublished package status. The [retained inventory](../../compatibility/retained-clients.json) contains all twelve client 0.2.0 and 0.3.0 products so future server releases test both generations.

## Installer, Catalog, Documentation, and Examples

Published Installer 0.3.0 passes disposable-store checks against both signed 0.8.0 server ZIPs: signed-catalog verification, schema-3 admission, exact receipts, side-by-side installation, verification, repair, and removal. These checks neither launch a server nor activate SA or its SDK. No installer application update or manifest-schema change is needed.

The reviewed catalog adds the two signed 0.8.0 server products while preserving all twelve existing package entries exactly. Publication uses the normal gated R2 workflow, existing signing key, signature verification, immutable asset checks, and public metadata verification. Installer setup metadata and trust configuration remain unchanged.

The documentation release adds immutable Server 0.8.0 and client 0.3.0 references for both targets. Node 24 production validation builds 25,945 pages and 22,671 API routes; eight search checks and 17 SEO/reference/release tests pass. Browser checks cover shortened names, qualifier notes, historical names, target persistence, related-language links, Back/Forward, mobile method navigation, and keyboard release selection. Earlier snapshots remain intact. Hosted search refresh is a separate post-deployment operation requiring the existing Algolia account.

Standalone examples contain no impacted identifiers. Their published 0.2.0 pins and previous licensed-validation claims remain reproducible; all 47 existing fake-server scenarios pass. Current documentation examples and client package consumers use 0.3.0. Evidence, brand, and governance repositories require no product changes.

## Compatibility and Validation Limits

The migration renames 168 members in 83 commands for SA 2024 and 172 in 86 commands for SA 2026. All names in the four collision-exception methods remain unchanged. Qualifier meanings remain in documentation; exact SDK labels, protobuf tags, types, presence, defaults, and behavioral contract 1.0 are unchanged.

Binary compatibility does not remove the source/JSON migration requirement. Applications must update named arguments, result properties, JSON keys, and reflection lookups when adopting client 0.3.0 or regenerating transports from Server 0.8.0. Installing a new server does not update an application's client dependency.

No licensed SpatialAnalyzer execution or SDK registration changes were performed for this refactoring or release. Portable and packaged checks do not establish new licensed MP validation.
