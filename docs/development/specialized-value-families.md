# Specialized value families

The SA `2026.1.0529.7` target models specialized SDK arguments as release-owned protobuf, worker, and adapter types. These types must not be shared with another SA release merely because its COM parameters look identical.

Issue #57 implements 43 reviewed families through 45 exact SDK calls: 33 enum-like setters, 10 structured setters, and the two usable scalar-option getters. The binding registry is the authoritative implementation matrix. A family being present in the installed SDK or extracted MP evidence does not by itself make it part of Briosa's public command catalog.

## Enum-like values

Public protobuf enums reserve numeric value zero for `UNSPECIFIED`. The server rejects zero and unknown values before creating a worker request. The private worker representation removes that wire-only sentinel, validates the release-specific range, and converts the typed value to the exact SDK text accepted by the corresponding specialized setter.

The adapter never substitutes `SetStringArg` or `SetIntegerArg` for a specialized setter. Exact text is centralized in `SdkSpecializedValueCodec`; tests invoke every reviewed enum member so an unmapped member fails before release.

`InstrumentType` contains the 190 non-stand instrument models installed with SA `2026.1.0529.7`. The exact SDK strings come from that release's `Instrument Models/Instrument.lst` and were cross-checked against ObjectiveSA's manually reviewed Add New Instrument list. Category 10 entries in `Instrument.lst` are stand or mount graphics, not Add New Instrument choices, and are intentionally excluded. The installed MP command reference describes the argument but does not enumerate its choices, while View SDK Code emits an empty default; neither source is sufficient by itself to construct this enum.

## Structured values

Structured protobuf fields use explicit presence for scalar components. Generated server bindings reject a request unless every exact-target component is present, including nested enum values and scalar tolerance limits. Worker-channel validation repeats the structural and range checks at the process boundary.

The adapter then calls the exact structured setter. Only `GetFitConstraintScalarOptionsArg` and `GetToleranceScalarOptionsArg` are currently usable result paths. A failed getter preserves `Retrieved = false` and does not publish default-like values.

Live testing against SA `2026.1.0529.7` confirmed that auto-filter proximity modes use SDK integers `0`, `1`, and `2` for `Both`, `Positive only`, and `Negative only`. Its getter uses the MP argument name `Filter Proximity Settings`. The getter is still `excluded_only` in the registry because its observed command is intentionally excluded; the live result does not independently approve a public operation.

## B-spline blocker

`GetBSPlineFitOptionsArg` and `SetBSplineFitOptionsArg` remain `blocked_semantics`, with [issue #79](https://github.com/spatialanalyzer/briosa/issues/79) as their blocker. The installed command documentation and View SDK Code do not define the `Sort Method` encoding needed by the specialized constructor. Live probes showed that generic `SetIntegerArg("Sort Method", value)` is accepted by the SDK call but every tested value causes the MP command to fail. The getter also populated reference parameters while returning `false`.

Consequently, Briosa publishes no B-spline protobuf or worker type and has no generic fallback. Resolving the blocker requires authoritative release-specific semantics or a verified exact SDK path, followed by reviewed contract and live conformance tests.

## Adding another exact SA target

For each specialized family:

1. Review the target's View SDK Code, installed command reference, and exact interop signature.
2. Define target-owned protobuf values and require complete component presence.
3. Define typed worker values with no COM or untyped `object` fields.
4. Map only to the exact SDK setter/getter and preserve the target's literal choice text or numeric encoding.
5. Add fake-SDK contract tests and, when specifically authorized, focused licensed-SA probes.
6. Record unresolved semantics as a registry blocker; do not copy assumptions from an older release.
