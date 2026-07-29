# Catalog release membership

A supported-command catalog is the maximum exact-target API that the binary can express. A release-membership document identifies the smaller, reviewed subset being delivered for a named release line and wave. It is planning and completeness metadata, not a compatibility promise and not runtime authorization.

For SA `2026.1.0529.7`, the initial v0.2 Wave 1 subset is declared in `catalog/sa/2026.1.0529.7/release-memberships/v0.2-wave1-initial.json`. It contains five collection-introspection operations. The earlier `file_operations.get_working_directory` vertical slice remains in the supported catalog but is not counted as a newly promoted Wave 1 member. The membership is deliberately additive and does not imply that all 101 disposition-approved Wave 1 candidates have shipped.

## Required coordinates

Every membership records:

- a stable membership ID, release line, and delivery wave;
- the exact catalog ID, SpatialAnalyzer target, and catalog revision; and
- a sorted, duplicate-free list of exact operation IDs.

The target catalog manifest lists every membership file. Validation rejects missing, unlisted, or stale files; duplicate or unknown operation IDs; and any catalog, target, or revision mismatch. Generation copies membership facts into the coverage manifest and tags every member operation, so completeness tests compare source membership with protocol, binding, service, registration, capability, documentation, generated coverage, and the portable conformance manifest. A release member missing from executable portable conformance therefore fails CI.

## Promotion workflow

When a reviewed operation is added to a release subset:

1. complete the normal catalog promotion review and increment the target catalog revision;
2. add the exact operation ID to the selected membership in ordinal order;
3. update the membership's catalog revision to the same value as the target manifest;
4. regenerate catalog artifacts rather than editing generated files; and
5. run catalog, scaffold, protocol, generated-client, and full-surface verification.

Membership never changes deployment policy. A member remains denied unless its exact ID is present in the server's runtime allowlist and absent from its denylist. Discovery reports that effective intersection.

Adding a `release_line` here does not settle Briosa semantic-version-to-catalog compatibility or SpatialAnalyzer support duration. Those remain explicit release and governance decisions.
