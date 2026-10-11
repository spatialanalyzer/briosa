# Callout views

The typed callout-view RPCs auto-arrange, delete, and rename a view; read the count and indexed position of its callouts; set an indexed position; and set the default view's properties. Named views use the target's callout-view item type when the request omits it. Delete carries a destructive risk flag. The two getters have safe replay status; mutations have unsafe replay status.

Index and position inputs default to zero. Rename overwrite defaults to false. The default-property setter sends lock, recall, thickness, color, line-division, and font inputs in MP order. The default view name is `Callout 1`; thicknesses default to 2, leader color to RGB 128/128/128, border color to RGB 0/0/255, and the font to the message defaults. Color channels are constrained to 0–255. The indexed getter returns six position, anchor, and size integers.

Portable generated-client/fake-worker tests verify all seven routes, bindings, defaults, output retrieval, input validation, and registration. Licensed execution on SpatialAnalyzer 2024.1.0508.5 remains unverified. A protected run should check arrangement and positions, property rendering, rename/overwrite, and deletion while recording exact SDK/SA identities and MP outcomes.

`SetCalloutViewProperties` was removed for contract major 3. SA rejected its callout view list on this target (MP code -1, probe 8 of the [licensed probe record](../testing/evidence/probe-277-2024.1.0508.5-2026-10-07.md)), and the exact-target SDK has no binding for that list type. See [operation retirement](../../../../docs/development/operation-retirement.md).
