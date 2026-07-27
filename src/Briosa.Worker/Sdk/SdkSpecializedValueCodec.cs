namespace Briosa.Worker.Sdk;

internal static class SdkSpecializedValueCodec
{
    public static string ToSdkString(SdkAsciiFileFormatValue value) => value switch
    {
        SdkAsciiFileFormatValue.Xyz => "X Y Z",
        SdkAsciiFileFormatValue.XyzOffsetOffset2 => "X Y Z Offset [Offset2]",
        SdkAsciiFileFormatValue.XyzNotes => "X Y Z [Notes]",
        SdkAsciiFileFormatValue.RadiusThetaPhi => "Radius Theta Phi (polar or spheric)",
        SdkAsciiFileFormatValue.RadiusThetaZ => "Radius Theta Z (cylindric)",
        SdkAsciiFileFormatValue.PointNameXyz => "PointName X Y Z",
        SdkAsciiFileFormatValue.PointNameXyzNotes => "PointName X Y Z [Notes]",
        SdkAsciiFileFormatValue.PointNameXyzOffsetOffset2 => "PointName X Y Z Offset [Offset2]",
        SdkAsciiFileFormatValue.PointNameXyzUxUyUz => "PointName X Y Z Ux Uy Uz (1 sigma)",
        SdkAsciiFileFormatValue.PointNameXyzTxTyTzTd => "PointName X Y Z Tx Ty Tz Td (Point Tolerance)",
        SdkAsciiFileFormatValue.PointNameXyzWxWyWzWmag => "PointName X Y Z Wx Wy Wz [Wmag]",
        SdkAsciiFileFormatValue.PointNameXyzHighLowTolerance => "PointName X Y Z THx TLx THy TLy THz TLz THd TLd (Point Tolerance)",
        SdkAsciiFileFormatValue.PointNameXyzTxTyTzTdWxWyWz => "PointName X Y Z Tx Ty Tz Td Wx Wy Wz",
        SdkAsciiFileFormatValue.PointNameXyzWxWyWzTxTyTzTd => "PointName X Y Z Wx Wy Wz Tx Ty Tz [Td]",
        SdkAsciiFileFormatValue.PointNameXyzHighLowToleranceWxWyWz => "PointName X Y Z THx TLx THy TLy THz TLz THd TLd Wx Wy Wz",
        SdkAsciiFileFormatValue.PointNameXyzWxWyWzHighLowTolerance => "PointName X Y Z Wx Wy Wz THx TLx THy TLy THz TLz [THd TLd]",
        SdkAsciiFileFormatValue.PointNameRadiusThetaPhi => "PointName Radius Theta Phi (polar or spheric)",
        SdkAsciiFileFormatValue.PointNameRadiusThetaZ => "PointName Radius Theta Z (cylindric)",
        SdkAsciiFileFormatValue.PointNameXyzGroupName => "PointName X Y Z GroupName",
        SdkAsciiFileFormatValue.PointNameYxzGroupName => "PointName Y X Z GroupName",
        SdkAsciiFileFormatValue.GroupNamePointNameXyz => "GroupName PointName X Y Z",
        SdkAsciiFileFormatValue.GroupNamePointNameXyzOffsetOffset2 => "GroupName PointName X Y Z Offset [Offset2]",
        SdkAsciiFileFormatValue.GroupNamePointNameXyzNotes => "GroupName PointName X Y Z [Notes]",
        SdkAsciiFileFormatValue.GroupNamePointNameXyzUxUyUz => "GroupName PointName X Y Z Ux Uy Uz (1 sigma)",
        SdkAsciiFileFormatValue.GroupNamePointNameRadiusThetaPhi => "GroupName PointName Radius Theta Phi",
        SdkAsciiFileFormatValue.GroupNamePointNameRadiusThetaZ => "GroupName PointName Radius Theta Z",
        SdkAsciiFileFormatValue.CollectionGroupPointXyz => "Collection Group Point X Y Z",
        SdkAsciiFileFormatValue.CollectionGroupPointXyzNotes => "Collection Group Point X Y Z [Notes]",
        SdkAsciiFileFormatValue.CollectionGroupPointRadiusThetaPhi => "Collection Group Point Radius Theta Phi",
        SdkAsciiFileFormatValue.CollectionGroupPointRadiusThetaZ => "Collection Group Point Radius Theta Z",
        SdkAsciiFileFormatValue.XyzIjk => "X Y Z I J K (Planes or Vectors)",
        SdkAsciiFileFormatValue.VectorNameXyzIjk => "VectorName X Y Z I J K",
        SdkAsciiFileFormatValue.VectorNameXyzDxDyDzSignedMagnitude => "VectorName X Y Z dX dY dZ [SignedMag]",
        SdkAsciiFileFormatValue.VectorGroupNameVectorNameXyzIjk => "VectorGroupName VectorName X Y Z I J K",
        SdkAsciiFileFormatValue.VectorGroupNameVectorNameXyzDxDyDzSignedMagnitude => "VectorGroupName VectorName X Y Z dX dY dZ [SignedMag]",
        SdkAsciiFileFormatValue.FrameNameXyzRxRyRzTimestamp => "FrameName X Y Z  Rx Ry Rz [Timestamp]",
        SdkAsciiFileFormatValue.FrameNameXyzEulerXyzTimestamp => "FrameName X Y Z  Euler XYZ [Timestamp]",
        SdkAsciiFileFormatValue.FrameNameXyzEulerZyxTimestamp => "FrameName X Y Z  Euler ZYX [Timestamp]",
        SdkAsciiFileFormatValue.FrameNameXyzEulerZyzTimestamp => "FrameName X Y Z  Euler ZYZ [Timestamp]",
        SdkAsciiFileFormatValue.FrameNameXyzEulerZxzTimestamp => "FrameName X Y Z  Euler ZXZ [Timestamp]",
        SdkAsciiFileFormatValue.FrameNameTransformationMatrixTimestamp => "FrameName Transformation Matrix (4x4) [Timestamp]",
        SdkAsciiFileFormatValue.TransformationMatrixTimestamp => "Transformation Matrix (4x4) [Timestamp]",
        SdkAsciiFileFormatValue.FrameNameXyzQuaternionTimestamp => "FrameName X Y Z  e1 e2 e3 e4 [Timestamp]",
        SdkAsciiFileFormatValue.PlaneNameXyzDxDyDzPlaneSize => "PlaneName X Y Z dX dY dZ [PlaneSize]",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(SdkAxisIdentifierValue value) => value switch
    {
        SdkAxisIdentifierValue.PositiveX => "+X Axis",
        SdkAxisIdentifierValue.NegativeX => "-X Axis",
        SdkAxisIdentifierValue.PositiveY => "+Y Axis",
        SdkAxisIdentifierValue.NegativeY => "-Y Axis",
        SdkAxisIdentifierValue.PositiveZ => "+Z Axis",
        SdkAxisIdentifierValue.NegativeZ => "-Z Axis",
        SdkAxisIdentifierValue.X => "X Axis",
        SdkAxisIdentifierValue.Y => "Y Axis",
        SdkAxisIdentifierValue.Z => "Z Axis",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(SdkBaseColorTypeValue value) => value switch { SdkBaseColorTypeValue.Red => "Red", SdkBaseColorTypeValue.Green => "Green", SdkBaseColorTypeValue.Blue => "Blue", _ => throw Unknown(value) };
    public static string ToSdkString(SdkBaseMidColorTypeValue value) => value switch { SdkBaseMidColorTypeValue.Red => "Red", SdkBaseMidColorTypeValue.Green => "Green", SdkBaseMidColorTypeValue.Gray => "Gray", SdkBaseMidColorTypeValue.Blue => "Blue", _ => throw Unknown(value) };
    public static string ToSdkString(SdkChartTypeValue value) => value switch { SdkChartTypeValue.RunChart => "Run Chart", SdkChartTypeValue.IndividualXMovingRange => "Individual X - Moving Range", SdkChartTypeValue.BullseyeChart => "Bullseye Chart", _ => throw Unknown(value) };
    public static string ToSdkString(SdkCollimationBaselineTypeValue value) => value switch { SdkCollimationBaselineTypeValue.DeterminedByValue => "Determined By Value", SdkCollimationBaselineTypeValue.DeterminedFromScale => "Determined From Scale", SdkCollimationBaselineTypeValue.DeterminedFromKnownPoint => "Determined From Known Point", _ => throw Unknown(value) };
    public static string ToSdkString(SdkCollimationTypeValue value) => value switch { SdkCollimationTypeValue.FullCollimation => "Full Collimation", SdkCollimationTypeValue.NoTiltCollimation => "No-Tilt Collimation", _ => throw Unknown(value) };
    public static string ToSdkString(SdkColorRangeMethodValue value) => value switch { SdkColorRangeMethodValue.SingleColor => "Single Color", SdkColorRangeMethodValue.Continuous => "Continuous", SdkColorRangeMethodValue.TolerancedContinuous => "Toleranced (Continuous)", SdkColorRangeMethodValue.TolerancedGoNoGo => "Toleranced (Go / No-Go)", SdkColorRangeMethodValue.TolerancedGoNoGoWithWarning => "Toleranced (Go / No-Go With Warning)", SdkColorRangeMethodValue.DiscreteColors => "Discrete Colors", _ => throw Unknown(value) };
    public static string ToSdkString(SdkCoordinateSystemTypeValue value) => value switch { SdkCoordinateSystemTypeValue.Cartesian => "Cartesian", SdkCoordinateSystemTypeValue.Cylindric => "Cylindric", SdkCoordinateSystemTypeValue.Polar => "Polar", _ => throw Unknown(value) };
    public static string ToSdkString(SdkDatasetTypeValue value) => value switch { SdkDatasetTypeValue.X => "X", SdkDatasetTypeValue.Y => "Y", SdkDatasetTypeValue.Z => "Z", SdkDatasetTypeValue.Magnitude => "Magnitude", _ => throw Unknown(value) };
    public static string ToSdkString(SdkDynamicCircleModeValue value) => value switch { SdkDynamicCircleModeValue.CylinderPlaneHoldPlaneNormal => "Cylinder and Plane Intersection - Hold Plane Normal", SdkDynamicCircleModeValue.CylinderPlaneHoldCylinderAxis => "Cylinder and Plane Intersection - Hold Cylinder Axis", SdkDynamicCircleModeValue.ConePlaneHoldPlaneNormal => "Cone and Plane Intersection - Hold Plane Normal", SdkDynamicCircleModeValue.ConePlaneHoldConeAxis => "Cone and Plane Intersection - Hold Cone Axis", SdkDynamicCircleModeValue.SpherePlaneIntersection => "Sphere and Plane Intersection", SdkDynamicCircleModeValue.TwoConesIntersection => "Two Cones Intersection", SdkDynamicCircleModeValue.ConeCylinderIntersection => "Cone and Cylinder Intersection", _ => throw Unknown(value) };
    public static string ToSdkString(SdkDynamicEllipseModeValue value) => value switch { SdkDynamicEllipseModeValue.CylinderPlaneIntersection => "Cylinder and Plane Intersection", SdkDynamicEllipseModeValue.ConePlaneIntersection => "Cone and Plane Intersection", _ => throw Unknown(value) };
    public static string ToSdkString(SdkDynamicLineModeValue value) => value switch { SdkDynamicLineModeValue.ConeAxis => "Cone Axis", SdkDynamicLineModeValue.CylinderAxis => "Cylinder Axis", SdkDynamicLineModeValue.IntersectionOfTwoPlanes => "Intersection of Two Planes", SdkDynamicLineModeValue.BisectTwoLines => "Bisect Two Lines", SdkDynamicLineModeValue.SlotCenterlineAlongLength => "Slot Centerline Along Length", _ => throw Unknown(value) };
    public static string ToSdkString(SdkDynamicPlaneModeValue value) => value switch { SdkDynamicPlaneModeValue.BisectTwoPlanes => "Bisect Two Planes", SdkDynamicPlaneModeValue.TwoConesBestFitPlane => "Two Cones Intersection - Hold Normal to Best-Fit Plane", SdkDynamicPlaneModeValue.TwoConesFirstConeAxis => "Twp Cones Intersection - Hold Normal to First Cone Axis", SdkDynamicPlaneModeValue.TwoConesSecondConeAxis => "Two Cones Intersection - Hold Normal to Second Cone Axis", SdkDynamicPlaneModeValue.ConeCylinderBestFitPlane => "Cone and Cylinder Intersection - Hold Normal to Best-Fit Plane", SdkDynamicPlaneModeValue.ConeCylinderConeAxis => "Cone and Cylinder Intersection - Hold Normal to Cone Axis", SdkDynamicPlaneModeValue.ConeCylinderCylinderAxis => "Cone and Cylinder Intersection - Hold Normal to Cylinder Axis", SdkDynamicPlaneModeValue.OffsetPlaneFromPlane => "Offset Plane From Plane", _ => throw Unknown(value) };
    public static string ToSdkString(SdkDynamicPointModeValue value) => value switch { SdkDynamicPointModeValue.IntersectionLinePlane => "Intersection of Line and Plane", SdkDynamicPointModeValue.IntersectionCylinderPlane => "Intersection of Cylinder and Plane", SdkDynamicPointModeValue.IntersectionConePlane => "Intersection of Cone and Plane", SdkDynamicPointModeValue.IntersectionThreePlanes => "Intersection of Three Planes", SdkDynamicPointModeValue.MidPointPerpendicularTwoLines => "Mid-Point of Perpendicular to Two Lines", _ => throw Unknown(value) };
    public static string ToSdkString(SdkEdgeModeValue value) => value switch { SdkEdgeModeValue.IncludeEdges => "Include Edges", SdkEdgeModeValue.ExcludeEdges => "Exclude Edges", SdkEdgeModeValue.EdgesOnly => "Edges Only", _ => throw Unknown(value) };
    public static string ToSdkString(SdkExportDataDelimiterTypeValue value) => value switch { SdkExportDataDelimiterTypeValue.Space => "Space", SdkExportDataDelimiterTypeValue.Comma => "Comma", SdkExportDataDelimiterTypeValue.Tab => "Tab", _ => throw Unknown(value) };
    public static string ToSdkString(SdkExportTargetNameFormatValue value) => value switch { SdkExportTargetNameFormatValue.CollectionGroupTarget => "Collection Group Target", SdkExportTargetNameFormatValue.GroupTarget => "Group Target", SdkExportTargetNameFormatValue.Target => "Target", SdkExportTargetNameFormatValue.None => "None", _ => throw Unknown(value) };
    public static string ToSdkString(SdkExportVectorNameFormatValue value) => value switch { SdkExportVectorNameFormatValue.CollectionGroupVector => "Collection Group Vector", SdkExportVectorNameFormatValue.GroupVector => "Group Vector", SdkExportVectorNameFormatValue.Vector => "Vector", SdkExportVectorNameFormatValue.None => "None", _ => throw Unknown(value) };
    public static string ToSdkString(SdkGeometryTypeValue value) => value switch { SdkGeometryTypeValue.Line => "Line", SdkGeometryTypeValue.Plane => "Plane", SdkGeometryTypeValue.Circle => "Circle", SdkGeometryTypeValue.Sphere => "Sphere", SdkGeometryTypeValue.Cylinder => "Cylinder", SdkGeometryTypeValue.Cone => "Cone", SdkGeometryTypeValue.Paraboloid => "Paraboloid", SdkGeometryTypeValue.Ellipse => "Ellipse", SdkGeometryTypeValue.Slot => "Slot", SdkGeometryTypeValue.Torus => "Torus", _ => throw Unknown(value) };

    public static string ToSdkString(SdkInstrumentTypeValue value) => value switch
    {
        SdkInstrumentTypeValue.AiconDpa => "AICON DPA",
        SdkInstrumentTypeValue.AiconMoveInspect => "AICON MoveInspect",
        SdkInstrumentTypeValue.ApiIlt => "API iLT",
        SdkInstrumentTypeValue.AssemblyGuidanceLaserProjector => "Assembly Guidance Laser Projector",
        SdkInstrumentTypeValue.CreaformVxElements => "Creaform VXelements",
        SdkInstrumentTypeValue.DigitalNetworkLevel => "Digital Network Level",
        SdkInstrumentTypeValue.FaroArm15m6Dof => "FARO Arm 1.5m 6 dof (QuantumS, QuantumM)",
        SdkInstrumentTypeValue.FaroArm25m6Dof => "FARO Arm 2.5m 6 dof (QuantumS, QuantumM)",
        SdkInstrumentTypeValue.FaroArm25m7Dof => "FARO Arm 2.5m 7 dof (QuantumS, QuantumM, Quantum Max)",
        SdkInstrumentTypeValue.FaroArm2m6Dof => "FARO Arm 2m 6 dof (QuantumS, QuantumM)",
        SdkInstrumentTypeValue.FaroArm2m7Dof => "FARO Arm 2m 7 dof (QuantumS, QuantumM, Quantum Max)",
        SdkInstrumentTypeValue.FaroArm35m6Dof => "FARO Arm 3.5m 6 dof (QuantumS, QuantumM)",
        SdkInstrumentTypeValue.FaroArm35m7Dof => "FARO Arm 3.5m 7 dof (QuantumS, QuantumM, Quantum Max)",
        SdkInstrumentTypeValue.FaroArm3m6Dof => "FARO Arm 3m 6 dof (QuantumS, QuantumM)",
        SdkInstrumentTypeValue.FaroArm3m7Dof => "FARO Arm 3m 7 dof (QuantumS, QuantumM, Quantum Max)",
        SdkInstrumentTypeValue.FaroArm4m6Dof => "FARO Arm 4m 6 dof (QuantumS, QuantumM)",
        SdkInstrumentTypeValue.FaroArm4m7Dof => "FARO Arm 4m 7 dof (QuantumS, QuantumM, Quantum Max)",
        SdkInstrumentTypeValue.FaroScannerPhotonLsFocus3d => "Faro Scanner Photon/LS/Focus 3D",
        SdkInstrumentTypeValue.GenericAuxDevice => "Generic Aux Device",
        SdkInstrumentTypeValue.GenericAuxDevice2 => "Generic Aux Device 2",
        SdkInstrumentTypeValue.GenericPhotogrammetrySystem => "Generic Photogrammetry System",
        SdkInstrumentTypeValue.GenericPhotogrammetrySystem2 => "Generic Photogrammetry System 2",
        SdkInstrumentTypeValue.LapCadProLaserProjector => "LAP CAD-Pro Laser Projector",
        SdkInstrumentTypeValue.LeicaGeosystemsRtc360 => "Leica Geosystems RTC360",
        SdkInstrumentTypeValue.LeicaGeosystemsScanStationPxx => "Leica Geosystems ScanStation PXX",
        SdkInstrumentTypeValue.LeicaT1200TotalStation => "Leica T1200 Total Station",
        SdkInstrumentTypeValue.LeicaTm6100aTheodolite => "Leica TM6100A Theodolite",
        SdkInstrumentTypeValue.LeicaTs09TotalStation => "Leica TS09 Total Station",
        SdkInstrumentTypeValue.LeicaTs15TotalStation => "Leica TS15 Total Station",
        SdkInstrumentTypeValue.LeicaTs16TotalStation => "Leica TS16 Total Station",
        SdkInstrumentTypeValue.LeicaTs20TotalStation => "Leica TS20 Total Station",
        SdkInstrumentTypeValue.LeicaTs30TotalStation => "Leica TS30 Total Station",
        SdkInstrumentTypeValue.LptLaserProjector => "LPT Laser Projector",
        SdkInstrumentTypeValue.SaOpenAuxiliaryInstrument => "SA Open Auxiliary Instrument",
        SdkInstrumentTypeValue.SaOpenInstrument => "SA Open Instrument",
        SdkInstrumentTypeValue.SaPipeline => "SA Pipeline",
        SdkInstrumentTypeValue.Surphaser10Scanner => "Surphaser 10 Scanner",
        SdkInstrumentTypeValue.SurphaserScanner => "Surphaser Scanner",
        SdkInstrumentTypeValue.ViconTracker => "Vicon Tracker",
        SdkInstrumentTypeValue.XyzReferenceFrame => "XYZ Reference Frame",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(SdkObjectTypeValue value) => value switch
    {
        SdkObjectTypeValue.Any => "Any",
        SdkObjectTypeValue.BSpline => "B-Spline",
        SdkObjectTypeValue.Circle => "Circle",
        SdkObjectTypeValue.Cloud => "Cloud",
        SdkObjectTypeValue.CrossSectionCloud => "Cross Section Cloud",
        SdkObjectTypeValue.Cylinder => "Cylinder",
        SdkObjectTypeValue.Datum => "Datum",
        SdkObjectTypeValue.Ellipse => "Ellipse",
        SdkObjectTypeValue.EnhancedCloud => "Enhanced Cloud",
        SdkObjectTypeValue.Frame => "Frame",
        SdkObjectTypeValue.FrameSet => "Frame Set",
        SdkObjectTypeValue.Line => "Line",
        SdkObjectTypeValue.Paraboloid => "Paraboloid",
        SdkObjectTypeValue.Perimeter => "Perimeter",
        SdkObjectTypeValue.Plane => "Plane",
        SdkObjectTypeValue.PointGroup => "Point Group",
        SdkObjectTypeValue.PointSet => "Point Set",
        SdkObjectTypeValue.PolySurface => "Poly Surface",
        SdkObjectTypeValue.ScanStripeCloud => "Scan Stripe Cloud",
        SdkObjectTypeValue.ScanStripeMesh => "Scan Stripe Mesh",
        SdkObjectTypeValue.Slot => "Slot",
        SdkObjectTypeValue.Sphere => "Sphere",
        SdkObjectTypeValue.Surface => "Surface",
        SdkObjectTypeValue.Torus => "Torus",
        SdkObjectTypeValue.VectorGroup => "Vector Group",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(SdkOffsetDirectionTypeValue value) => value switch { SdkOffsetDirectionTypeValue.Both => "Both", SdkOffsetDirectionTypeValue.PositiveOnly => "Positive only", SdkOffsetDirectionTypeValue.NegativeOnly => "Negative only", _ => throw Unknown(value) };
    public static string ToSdkString(SdkPointFilterInputTypeValue value) => value switch { SdkPointFilterInputTypeValue.CardinalPoints => "Cardinal Points", SdkPointFilterInputTypeValue.InputPoints => "Input Points", SdkPointFilterInputTypeValue.NominalCardinalPoints => "Nominal Cardinal Points", _ => throw Unknown(value) };
    public static string ToSdkString(SdkRelationshipWeightingModeValue value) => value switch { SdkRelationshipWeightingModeValue.NormalizeEquationCount => "Normalize on equation count", SdkRelationshipWeightingModeValue.NormalizeEquationCountAndToleranceWidth => "Normalize on equation count AND tolerance width", SdkRelationshipWeightingModeValue.ResetAllWeights => "Reset All weights to 1.0", SdkRelationshipWeightingModeValue.NormalizeSquareRootEquationCount => "Normalize on square root of equation count", SdkRelationshipWeightingModeValue.NormalizeSquareRootAndToleranceWidth => "Normalize on square root AND tolerance width", _ => throw Unknown(value) };
    public static string ToSdkString(SdkRenderModeTypeValue value) => value switch { SdkRenderModeTypeValue.Wireframe => "Wireframe", SdkRenderModeTypeValue.HiddenLineRemoved => "Hidden Line Removed", SdkRenderModeTypeValue.SolidAndEdges => "Solid+Edges", SdkRenderModeTypeValue.Solid => "Solid", _ => throw Unknown(value) };
    public static string ToSdkString(SdkReportPageOrientationValue value) => value switch { SdkReportPageOrientationValue.Portrait => "Portrait", SdkReportPageOrientationValue.Landscape => "Landscape", _ => throw Unknown(value) };
    public static string ToSdkString(SdkSaturationLimitTypeValue value) => value switch { SdkSaturationLimitTypeValue.Deviation => "Deviation", SdkSaturationLimitTypeValue.SigmaRule => "Sigma Rule", SdkSaturationLimitTypeValue.Custom => "Custom", _ => throw Unknown(value) };
    public static string ToSdkString(SdkShowUsmnDialogTypeValue value) => value switch { SdkShowUsmnDialogTypeValue.No => "No", SdkShowUsmnDialogTypeValue.Yes => "Yes", SdkShowUsmnDialogTypeValue.OnToleranceViolation => "On Tolerance Violation", _ => throw Unknown(value) };
    public static string ToSdkString(SdkSurfaceAnalysisModeValue value) => value switch { SdkSurfaceAnalysisModeValue.None => "None", SdkSurfaceAnalysisModeValue.Relationship => "Relationship", SdkSurfaceAnalysisModeValue.Normals => "Normals", SdkSurfaceAnalysisModeValue.Curvature => "Curvature", SdkSurfaceAnalysisModeValue.DeviationRms => "Deviation RMS", SdkSurfaceAnalysisModeValue.DeviationMax => "Deviation MAX", SdkSurfaceAnalysisModeValue.DeviationAverage => "Deviation AVG", SdkSurfaceAnalysisModeValue.DeviationMin => "Deviation MIN", SdkSurfaceAnalysisModeValue.DeviationMaxAbsolute => "Deviation MAX ABS", SdkSurfaceAnalysisModeValue.DeviationMaxDelta => "Deviation MAX DELTA", SdkSurfaceAnalysisModeValue.PseudoSurface => "Pseudo Surface", _ => throw Unknown(value) };
    public static string ToSdkString(SdkSurfaceDissectionModeTypeValue value) => value switch { SdkSurfaceDissectionModeTypeValue.EntireSolid => "Entire Solid", SdkSurfaceDissectionModeTypeValue.SelectFaces => "Select Faces", _ => throw Unknown(value) };
    public static string ToSdkString(SdkTargetComputationMethodValue value) => value switch { SdkTargetComputationMethodValue.UseMostRecentShotFromEachFace => "Use most recent shot from each face", SdkTargetComputationMethodValue.UseOnlyMostRecentShot => "Use only most recent shot", SdkTargetComputationMethodValue.DoNotChangePriorMeasurements => "Do not change prior measurements at all", SdkTargetComputationMethodValue.ForceNewPointForEachMeasurement => "Force a new point for each measurement", SdkTargetComputationMethodValue.RemoveAllPriorShots => "Remove all prior shots", SdkTargetComputationMethodValue.DeactivateAllPriorShots => "Deactivate all prior shots", _ => throw Unknown(value) };
    public static string ToSdkString(SdkTranslucencyTypeValue value) => value switch { SdkTranslucencyTypeValue.Solid => "Solid", SdkTranslucencyTypeValue.Translucent => "Translucent", SdkTranslucencyTypeValue.Wireframe => "Wireframe", _ => throw Unknown(value) };
    public static string ToSdkString(SdkCloudThinningModeValue value) => value switch { SdkCloudThinningModeValue.None => "None", SdkCloudThinningModeValue.Random => "Random", SdkCloudThinningModeValue.NthPoint => "Nth Point", _ => throw Unknown(value) };
    public static string ToSdkString(SdkProjectionTypeValue value) => value switch { SdkProjectionTypeValue.TargetToOffsetObjectVectors => "Target To Offset Object Vectors", SdkProjectionTypeValue.OffsetObjectToTargetVectors => "Offset Object To Target Vectors", SdkProjectionTypeValue.ProbeToObjectVectors => "Probe To Object Vectors", SdkProjectionTypeValue.ObjectToProbeVectors => "Object To Probe Vectors", SdkProjectionTypeValue.PointsOnProbeSurface => "Points on Probe Surface", SdkProjectionTypeValue.PointsOnOffsetObject => "Points on Offset Object", SdkProjectionTypeValue.PointsOnObject => "Points on Object", _ => throw Unknown(value) };
    public static string ToSdkString(SdkReportDetailsFormatValue value) => value switch { SdkReportDetailsFormatValue.None => "None", SdkReportDetailsFormatValue.Single => "Single", SdkReportDetailsFormatValue.MultiHorizontal => "Multi Horiz", SdkReportDetailsFormatValue.MultiVertical => "Multi Vert", _ => throw Unknown(value) };
    public static string ToSdkString(SdkReportOutputTypeValue value) => value switch { SdkReportOutputTypeValue.None => "None", SdkReportOutputTypeValue.SaReport => "SAReport", SdkReportOutputTypeValue.SaDocument => "SADoc", SdkReportOutputTypeValue.Pdf => "PDF", SdkReportOutputTypeValue.Rtf => "RTF", _ => throw Unknown(value) };
    public static string ToSdkString(SdkReportViewTypeValue value) => value switch { SdkReportViewTypeValue.None => "None", SdkReportViewTypeValue.CurrentView => "Current View", SdkReportViewTypeValue.CalloutView => "Callout View", _ => throw Unknown(value) };

    public static int ToSdkOffsetMode(SdkOffsetDirectionTypeValue value) => value switch
    {
        SdkOffsetDirectionTypeValue.Both => 0,
        SdkOffsetDirectionTypeValue.PositiveOnly => 1,
        SdkOffsetDirectionTypeValue.NegativeOnly => 2,
        _ => throw Unknown(value)
    };

    private static ArgumentOutOfRangeException Unknown<T>(T value) where T : struct, Enum =>
        new(nameof(value), value, $"Unknown {typeof(T).Name} value.");
}
