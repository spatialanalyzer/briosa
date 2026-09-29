# Relationship fitting and filtering

Briosa exposes these handwritten `RelationshipOperations` routes for SpatialAnalyzer 2026.1.0529.7:

| Family | RPCs |
| --- | --- |
| Fit and evaluation | `ComputeGeometryRelationshipUncertainties`, `DoRelationshipFit`, `MoveCollectionsByMinimizingRelationships`, `FilterGeometryRelationshipOutlierCloudPoints`, `GenerateGeometryRelationshipSummary`, `ExtractGeometryFromPointClouds` |
| Automatic filtering | `AutoFilterCloudsToNominalGeometry2D`, `AutoFilterCloudsToNominalGeometry3D`, `AutoFilterPointsGroupsCloudsToSurfaceFaces`, `AutoFilterPointsToNominalGeometry3D` |
| Relationship options | `RelationshipWatchWindowTemplate`, `SetObjectToObjectDirectionRelationshipTolerances`, `SetRelationshipOutlierRejectionScalarType`, `SetRelationshipVoxelCloudDisplay`, `SetRelationshipSigmoidalGapFitConstraints` |

Requests use typed relationship, object, point, fit-motion, thinning, and proximity values. Required lists and required choice fields are checked before enqueue. The route keeps the exact MP step labels, setter/getter bindings, output order, and target defaults; in particular `DoRelationshipFit` returns its reference transform, working/world transforms, and objective value, while the outlier filter returns ten metrics. These calls change application state and have unsafe replay semantics, so a lost response after enqueue must not be treated as proof that the command did not run.

2026.1.0529.7_NOTE

Validation: the target server builds and focused fake-worker/generated-client tests pass without SpatialAnalyzer. These tests cover request mapping, invalid input, capability registration, and a generated-client call; they do not establish behavior in a licensed SA session. `SetRelationshipOutlierRejectionScalarType` retains `fixture_validation_pending` until its licensed fixture is available. Real-SA integration remains opt-in in a separately licensed environment.
