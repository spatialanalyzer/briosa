# SA 2026.1.0529.7 extracted MP command inventory

This report summarizes derived facts only. Installed HTML and generated SDK samples remain local evidence and are not committed.

## Evidence

| Source | Files | Records | Aggregate SHA-256 |
| --- | ---: | ---: | --- |
| Installed MP documentation | 1396 | 1302 | `21d20f9cc79c37ca3515d184a5de3d820b8ecabff4a2da4f24977628d79b8d3a` |
| View SDK Code (VB) | 89 | 1360 | `cc12ba5bd8ded0e9af45eecb59c7894b1f19d0e45aa961cebb60c877cc72ef86` |

## Coverage

- Inventory commands: 1412
- Documentation and SDK matched: 1250
- Documentation only: 52
- SDK only: 110
- Ambiguous evidence matches: 0

## Finding counts

| Finding | Count |
| --- | ---: |
| `argument_name_text_difference` | 382 |
| `conflicting_documented_ordinal` | 32 |
| `direction_disagreement_sdk_getter_observed` | 42 |
| `direction_disagreement_sdk_setter_observed` | 4 |
| `documentation_command_missing` | 110 |
| `missing_input_arguments_section` | 4 |
| `missing_return_arguments_section` | 11 |
| `missing_returned_status_section` | 1 |
| `mp_step_text_difference` | 80 |
| `not_documented` | 706 |
| `sdk_argument_not_documented` | 288 |
| `sdk_getter_ambiguous` | 3 |
| `sdk_getter_missing` | 218 |
| `sdk_setter_missing` | 480 |
| `sdk_setter_unavailable` | 224 |
| `sdk_step_missing` | 52 |
| `unparsed_documentation_argument_row` | 10 |

## Metadata gaps requiring review or Hexagon input

- The installed command reference is useful evidence but is not an authoritative machine-readable contract.
- Generated SDK sample values do not establish whether inputs are required or whether sample values are meaningful defaults.
- `NOT_SUPPORTED` establishes that View SDK Code has no binding; it does not establish that a generic or undocumented SDK binding is safe.
- Setter/getter presence is compared with documented direction, but mismatches remain unresolved evidence rather than silently corrected metadata.
- No compatibility or semantic equivalence is inferred for any other SpatialAnalyzer release.

## Commands with findings

The inventory JSON contains exact argument-level evidence. This table intentionally excludes vendor prose and raw SDK code.

| Inventory key | MP step | Findings |
| --- | --- | --- |
| documentation:AnalysisOperations/AngleBetweenTwoPlanesNormals.htm | Angle Between Two Planes’ normals | `mp_step_text_difference` |
| documentation:AnalysisOperations/ComputeGroupToGroupOrientation.htm | Compute Group to Group Orientation (Rx, Ry, Rz) | `direction_disagreement_sdk_getter_observed`, `mp_step_text_difference`, `sdk_setter_missing` |
| documentation:AnalysisOperations/Coordinate.htm | Coordinate | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:AnalysisOperations/FitGeometryToPointGroup.htm | Fit Geometry to Point Group | `argument_name_text_difference` |
| documentation:AnalysisOperations/FitGeometryToPointGroupProjected.htm | Fit Geometry to Point Group Projected to Plane | `argument_name_text_difference` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakeCircleFitProfile.htm | Make Circle Fit Profile | `argument_name_text_difference` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakeConeFitProfile.htm | Make Cone Fit Profile | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakeCylinderFitProfile.htm | Make Cylinder Fit Profile | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `unparsed_documentation_argument_row` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakeEllipseFitProfile.htm | Make Ellipse Fit Profile | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakeLineFitProfile.htm | Make Line Fit Profile | `argument_name_text_difference`, `conflicting_documented_ordinal` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakeParaboloidFitProfile.htm | Make Paraboloid Fit Profile | `argument_name_text_difference` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakePlaneFitProfile.htm | Make Plane Fit Profile | `argument_name_text_difference` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakeSlotFitProfile.htm | Make Slot Fit Profile | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:AnalysisOperations/GeometryFitProfiles/MakeSphereFitProfile.htm | Make Sphere Fit Profile | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:AnalysisOperations/GetCoordinateForI-thPoint.htm | Get Coordinate for i-th Point in Point Set | `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/GetCylinderProperties.htm | Get Cylinder Properties | `not_documented`, `sdk_argument_not_documented` |
| documentation:AnalysisOperations/GetI-thObjectFromCollectionIterator.htm | Get i-th Object From Collection Object Name Ref List (Iterator) | `sdk_setter_unavailable` |
| documentation:AnalysisOperations/GetI-thPointFromGroup.htm | Get i-th Point From Group | `argument_name_text_difference`, `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/GetI-thPointNameFromPointNameIterator.htm | Get i-th Point Name From Point Name Ref List (Iterator) | `sdk_setter_unavailable` |
| documentation:AnalysisOperations/GetI-thReportRefList.htm | Get i-th Report Ref List | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:AnalysisOperations/GetI-thStringFromStringIterator.htm | Get i-th String From String Ref List (Iterator) | `sdk_setter_unavailable` |
| documentation:AnalysisOperations/GetObjectReportingFrame.htm | Get Object Reporting Frame | `missing_return_arguments_section`, `missing_returned_status_section`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing`, `unparsed_documentation_argument_row` |
| documentation:AnalysisOperations/GetPointCoordinateCylindrical.htm | Get Point Coordinate (Cylindrical) | `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/GetPointCoordinatePolar.htm | Get Point Coordinate (Polar) | `sdk_getter_missing` |
| documentation:AnalysisOperations/GetPointTolerance.htm | Get Point Tolerance | `conflicting_documented_ordinal` |
| documentation:AnalysisOperations/GetSlotProperties.htm | Get Slot Properties | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/GetSurfacePhysicalStats.htm | Get Surface Physical Stats | `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:AnalysisOperations/GetTimestampForI-thFrame.htm | Get Timestamp for i-th Frame in Frame Set | `conflicting_documented_ordinal` |
| documentation:AnalysisOperations/GetTimestampForI-thPoint.htm | Get Timestamp for i-th Point in Point Set | `conflicting_documented_ordinal` |
| documentation:AnalysisOperations/GetTransformForI-thFrame.htm | Get Transform for i-th Frame in Frame Set | `conflicting_documented_ordinal` |
| documentation:AnalysisOperations/GetVectorGroupColorizationOptions.htm | Get Vector Group Colorization Options | `sdk_getter_missing` |
| documentation:AnalysisOperations/GetVectorGroupDisplayAttributes.htm | Get Vector Group Display Attributes | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/GetVectorGroupProperties.htm | Get Vector Group Properties | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/GroupToSurfaceFit.htm | Group To Surface Fit | `argument_name_text_difference` |
| documentation:AnalysisOperations/PipeRelationships/GetPipeRelationshipCut.htm | Get Pipe Relationship Cut Status | `argument_name_text_difference` |
| documentation:AnalysisOperations/PipeRelationships/GetPipeRelationshipProperties.htm | Get Pipe Relationship Properties | `conflicting_documented_ordinal`, `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:AnalysisOperations/PipeRelationships/GetPipeRelationshipWeights.htm | Get Pipe Relationship Weights | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/PipeRelationships/MakePipeRelationshipCut.htm | Make pipe Relationship Cut | `conflicting_documented_ordinal`, `mp_step_text_difference` |
| documentation:AnalysisOperations/PipeRelationships/PipeRelationshipForceCut.htm | Pipe Relationship Force Cut to Frame | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented` |
| documentation:AnalysisOperations/PipeRelationships/SetPipeRelationshipWeights.htm | Set Pipe Relationship Weights | `argument_name_text_difference` |
| documentation:AnalysisOperations/RelationshipAttributes/GeomRelationshipIgnoreInput.htm | Geom Relationship Ignore Input Points | `sdk_setter_unavailable` |
| documentation:AnalysisOperations/RelationshipAttributes/GetGeomRelationshipAuto.htm | Get Geom Relationship Auto Vectors | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/RelationshipAttributes/GetGeomRelationshipCriteria.htm | Get Geom Relationship Criteria | `argument_name_text_difference` |
| documentation:AnalysisOperations/RelationshipAttributes/GetGeomRelationshipPointList.htm | Get Geom Relationship Point List | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:AnalysisOperations/RelationshipAttributes/GetRelationshipProjection.htm | Get Relationship Projection Options | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/RelationshipAttributes/GetRelationshipReportingFrame.htm | Get Relationship Reporting Frame | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:AnalysisOperations/RelationshipAttributes/GetRelationshipTolerance.htm | Get Relationship Tolerance (Vector Type) | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:AnalysisOperations/RelationshipAttributes/SetGeomRelationshipAuto.htm | Set Geom Relationship Auto Vectors Nominal (AVN) | `not_documented`, `sdk_argument_not_documented` |
| documentation:AnalysisOperations/RelationshipAttributes/SetGeomRelationshipAutoMeasureNom.htm | Set Geom Relationship Auto Measure Nominal Feature | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:AnalysisOperations/RelationshipAttributes/SetGeomRelationshipCardinal.htm | Set Geom Relationship Cardinal Points | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:AnalysisOperations/RelationshipAttributes/SetGeomRelationshipNominal.htm | Set Geom Relationship Nominal Geometry | `argument_name_text_difference` |
| documentation:AnalysisOperations/RelationshipAttributes/SetGeomRelationshipNominalAvg.htm | Set Geom Relationship Nominal Avg Point | `argument_name_text_difference` |
| documentation:AnalysisOperations/RelationshipAttributes/SetRelationshipAutoVectors.htm | Set Relationship Auto Vectors Fit (AVF) | `not_documented`, `sdk_argument_not_documented` |
| documentation:AnalysisOperations/RelationshipAttributes/SetRelationshipVoxelCloud.htm | Set Relationship Voxel Cloud Display | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/GetRelSigmoidConstraints.htm | Get Relationship Sigmoid Gap Fit Constraints | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/GetRelationshipFitConstraints.htm | Get Relationship Fit Constraints (Scalar Type) | `not_documented`, `sdk_argument_not_documented` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/GetRelationshipOutlierRejection.htm | Get Relationship Outlier Rejection (Scalar Type) | `sdk_getter_missing` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/MakeOutlierRejectionOptions.htm | Make Outlier Rejection Options | `sdk_getter_missing` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/MakeSymmetricOutlierRejection.htm | Make Symmetric Outlier Rejection Options | `sdk_getter_missing` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/RejectionScalarType.htm | Rejection (Scalar Type) | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/SetObjectToObjectDirectionRelationship.htm | Set Object to Object Direction Relationship Tolerance | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/SetRelSigmoidConstraints.htm | Set Relationship Sigmoidal Gap Fit Constraints | `sdk_setter_missing` |
| documentation:AnalysisOperations/RelationshipAttributesScalarTypes/SetRelationshipOutlierRejectionScalarType.htm | Set Relationship Outlier Rejection (Scalar Type) | `sdk_setter_unavailable` |
| documentation:AnalysisOperations/RemoveI-thStringFromString.htm | Remove i-th String from String Ref List | `mp_step_text_difference` |
| documentation:AnalysisOperations/SetInwardPositiveNormal.htm | Set Inward Positive Normal | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:AnalysisOperations/SetObjectReportingFrame.htm | Set Object Reporting Frame | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:AnalysisOperations/SetPointProperties.htm | Set Point Properties | `not_documented`, `sdk_argument_not_documented` |
| documentation:AnalysisOperations/SortPointGroupInDatabase.htm | Sort Point Group in Database | `sdk_setter_unavailable` |
| documentation:CloudMeshOps/CloudFilters/DeleteCloudPointsByXYZ.htm | Delete Cloud Points by X Y Z Range | `sdk_setter_unavailable` |
| documentation:CloudMeshOps/CloudFilters/FilterCloudsToGroup.htm | Filter Clouds to Group | `argument_name_text_difference` |
| documentation:CloudMeshOps/CloudFilters/FilterCloudsToPlane.htm | Filter Clouds to Plane | `argument_name_text_difference` |
| documentation:CloudMeshOps/CloudFilters/FilterCloudsToSurface.htm | Filter Clouds to Surface | `argument_name_text_difference` |
| documentation:CloudMeshOps/CloudFilters/FilterCloudsToVectorGroups.htm | Filter Clouds to Vector Groups - Resolve Points | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented` |
| documentation:CloudMeshOps/CloudFilters/RGBCloudPointFilter.htm | RGB Cloud Point Filter | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:CloudMeshOps/CrossSections/EnableDisableCloudCross.htm | Enable/Disable Cloud Cross Sections | `argument_name_text_difference` |
| documentation:CloudMeshOps/GetCloudPointCount.htm | Get Cloud Point Count | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:CloudMeshOps/MeshOperations/GenerateGeneralMesh.htm | Generate General Mesh | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:CloudMeshOps/MeshOperations/GenerateRegenerateCoarseMesh.htm | Generate/Regenerate Coarse Mesh | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:CloudMeshOps/NewRasterScanEdgeInspection.htm | New Raster Scan Edge Inspection | `argument_name_text_difference` |
| documentation:CloudMeshOps/RasterScanEdgeInspection.htm | Raster Scan Edge Inspection | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:CloudMeshOps/ResetCloudBoundingBox.htm | Reset Cloud Bounding Box | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing`, `sdk_setter_missing` |
| documentation:CloudMeshOps/SetCloudDefaultClippingPlane.htm | Set Cloud Default Clipping Plane | `sdk_setter_unavailable` |
| documentation:ConstructionOperations/B-Splines/ConstructB-SplineFitOptions.htm | Construct B-Spline Fit Options | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing`, `sdk_setter_unavailable` |
| documentation:ConstructionOperations/B-Splines/ConstructB-SplineFromIntersectionofPlaneandMesh.htm | Construct B-Spline From Intersection of Plane and Mesh | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ConstructionOperations/B-Splines/ConstructB-SplineFromPointSets.htm | Construct B-Spline From Point Sets | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ConstructionOperations/B-Splines/ConstructB-SplineFromSeveralB-Splines.htm | Construct B-Spline From Several B-Splines | `argument_name_text_difference` |
| documentation:ConstructionOperations/B-Splines/ConstructB-SplinesFromLines.htm | Construct B-Splines From Lines | `argument_name_text_difference` |
| documentation:ConstructionOperations/B-Splines/ConstructB-SplinesFromSurfaces.htm | Construct B-Splines From Surfaces | `argument_name_text_difference` |
| documentation:ConstructionOperations/Callouts/AddACalloutViewToCallout.htm | Add a Callout View to Callout View Ref List | `sdk_setter_unavailable` |
| documentation:ConstructionOperations/Callouts/CreateMinMaxVectorGroup.htm | Create Min/Max Vector Group Callout | `argument_name_text_difference` |
| documentation:ConstructionOperations/Callouts/CreatePointCallout.htm | Create Point Callout | `argument_name_text_difference` |
| documentation:ConstructionOperations/Callouts/CreatePointComparisonCallout.htm | Create Point Comparison Callout | `argument_name_text_difference` |
| documentation:ConstructionOperations/Callouts/CreateVectorCallout.htm | Create Vector Callout | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/Callouts/GetI-thCalloutPosition.htm | Get I-th Callout Position in Callout View | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/Callouts/GetI-thCalloutViewFrom.htm | Get i-th Callout View From Callout View Ref List | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing`, `sdk_setter_unavailable` |
| documentation:ConstructionOperations/Callouts/GetNumberOfCalloutViews.htm | Get Number of Callout Views in Callout View Ref List | `sdk_setter_unavailable` |
| documentation:ConstructionOperations/Callouts/GetNumberOfCalloutsIn.htm | Get Number of Callouts in Callout View | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/Callouts/MakeACalloutViewRefList.htm | Make a Callout View Ref List | `sdk_getter_missing` |
| documentation:ConstructionOperations/Callouts/MakeACalloutViewRefListWildCard.htm | Make a Callout View Ref List - WildCard Selection | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/Callouts/SetDefaultCalloutViewProperties.htm | Set Default Callout View Properties | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/Callouts/SetI-thCalloutPosition.htm | Set I-th Callout Position in Callout View | `argument_name_text_difference` |
| documentation:ConstructionOperations/Callouts/SortCalloutViewRefList.htm | Sort Callout View Ref List | `sdk_getter_missing`, `sdk_setter_unavailable` |
| documentation:ConstructionOperations/Circles/ConstructCirclesFromSurfaceFaces-RuntimeSelect.htm | Construct Circles From Surface Faces-Runtime Select | `mp_step_text_difference` |
| documentation:ConstructionOperations/Circles/ConstructCirclesLinesFromSurfaces.htm | Construct Circles (Lines) From Surfaces | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_unavailable` |
| documentation:ConstructionOperations/Cones/ConstructConesFromSurfaceFaces-RuntimeSelect.htm | Construct Cones From Surface Faces-Runtime Select | `mp_step_text_difference` |
| documentation:ConstructionOperations/CopyObject.htm | Copy Object | `argument_name_text_difference` |
| documentation:ConstructionOperations/CopyObjects-PointtoPointDelta.htm | Copy Objects - Point to Point Delta | `argument_name_text_difference` |
| documentation:ConstructionOperations/Cylinders/ConstructCylindersFromSurfaceFaces-RuntimeSelect.htm | Construct Cylinders From Surface Faces-Runtime Select | `mp_step_text_difference` |
| documentation:ConstructionOperations/Ellipsoids/ConstructEllipsoid.htm | Construct Ellipsoid | `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/Frames/ConstructFrameOnObject.htm | Construct Frame On Object | `mp_step_text_difference` |
| documentation:ConstructionOperations/Frames/ConstructFramefromPointMeasurement.htm | Construct Frame from Point Measurement Probing Frames | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/Lines/ConstructLine-NormaltoObject.htm | Construct Line - Normal to Object through Point | `argument_name_text_difference` |
| documentation:ConstructionOperations/Lines/ConstructLine-ProjectLinetoObjectReferencePlane.htm | Construct Line - Project Line to Object Reference Plane | `argument_name_text_difference` |
| documentation:ConstructionOperations/Lines/ConstructLineCenterofSlot.htm | Construct Line Center of Slot | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/Lines/ConstructLinesFromSurfaceFaces-RuntimeSelect.htm | Construct Lines From Surface Faces-Runtime Select | `mp_step_text_difference` |
| documentation:ConstructionOperations/MirrorObjects.htm | Mirror Object(s) | `argument_name_text_difference`, `sdk_setter_unavailable` |
| documentation:ConstructionOperations/OtherMPTypes/AppendTwoPointNameRefLists.htm | Append Two Point Name Ref Lists | `mp_step_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/AppendTwoRelationshipRef.htm | Append two Relationship Ref Lists | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/OtherMPTypes/ConvertToEulerAnglesFromFixedAngles.htm | Convert to Euler Angles from Fixed Angles | `sdk_setter_unavailable` |
| documentation:ConstructionOperations/OtherMPTypes/GetCollectionInstrument.htm | Get Collection Instrument Reference List Variable | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ConstructionOperations/OtherMPTypes/MakeACollectionInstrument.htm | Make a Collection Instrument Reference List | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/OtherMPTypes/MakeACollectionInstrumentReferenceListRuntime.htm | Make a Collection Instrument Reference List - Runtime Select | `mp_step_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/MakeACollectionItemName.htm | Make a Collection Item Name from Strings | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/OtherMPTypes/MakeACollectionItemNameReference.htm | Make a Collection Item Name Reference List - Wildcard Selection | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing`, `sdk_setter_missing` |
| documentation:ConstructionOperations/OtherMPTypes/MakeACollectionObjectNameRefListColor.htm | Make a Collection Object Name Ref List - By Type and Color | `conflicting_documented_ordinal` |
| documentation:ConstructionOperations/OtherMPTypes/MakeACollectionObjectNameReferenceListRuntime.htm | Make a Collection Object Name Reference List - Runtime Select | `mp_step_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/MakeACollectionObjectNameReferenceListWildCard.htm | Make a Collection Object Name Reference List - WildCard Selection | `mp_step_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/MakeAIntegerFromString.htm | Make a Integer From String | `mp_step_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/MakeARelationshipReferenceListRuntime.htm | Make a Relationship Reference ListRuntime Selection | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ConstructionOperations/OtherMPTypes/MakeARelationshipReferenceListWildCard.htm | Make a Relationship Reference List-WildCard Selection | `mp_step_text_difference`, `sdk_setter_unavailable` |
| documentation:ConstructionOperations/OtherMPTypes/MakeAReportItemsRefList.htm | Make a Report Items Ref List | `direction_disagreement_sdk_setter_observed`, `sdk_getter_missing` |
| documentation:ConstructionOperations/OtherMPTypes/MakeAString.htm | Make a String | `argument_name_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/MakeAStringFromAString.htm | Make a String From A String Ref List | `mp_step_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/MakeAnEventReferenceList.htm | Make an Event Reference List-Wildcard Selection | `mp_step_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/MakeAxisIdentifierFromString.htm | Make Axis Identifier from String | `sdk_getter_missing` |
| documentation:ConstructionOperations/OtherMPTypes/MakeProjectionOptions.htm | Make Projection Options | `sdk_getter_missing`, `sdk_setter_unavailable` |
| documentation:ConstructionOperations/OtherMPTypes/MakeStringFromDouble.htm | Make String from Double | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/OtherMPTypes/MakeStringFromInteger.htm | Make String from Integer | `argument_name_text_difference` |
| documentation:ConstructionOperations/OtherMPTypes/MakeUDPSettings.htm | Make UDP Settings | `argument_name_text_difference`, `sdk_getter_missing` |
| documentation:ConstructionOperations/OtherMPTypes/SetCollectionInstrument.htm | Set Collection Instrument Reference List Variable | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ConstructionOperations/OtherMPTypes/SubtractTwoPointNameRef.htm | Subtract Two Point Name Ref Lists | `mp_step_text_difference` |
| documentation:ConstructionOperations/Planes/ConstructPlane-Bisect2Planes.htm | Construct Plane, Bisect 2 Planes | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ConstructionOperations/Planes/ConstructPlane-NormaltoObject-ThroughPoint.htm | Construct Plane, Normal to Object, Through Point | `argument_name_text_difference` |
| documentation:ConstructionOperations/Planes/ConstructPlanes-BoundingPointGroup.htm | Construct Planes, Bounding Point Group | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/Planes/ConstructPlanesFromSurfaceFaces-RuntimeSelect.htm | Construct Planes From Surface Faces-Runtime Select | `mp_step_text_difference` |
| documentation:ConstructionOperations/PointClouds/ConstructCrossSectionCloud-UserSelect.htm | Construct Cross Section Cloud - User Select | `sdk_setter_missing` |
| documentation:ConstructionOperations/PointClouds/ConstructPointCloudLimitingProbingDirections.htm | Construct Point Cloud Limiting Probing Directions | `argument_name_text_difference` |
| documentation:ConstructionOperations/PointClouds/ConstructPointCloudfromExistingClouds.htm | Construct Point Cloud from Existing Clouds | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/PointClouds/ConstructPointCloudsfromVisibleCloudPoints.htm | Construct Point Clouds from Visible Cloud Points | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ConstructionOperations/PointClouds/ExtractSphereCentersfromPointCloud.htm | Extract Sphere Centers from Point Cloud | `conflicting_documented_ordinal` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointAtIntersectionOfB-SplineAndSurfaces.htm | Construct Point at Intersection of B-Spline and Surfaces | `mp_step_text_difference` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointAtIntersectionOfPlanes.htm | Construct Point at Intersection of Planes | `missing_input_arguments_section`, `not_documented`, `sdk_argument_not_documented` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointAtIntersectionOfTwoLines.htm | Construct Point at Intersection of Two Lines | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointGroupsfromVectorGroups.htm | Construct Point Groups from Vector Groups | `missing_input_arguments_section`, `not_documented`, `sdk_argument_not_documented` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointfromCloudPoint-RuntimeSelect.htm | Construct Point from Cloud Point - Runtime Select | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointsLayoutonGrid.htm | Construct Points Layout on Grid | `conflicting_documented_ordinal` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointsSubsetwithGreatestSpacing.htm | Construct Points Subset with Greatest Spacing | `mp_step_text_difference` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointsatProjectionOnMeshAlongDirection.htm | Construct Points at Projection On Mesh Along Direction | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointsonCurves.htm | Construct Points on Curves Using Max Chordal Deviation | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/PointsandGroups/ConstructPointsonObjectsVertices.htm | Construct Points on Object’s Vertices | `mp_step_text_difference` |
| documentation:ConstructionOperations/PointsandGroups/CreateHiddenPoint.htm | Create Hidden Point | `argument_name_text_difference` |
| documentation:ConstructionOperations/PointsandGroups/CreateHiddenPointRod.htm | Create Hidden Point Rod | `argument_name_text_difference`, `conflicting_documented_ordinal` |
| documentation:ConstructionOperations/PointsandGroups/DeleteHiddenpointRod.htm | Delete Hidden point Rod | `argument_name_text_difference`, `mp_step_text_difference` |
| documentation:ConstructionOperations/PointsandGroups/GetGradientAtProjectedPointOnSurfaceEdge.htm | Get Gradient At Projected Point On Surface Edge | `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/PointsandGroups/SetPointPositioninWorkingCoordinates.htm | Set Point Position in Working Coordinates | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/ScaleBars/ConstructScaleBar.htm | Construct Scale Bar | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/Spheres/ConstructSpheresFromSurfaceFaces-RuntimeSelect.htm | Construct Spheres From Surface Faces-Runtime Select | `mp_step_text_difference` |
| documentation:ConstructionOperations/Surfaces/ConstructGeometryFromSurfaces.htm | Construct Geometry From Surfaces | `sdk_setter_unavailable` |
| documentation:ConstructionOperations/Surfaces/ConstructSurfaceByDissecting.htm | Construct Surface by Dissecting Surface(s) | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/Surfaces/ConstructSurfaceByOffsetting.htm | Construct Surface by offsetting a surface | `argument_name_text_difference`, `mp_step_text_difference` |
| documentation:ConstructionOperations/Surfaces/ConstructSurfaceFitFromNominal.htm | Construct Surface Fit From Nominal Surfaces and Actual Data | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/Surfaces/ConstructSurfaceFromAnnotation.htm | Construct Surface from Annotation Links | `mp_step_text_difference` |
| documentation:ConstructionOperations/Surfaces/ConstructSurfaceFromB-Splines.htm | Construct Surface From B-Splines | `mp_step_text_difference` |
| documentation:ConstructionOperations/Surfaces/ConstructSurfaceFromCylinder.htm | Construct Surface From Cylinder | `not_documented`, `sdk_argument_not_documented` |
| documentation:ConstructionOperations/Surfaces/ConstructSurfacesByDissecting.htm | Construct Surfaces by Dissecting Surfaces from Ref List | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ConstructionOperations/VectorGroups/ConstructAVectorGroupFromVectorNameRefList.htm | Construct a Vector Group From Vector Name Ref List | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ConstructionOperations/VectorGroups/ConstructAVectorInWorkingBeginDelta.htm | Construct a Vector in Working Coordinates (Begin/Delta) | `argument_name_text_difference`, `mp_step_text_difference` |
| documentation:ConstructionOperations/VectorGroups/ConstructAVectorInWorkingMag.htm | Construct a Vector in Working Coordinates (Begin/Direction/Mag.) | `argument_name_text_difference`, `mp_step_text_difference` |
| documentation:ConstructionOperations/VectorGroups/ConstructVectorsWildCard.htm | Construct Vectors WildCard Selection | `argument_name_text_difference`, `sdk_setter_unavailable` |
| documentation:ConstructionOperations/VectorGroups/MakeAVectorNameRefList.htm | Make a Vector Name Ref List From a Vector Group | `missing_input_arguments_section`, `not_documented`, `sdk_argument_not_documented` |
| documentation:ConstructionOperations/VectorGroups/MakeVectorNamesUniqueIn.htm | Make Vector Names Unique in Vector Group | `missing_return_arguments_section` |
| documentation:Dimensions/AddADimensionToDimension.htm | Add a Dimension to Dimension Ref List | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:Dimensions/CreateDiameterDimension.htm | Create Diameter Dimension | `not_documented`, `sdk_argument_not_documented` |
| documentation:Dimensions/CreateObjectToObjectDimension.htm | Create Object to Object Dimension | `not_documented`, `sdk_argument_not_documented` |
| documentation:Dimensions/CreatePointToObjectDimension.htm | Create Point to Object Dimension | `not_documented`, `sdk_argument_not_documented` |
| documentation:Dimensions/CreatePointToPointDimension.htm | Create Point to Point Dimension | `not_documented`, `sdk_argument_not_documented` |
| documentation:Dimensions/CreateRadiusDimension.htm | Create Radius Dimension | `not_documented`, `sdk_argument_not_documented` |
| documentation:Dimensions/GetDimensionValue.htm | Get Dimension Value | `argument_name_text_difference` |
| documentation:Dimensions/GetI-thDimensionFromDimension.htm | Get i-th Dimension From Dimension Ref List | `sdk_setter_unavailable` |
| documentation:Dimensions/GetI-thDimensionFromDimensionIterator.htm | Get i-th Dimension From Dimension Ref List (Iterator) | `argument_name_text_difference`, `conflicting_documented_ordinal`, `sdk_setter_unavailable` |
| documentation:Dimensions/GetNumberOfDimensionsIn.htm | Get Number of Dimensions in Dimension Ref List | `sdk_setter_unavailable` |
| documentation:Dimensions/MakeADimensionRefListCollection.htm | Make a Dimension Ref List from a Collection | `sdk_getter_missing` |
| documentation:Dimensions/MakeADimensionRefListSelection.htm | Make a Dimension Ref List- WildCard Selection | `argument_name_text_difference`, `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:Dimensions/SetCommonPropertiesToDimensions.htm | Set Common Properties to Dimensions | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing`, `sdk_setter_unavailable` |
| documentation:Dimensions/SetDiameterDimensionProperties.htm | Set Diameter Dimension Properties | `sdk_setter_unavailable` |
| documentation:Dimensions/SetObjectToObjectDimension.htm | Set Object to Object Dimension Properties | `sdk_setter_unavailable` |
| documentation:Dimensions/SetPointToObjectDimension.htm | Set Point to Object Dimension Properties | `sdk_setter_unavailable` |
| documentation:Dimensions/SetPointToPointDimension.htm | Set Point to Point Dimension Properties | `sdk_setter_unavailable` |
| documentation:Dimensions/SetRadiusDimensionProperties.htm | Set Radius Dimension Properties | `sdk_setter_unavailable` |
| documentation:Events/GetI-thEventFromEventIterator.htm | Get i-th Event From Event Ref List (Iterator) | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_unavailable` |
| documentation:ExcelDirectConnect/GetWorkbookAddress.htm | Get Workbook Address | `sdk_getter_missing` |
| documentation:ExcelDirectConnect/Read/ReadDouble.htm | Read Double | `not_documented`, `sdk_argument_not_documented` |
| documentation:ExcelDirectConnect/Read/ReadInteger.htm | Read Integer | `not_documented`, `sdk_argument_not_documented` |
| documentation:ExcelDirectConnect/Read/ReadString.htm | Read String | `not_documented`, `sdk_argument_not_documented` |
| documentation:FileOperations/ASIIDataFileOperations/ReadASCIILineIterator.htm | Read ASCII Line (Iterator) | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_unavailable` |
| documentation:FileOperations/BrowseForDirectory.htm | Browse For Directory | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:FileOperations/BrowseForFile.htm | Browse For File | `argument_name_text_difference`, `mp_step_text_difference`, `sdk_setter_unavailable` |
| documentation:FileOperations/CopyDirectory.htm | Copy Directory | `sdk_setter_unavailable` |
| documentation:FileOperations/DatabaseOperations/GetfromODBCDatabase.htm | Get from ODBC Database | `unparsed_documentation_argument_row` |
| documentation:FileOperations/DatabaseOperations/PuttoODBCDatabase.htm | Put to ODBC Database | `unparsed_documentation_argument_row` |
| documentation:FileOperations/DatashareOperations/LoadHTMLForm.htm | Load HTML Form | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/DatashareOperations/LoadHTMLForminEdgeBrowser.htm | Load HTML Form in Edge Browser | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/DatashareOperations/SaveDataShareFile.htm | Save DataShare File | `unparsed_documentation_argument_row` |
| documentation:FileOperations/DeleteDirectory.htm | Delete Directory | `sdk_setter_unavailable` |
| documentation:FileOperations/DirectoryExistence.htm | Directory Existence | `sdk_setter_unavailable` |
| documentation:FileOperations/FileExport/ExportASCIIFrameSet.htm | Export ASCII Frame Set | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FileExport/ExportASCIIPointClouds.htm | Export ASCII Point Clouds | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FileExport/ExportASCIIPointSets.htm | Export ASCII Point Sets | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:FileOperations/FileExport/ExportASIIPoints.htm | Export ASII Points | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:FileOperations/FileExport/ExportDXF.htm | Export DXF | `not_documented`, `sdk_argument_not_documented` |
| documentation:FileOperations/FileExport/ExportHiddenPointBarXMLFile.htm | Export Hidden Point Bar XML File | `sdk_setter_missing` |
| documentation:FileOperations/FileExport/ExportIGESFileEntireModel.htm | Export IGES File - Entire Model | `mp_step_text_difference` |
| documentation:FileOperations/FileExport/ExportPTXPointClouds.htm | Export PTX Point Clouds | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FileExport/ExportVDAFSFileEntireModel.htm | Export VDA/FS File - Entire Model | `mp_step_text_difference` |
| documentation:FileOperations/FileExport/ExportVectorContainerToASCIIFile.htm | Export Vector Container to ASCII File | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FileExport/ExportVectorContainerToExcelFile.htm | Export Vector Container to Excel File | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FileImport/DirectCADAccess.htm | Direct CAD Access | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FileImport/ImportASCIIPredefinedFormats.htm | Import ASCII: Predefined Formats | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented` |
| documentation:FileOperations/FileImport/ImportE57File.htm | Import E57 File | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FileImport/ImportFileAsEmbedded File.htm | Import File As Embedded File | `mp_step_text_difference` |
| documentation:FileOperations/FileImport/ImportFileAsPicture.htm | Import File as Picture | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FileImport/ImportMPFileAsEmbeddedMP.htm | Import MP File As Embedded MP | `mp_step_text_difference` |
| documentation:FileOperations/FileImport/ImportSAFile.htm | Import SA File | `argument_name_text_difference` |
| documentation:FileOperations/FileImport/ImportSTLFile.htm | Import STL File | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/FindSubDirectoriesInDirectory.htm | Find Sub-Directories in Directory | `argument_name_text_difference` |
| documentation:FileOperations/GetDirectoryAndFilename.htm | Get Directory and Filename from Path | `not_documented`, `sdk_argument_not_documented` |
| documentation:FileOperations/JSON/CloseJSONFile.htm | Close JSON File | `direction_disagreement_sdk_setter_observed`, `sdk_getter_missing` |
| documentation:FileOperations/JSON/GetJSONArraySize.htm | Get JSON Array Size | `conflicting_documented_ordinal` |
| documentation:FileOperations/JSON/GetJSONDoubleValue.htm | Get JSON Double Value | `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:FileOperations/JSON/GetJSONIntegerValue.htm | Get JSON Integer Value | `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:FileOperations/JSON/GetJSONObjectValue.htm | Get JSON Object Value | `conflicting_documented_ordinal` |
| documentation:FileOperations/JSON/GetJSONStringValue.htm | Get JSON String Value | `conflicting_documented_ordinal` |
| documentation:FileOperations/JSON/OpenJSONFile.htm | Open JSON File | `direction_disagreement_sdk_getter_observed`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/MakeDirectory.htm | Make Directory | `sdk_getter_missing` |
| documentation:FileOperations/QDASFileExport/AddK-FieldtoQDASDataList.htm | Add K-Field to QDAS Data List | `sdk_setter_unavailable` |
| documentation:FileOperations/QDASFileExport/ExportQDASCharacteristics.htm | Export QDAS Characteristics | `argument_name_text_difference`, `sdk_setter_unavailable` |
| documentation:FileOperations/QDASFileExport/GetQDASCatalogEntries.htm | Get QDAS Catalog Entries | `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:FileOperations/QDASFileExport/ImportQDasCatalogFile.htm | Import QDas Catalog File | `mp_step_text_difference` |
| documentation:FileOperations/QDASFileExport/PrepareQDASDataList.htm | Prepare QDAS Data List | `sdk_setter_unavailable` |
| documentation:FileOperations/QDASFileExport/SetK-FieldfromQDASCatalog.htm | Set K-Field from QDAS Catalog | `sdk_getter_missing` |
| documentation:FileOperations/RunAnotherProgram.htm | Run Another Program | `argument_name_text_difference`, `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:FileOperations/RunPowershellScript.htm | Run Powershell Script | `argument_name_text_difference` |
| documentation:FileOperations/SaveAs.htm | Save As | `mp_step_text_difference` |
| documentation:FileOperations/SetBackupDirectory.htm | Set Backup Directory | `sdk_setter_unavailable` |
| documentation:FileOperations/SetDataRootDirectory.htm | Set Data Root Directory | `sdk_setter_unavailable` |
| documentation:FileOperations/SetReportsDirectory.htm | Set Reports Directory | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/SetTemplatesDirectory.htm | Set Templates Directory | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/SetWorkingDirectory.htm | Set Working Directory | `sdk_setter_unavailable` |
| documentation:FileOperations/VerifyGeneralFileExists.htm | Verify General File Exists | `argument_name_text_difference`, `sdk_setter_unavailable` |
| documentation:FileOperations/VerifyMPFileExists.htm | Verify MP File Exists | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:FileOperations/XML/SetXMLAttribute.htm | Set XML Attribute | `unparsed_documentation_argument_row` |
| documentation:GDT/DatumAlignment.htm | Datum Alignment | `not_documented`, `sdk_argument_not_documented` |
| documentation:GDT/EnableDisableDatumAlignment.htm | Enable/Disable Datum Alignment for Feature Check | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented` |
| documentation:GDT/EvaluateFeatureCheck.htm | Evaluate Feature Check | `not_documented`, `sdk_argument_not_documented` |
| documentation:GDT/EvaluateFeatureChecks.htm | Evaluate Feature Checks | `not_documented`, `sdk_argument_not_documented` |
| documentation:GDT/FeatureInspectionAutoFilter.htm | Feature Inspection Auto Filter | `argument_name_text_difference` |
| documentation:GDT/GDTConstruct/MakeAFeatureCheckReference.htm | Make a Feature Check Reference List - WildCard Selection | `mp_step_text_difference` |
| documentation:GDT/GDTConstruct/MakeAnnotationRefListFrom.htm | Make Annotation Ref List from a Collection | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:GDT/GDTConstruct/MakeAnnotationRefListWildCard.htm | Make Annotation Ref List - WildCard Selection | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:GDT/GDTConstruct/MakeGDTDatumAnnotation.htm | Make GD&T Datum Annotation | `argument_name_text_difference` |
| documentation:GDT/GDTConstruct/MakeGDTFeatureCheckAnnotation.htm | Make GD&T Feature Check Annotation | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:GDT/GetFeatureCheckDatumReferences.htm | Get Feature Check Datum References | `conflicting_documented_ordinal` |
| documentation:GDT/GetFeatureCheckReporting.htm | Get Feature Check Reporting Options | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:GDT/GetGDTExtendedOps.htm | Get GD&T Extended Options | `sdk_getter_missing` |
| documentation:GDT/GetGDTOptions.htm | Get GD&T Options | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:GDT/GetI-thAnnotationFromAnnotationIterator.htm | Get i-th Annotation From Annotation Ref List (Iterator) | `sdk_setter_unavailable` |
| documentation:GDT/GetI-thDatumFromDatumIterator.htm | Get i-th Datum From Datum Ref List (Iterator) | `sdk_setter_unavailable` |
| documentation:GDT/GetI-thFeatureCheckFromFeatureIterator.htm | Get i-th Feature Check From Feature Check Ref List (Iterator) | `sdk_setter_unavailable` |
| documentation:GDT/SetDatumMeasurements.htm | Set Datum Measurements | `sdk_getter_missing` |
| documentation:GDT/SetFeatureCheckReporting.htm | Set Feature Check Reporting Options | `missing_return_arguments_section`, `sdk_setter_unavailable` |
| documentation:GDT/SetGDTOptions.htm | Set GD&T Options | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:GoogleSheets/GetGoogleSheetsCellAddress.htm | Get Google Sheets Spreadsheet Cell Address | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:GoogleSheets/SetGoogleSheetsCellAddress.htm | Set Google Sheets Spreadsheet Cell Address | `argument_name_text_difference` |
| documentation:InstrumentOperations/APILadar/Set LADARFeatureMeasSphere.htm | Set LADAR Feature Measure Sphere | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/APILadar/SetLADARAutoMeasPoint.htm | Set LADAR Auto Meas Point | `mp_step_text_difference` |
| documentation:InstrumentOperations/APILadar/SetLADARAutoMeasSphere.htm | Set LADAR Auto Meas Sphere | `argument_name_text_difference`, `mp_step_text_difference` |
| documentation:InstrumentOperations/APILadar/SetLADARFeatureMeasCircle.htm | Set LADAR Feature Measure Circle | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/APILadar/SetLADARFeatureMeasCylinder.htm | Set LADAR Feature Measure Cylinder | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/APILadar/SetLADARFeatureMeasSlot.htm | Set LADAR Feature Measure Slot | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/ActivateDeactivateInstrument.htm | Activate/Deactivate Instrument Toolbar | `argument_name_text_difference` |
| documentation:InstrumentOperations/AddAUSMNTemplatedInstrument.htm | Add a USMN Templated Instrument to a USMN Templated Instrument List | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/AddNominalPointToTCPFixture.htm | Add Nominal Point to TCP Fixture | `argument_name_text_difference` |
| documentation:InstrumentOperations/AlignCloudToCAD.htm | Align Cloud to CAD | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/AlignTwoTargetsWithAxis.htm | Align Two Targets with Axis (WCF-X) | `mp_step_text_difference` |
| documentation:InstrumentOperations/AutoCorrespondClosestPoint.htm | Auto-Correspond Closest Point | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/AutoMeasurePoints.htm | Auto Measure Points | `not_documented`, `sdk_argument_not_documented` |
| documentation:InstrumentOperations/AutoMeasureSurfaceVector.htm | Auto-Measure Surface Vector Intersections | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/BuildTarget.htm | ‘Build’ Target | `mp_step_text_difference` |
| documentation:InstrumentOperations/CalculateTCPFixtureUncertainties.htm | Calculate TCP Fixture Uncertainties | `argument_name_text_difference` |
| documentation:InstrumentOperations/Collimation.htm | Collimation | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/ConfigureAndMeasure.htm | Configure and Measure | `argument_name_text_difference` |
| documentation:InstrumentOperations/ConstructMirrorFromPlane.htm | Construct Mirror From Plane | `mp_step_text_difference` |
| documentation:InstrumentOperations/ConstructTCPFixture.htm | Construct TCP Fixture | `direction_disagreement_sdk_getter_observed`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/CreateTemplatedInstrument.htm | Create Templated Instrument (USMN) | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/CribSheetOperations/RunCribSheet.htm | Run Crib Sheet | `argument_name_text_difference` |
| documentation:InstrumentOperations/DeleteMeasurementObservation.htm | Delete Measurement Observation | `argument_name_text_difference` |
| documentation:InstrumentOperations/DisassociateObjectsFrom.htm | Disassociate Objects from Instrument | `sdk_setter_missing` |
| documentation:InstrumentOperations/DissectPointGroups.htm | Dissect Point Groups | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/DriftCheck.htm | Drift Check | `argument_name_text_difference`, `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/EnableDisableFrameSetScanAll.htm | Enable/Disable Frame Set Scan Mode (All Instruments) | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/EnableDisableFrameSetScanBy.htm | Enable/Disable Frame Set Scan Mode (By Instruments) | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/EnableDisablePointSetScan.htm | Enable/Disable Point Set Scan Mode | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/GetCurrentInstrumentPosition.htm | Get Current Instrument Position Update | `argument_name_text_difference` |
| documentation:InstrumentOperations/GetInstrumentBaseUncertainty.htm | Get Instrument Base Uncertainty Covariance Matrix WRT World | `mp_step_text_difference` |
| documentation:InstrumentOperations/GetInstrumentInterfaceResponse.htm | Get Instrument Interface Response Timeout | `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:InstrumentOperations/GetInstrumentPartTemperature.htm | Get Instrument Part Temperature | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:InstrumentOperations/GetInstrumentScaleFactor.htm | Get Instrument Scale Factor | `argument_name_text_difference` |
| documentation:InstrumentOperations/GetInstrumentTargetStatus.htm | Get Instrument Target Status | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/GetInstrumentWeatherSetting.htm | Get Instrument Weather Setting | `argument_name_text_difference` |
| documentation:InstrumentOperations/GetInstrumentXYZUncertainties.htm | Get Instrument XYZ Uncertainties | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/GetLastInstrumentIndex.htm | Get Last Instrument Index | `sdk_getter_ambiguous` |
| documentation:InstrumentOperations/GetObscuredPointsFromInstrument.htm | Get Obscured Points from Instrument | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/GetObservationInfo.htm | Get Observation Info | `argument_name_text_difference` |
| documentation:InstrumentOperations/GetPCMMInstrumentXYZUncertainties.htm | Get PCMM Instrument XYZ Uncertainties | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/GetTargetsMeasuredByInstrument.htm | Get Targets Measured by Instrument | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:InstrumentOperations/GetTrackerEDMTheodolite.htm | Get Tracker/EDM Theodolite Uncertainties | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing`, `sdk_setter_missing` |
| documentation:InstrumentOperations/JumpInstrumentToNewLocation.htm | Jump Instrument to New Location | `mp_step_text_difference` |
| documentation:InstrumentOperations/LaserProjection/ProjectObjects.htm | Project Objects | `argument_name_text_difference` |
| documentation:InstrumentOperations/LocateInstrumentBestFitGroup.htm | Locate Instrument (Best Fit - Group to Group) | `conflicting_documented_ordinal` |
| documentation:InstrumentOperations/LocateInstrumentBestFitNominal.htm | Locate Instrument (Best Fit - Nominal Geometry) | `conflicting_documented_ordinal` |
| documentation:InstrumentOperations/LocateInstrumentRefTie.htm | Locate Instrument (Ref. Tie-In) | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/LocateInstrumentsUSMN.htm | Locate Instruments (USMN) | `argument_name_text_difference` |
| documentation:InstrumentOperations/LocateTemplatedInstruments.htm | Locate Templated Instruments (USMN) | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_unavailable` |
| documentation:InstrumentOperations/MakeAUSMNTemplatedInstrument.htm | Make a USMN Templated Instrument List | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:InstrumentOperations/MakeCollectionObjectName.htm | Make Collection Object Name Ref List from Objects associated with Instrument | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/MakeSurfaceFaceListFrom.htm | Make Surface Face List from Point Proximity | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:InstrumentOperations/Measure.htm | Measure | `argument_name_text_difference` |
| documentation:InstrumentOperations/MoveMeasurementObservation.htm | Move Measurement Observation | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented` |
| documentation:InstrumentOperations/NikonMetrologyLaserRadar/CloudViewerOperations/SendCloudtoSA.htm | Send Cloud to SA | `mp_step_text_difference` |
| documentation:InstrumentOperations/NikonMetrologyLaserRadar/LRAPDISActivateMCMCalibration.htm | LR APDIS Activate MCM Calibration | `conflicting_documented_ordinal` |
| documentation:InstrumentOperations/NikonMetrologyLaserRadar/LRGetMostRecentSNRInfo.htm | LR Get Most Recent SNR Info | `argument_name_text_difference` |
| documentation:InstrumentOperations/NikonMetrologyLaserRadar/LRSelfTest.htm | LR Self Test | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:InstrumentOperations/NikonMetrologyLaserRadar/LRSelfTestLOSep.htm | LR Self Test - LO Sep | `argument_name_text_difference` |
| documentation:InstrumentOperations/ScanWithinPerimeter.htm | Scan within Perimeter | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented` |
| documentation:InstrumentOperations/SetAbsoluteInstrumentScale.htm | Set (absolute) Instrument Scale Factor (CAUTION!) | `argument_name_text_difference` |
| documentation:InstrumentOperations/SetInstrumentBaseUncertaintyWorld.htm | Set Instrument Base Uncertainty Covariance Matrix WRT World | `mp_step_text_difference` |
| documentation:InstrumentOperations/SetInstrumentWeatherSetting.htm | Set Instrument Weather Setting | `argument_name_text_difference` |
| documentation:InstrumentOperations/SetInstrumentXYZUncertainties.htm | Set Instrument XYZ Uncertainties | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:InstrumentOperations/SetObservationCollimation.htm | Set Observation Collimation Shot Options | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/SetObservationMirrorCube.htm | Set Observation Mirror Cube Shot Face | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/SetPCMMInstrumentXYZUncertainties.htm | Set PCMM Instrument XYZ Uncertainties | `argument_name_text_difference`, `missing_return_arguments_section`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/SetProbeOffsetFrameOffline.htm | Set Probe Offset Frame Offline (Select Previously Measured Frame) | `argument_name_text_difference` |
| documentation:InstrumentOperations/SetProbeOffsetFrameOnline.htm | Set Probe Offset Frame Online (Measure Raw Frame) | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/SetTrackerEDMTheodolite.htm | Set Tracker/EDM Theodolite Uncertainties | `missing_return_arguments_section`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/SetXYZReferenceFrameInstrument.htm | Set XYZ Reference Frame Instrument Base Anchor Frame | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/SetmultiplyInstrumentScale.htm | Set (multiply) Instrument Scale Factor (CAUTION!) | `argument_name_text_difference` |
| documentation:InstrumentOperations/ShowHideInstrumentInterface.htm | Show/Hide Instrument Interface | `argument_name_text_difference` |
| documentation:InstrumentOperations/StartInstrumentInterface.htm | Start Instrument Interface | `argument_name_text_difference` |
| documentation:InstrumentOperations/StartTheodoliteInterface.htm | Start Theodolite Interface | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/StopInstrumentInterface.htm | Stop Instrument Interface | `argument_name_text_difference` |
| documentation:InstrumentOperations/SynchronizedMeasurementMaster.htm | Synchronized Measurement (Master/Slave) | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/TransformMultipleInstruments.htm | Transform Multiple Instruments by Delta | `mp_step_text_difference` |
| documentation:InstrumentOperations/VerifyInstrumentConnection.htm | Verify Instrument Connection | `argument_name_text_difference` |
| documentation:InstrumentOperations/WatchClosestPoint.htm | Watch Closest Point | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/WatchInstrument.htm | Watch Instrument | `argument_name_text_difference` |
| documentation:InstrumentOperations/WatchPointToEdge.htm | Watch Point to Edge | `argument_name_text_difference`, `conflicting_documented_ordinal`, `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/WatchPointToObjects.htm | Watch Point to Objects | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/WatchPointToPoint.htm | Watch Point to Point | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:InstrumentOperations/WatchPointToPointWith.htm | Watch Point to Point With View Zooming | `argument_name_text_difference`, `mp_step_text_difference` |
| documentation:InstrumentOperations/WatchWindowTemplate3D.htm | Watch Window Template 3D | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:MPSubroutines/DefineSubroutineInputValues.htm | Define Subroutine Input Values | `unparsed_documentation_argument_row` |
| documentation:MPSubroutines/DefineSubroutineReturnValues.htm | Define Subroutine Return Values | `unparsed_documentation_argument_row` |
| documentation:MPSubroutines/ReturnFromSubroutineNow.htm | Return from Subroutine Now | `unparsed_documentation_argument_row` |
| documentation:MPSubroutines/RunSubroutine.htm | Run Subroutine | `unparsed_documentation_argument_row` |
| documentation:MPTaskOverview/SetTaskItemStatus.htm | Set Task Item Status | `sdk_setter_unavailable` |
| documentation:MSOfficeReportingOperations/AddSectionHeadingToReport.htm | Add Section Heading to Report | `sdk_setter_unavailable` |
| documentation:MSOfficeReportingOperations/AddsAnImageToAnMSOffice.htm | Adds an image to an MS Office report. | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:MSOfficeReportingOperations/MakeReportTable.htm | Make Report Table | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforDouble.htm | Ask for Double | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforInteger.htm | Ask for Integer | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforPointName.htm | Ask for Point Name | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforString.htm | Ask for String | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforStringPullDownVersion.htm | Ask for String (Pull-Down Version) | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforUserDecisionExtended.htm | Ask for User Decision Extended | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforUserDecisionHTML.htm | Ask for User Decision(HTML) | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforUserDecisionPullDownVersion.htm | Ask for User Decision (Pull-Down Version) | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforUserDecisionfromImage.htm | Ask for User Decision from Image | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/AskforUserDecisionfromStrings.htm | Ask for User Decision from Strings | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/CollectionExistenceTest.htm | Collection Existence Test | `argument_name_text_difference`, `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/CreateCounter.htm | Create Counter | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/DecrementCounter.htm | Decrement Counter | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/GoNoGoRangeCheckResults.htm | Go/No Go - Range Check Results | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/IncrementCounter.htm | Increment Counter | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/JumpBasedonRangedStatus.htm | Jump Based on Ranged Status Test | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/JumpToOtherMeasurement.htm | Jump To Other Measurement Plan | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/JumpToStep.htm | Jump To Step | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/ObjectExistenceTest.htm | Object Existence Test | `argument_name_text_difference`, `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/ResetCounter.htm | Reset Counter | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/StepStatusTest.htm | Step Status Test | `sdk_setter_unavailable` |
| documentation:ProcessFlowOperations/WaitforStepstoComplete.htm | Wait for Steps to Complete | `sdk_setter_unavailable` |
| documentation:RelationshipOperations/AutoFilterPointsGroupsClouds.htm | Auto Filter Points/Groups/Clouds to Surface Faces | `argument_name_text_difference` |
| documentation:RelationshipOperations/BuildsAGeometryRelationshipSummaryTable.htm | Builds a geometry relationship summary table from the selected relationships | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:RelationshipOperations/CreatesAGroupsToObjects.htm | Creates a groups to objects relationship. | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:RelationshipOperations/DoRelationshipFit.htm | Do Relationship Fit | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing`, `sdk_setter_missing` |
| documentation:RelationshipOperations/GetI-thRelationshipFromRelationshipIterator.htm | Get i-th Relationship From Relationship Ref List (Iterator) | `sdk_setter_unavailable` |
| documentation:RelationshipOperations/GetObjectsFromPointsTo.htm | Get Objects From Points to Objects Map | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:RelationshipOperations/GetRelationshipAssociated.htm | Get Relationship Associated Data | `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:RelationshipOperations/MakeAutoFilterProximity.htm | Make Auto Filter Proximity Settings | `argument_name_text_difference` |
| documentation:RelationshipOperations/MakeFrameToFrameRelationship.htm | Make Frame to Frame Relationship | `not_documented`, `sdk_argument_not_documented` |
| documentation:RelationshipOperations/MakeGroupToNominalGroup.htm | Make Group to Nominal Group Relationship | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:RelationshipOperations/MakeVectorGroupToVector.htm | Make Vector Group To Vector Group Relationship | `argument_name_text_difference` |
| documentation:RelationshipOperations/MoveCollectionsByMinimizing.htm | Move Collections by Minimizing Relationships | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:RelationshipOperations/RelationshipWatchWindowTemplate.htm | Relationship Watch Window Template | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:RelationshipOperations/SetOptimizationOptions.htm | Set Optimization Options | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:RelationshipOperations/SetOptimizationPerturbation.htm | Set Optimization Perturbation Parameters | `argument_name_text_difference` |
| documentation:RelationshipOperations/SetPointsToPointsRelationship.htm | Set Points to Points Relationship Associated Data | `argument_name_text_difference` |
| documentation:RelationshipOperations/SetVectorGroupToVector.htm | Set Vector Group To Vector Group Cylindrical Zone | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:RelationshipOperations/SetVectorGroupToVectorFactor.htm | Set Vector Group To Vector Group Fit Gradient Factor | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:RelationshipOperations/SetVectorGroupToVectorGroupRelativePolarity.htm | Set Vector Group To Vector Group Relative Polarity | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:RelationshipOperations/SetVectorGroupToVectorWeights.htm | Set Vector Group To Vector Group Fit Weights | `conflicting_documented_ordinal`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:RelationshipOperations/ShowHideRelationshipReport.htm | Show/Hide Relationship Report | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:RelationshipOperations/ShowHideRelationshipWatch.htm | Show/Hide Relationship Watch | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:RelationshipOperations/SortRelationshipRefList.htm | Sort Relationship Ref List | `argument_name_text_difference` |
| documentation:ReportingOperations/AppendItemsToSAReport.htm | Append Items to SA Report | `not_documented`, `sdk_argument_not_documented` |
| documentation:ReportingOperations/CustomReportTables/SetCustomTableCellDouble.htm | Set Custom Table Cell Double | `sdk_setter_unavailable` |
| documentation:ReportingOperations/CustomReportTables/SetCustomTableCellString.htm | Set Custom Table Cell String | `sdk_setter_unavailable` |
| documentation:ReportingOperations/CustomReportTables/SetCustomTableHeaderCell.htm | Set Custom Table Header Cell | `sdk_setter_unavailable` |
| documentation:ReportingOperations/CustomReportTables/SetCustomTableHeaderRow.htm | Set Custom Table Header Row | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing`, `sdk_setter_unavailable` |
| documentation:ReportingOperations/DefineReportTemplate.htm | Define Report Template | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented` |
| documentation:ReportingOperations/GenerateCustomHTMLReport.htm | Generate Custom HTML Report | `sdk_setter_unavailable` |
| documentation:ReportingOperations/HTMLDisplayBoard.htm | HTML Display Board | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ReportingOperations/MakeReportOutputOptions.htm | Make Report Output Options | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ReportingOperations/NotifyUserDouble.htm | Notify User Double | `conflicting_documented_ordinal` |
| documentation:ReportingOperations/NotifyUserHTML.htm | Notify User HTML | `sdk_setter_unavailable` |
| documentation:ReportingOperations/ReportBar/AddCalloutViewsToReport.htm | Add Callout Views to Report Bar | `sdk_setter_unavailable` |
| documentation:ReportingOperations/ReportBar/AddDimensionsToReportBar.htm | Add Dimensions to Report Bar | `sdk_setter_unavailable` |
| documentation:ReportingOperations/ReportBar/AddScaleBarsToReportBar.htm | Add Scale Bars to Report Bar | `sdk_setter_unavailable` |
| documentation:ReportingOperations/ReportBar/GenerateQuickReportFrom.htm | Generate Quick Report from Tab Order | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ReportingOperations/SaveChartToJPEGFile.htm | Save Chart to JPEG File | `mp_step_text_difference` |
| documentation:ReportingOperations/SetPointDeltaReportOptions.htm | Set Point Delta Report Options | `sdk_getter_missing`, `sdk_setter_unavailable` |
| documentation:RobotOperations/Get Robot Machine Model Link.htm | Get Robot/Machine Model Link Parameters | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:RobotOperations/GetCalibrationApplianceData.htm | Get Calibration Appliance Data | `sdk_getter_missing` |
| documentation:RobotOperations/Perform Robot Calibration.htm | Perform Robot Calibration | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_unavailable` |
| documentation:RobotOperations/PerformRobotCalibrationAlternate.htm | Perform Robot Calibration (Alternate) | `not_documented`, `sdk_argument_not_documented` |
| documentation:RobotOperations/Set Calibration Appliance.htm | Set Calibration Appliance Integer Value | `argument_name_text_difference` |
| documentation:RobotOperations/Set Robot Calibration Measurement.htm | Set Robot Calibration Measurement Offset in Tool Frame | `mp_step_text_difference` |
| documentation:RobotOperations/Set Robot Machine Model Link.htm | Set Robot/Machine Model Link Parameters | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:RobotOperations/SetCalibrationApplianceData.htm | Set Calibration Appliance Data | `missing_return_arguments_section`, `sdk_setter_unavailable` |
| documentation:RobotOperations/SetCalibrationApplianceReal.htm | Set Calibration Appliance Real Value | `missing_return_arguments_section` |
| documentation:ScalarMathOperations/BooleanComparison.htm | Boolean Comparison | `sdk_setter_unavailable` |
| documentation:ScalarMathOperations/BooleanComparisonResult.htm | Boolean Comparison (result) | `sdk_setter_missing` |
| documentation:ScalarMathOperations/ColorComparison.htm | Color Comparison | `sdk_setter_unavailable` |
| documentation:ScalarMathOperations/DoesStringContainSub-String.htm | Does String Contain Sub-String | `argument_name_text_difference`, `sdk_setter_missing`, `sdk_setter_unavailable` |
| documentation:ScalarMathOperations/DoubleAngleConversion.htm | Double Angle Conversion | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ScalarMathOperations/DoubleComparison.htm | Double Comparison | `sdk_setter_unavailable` |
| documentation:ScalarMathOperations/DoubleComparisonResult.htm | Double Comparison (result) | `direction_disagreement_sdk_getter_observed`, `missing_return_arguments_section`, `sdk_setter_missing` |
| documentation:ScalarMathOperations/IntegerComparison.htm | Integer Comparison | `sdk_setter_unavailable` |
| documentation:ScalarMathOperations/IntegerComparisonResult.htm | Integer Comparison (result) | `direction_disagreement_sdk_getter_observed`, `missing_return_arguments_section`, `sdk_setter_missing` |
| documentation:ScalarMathOperations/StringComparison.htm | String Comparison | `sdk_setter_unavailable` |
| documentation:ScalarMathOperations/TrigFunction.htm | Trig Function | `not_documented`, `sdk_argument_not_documented` |
| documentation:ScaleBars/GetScaleBarStats.htm | Get Scale Bar Stats | `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:UtilityOperations/Folders/SetFolderNotes.htm | Set Folder Notes | `argument_name_text_difference` |
| documentation:UtilityOperations/Language/GetActiveLanguage.htm | Get Active Language | `conflicting_documented_ordinal`, `sdk_getter_missing` |
| documentation:UtilityOperations/Language/SetActiveIntegratedLanguage.htm | Set Active Integrated Language | `sdk_setter_unavailable` |
| documentation:UtilityOperations/Network/GetScreenResolution.htm | Get Screen Resolution | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:UtilityOperations/Network/HTTPGETRequest.htm | HTTP GET Request | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Network/SetWildCardAsteriskMode.htm | Set Wild Card Asterisk Mode | `mp_step_text_difference` |
| documentation:UtilityOperations/Network/UDPReceiveString.htm | UDP Receive String | `argument_name_text_difference` |
| documentation:UtilityOperations/Notes/SetCollectionNotes.htm | Set Collection Notes | `argument_name_text_difference` |
| documentation:UtilityOperations/Notes/SetObjectNotes.htm | Set Object Notes | `argument_name_text_difference` |
| documentation:UtilityOperations/Notes/SetPointNotes.htm | Set Point Notes | `argument_name_text_difference` |
| documentation:UtilityOperations/OPC-UAClient/SetOPCUANodeValueInteger.htm | Set OPC UA Node Value Integer | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:UtilityOperations/OPC-UAClient/UnsubscribeFromOPCUANodes.htm | Unsubscribe From OPC UA Node | `argument_name_text_difference` |
| documentation:UtilityOperations/Units/CopyDirectory.htm | Copy Directory | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/DelayForSpecifiedTime.htm | Delay for Specified Time | `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_unavailable` |
| documentation:UtilityOperations/Units/DeleteDirectory.htm | Delete Directory | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/DirectoryExistence.htm | Directory Existence | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/GetAngularRepresentation.htm | Get Angular Representation | `argument_name_text_difference` |
| documentation:UtilityOperations/Units/GetObjectColor.htm | Get Object Color | `sdk_getter_missing`, `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/HighlightObjects.htm | Highlight Objects | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/HighlightPoint.htm | Highlight Point | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/Highlight_Objects.htm | Highlight Relationships | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/IncrementPointName.htm | Increment Point Name | `argument_name_text_difference`, `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:UtilityOperations/Units/MakeDirectory.htm | Make Directory | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/RefreshViews.htm | Refresh Views | `sdk_step_missing` |
| documentation:UtilityOperations/Units/RemoveSpecifiedCharacters.htm | Remove Specified Characters From String | `direction_disagreement_sdk_getter_observed`, `sdk_setter_missing` |
| documentation:UtilityOperations/Units/SendMPStepsStatusToExternal.htm | Send MP Step’s Status to External Device | `mp_step_text_difference`, `sdk_setter_unavailable` |
| documentation:UtilityOperations/Units/SetActiveUnits.htm | Set Active Units | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:UtilityOperations/Units/SetMPStepMode.htm | Set MP Step Mode | `sdk_setter_unavailable` |
| documentation:UtilityOperations/Units/SetObjectsColor.htm | Set Object(s) Color | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/SetObjectsTranslucency.htm | Set Object(s) Translucency | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/SetUserInterfaceProfile.htm | Set User Interface Profile | `argument_name_text_difference` |
| documentation:UtilityOperations/Units/SetWorkingColor.htm | Set Working Color | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/SetWorkingColorAutoIncrement.htm | Set Working Color Auto Increment | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/SpeakToUser.htm | Speak To User | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:UtilityOperations/Units/StepComment.htm | Step Comment | `sdk_step_missing` |
| documentation:Variables/DeleteVariablesWildcard.htm | Delete Variables - Wildcard Match | `mp_step_text_difference` |
| documentation:Variables/GetFontVariable.htm | Get Font Variable | `sdk_getter_missing` |
| documentation:Variables/GetI-thDoubleFromList.htm | Get i-th Double From List | `mp_step_text_difference` |
| documentation:Variables/GetNumberOfDoublesInList.htm | Get number of Doubles in List | `mp_step_text_difference` |
| documentation:Vector Operations/AddAVectorToVectorName.htm | Add a Vector to Vector Name Ref List | `missing_return_arguments_section`, `mp_step_text_difference` |
| documentation:Vector Operations/GetI-thVectorFromVectorName.htm | Get i-th Vector From Vector Name Ref List | `sdk_getter_missing` |
| documentation:Vector Operations/SetVectorGroupDisplayAttributes.htm | Set Vector Group Display Attributes | `argument_name_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:Vector Operations/VectorMathOperations/VectorDotProduct.htm | Vector Dot Product | `not_documented`, `sdk_argument_not_documented`, `sdk_getter_missing` |
| documentation:ViewControl/Colors/ConvertIntegerValuestoRGB.htm | Convert Integer Values to RGB | `sdk_getter_missing` |
| documentation:ViewControl/Colors/ConvertRGBValuestoInteger.htm | Convert RGB Values to Integer | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ViewControl/Colors/GetObjectColor.htm | Get Object Color | `sdk_getter_missing` |
| documentation:ViewControl/Colors/SetBackgroundColor.htm | Set Background Color | `sdk_setter_unavailable` |
| documentation:ViewControl/Colors/SetObjectsColor.htm | Set Object(s) Color | `argument_name_text_difference` |
| documentation:ViewControl/Colors/SetWorkingColorAutoIncrement.htm | Set Working Color Auto Increment | `argument_name_text_difference` |
| documentation:ViewControl/HideShowOperations/HideAllCalloutView.htm | Hide All Callout View | `sdk_step_missing` |
| documentation:ViewControl/HideShowOperations/HideObjects.htm | Hide Objects | `argument_name_text_difference` |
| documentation:ViewControl/HideShowOperations/SetToolkitVisibility.htm | Set Toolkit Visibility | `sdk_setter_unavailable` |
| documentation:ViewControl/HideShowOperations/ShowHideCalloutView.htm | Show/Hide Callout View | `argument_name_text_difference`, `mp_step_text_difference` |
| documentation:ViewControl/HideShowOperations/ShowHideDimensions.htm | Show/Hide Dimensions | `sdk_setter_missing`, `sdk_step_missing` |
| documentation:ViewControl/HideShowOperations/ShowHidePoints.htm | Show/Hide Points | `mp_step_text_difference` |
| documentation:ViewControl/HideShowOperations/ShowHidebyObjectType.htm | Show/Hide by Object Type | `argument_name_text_difference`, `mp_step_text_difference` |
| documentation:ViewControl/HideShowOperations/ShowLabels.htm | Show Labels | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ViewControl/HideShowOperations/ShowObjects.htm | Show Objects | `argument_name_text_difference`, `sdk_setter_missing` |
| documentation:ViewControl/HideShowOperations/ShowbyObjectType.htm | Show by Object Type | `argument_name_text_difference` |
| documentation:ViewControl/HighlightOperations/HighlightRelationships.htm | Highlight Relationships | `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ViewControl/PointofView/DefinePointofView.htm | Define Point of View | `mp_step_text_difference` |
| documentation:ViewControl/PointofView/GetPointofViewParameters.htm | Get Point of View Parameters | `mp_step_text_difference`, `sdk_getter_missing` |
| documentation:ViewControl/PointofView/SavePointofView.htm | Save Point of View | `mp_step_text_difference` |
| documentation:ViewControl/PointofView/SetPointofView.htm | Set Point of View | `mp_step_text_difference` |
| documentation:ViewControl/PointofView/SetPointofViewFromFrame.htm | Set Point of View From Frame | `mp_step_text_difference` |
| documentation:ViewControl/PointofView/SetPointofViewFromInstrument.htm | Set Point of View From Instrument Updates | `argument_name_text_difference`, `mp_step_text_difference`, `not_documented`, `sdk_argument_not_documented`, `sdk_setter_missing` |
| documentation:ViewControl/RibbonBar/LoadRibbonBarfromXMLFile.htm | Load Ribbon Bar from XML File | `missing_return_arguments_section` |
| documentation:ViewControl/SetMPsWindowState.htm | Set MP’s Window State | `mp_step_text_difference`, `sdk_setter_missing` |
| documentation:ViewControl/SetObjectsTranslucency.htm | Set Object(s) Translucency | `missing_input_arguments_section`, `not_documented`, `sdk_argument_not_documented` |
| documentation:ViewControl/SetSAsWindowPos.htm | Set SA’s Window Pos | `mp_step_text_difference` |
| documentation:ViewControl/SetSAsWindowSize.htm | Set SA’s Window Size | `mp_step_text_difference` |
| documentation:ViewControl/SetSAsWindowState.htm | Set SA’s Window State | `mp_step_text_difference`, `sdk_setter_missing` |
| sdk:AnalysisOperations.txt#19 | Create Point Uncertainty Cloud Point Sets | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#20 | Set Point Weights From Uncertainties | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#21 | Get Point Coordinate | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#37 | Set Transform for i-th Frame in Frame Set | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#38 | Get Euler Parameters for i-th Frame in Frame Set | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#39 | Get Euler Parameters for Frame | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#48 | Get i-th Report From Report Ref List | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#55 | Set Line Properties | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#60 | Set Cylinder Properties | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:AnalysisOperations.txt#62 | Set Ellipse Properties | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:CloudAndMeshOperations.txt#7 | Clear Cloud Point Deviations | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_BSplines.txt#3 | Construct B-Spline From Point Set | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_BSplines.txt#9 | Construct B-Splines From Intersection of Plane and Mesh | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_CalloutViewsAndCallouts.txt#15 | Set Callout View Properties | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_Ellipses.txt#1 | Construct Ellipse | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_Frames.txt#16 | Construct Frames By Projecting Frames On Mesh Along Frame Direction | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_Frames.txt#17 | Construct Frames By Projecting Frames On Mesh Along Reference Direction | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_Frames.txt#18 | Add Surface To Mesh Offset Along Reference Direction | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_Frames.txt#3 | Construct Frame From Transform In World | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_OtherMPTypes.txt#37 | Get Collection Instrument Ref List Variable | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_OtherMPTypes.txt#38 | Set Collection Instrument Ref List Variable | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_OtherMPTypes.txt#43 | Make a Relationship Reference List- Runtime Select | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_OtherMPTypes.txt#55 | Make a Transform from Doubles (Euler Parameters) | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_Planes.txt#4 | Construct Planes, Bisect 2 Planes | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_PointClouds.txt#11 | Create Cloud Thinning Settings | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_PointClouds.txt#5 | Construct Point Cloud from Visible Cloud Points | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ConstructionOperations_PointsAndGroups.txt#26 | Construct Points By Projecting Points On Mesh Along Direction | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:DimensionOperations.txt#12 | Set Dimension Tolerance | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:EventOperations.txt#4 | Export Event Ref List | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:FileOperations.txt#19 | Terminate All Running MPs | `documentation_command_missing` |
| sdk:FileOperations.txt#34 | Pop PolyBay Analysis Window | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:FileOperations_FileExport.txt#1 | Export ASCII Points | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:FileOperations_FileExport.txt#2 | Export ASCII Point Set | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:FileOperations_FileImport.txt#16 | Import VSTARS Cameras | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:FileOperations_FileImport.txt#19 | Import Polyworks File | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:FileOperations_XML.txt#1 | Use NRKXML Library | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:GDTOperations_GDTAnalysis.txt#17 | Set Global Force Simultaneous Evaluation | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:GDTOperations_GDTAnalysis.txt#20 | Generate Feature Check Summary | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:GDTOperations_GDTConstruction.txt#2 | Make Surface Face List From Surface | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:GoogleSheetsOperations.txt#8 | Google Sheets Run Script | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#104 | Construct Mirror from Two Points | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#120 | Get Inspection Verification Mode | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#121 | Set Inspection Verification Mode | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#125 | Make Collection Object Name Ref List from Objects associated with Instruments | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#127 | Dissect Point Group | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#138 | Get WRTL Channel and Status | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#139 | Set WRTL Channel | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#20 | Enable/Disable Frame Set Scan Mode (By Instrument) | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#39 | Multi Measurement Initiate | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#40 | Multi Measurement Stop | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#41 | Align Laser Projector | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#55 | Get XYZ Instrument Uncertainties | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#56 | Set XYZ Instrument Uncertainties | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#75 | Get Instrument Targets and Mode/Profiles | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#8 | Export Instrument History to XML File | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#90 | Get Estimated Scan Time | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations.txt#91 | Construct Perimeters from Surface Face List | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations_APILADAR.txt#3 | Set LADAR FeatureMeas Sphere | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations_APILADAR.txt#4 | Set LADAR FeatureMeas Circle | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations_APILADAR.txt#5 | Set LADAR FeatureMeas Slot | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:InstrumentOperations_APILADAR.txt#6 | Set LADAR FeatureMeas Cylinder | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:MSOfficeReportingOperations.txt#6 | Insert Graphics from file | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations.txt#1 | Generate Geometry Relationship Summary | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations.txt#13 | Make Groups to Objects Relationship | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations.txt#33 | Set Optimization Search Options | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations.txt#41 | Set Group To Nominal Group View Zooming | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations.txt#53 | Get Objects From Points to Objects Map (Point List) | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations.txt#55 | Make Cloud to Swatch Relationship | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations_RelationshipAttributes.txt#13 | Get Geom Relationship Criteria Name List | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations_RelationshipAttributes.txt#35 | Get Relationship Status | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations_RelationshipAttributesScalarTypes.txt#10 | Set Object to Object Direction Relationship Tolerances | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RelationshipOperations_RelationshipAttributesScalarTypes.txt#8 | Get Relationship Sigmoidal Gap Fit Constraints | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ReportingOperations.txt#22 | Refresh Callout Views in SA Report | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#1 | Add Calibration Appliance Node | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#10 | Set Calibration Appliance Node Measurement Frame | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#11 | Set Calibration Appliance Node Measurement Offset Transform | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#12 | Set Calibration Appliance Node Measurement Point Group | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#13 | Set Calibration Appliance Node Calibration Appliance IP Address | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#14 | Set Calibration Appliance Node Trapping Node ID | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#15 | Enable/Disable Calibration Appliance Node Trap Manager | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#16 | Clear Calibration Appliance Node Trap Manager Requests | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#17 | Set Calibration Appliance Node Integer Value | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#18 | Get Calibration Appliance Node Integer Value | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#19 | Set Calibration Appliance Node Real Value | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#2 | Delete Calibration Appliance Node | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#20 | Get Calibration Appliance Node Real Value | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#21 | Set Calibration Appliance Node Data | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#22 | Get Calibration Appliance Node Data | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#23 | Set Calibration Appliance Node Display Robot | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#24 | Update Calibration Appliance Node Display Robot Joints | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#25 | Get Calibration Appliance Node Status | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#3 | Connect/Disconnect Calibration Appliance Node | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#4 | Set Calibration Appliance Node Instrument | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#5 | Set Calibration Appliance Node Measurement Profile | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#6 | Set Calibration Appliance Node Measurement Target | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#7 | Enable/Disable Calibration Appliance Node Instrument Auto Point | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#8 | Set Calibration Appliance Node Instrument Dwell Time | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotCalibrationApplianceNodeOperations.txt#9 | Skip Calibration Appliance Node Measurement | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:RobotOperations.txt#11 | Set Robot/Machine Base Transform | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:UtilityOperations_Network.txt#4 | HTTPS GET Request | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:UtilityOperations_OPCUAClient.txt#23 | OPC UA MP Configuration Auto Run Settings | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:UtilityOperations_OPCUAClient.txt#24 | Set OPC UA Node Named Coordinate Frame | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:UtilityOperations_OPCUAClient.txt#25 | Get OPC UA Node Named Coordinate Frame | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:Variables.txt#10 | Set Double List Variable | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:Variables.txt#17 | Get Boolean Variable | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:Variables.txt#34 | Set Vector Name Ref List Variable | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:Variables.txt#35 | Get Vector Name Ref List Variable | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ViewControl_HideShowOperations.txt#10 | Hide All Callout Views | `documentation_command_missing` |
| sdk:ViewControl_HideShowOperations.txt#16 | Show/Hide Inspection Bar | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
| sdk:ViewControl_HideShowOperations.txt#8 | Show / Hide Dimension | `documentation_command_missing`, `not_documented`, `sdk_argument_not_documented` |
