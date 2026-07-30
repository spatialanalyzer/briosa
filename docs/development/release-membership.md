# Catalog release membership

A supported-command catalog is the maximum exact-target API that the binary can express. A release-membership document identifies the smaller, reviewed subset being delivered for a named release line and wave. It is planning and completeness metadata, not a compatibility promise and not runtime authorization.

For SA `2026.1.0529.7`, the initial v0.2 Wave 1 subset is declared in `catalog/sa/2026.1.0529.7/release-memberships/v0.2-wave1-initial.json`. It contains five collection-introspection operations. The growing Wave 2 subset is independently declared in `v0.2-wave2-initial.json` and contains eighteen mutations. Seven cover the point lifecycle and derived construction: construct a point in working coordinates, rename a point, delete points, construct a point at a circle center, construct a point at a line midpoint, fit a point to points, and construct a point group from a point-name list. Five cover collection state: set or construct the default collection, rename a collection, delete a collection, copy objects to a collection, and move objects to a collection. Three cover exact object lifecycle: copy an object, rename an object, and delete an object-name reference list. Three set notes on an explicitly identified collection, object, or point. The earlier `file_operations.get_working_directory` vertical slice remains in the supported catalog but is not counted as a newly promoted member. Membership is deliberately additive and does not imply that all disposition-approved candidates for either wave have shipped.

All eighteen Wave 2 members mutate application-global state and have `unknown` replay safety. Their membership records delivery, not an endorsement of automatic retry: cancellation, timeout, worker loss, or a lost response after admission can require the caller to inspect current model state before deciding what to do next.

The four derived-constructor additions were authorized under the maintainer-approved Wave 2 promotion rule: exact-target installed documentation and View SDK Code establish the SA `2026.1.0529.7` contract, while a matching ObjectiveSA counterpart corroborates the command and argument shape. The review found these one-to-one counterparts:

| Exact MP step | ObjectiveSA counterpart | Matching shape |
| --- | --- | --- |
| `Construct a Point at Circle Center` | `ConstructPointAtCircleCenter` | circle collection-object input followed by resultant point-name input; no getter-returned output |
| `Construct a Point at line MidPoint` | `ConstructPointAtLineMidpoint` | line collection-object input followed by resultant point-name input; no getter-returned output |
| `Construct Point (Fit to Points)` | `ConstructPointFitToPoints` | point-name reference-list input followed by resultant point-name input; no getter-returned output |
| `Construct Point Group from Point Name Ref List` | `ConstructPointGroupFromPointNameRefList` | point-name reference-list input followed by resultant group collection-object input; no getter-returned output |

ObjectiveSA remains prior-release secondary evidence, not live runtime conformance for the exact target. All four operations therefore remain `cataloged_portable_only` with `validation_status: not_performed`; the membership and portable tests do not claim that a licensed-SA run has occurred.

The five collection mutations use the same maintainer-approved promotion rule. Their exact MP names, argument order, and setter families agree one-to-one with ObjectiveSA:

| Exact MP step | ObjectiveSA counterpart | Matching shape |
| --- | --- | --- |
| `Set (or construct) default collection` | `SetOrConstructDefaultCollection` | required collection-name input |
| `Rename Collection` | `RenameCollection` | required original and new collection-name inputs |
| `Delete Collection` | `DeleteCollection` | required collection-name input |
| `Copy Objects to a collection` | `CopyObjectsToACollection` | required collection-object reference list followed by destination collection name |
| `Move Objects to a collection` | `MoveObjectsToACollection` | required collection-object reference list followed by destination collection name |

They remain `cataloged_portable_only`: generated adapter and server tests plus the packaged-host client fixture prove Briosa-owned behavior against deterministic fakes, while protected licensed-SA conformance remains a separate release gate.

The three object lifecycle operations are another independently reviewable promotion. Their installed command documentation and View SDK Code shapes match ObjectiveSA exactly; Briosa retains the exact-target `SetCollectionObjectNameArg2` binding selected by the reviewed collection-object value-family evidence:

| Exact MP step | ObjectiveSA counterpart | Matching shape |
| --- | --- | --- |
| `Copy Object` | `CopyObject` | required source and destination collection-object names followed by optional `overwrite`, defaulting to `false` |
| `Rename Object` | `RenameObject` | required original and new collection-object names followed by optional `overwrite`, defaulting to `false` |
| `Delete Objects` | `DeleteObjects` | required collection-object-name reference list |

These operations also remain `cataloged_portable_only`. Their packaged-host scenarios prove that each generated RPC is advertised and succeeds through the deterministic fake; generated conformance tests separately cover required-input validation, setter rejection, MP failure, cancellation/deadline disposition, watchdog recovery, policy denial, and malformed results without claiming licensed SpatialAnalyzer execution.

The three note mutations share one reviewed contract across distinct identity families. Exact-target documentation and View SDK Code match the ObjectiveSA implementations, including required ordered edit-text lines and the optional `append` input defaulting to `true`:

| Exact MP step | ObjectiveSA counterpart | Matching shape |
| --- | --- | --- |
| `Set Collection Notes` | `SetCollectionNotes` | required collection name and edit-text lines followed by optional `append`, defaulting to `true` |
| `Set Object Notes` | `SetObjectNotes` | required collection-object name and edit-text lines followed by optional `append`, defaulting to `true` |
| `Set Point Notes` | `SetPointNotes` | required point name and edit-text lines followed by optional `append`, defaulting to `true` |

Note text is classified as proprietary and is never included in default logs or conformance reports. These operations remain `cataloged_portable_only`; their packaged-host fixture proves each generated RPC through deterministic fake execution, while generated conformance covers validation, setter rejection, MP failure, interruption/watchdog dispositions, policy, and malformed results.

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
