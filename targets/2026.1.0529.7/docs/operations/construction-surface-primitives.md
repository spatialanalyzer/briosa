# Construction curves and surface primitives

The `ConstructionOperations` service exposes these MP steps as typed RPCs for this exact SA target:

| RPCs | Inputs and results |
| --- | --- |
| `ConstructBSplineFromIntersectionOfPlaneAndSurface`, `ConstructBSplineFromIntersectionOfSurfaces` | Name the result and source geometry. Omitted approximation tolerance is `0.0001`. |
| `ConstructBSplinesFromSurfaces` | Provide at least one surface. The name prefix is optional; the returned B-spline list comes from MP result retrieval. |
| `ConstructCirclesLinesFromSurfaces` | Provide surfaces, a destination collection, and `Circle` or `Line` mode. Omitted tolerance is `0.02`, base name is `Geometry Object`, and the returned geometry list comes from MP result retrieval. |
| `ConstructLineFromInstrumentShot` | Provide a point and resulting line name. Omitted observation index is `0`. |
| `ConstructPlaneNormalToObjectThroughPoint`, `ConstructPlanesBisectTwoPlanes`, `ConstructPlanesBoundingPointGroup` | Provide the named source objects and resulting planes. Omitted bounding offset and plane edge dimension are zero. |
| `ConstructCirclesFromSurfaceFacesRuntimeSelect`, `ConstructConesFromSurfaceFacesRuntimeSelect`, `ConstructCylindersFromSurfaceFacesRuntimeSelect`, `ConstructLinesFromSurfaceFacesRuntimeSelect`, `ConstructPlanesFromSurfaceFacesRuntimeSelect`, `ConstructSpheresFromSurfaceFacesRuntimeSelect` | These MP steps take no protobuf arguments and select surface faces inside SpatialAnalyzer. They require an attended SA session. |

All of these operations mutate the shared SA application state and have unsafe replay classification. A deadline or worker loss after enqueue can leave completion unknown; inspect the application before deciding whether to issue the command again. Briosa does not provide cross-client workflow isolation.

The mappings above have portable generated-client, fake-worker, default, and registration coverage. Licensed SA execution has **not** been performed for this migration. An opt-in protected integration run should exercise one named spline intersection, one geometry extraction with returned objects, one instrument observation, one bounding-plane construction, and each attended surface-face selector. Record the exact SDK and connected SA versions, input fixture identities, MP result codes, and output retrieval status without publishing proprietary geometry or paths.
