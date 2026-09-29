# Points from surfaces and planes

The typed `ConstructionOperations` RPCs can intersect principal object axes with surfaces; project named points onto surfaces parallel or radial to a WCF axis or spherically from the WCF origin; create points from a cylinder or an attended surface-face selection; make a UV grid of points; click points on surfaces; and shift a plane.

For axis-based projection, `X`, `Y`, and `Z` select the corresponding positive WCF axis. Surface and source-point lists must be nonempty. Optional group name, point prefix, and suffix default to empty strings. UV-grid construction defaults to five grids in each direction and base name `UV Points`; an edge-point mode is required. The attended selector has no input arguments. Clicking defaults the first point name to `p0`. Plane shift and bound-growth values default to zero.

These steps mutate shared application state and have unsafe replay classification. Portable generated-client and fake-worker tests cover representative RPCs plus each command's argument order, bindings, defaults, invalid inputs, and registration. Licensed SA behavior remains unverified for this migration. A protected opt-in scenario should exercise every projection geometry, all three WCF axes, each edge mode, attended selection, clicking, and plane shifting on disposable fixtures while recording exact SA/SDK versions and MP outcomes.
