# Object edits and scale bar

The typed `CopyObject`, `RenameObject`, and `RenameItem` RPCs edit named objects or items in the connected SpatialAnalyzer session. Copy and rename require the source and destination names. Their overwrite flag defaults to false. Item names use the exact target's item-name domain; object names use its object-name domain.

`ConstructScaleBar` requires a scale-bar item name and both target points. Length, uncertainty, and high/low tolerances default to zero. Relative tolerances default to enabled, while high and low tolerance switches default to disabled.

These calls mutate application state and have unsafe replay status. Portable generated-client tests verify exact MP input order, SDK bindings, defaults, item/object type handling, invalid input rejection, and capability registration. Licensed execution on SpatialAnalyzer 2026.1.0529.7 remains unverified. A protected run should check copy, overwrite behavior, and the resulting scale-bar geometry and tolerances; record exact SDK and connected SA versions with the MP outcomes.

`MirrorObjects` was removed for contract major 3. SA rejected its frame plane sent as text (MP code -1, probe 10 of the [licensed probe record](../testing/evidence/probe-277-2026.1.0529.7-2026-10-07.md)), and the exact-target SDK has no setter for the MP Plane Type. See [operation retirement](../../../../docs/development/operation-retirement.md).
