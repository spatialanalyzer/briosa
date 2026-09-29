# Constructing surfaces

This exact SA product exposes typed `ConstructionOperations` RPCs for dissecting or offsetting surfaces; fitting a surface to nominal surface and actual points; making surfaces from annotation links, B-splines, point groups, or other objects; combining surfaces; and projecting points to form surfaces.

The source lists required by each MP step must be nonempty. `ConstructSurfaceByDissectingSurfaces` requires `EntireSolid` or `SelectFaces` mode and returns a retrieved list of surfaces. `ConstructSurfaceFromPointGroups` sends the documented B-spline fit options, including message defaults when omitted. For combining surfaces, omitted flags hide the originals without deleting them, sewing tolerance is disabled, and the tolerance value is `-1`. For offsetting, omitted offset is zero and original surfaces are hidden.

These operations mutate application-global state and have unsafe replay classification. A request deadline does not establish whether SpatialAnalyzer executed a queued step. Inspect application state before replaying an unknown outcome.

Generated-client, fake-worker, binding/default, invalid-input, and registration tests are portable and pass without SpatialAnalyzer. Licensed execution remains unverified for this migration. An opt-in protected integration scenario should cover each source family, both dissection modes, retrieval of dissected surfaces, and the combine/offset defaults on disposable fixtures. Record the exact SA and SDK versions and MP result status without publishing proprietary geometry or paths.
