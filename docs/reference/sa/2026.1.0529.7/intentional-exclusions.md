# SA 2026.1.0529.7 intentional exclusions

Briosa intentionally excludes 349 of the 1,412 inventoried SpatialAnalyzer commands under the product-scope review recorded in issue #52. These commands are accounted for, but they are not supported candidates and will not appear in capability discovery or generated public RPCs.

The deterministic [command disposition report](../../../../disposition/sa/2026.1.0529.7/report.md#reviewed-intentional-exclusions) is the exact command-level support matrix. It records the MP step, category, reason code, Briosa-authored rationale, and decision reference for every reviewed exclusion.

## Exclusion reasons

| Reason code | Commands | Product boundary |
| --- | ---: | --- |
| `client_owned_external_integration` | 39 | HTTP, HTTPS, UDP, OPC DA, and OPC UA belong in the client application or a future Briosa-owned integration. |
| `client_owned_office_integration` | 14 | Microsoft Office document generation belongs in a dedicated reporting library. |
| `client_owned_serialization` | 17 | Generic JSON and XML parsing or mutation belongs in serialization libraries. |
| `client_owned_spreadsheet_integration` | 38 | Excel and Google Sheets integration belongs in spreadsheet libraries. |
| `client_owned_state_and_control_flow` | 63 | Client-language variables, counters, branching, waiting, and subroutine control replace MP programming helpers. |
| `client_owned_user_experience` | 54 | The client application owns prompts, task progress, dialogs, speech, drag interaction, and runtime-selection UI. |
| `client_owned_value_computation` | 60 | Scalar, vector, accumulator, list-cardinality, min/max, transform, and similar pure calculations do not require SA. |
| `client_owned_value_construction` | 64 | Briosa client types and client-language collections replace primitive constructors, reference-list algebra, indexing, and decomposition helpers. |

Product-scope exclusion takes precedence when an excluded command also lacks an SDK binding. `sdk_unavailable` is reserved for an operation Briosa would otherwise want to expose but cannot execute through the exact-target SDK.

## Boundaries retained for later review

The review is command-specific rather than category-wide:

- Vector math and vector reference-list helpers are excluded, while vector-group queries, display settings, sorting, and mutations remain for domain curation.
- Generic XML/JSON manipulation is excluded, while SA-domain operations such as importing nominals or merging measurements through XML remain for file/domain review.
- Pure constructors and runtime-selection variants are excluded, while commands that query SA state by type, color, wildcard, group, or uniqueness remain candidates.
- Excel output is excluded, while SA report output to PDF remains for reporting review.
- Operator prompts and drag-selection UI are excluded, while non-interactive view and visualization commands remain for view-domain review.

An excluded command may be reopened only through a reviewed policy change that identifies a Briosa server use case, supplies complete risk and metadata review, and moves the command back through the disposition workflow. Adding a dedicated Briosa integration in the future does not imply wrapping the corresponding SA-hosted integration command.
