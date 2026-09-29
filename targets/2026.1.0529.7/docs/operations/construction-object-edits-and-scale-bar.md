# Object edits and scale bar

The typed `CopyObject`, `RenameObject`, `RenameItem`, and `MirrorObjects` RPCs edit named objects or items in the connected SpatialAnalyzer session. Copy and rename require the source and destination names. Their overwrite flag defaults to false. Item names use the exact target's item-name domain; object names use its object-name domain.

`MirrorObjects` requires a nonempty object list, a frame, and one of the XY, XZ, or YZ frame planes. Its copy flag defaults to true; setting it false moves the source objects. `ConstructScaleBar` requires a scale-bar item name and both target points. Length, uncertainty, and high/low tolerances default to zero. Relative tolerances default to enabled, while high and low tolerance switches default to disabled.

These calls mutate application state and have unsafe replay status. Portable generated-client tests verify exact MP input order, SDK bindings, defaults, item/object type handling, invalid input rejection, and capability registration. Licensed execution on SpatialAnalyzer 2026.1.0529.7 remains unverified. A protected run should check copy versus move, overwrite behavior, and the resulting scale-bar geometry and tolerances; record exact SDK and connected SA versions with the MP outcomes.
