# Relationship creation

`RelationshipOperations` exposes the following typed creation RPCs for this exact SA 2026.1.0529.7 product. Names in the request are SpatialAnalyzer collection, object, item, or point references; they are not file paths. The server validates required references and lists before submitting an MP step. Each call mutates shared application state, so clients should treat an interrupted call as having an unknown completion outcome and should not replay it automatically.

| Family | RPCs |
| --- | --- |
| Dynamic geometry | `MakeDynamicCircleRelationship`, `MakeDynamicEllipseRelationship`, `MakeDynamicLineRelationship`, `MakeDynamicPlaneRelationship`, `MakeDynamicPointRelationship` |
| Frame and geometry | `MakeFrameToFrameRelationship`, `MakeGeometryCompareOnlyRelationship`, `MakeGeometryFitAndCompareToNominalRelationship`, `MakeGeometryFitOnlyRelationship` |
| Groups and objects | `MakeGroupToGroupRelationship`, `MakeGroupToNominalGroupRelationship`, `MakeGroupsToObjectsRelationship`, `MakeObjectToObjectDirectionRelationship`, `MakePointCloudsToObjectsRelationship`, `MakePointsToObjectsRelationship`, `MakePointsToPointsRelationship`, `MakeVectorGroupToVectorGroupRelationship` |
| SA 2026 only | `MakeCloudToSwatchRelationship` |

Dynamic construction modes must be a supported named choice. The `MakeDynamicPlaneRelationship` offset defaults to zero. Geometry fit creation requires at least one point group; `resulting_object_name` and `fit_profile_name` are submitted only when supplied. Group and point-pair relationships require both tolerance and constraint options. The group-to-nominal-group relationship defaults to closest-point matching, a 0.01 proximity threshold, and fit weight 1. Object projection relationships require projection options. The vector-group relationship defaults to opposing polarity. `MakeCloudToSwatchRelationship` defaults to the `Empty` surface-face list and radial/axial offsets of 0.125, -0.125, and 0.125.

These routes have portable typed-mapping, generated-client, registry, and policy tests. They have **not** been exercised against a licensed SA 2026.1.0529.7 installation in this change. Runtime discovery remains the authoritative list of operations admitted by a running server.
