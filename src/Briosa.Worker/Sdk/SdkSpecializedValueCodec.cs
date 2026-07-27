namespace Briosa.Worker.Sdk;

internal static class SdkSpecializedValueCodec
{
    public static string ToSdkString(SdkAsciiImportFileFormatValue value) => value switch
    {
        SdkAsciiImportFileFormatValue.Xyz => "X Y Z",
        SdkAsciiImportFileFormatValue.XyzOffsetOffset2 => "X Y Z Offset [Offset2]",
        SdkAsciiImportFileFormatValue.XyzNotes => "X Y Z [Notes]",
        SdkAsciiImportFileFormatValue.RadiusThetaPhi => "Radius Theta Phi (polar or spheric)",
        SdkAsciiImportFileFormatValue.RadiusThetaZ => "Radius Theta Z (cylindric)",
        SdkAsciiImportFileFormatValue.PointNameXyz => "PointName X Y Z",
        SdkAsciiImportFileFormatValue.PointNameXyzNotes => "PointName X Y Z [Notes]",
        SdkAsciiImportFileFormatValue.PointNameXyzOffsetOffset2 => "PointName X Y Z Offset [Offset2]",
        SdkAsciiImportFileFormatValue.PointNameXyzUxUyUz => "PointName X Y Z Ux Uy Uz (1 sigma)",
        SdkAsciiImportFileFormatValue.PointNameXyzTxTyTzTd => "PointName X Y Z Tx Ty Tz Td (Point Tolerance)",
        SdkAsciiImportFileFormatValue.PointNameXyzWxWyWzWmag => "PointName X Y Z Wx Wy Wz [Wmag]",
        SdkAsciiImportFileFormatValue.PointNameXyzHighLowTolerance => "PointName X Y Z THx TLx THy TLy THz TLz THd TLd (Point Tolerance)",
        SdkAsciiImportFileFormatValue.PointNameXyzTxTyTzTdWxWyWz => "PointName X Y Z Tx Ty Tz Td Wx Wy Wz",
        SdkAsciiImportFileFormatValue.PointNameXyzWxWyWzTxTyTzTd => "PointName X Y Z Wx Wy Wz Tx Ty Tz [Td]",
        SdkAsciiImportFileFormatValue.PointNameXyzHighLowToleranceWxWyWz => "PointName X Y Z THx TLx THy TLy THz TLz THd TLd Wx Wy Wz",
        SdkAsciiImportFileFormatValue.PointNameXyzWxWyWzHighLowTolerance => "PointName X Y Z Wx Wy Wz THx TLx THy TLy THz TLz [THd TLd]",
        SdkAsciiImportFileFormatValue.PointNameRadiusThetaPhi => "PointName Radius Theta Phi (polar or spheric)",
        SdkAsciiImportFileFormatValue.PointNameRadiusThetaZ => "PointName Radius Theta Z (cylindric)",
        SdkAsciiImportFileFormatValue.PointNameXyzGroupName => "PointName X Y Z GroupName",
        SdkAsciiImportFileFormatValue.PointNameYxzGroupName => "PointName Y X Z GroupName",
        SdkAsciiImportFileFormatValue.GroupNamePointNameXyz => "GroupName PointName X Y Z",
        SdkAsciiImportFileFormatValue.GroupNamePointNameXyzOffsetOffset2 => "GroupName PointName X Y Z Offset [Offset2]",
        SdkAsciiImportFileFormatValue.GroupNamePointNameXyzNotes => "GroupName PointName X Y Z [Notes]",
        SdkAsciiImportFileFormatValue.GroupNamePointNameXyzUxUyUz => "GroupName PointName X Y Z Ux Uy Uz (1 sigma)",
        SdkAsciiImportFileFormatValue.GroupNamePointNameRadiusThetaPhi => "GroupName PointName Radius Theta Phi",
        SdkAsciiImportFileFormatValue.GroupNamePointNameRadiusThetaZ => "GroupName PointName Radius Theta Z",
        SdkAsciiImportFileFormatValue.CollectionGroupPointXyz => "Collection Group Point X Y Z",
        SdkAsciiImportFileFormatValue.CollectionGroupPointXyzNotes => "Collection Group Point X Y Z [Notes]",
        SdkAsciiImportFileFormatValue.CollectionGroupPointRadiusThetaPhi => "Collection Group Point Radius Theta Phi",
        SdkAsciiImportFileFormatValue.CollectionGroupPointRadiusThetaZ => "Collection Group Point Radius Theta Z",
        SdkAsciiImportFileFormatValue.XyzIjk => "X Y Z I J K (Planes or Vectors)",
        SdkAsciiImportFileFormatValue.VectorNameXyzIjk => "VectorName X Y Z I J K",
        SdkAsciiImportFileFormatValue.VectorNameXyzDxDyDzSignedMagnitude => "VectorName X Y Z dX dY dZ [SignedMag]",
        SdkAsciiImportFileFormatValue.VectorGroupNameVectorNameXyzIjk => "VectorGroupName VectorName X Y Z I J K",
        SdkAsciiImportFileFormatValue.VectorGroupNameVectorNameXyzDxDyDzSignedMagnitude => "VectorGroupName VectorName X Y Z dX dY dZ [SignedMag]",
        SdkAsciiImportFileFormatValue.FrameNameXyzRxRyRzTimestamp => "FrameName X Y Z  Rx Ry Rz [Timestamp]",
        SdkAsciiImportFileFormatValue.FrameNameXyzEulerXyzTimestamp => "FrameName X Y Z  Euler XYZ [Timestamp]",
        SdkAsciiImportFileFormatValue.FrameNameXyzEulerZyxTimestamp => "FrameName X Y Z  Euler ZYX [Timestamp]",
        SdkAsciiImportFileFormatValue.FrameNameXyzEulerZyzTimestamp => "FrameName X Y Z  Euler ZYZ [Timestamp]",
        SdkAsciiImportFileFormatValue.FrameNameXyzEulerZxzTimestamp => "FrameName X Y Z  Euler ZXZ [Timestamp]",
        SdkAsciiImportFileFormatValue.FrameNameTransformationMatrixTimestamp => "FrameName Transformation Matrix (4x4) [Timestamp]",
        SdkAsciiImportFileFormatValue.TransformationMatrixTimestamp => "Transformation Matrix (4x4) [Timestamp]",
        SdkAsciiImportFileFormatValue.FrameNameXyzQuaternionTimestamp => "FrameName X Y Z  e1 e2 e3 e4 [Timestamp]",
        SdkAsciiImportFileFormatValue.PlaneNameXyzDxDyDzPlaneSize => "PlaneName X Y Z dX dY dZ [PlaneSize]",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(SdkAsciiFrameSetFormatValue value) => value switch
    {
        SdkAsciiFrameSetFormatValue.FrameNameXyzRxRyRzTimestamp => "FrameName X Y Z  Rx Ry Rz [Timestamp]",
        SdkAsciiFrameSetFormatValue.FrameNameXyzEulerXyzTimestamp => "FrameName X Y Z  Euler XYZ [Timestamp]",
        SdkAsciiFrameSetFormatValue.FrameNameXyzEulerZyxTimestamp => "FrameName X Y Z  Euler ZYX [Timestamp]",
        SdkAsciiFrameSetFormatValue.FrameNameXyzEulerZyzTimestamp => "FrameName X Y Z  Euler ZYZ [Timestamp]",
        SdkAsciiFrameSetFormatValue.FrameNameXyzEulerZxzTimestamp => "FrameName X Y Z  Euler ZXZ [Timestamp]",
        SdkAsciiFrameSetFormatValue.FrameNameTransformationMatrixTimestamp => "FrameName Transformation Matrix (4x4) [Timestamp]",
        SdkAsciiFrameSetFormatValue.TransformationMatrixTimestamp => "Transformation Matrix (4x4) [Timestamp]",
        SdkAsciiFrameSetFormatValue.FrameNameXyzQuaternionTimestamp => "FrameName X Y Z  e1 e2 e3 e4 [Timestamp]",
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
        _ => throw Unknown(value)
    };

    public static string ToSdkString(SdkWcfAxisIdentifierValue value) => value switch
    {
        SdkWcfAxisIdentifierValue.X => "X Axis",
        SdkWcfAxisIdentifierValue.Y => "Y Axis",
        SdkWcfAxisIdentifierValue.Z => "Z Axis",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(SdkBaseColorTypeValue value) => value switch { SdkBaseColorTypeValue.Red => "Red", SdkBaseColorTypeValue.Green => "Green", SdkBaseColorTypeValue.Blue => "Blue", _ => throw Unknown(value) };
    public static string ToSdkString(SdkBaseMidColorTypeValue value) => value switch { SdkBaseMidColorTypeValue.Red => "Red", SdkBaseMidColorTypeValue.Green => "Green", SdkBaseMidColorTypeValue.Gray => "Gray", SdkBaseMidColorTypeValue.Blue => "Blue", _ => throw Unknown(value) };
    public static string ToSdkString(SdkChartTypeValue value) => value switch { SdkChartTypeValue.RunChart => "Run Chart", SdkChartTypeValue.IndividualXMovingRange => "Individual X - Moving Range", SdkChartTypeValue.BullseyeChart => "Bullseye Chart", _ => throw Unknown(value) };
    public static string ToSdkString(SdkCollimationBaselineTypeValue value) => value switch { SdkCollimationBaselineTypeValue.DeterminedByValue => "Determined By Value", SdkCollimationBaselineTypeValue.DeterminedFromScale => "Determined From Scale", SdkCollimationBaselineTypeValue.DeterminedFromKnownPoint => "Determined From Known Point", _ => throw Unknown(value) };
    public static string ToSdkString(SdkCollimationTypeValue value) => value switch { SdkCollimationTypeValue.FullCollimation => "Full Collimation", SdkCollimationTypeValue.NoTiltCollimation => "No-Tilt Collimation", _ => throw Unknown(value) };
    public static string ToSdkString(SdkColorRangeMethodValue value) => value switch { SdkColorRangeMethodValue.SingleColor => "Single Color", SdkColorRangeMethodValue.Continuous => "Continuous", SdkColorRangeMethodValue.TolerancedContinuous => "Toleranced (Continuous)", SdkColorRangeMethodValue.TolerancedGoNoGo => "Toleranced (Go / No-Go)", SdkColorRangeMethodValue.TolerancedGoNoGoWithWarning => "Toleranced (Go / No-Go With Warning)", SdkColorRangeMethodValue.DiscreteColors => "Discrete Colors", _ => throw Unknown(value) };
    public static string ToSdkString(SdkCoordinateSystemTypeValue value) => value switch { SdkCoordinateSystemTypeValue.Cartesian => "Cartesian", SdkCoordinateSystemTypeValue.Cylindric => "Cylindric", SdkCoordinateSystemTypeValue.Polar => "Polar", _ => throw Unknown(value) };
    public static string ToSdkString(SdkVectorComponentValue value) => value switch { SdkVectorComponentValue.X => "X", SdkVectorComponentValue.Y => "Y", SdkVectorComponentValue.Z => "Z", SdkVectorComponentValue.Magnitude => "Magnitude", _ => throw Unknown(value) };
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
        SdkInstrumentTypeValue.AiconProCam3DProbe => "AICON ProCam 3D Probe",
        SdkInstrumentTypeValue.ApiLadar => "API Ladar",
        SdkInstrumentTypeValue.ApiLaserRail => "API Laser Rail",
        SdkInstrumentTypeValue.ApiOmniTrac => "API OmniTrac",
        SdkInstrumentTypeValue.ApiOmniTrac2 => "API OmniTrac2",
        SdkInstrumentTypeValue.ApiRadian => "API Radian",
        SdkInstrumentTypeValue.ApiRadianPlusCore => "API Radian Plus/Core",
        SdkInstrumentTypeValue.ApiRadianPro => "API Radian Pro",
        SdkInstrumentTypeValue.ApiTrackerDeviceInterface => "API Tracker Device Interface",
        SdkInstrumentTypeValue.ApiTrackerIi => "API Tracker II",
        SdkInstrumentTypeValue.ApiTrackerIii => "API Tracker III",
        SdkInstrumentTypeValue.Axxis6100Arm26m6Dof => "Axxis 6-100 Arm (2.6m 6 dof)",
        SdkInstrumentTypeValue.Axxis6200Arm32m6Dof => "Axxis 6-200 Arm (3.2m 6 dof)",
        SdkInstrumentTypeValue.Axxis7100ArmProbe26m7Dof => "Axxis 7-100 Arm Probe (2.6m 7 dof)",
        SdkInstrumentTypeValue.Axxis7100ArmScanner26m7Dof => "Axxis 7-100 Arm Scanner (2.6m 7 dof)",
        SdkInstrumentTypeValue.CimCoreArm1024 => "CimCore Arm 1024",
        SdkInstrumentTypeValue.CimCoreArm1028 => "CimCore Arm 1028",
        SdkInstrumentTypeValue.CimCoreArm1030 => "CimCore Arm 1030",
        SdkInstrumentTypeValue.CimCoreArm2200 => "CimCore Arm 2200",
        SdkInstrumentTypeValue.CimCoreArm2500 => "CimCore Arm 2500",
        SdkInstrumentTypeValue.CimCoreArm6Dof3012i501212m => "CimCore Arm 6DOF: 3012i, 5012, 1.2m",
        SdkInstrumentTypeValue.CimCoreArm6Dof3018i501818m => "CimCore Arm 6DOF: 3018i, 5018, 1.8m",
        SdkInstrumentTypeValue.CimCoreArm6Dof3024i502424m => "CimCore Arm 6DOF: 3024i, 5024, 2.4m",
        SdkInstrumentTypeValue.CimCoreArm6Dof3028i502828m => "CimCore Arm 6DOF: 3028i, 5028, 2.8m",
        SdkInstrumentTypeValue.CimCoreArm6Dof3036i503636m => "CimCore Arm 6DOF: 3036i, 5036, 3.6m",
        SdkInstrumentTypeValue.CimCoreArm6Dof511212m => "CimCore Arm 6DOF: 5112, 1.2m",
        SdkInstrumentTypeValue.CimCoreArm6Dof511818m => "CimCore Arm 6DOF: 5118, 1.8m",
        SdkInstrumentTypeValue.CimCoreArm6Dof512424m => "CimCore Arm 6DOF: 5124, 2.4m",
        SdkInstrumentTypeValue.CimCoreArm6Dof512828m => "CimCore Arm 6DOF: 5128, 2.8m",
        SdkInstrumentTypeValue.CimCoreArm6Dof513030m => "CimCore Arm 6DOF: 5130, 3.0m",
        SdkInstrumentTypeValue.CimCoreArm6Dof513636m => "CimCore Arm 6DOF: 5136, 3.6m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5012Sc301212m => "CimCore Arm 7DOF: 5012Sc, 3012, 1.2m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5018Sc301818m => "CimCore Arm 7DOF: 5018Sc, 3018, 1.8m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5024Sc302424m => "CimCore Arm 7DOF: 5024Sc, 3024, 2.4m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5028Sc302828m => "CimCore Arm 7DOF: 5028Sc, 3028, 2.8m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5030Sc303030m => "CimCore Arm 7DOF: 5030Sc, 3030, 3.0m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5036Sc303636m => "CimCore Arm 7DOF: 5036Sc, 3036, 3.6m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5112Sc12m => "CimCore Arm 7DOF: 5112Sc, 1.2m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5118Sc18m => "CimCore Arm 7DOF: 5118Sc, 1.8m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5124Sc24m => "CimCore Arm 7DOF: 5124Sc, 2.4m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5128Sc28m => "CimCore Arm 7DOF: 5128Sc, 2.8m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5130Sc30m => "CimCore Arm 7DOF: 5130Sc, 3.0m",
        SdkInstrumentTypeValue.CimCoreArm7Dof5136Sc36m => "CimCore Arm 7DOF: 5136Sc, 3.6m",
        SdkInstrumentTypeValue.CubicKitTheodolite => "Cubic KIT Theodolite",
        SdkInstrumentTypeValue.DavisPerceptionIiWeatherStation => "Davis Perception II Weather Station",
        SdkInstrumentTypeValue.FaroArm => "FARO Arm",
        SdkInstrumentTypeValue.FaroArmG04 => "FARO Arm G04",
        SdkInstrumentTypeValue.FaroArmG04057Dof => "FARO Arm G04-05 (7dof)",
        SdkInstrumentTypeValue.FaroArmG08 => "FARO Arm G08",
        SdkInstrumentTypeValue.FaroArmG08057Dof => "FARO Arm G08-05 (7dof)",
        SdkInstrumentTypeValue.FaroArmG12 => "FARO Arm G12",
        SdkInstrumentTypeValue.FaroArmG12057Dof => "FARO Arm G12-05 (7dof)",
        SdkInstrumentTypeValue.FaroArmS08 => "FARO Arm S08",
        SdkInstrumentTypeValue.FaroArmS12 => "FARO Arm S12",
        SdkInstrumentTypeValue.FaroArmUsb10FtQuantumFusionPrimePlatinum => "FARO Arm USB 10 ft. (Quantum, Fusion, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb10Ft7DofQuantumFusionPrimePlatinum => "FARO Arm USB 10 ft. 7 dof (Quantum, Fusion, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb12FtQuantumFusionPrimePlatinum => "FARO Arm USB 12 ft. (Quantum, Fusion, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb12Ft7DofEdgeQuantumFusionPrimePlatinum => "FARO Arm USB 12 ft. 7 dof (Edge, Quantum, Fusion, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb4FtQuantumPrimePlatinum => "FARO Arm USB 4 ft. (Quantum, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb4Ft7DofQuantumPrimePlatinum => "FARO Arm USB 4 ft. 7 dof (Quantum, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb6FtQuantumFusionPrimePlatinum => "FARO Arm USB 6 ft. (Quantum, Fusion, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb6Ft7DofEdgeQuantumFusionPrimePlatinum => "FARO Arm USB 6 ft. 7 dof (Edge, Quantum, Fusion, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb8FtQuantumFusionPrimePlatinum => "FARO Arm USB 8 ft.  (Quantum, Fusion, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb8Ft7DofQuantumFusionPrimePlatinum => "FARO Arm USB 8 ft. 7 dof (Quantum, Fusion, Prime, Platinum)",
        SdkInstrumentTypeValue.FaroArmUsb9Ft7DofEdge => "FARO Arm USB 9 ft. 7 dof (Edge)",
        SdkInstrumentTypeValue.FaroIonTracker => "Faro Ion Tracker",
        SdkInstrumentTypeValue.FaroTracker => "Faro Tracker",
        SdkInstrumentTypeValue.FaroVantage => "Faro Vantage",
        SdkInstrumentTypeValue.GsiVStarsPhotogrammetrySystem => "GSI V-STARS Photogrammetry System",
        SdkInstrumentTypeValue.HexagonAbsolute86Dof12mCompact => "Hexagon Absolute 8 6dof-1.2m Compact",
        SdkInstrumentTypeValue.HexagonAbsolute86Dof25m => "Hexagon Absolute 8 6dof-2.5m",
        SdkInstrumentTypeValue.HexagonAbsolute86Dof2m => "Hexagon Absolute 8 6dof-2m",
        SdkInstrumentTypeValue.HexagonAbsolute86Dof35m => "Hexagon Absolute 8 6dof-3.5m",
        SdkInstrumentTypeValue.HexagonAbsolute86Dof3m => "Hexagon Absolute 8 6dof-3m",
        SdkInstrumentTypeValue.HexagonAbsolute86Dof45m => "Hexagon Absolute 8 6dof-4.5m",
        SdkInstrumentTypeValue.HexagonAbsolute86Dof4m => "Hexagon Absolute 8 6dof-4m",
        SdkInstrumentTypeValue.HexagonAbsolute87Dof25m => "Hexagon Absolute 8 7dof-2.5m",
        SdkInstrumentTypeValue.HexagonAbsolute87Dof2m => "Hexagon Absolute 8 7dof-2m",
        SdkInstrumentTypeValue.HexagonAbsolute87Dof35m => "Hexagon Absolute 8 7dof-3.5m",
        SdkInstrumentTypeValue.HexagonAbsolute87Dof3m => "Hexagon Absolute 8 7dof-3m",
        SdkInstrumentTypeValue.HexagonAbsolute87Dof45m => "Hexagon Absolute 8 7dof-4.5m",
        SdkInstrumentTypeValue.HexagonAbsolute87Dof4m => "Hexagon Absolute 8 7dof-4m",
        SdkInstrumentTypeValue.HexagonHandheld3DScanner => "Hexagon Handheld 3D Scanner",
        SdkInstrumentTypeValue.ImportedMeasurementsWithUncertainty => "Imported Measurements with Uncertainty",
        SdkInstrumentTypeValue.KernE2Theodolite => "Kern E2 Theodolite",
        SdkInstrumentTypeValue.KreonApiAce620 => "Kreon/API Ace-6-20",
        SdkInstrumentTypeValue.KreonApiAce625 => "Kreon/API Ace-6-25",
        SdkInstrumentTypeValue.KreonApiAce630 => "Kreon/API Ace-6-30",
        SdkInstrumentTypeValue.KreonApiAce635 => "Kreon/API Ace-6-35",
        SdkInstrumentTypeValue.KreonApiAce640 => "Kreon/API Ace-6-40",
        SdkInstrumentTypeValue.KreonApiAce645 => "Kreon/API Ace-6-45",
        SdkInstrumentTypeValue.KreonApiAce720 => "Kreon/API Ace-7-20",
        SdkInstrumentTypeValue.KreonApiAce725 => "Kreon/API Ace-7-25",
        SdkInstrumentTypeValue.KreonApiAce730 => "Kreon/API Ace-7-30",
        SdkInstrumentTypeValue.KreonApiAce735 => "Kreon/API Ace-7-35",
        SdkInstrumentTypeValue.KreonApiAce740 => "Kreon/API Ace-7-40",
        SdkInstrumentTypeValue.KreonApiAce745 => "Kreon/API Ace-7-45",
        SdkInstrumentTypeValue.LeicaAt500 => "Leica AT500",
        SdkInstrumentTypeValue.LeicaAt960930 => "Leica AT960/930",
        SdkInstrumentTypeValue.LeicaAts600 => "Leica ATS600",
        SdkInstrumentTypeValue.LeicaAts800 => "Leica ATS800",
        SdkInstrumentTypeValue.LeicaEmSconAbsoluteTrackerAt901Series => "Leica emScon Absolute Tracker (AT901 Series)",
        SdkInstrumentTypeValue.LeicaEmSconAt401 => "Leica emScon AT401",
        SdkInstrumentTypeValue.LeicaEmSconAt402 => "Leica emScon AT402",
        SdkInstrumentTypeValue.LeicaEmSconAt403 => "Leica emScon AT403",
        SdkInstrumentTypeValue.LeicaEmSconTrackerLt500800Series => "Leica emScon Tracker (LT500-800 Series)",
        SdkInstrumentTypeValue.LeicaNovaMs50TotalStation => "Leica Nova MS50 Total Station",
        SdkInstrumentTypeValue.LeicaNovaMs60TotalStation => "Leica Nova MS60 Total Station",
        SdkInstrumentTypeValue.LeicaTda5005TotalStationGeoCOM => "Leica TDA5005 Total Station (GeoCOM)",
        SdkInstrumentTypeValue.LeicaTdra6000TotalStation => "Leica TDRA6000 Total Station",
        SdkInstrumentTypeValue.LeicaTotalStationTc2000Tc2002 => "Leica Total Station TC2000, TC2002",
        SdkInstrumentTypeValue.LeicaTpsTheodolite1800 => "Leica TPS Theodolite (1800)",
        SdkInstrumentTypeValue.LeicaTpsTheodolite5100 => "Leica TPS Theodolite (5100)",
        SdkInstrumentTypeValue.LeicaTpsTotalStation200350005005 => "Leica TPS Total Station (2003,5000,5005)",
        SdkInstrumentTypeValue.LeicaTrackerTpLink => "Leica Tracker TP-LINK",
        SdkInstrumentTypeValue.LeicaWildTheodolitesT2000T2002T3000 => "Leica/Wild Theodolites T2000,T2002,T3000",
        SdkInstrumentTypeValue.MetronorPortableMeasurementSystem => "METRONOR Portable Measurement System",
        SdkInstrumentTypeValue.MitutoyoSpaceTracA => "Mitutoyo SpaceTrac-A",
        SdkInstrumentTypeValue.MitutoyoSpaceTracAi => "Mitutoyo SpaceTrac-AI",
        SdkInstrumentTypeValue.MitutoyoSpaceTracAp => "Mitutoyo SpaceTrac-AP",
        SdkInstrumentTypeValue.NikonMetrologyApdisMv400 => "Nikon Metrology APDIS MV400",
        SdkInstrumentTypeValue.NikonMetrologyLaserRadarMv200 => "Nikon Metrology Laser Radar MV200",
        SdkInstrumentTypeValue.NikonMetrologyLaserRadarMv300 => "Nikon Metrology Laser Radar MV300",
        SdkInstrumentTypeValue.NikonMetrologySurveyorV2 => "Nikon Metrology Surveyor v2",
        SdkInstrumentTypeValue.Nivel20TwoAxisLevel => "Nivel 20 Two Axis Level",
        SdkInstrumentTypeValue.OnTrakLaserLineSystemOt4040Ot6000 => "On-Trak Laser Line System (OT-4040, OT-6000)",
        SdkInstrumentTypeValue.RomerAbsolute7315 => "Romer Absolute 7315",
        SdkInstrumentTypeValue.RomerAbsolute7x20 => "Romer Absolute 7x20",
        SdkInstrumentTypeValue.RomerAbsolute7x20SiSe => "Romer Absolute 7x20SI/SE",
        SdkInstrumentTypeValue.RomerAbsolute7x25 => "Romer Absolute 7x25",
        SdkInstrumentTypeValue.RomerAbsolute7x25SiSe => "Romer Absolute 7x25SI/SE",
        SdkInstrumentTypeValue.RomerAbsolute7x30 => "Romer Absolute 7x30",
        SdkInstrumentTypeValue.RomerAbsolute7x30SiSe => "Romer Absolute 7x30SI/SE",
        SdkInstrumentTypeValue.RomerAbsolute7x35 => "Romer Absolute 7x35",
        SdkInstrumentTypeValue.RomerAbsolute7x35SiSe => "Romer Absolute 7x35SI/SE",
        SdkInstrumentTypeValue.RomerAbsolute7x40 => "Romer Absolute 7x40",
        SdkInstrumentTypeValue.RomerAbsolute7x40SiSe => "Romer Absolute 7x40SI/SE",
        SdkInstrumentTypeValue.RomerAbsolute7x45 => "Romer Absolute 7x45",
        SdkInstrumentTypeValue.RomerAbsolute7x45SiSe => "Romer Absolute 7x45SI/SE",
        SdkInstrumentTypeValue.RomerMultiGage => "Romer Multi-Gage",
        SdkInstrumentTypeValue.SokkiaNet1TotalStation => "Sokkia Net-1 Total Station",
        SdkInstrumentTypeValue.SokkiaNet2TotalStation => "Sokkia Net-2 Total Station",
        SdkInstrumentTypeValue.SokkiaNet05AXTotalStation => "Sokkia Net05AX Total Station",
        SdkInstrumentTypeValue.SokkiaNet05XTotalStation => "Sokkia Net05X Total Station",
        SdkInstrumentTypeValue.SokkiaSetxTotalStation => "Sokkia SETX Total Station",
        SdkInstrumentTypeValue.ThommenHm30WeatherStation => "Thommen HM30 Weather Station",
        SdkInstrumentTypeValue.TopconMsAxSeriesTotalStation => "Topcon MS AX Series Total Station",
        SdkInstrumentTypeValue.UltrasonicThicknessGaugeCl400 => "Ultrasonic Thickness Gauge (CL400)",
        SdkInstrumentTypeValue.VirtekLaserProjector => "Virtek Laser Projector",
        SdkInstrumentTypeValue.ZeissETh2Theodolite => "Zeiss ETh 2 Theodolite",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(SdkObjectTypeValue value) => value switch
    {
        SdkObjectTypeValue.Any => "Any",
        SdkObjectTypeValue.BSpline => "B-Spline",
        SdkObjectTypeValue.Circle => "Circle",
        SdkObjectTypeValue.Cloud => "Cloud",
        SdkObjectTypeValue.EnhancedCloud => "Enhanced Cloud",
        SdkObjectTypeValue.ScanStripeCloud => "Scan Stripe Cloud",
        SdkObjectTypeValue.CrossSectionCloud => "Cross Section Cloud",
        SdkObjectTypeValue.Cone => "Cone",
        SdkObjectTypeValue.Cylinder => "Cylinder",
        SdkObjectTypeValue.Datum => "Datum",
        SdkObjectTypeValue.Ellipse => "Ellipse",
        SdkObjectTypeValue.Frame => "Frame",
        SdkObjectTypeValue.FrameSet => "Frame Set",
        SdkObjectTypeValue.Line => "Line",
        SdkObjectTypeValue.Paraboloid => "Paraboloid",
        SdkObjectTypeValue.Perimeter => "Perimeter",
        SdkObjectTypeValue.Plane => "Plane",
        SdkObjectTypeValue.PointGroup => "Point Group",
        SdkObjectTypeValue.PointSet => "Point Set",
        SdkObjectTypeValue.PolySurface => "Poly Surface",
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
