# Callout views

The typed callout-view RPCs auto-arrange, delete, and rename a view; read the count and indexed position of its callouts; set an indexed position; and set properties for a view list or the default view. Named views use the target's callout-view item type when the request omits it. Delete carries a destructive risk flag. The two getters have safe replay status; mutations have unsafe replay status.

Index and position inputs default to zero. Rename overwrite defaults to false. Both property setters send lock, recall, thickness, color, line-division, and font inputs in MP order. The default view name is `Callout 1`; thicknesses default to 2, leader color to RGB 128/128/128, border color to RGB 0/0/255, and the font to the message defaults. Color channels are constrained to 0–255. The indexed getter returns six position, anchor, and size integers.

Portable generated-client/fake-worker tests verify all eight routes, bindings, defaults, output retrieval, input validation, and registration. Licensed execution on SpatialAnalyzer 2024.1.0508.5 remains unverified. A protected run should check arrangement and positions, property rendering, rename/overwrite, and deletion while recording exact SDK/SA identities and MP outcomes.
