# SA 2026.1.0529.7 geometry command review

Issue [#49](https://github.com/spatialanalyzer/briosa/issues/49) reviews the exact-target Analysis, Construction, Cloud/Mesh, Dimension, GD&T, and Scale Bar inventory domains. The review covers 450 previously unreviewed inventory keys; 112 earlier decisions in the same top-level categories remain governed by issue #52.

## Results

| Disposition | Commands |
| --- | ---: |
| Approved candidate | 220 |
| Blocked | 156 |
| Intentional exclusion | 36 |
| SDK unavailable | 38 |

Approved candidates are assigned to the risk-ordered delivery plan:

| Delivery wave | Commands |
| --- | ---: |
| Wave 1: low-risk read-only queries | 48 |
| Wave 2: non-device state and geometry mutations | 98 |
| Wave 3: dimension, GD&T, relationship, callout, and filesystem operations | 44 |
| Wave 4: interactive or potentially long-running operations | 30 |

These are candidate and scheduling decisions, not public API support. Catalog promotion, generated contracts, runtime policy, and operation tests remain separate gates.

## Review rules

- A command is an approved candidate only when the extracted direction and type evidence is coherent and every required setter and result getter maps to a usable exact-target binding-registry entry.
- An explicit unavailable input binding or absence from the complete exact-target View SDK Code export produces `sdk_unavailable` unless product-scope exclusion takes precedence.
- Binding, direction, or semantic disagreements produce a command-scoped `blocked` decision linked to issue #53. Other commands in the same category remain independently reviewable.
- Pure value construction, decomposition, and reference-list algebra remain client-owned under issue #52. Commands that query or mutate SpatialAnalyzer state remain eligible.
- Operations requiring an operator-driven SpatialAnalyzer wizard, click workflow, or trapping session are intentionally excluded from unattended gRPC execution. Noninteractive view-state changes remain candidates with `interactive_ui` risk.
- Commands that only reference existing instrument or robot state are not labeled `device_control`; issue #50 owns commands that actually configure, measure with, or control devices.
- Empty `risk_flags` means the review identified no special command-level risk flag. It is distinct from the explicit unresolved value `unknown`.

The committed category shards are the machine-readable source of truth. The generated disposition report provides complete counts and command-level rationale without republishing vendor documentation.
