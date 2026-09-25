using System.Collections.Frozen;
using Briosa.Worker.Control;
namespace Briosa.Worker.Sdk;

internal static class SdkSpecializedValueCodec
{
    public static string ToSdkString(WorkerAsciiImportFileFormatValue value) => value switch
    {
        WorkerAsciiImportFileFormatValue.Xyz => "X Y Z",
        WorkerAsciiImportFileFormatValue.XyzOffsetOffset2 => "X Y Z Offset [Offset2]",
        WorkerAsciiImportFileFormatValue.XyzNotes => "X Y Z [Notes]",
        WorkerAsciiImportFileFormatValue.RadiusThetaPhi => "Radius Theta Phi (polar or spheric)",
        WorkerAsciiImportFileFormatValue.RadiusThetaZ => "Radius Theta Z (cylindric)",
        WorkerAsciiImportFileFormatValue.PointNameXyz => "PointName X Y Z",
        WorkerAsciiImportFileFormatValue.PointNameXyzNotes => "PointName X Y Z [Notes]",
        WorkerAsciiImportFileFormatValue.PointNameXyzOffsetOffset2 => "PointName X Y Z Offset [Offset2]",
        WorkerAsciiImportFileFormatValue.PointNameXyzUxUyUz => "PointName X Y Z Ux Uy Uz (1 sigma)",
        WorkerAsciiImportFileFormatValue.PointNameXyzTxTyTzTd => "PointName X Y Z Tx Ty Tz Td (Point Tolerance)",
        WorkerAsciiImportFileFormatValue.PointNameXyzWxWyWzWmag => "PointName X Y Z Wx Wy Wz [Wmag]",
        WorkerAsciiImportFileFormatValue.PointNameXyzHighLowTolerance => "PointName X Y Z THx TLx THy TLy THz TLz THd TLd (Point Tolerance)",
        WorkerAsciiImportFileFormatValue.PointNameXyzTxTyTzTdWxWyWz => "PointName X Y Z Tx Ty Tz Td Wx Wy Wz",
        WorkerAsciiImportFileFormatValue.PointNameXyzWxWyWzTxTyTzTd => "PointName X Y Z Wx Wy Wz Tx Ty Tz [Td]",
        WorkerAsciiImportFileFormatValue.PointNameXyzHighLowToleranceWxWyWz => "PointName X Y Z THx TLx THy TLy THz TLz THd TLd Wx Wy Wz",
        WorkerAsciiImportFileFormatValue.PointNameXyzWxWyWzHighLowTolerance => "PointName X Y Z Wx Wy Wz THx TLx THy TLy THz TLz [THd TLd]",
        WorkerAsciiImportFileFormatValue.PointNameRadiusThetaPhi => "PointName Radius Theta Phi (polar or spheric)",
        WorkerAsciiImportFileFormatValue.PointNameRadiusThetaZ => "PointName Radius Theta Z (cylindric)",
        WorkerAsciiImportFileFormatValue.PointNameXyzGroupName => "PointName X Y Z GroupName",
        WorkerAsciiImportFileFormatValue.PointNameYxzGroupName => "PointName Y X Z GroupName",
        WorkerAsciiImportFileFormatValue.GroupNamePointNameXyz => "GroupName PointName X Y Z",
        WorkerAsciiImportFileFormatValue.GroupNamePointNameXyzOffsetOffset2 => "GroupName PointName X Y Z Offset [Offset2]",
        WorkerAsciiImportFileFormatValue.GroupNamePointNameXyzNotes => "GroupName PointName X Y Z [Notes]",
        WorkerAsciiImportFileFormatValue.GroupNamePointNameXyzUxUyUz => "GroupName PointName X Y Z Ux Uy Uz (1 sigma)",
        WorkerAsciiImportFileFormatValue.GroupNamePointNameRadiusThetaPhi => "GroupName PointName Radius Theta Phi",
        WorkerAsciiImportFileFormatValue.GroupNamePointNameRadiusThetaZ => "GroupName PointName Radius Theta Z",
        WorkerAsciiImportFileFormatValue.CollectionGroupPointXyz => "Collection Group Point X Y Z",
        WorkerAsciiImportFileFormatValue.CollectionGroupPointXyzNotes => "Collection Group Point X Y Z [Notes]",
        WorkerAsciiImportFileFormatValue.CollectionGroupPointRadiusThetaPhi => "Collection Group Point Radius Theta Phi",
        WorkerAsciiImportFileFormatValue.CollectionGroupPointRadiusThetaZ => "Collection Group Point Radius Theta Z",
        WorkerAsciiImportFileFormatValue.XyzIjk => "X Y Z I J K (Planes or Vectors)",
        WorkerAsciiImportFileFormatValue.VectorNameXyzIjk => "VectorName X Y Z I J K",
        WorkerAsciiImportFileFormatValue.VectorNameXyzDxDyDzSignedMagnitude => "VectorName X Y Z dX dY dZ [SignedMag]",
        WorkerAsciiImportFileFormatValue.VectorGroupNameVectorNameXyzIjk => "VectorGroupName VectorName X Y Z I J K",
        WorkerAsciiImportFileFormatValue.VectorGroupNameVectorNameXyzDxDyDzSignedMagnitude => "VectorGroupName VectorName X Y Z dX dY dZ [SignedMag]",
        WorkerAsciiImportFileFormatValue.FrameNameXyzRxRyRzTimestamp => "FrameName X Y Z  Rx Ry Rz [Timestamp]",
        WorkerAsciiImportFileFormatValue.FrameNameXyzEulerXyzTimestamp => "FrameName X Y Z  Euler XYZ [Timestamp]",
        WorkerAsciiImportFileFormatValue.FrameNameXyzEulerZyxTimestamp => "FrameName X Y Z  Euler ZYX [Timestamp]",
        WorkerAsciiImportFileFormatValue.FrameNameXyzEulerZyzTimestamp => "FrameName X Y Z  Euler ZYZ [Timestamp]",
        WorkerAsciiImportFileFormatValue.FrameNameXyzEulerZxzTimestamp => "FrameName X Y Z  Euler ZXZ [Timestamp]",
        WorkerAsciiImportFileFormatValue.FrameNameTransformationMatrixTimestamp => "FrameName Transformation Matrix (4x4) [Timestamp]",
        WorkerAsciiImportFileFormatValue.TransformationMatrixTimestamp => "Transformation Matrix (4x4) [Timestamp]",
        WorkerAsciiImportFileFormatValue.FrameNameXyzQuaternionTimestamp => "FrameName X Y Z  e1 e2 e3 e4 [Timestamp]",
        WorkerAsciiImportFileFormatValue.PlaneNameXyzDxDyDzPlaneSize => "PlaneName X Y Z dX dY dZ [PlaneSize]",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(WorkerAsciiFrameSetFormatValue value) => value switch
    {
        WorkerAsciiFrameSetFormatValue.FrameNameXyzRxRyRzTimestamp => "FrameName X Y Z  Rx Ry Rz [Timestamp]",
        WorkerAsciiFrameSetFormatValue.FrameNameXyzEulerXyzTimestamp => "FrameName X Y Z  Euler XYZ [Timestamp]",
        WorkerAsciiFrameSetFormatValue.FrameNameXyzEulerZyxTimestamp => "FrameName X Y Z  Euler ZYX [Timestamp]",
        WorkerAsciiFrameSetFormatValue.FrameNameXyzEulerZyzTimestamp => "FrameName X Y Z  Euler ZYZ [Timestamp]",
        WorkerAsciiFrameSetFormatValue.FrameNameXyzEulerZxzTimestamp => "FrameName X Y Z  Euler ZXZ [Timestamp]",
        WorkerAsciiFrameSetFormatValue.FrameNameTransformationMatrixTimestamp => "FrameName Transformation Matrix (4x4) [Timestamp]",
        WorkerAsciiFrameSetFormatValue.TransformationMatrixTimestamp => "Transformation Matrix (4x4) [Timestamp]",
        WorkerAsciiFrameSetFormatValue.FrameNameXyzQuaternionTimestamp => "FrameName X Y Z  e1 e2 e3 e4 [Timestamp]",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(WorkerAxisIdentifierValue value) => value switch
    {
        WorkerAxisIdentifierValue.PositiveX => "+X Axis",
        WorkerAxisIdentifierValue.NegativeX => "-X Axis",
        WorkerAxisIdentifierValue.PositiveY => "+Y Axis",
        WorkerAxisIdentifierValue.NegativeY => "-Y Axis",
        WorkerAxisIdentifierValue.PositiveZ => "+Z Axis",
        WorkerAxisIdentifierValue.NegativeZ => "-Z Axis",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(WorkerWcfAxisIdentifierValue value) => value switch
    {
        WorkerWcfAxisIdentifierValue.X => "X Axis",
        WorkerWcfAxisIdentifierValue.Y => "Y Axis",
        WorkerWcfAxisIdentifierValue.Z => "Z Axis",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(WorkerBaseColorTypeValue value) => value switch { WorkerBaseColorTypeValue.Red => "Red", WorkerBaseColorTypeValue.Green => "Green", WorkerBaseColorTypeValue.Blue => "Blue", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerBaseMidColorTypeValue value) => value switch { WorkerBaseMidColorTypeValue.Red => "Red", WorkerBaseMidColorTypeValue.Green => "Green", WorkerBaseMidColorTypeValue.Gray => "Gray", WorkerBaseMidColorTypeValue.Blue => "Blue", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerChartTypeValue value) => value switch { WorkerChartTypeValue.RunChart => "Run Chart", WorkerChartTypeValue.IndividualXMovingRange => "Individual X - Moving Range", WorkerChartTypeValue.BullseyeChart => "Bullseye Chart", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerCollimationBaselineTypeValue value) => value switch { WorkerCollimationBaselineTypeValue.DeterminedByValue => "Determined By Value", WorkerCollimationBaselineTypeValue.DeterminedFromScale => "Determined From Scale", WorkerCollimationBaselineTypeValue.DeterminedFromKnownPoint => "Determined From Known Point", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerCollimationTypeValue value) => value switch { WorkerCollimationTypeValue.FullCollimation => "Full Collimation", WorkerCollimationTypeValue.NoTiltCollimation => "No-Tilt Collimation", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerColorRangeMethodValue value) => value switch { WorkerColorRangeMethodValue.SingleColor => "Single Color", WorkerColorRangeMethodValue.Continuous => "Continuous", WorkerColorRangeMethodValue.TolerancedContinuous => "Toleranced (Continuous)", WorkerColorRangeMethodValue.TolerancedGoNoGo => "Toleranced (Go / No-Go)", WorkerColorRangeMethodValue.TolerancedGoNoGoWithWarning => "Toleranced (Go / No-Go With Warning)", WorkerColorRangeMethodValue.DiscreteColors => "Discrete Colors", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerCoordinateSystemTypeValue value) => value switch { WorkerCoordinateSystemTypeValue.Cartesian => "Cartesian", WorkerCoordinateSystemTypeValue.Cylindric => "Cylindric", WorkerCoordinateSystemTypeValue.Polar => "Polar", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerVectorComponentValue value) => value switch { WorkerVectorComponentValue.X => "X", WorkerVectorComponentValue.Y => "Y", WorkerVectorComponentValue.Z => "Z", WorkerVectorComponentValue.Magnitude => "Magnitude", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerDynamicCircleModeValue value) => value switch { WorkerDynamicCircleModeValue.CylinderPlaneHoldPlaneNormal => "Cylinder and Plane Intersection - Hold Plane Normal", WorkerDynamicCircleModeValue.CylinderPlaneHoldCylinderAxis => "Cylinder and Plane Intersection - Hold Cylinder Axis", WorkerDynamicCircleModeValue.ConePlaneHoldPlaneNormal => "Cone and Plane Intersection - Hold Plane Normal", WorkerDynamicCircleModeValue.ConePlaneHoldConeAxis => "Cone and Plane Intersection - Hold Cone Axis", WorkerDynamicCircleModeValue.SpherePlaneIntersection => "Sphere and Plane Intersection", WorkerDynamicCircleModeValue.TwoConesIntersection => "Two Cones Intersection", WorkerDynamicCircleModeValue.ConeCylinderIntersection => "Cone and Cylinder Intersection", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerDynamicEllipseModeValue value) => value switch { WorkerDynamicEllipseModeValue.CylinderPlaneIntersection => "Cylinder and Plane Intersection", WorkerDynamicEllipseModeValue.ConePlaneIntersection => "Cone and Plane Intersection", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerDynamicLineModeValue value) => value switch { WorkerDynamicLineModeValue.ConeAxis => "Cone Axis", WorkerDynamicLineModeValue.CylinderAxis => "Cylinder Axis", WorkerDynamicLineModeValue.IntersectionOfTwoPlanes => "Intersection of Two Planes", WorkerDynamicLineModeValue.BisectTwoLines => "Bisect Two Lines", WorkerDynamicLineModeValue.SlotCenterlineAlongLength => "Slot Centerline Along Length", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerDynamicPlaneModeValue value) => value switch { WorkerDynamicPlaneModeValue.BisectTwoPlanes => "Bisect Two Planes", WorkerDynamicPlaneModeValue.TwoConesBestFitPlane => "Two Cones Intersection - Hold Normal to Best-Fit Plane", WorkerDynamicPlaneModeValue.TwoConesFirstConeAxis => "Twp Cones Intersection - Hold Normal to First Cone Axis", WorkerDynamicPlaneModeValue.TwoConesSecondConeAxis => "Two Cones Intersection - Hold Normal to Second Cone Axis", WorkerDynamicPlaneModeValue.ConeCylinderBestFitPlane => "Cone and Cylinder Intersection - Hold Normal to Best-Fit Plane", WorkerDynamicPlaneModeValue.ConeCylinderConeAxis => "Cone and Cylinder Intersection - Hold Normal to Cone Axis", WorkerDynamicPlaneModeValue.ConeCylinderCylinderAxis => "Cone and Cylinder Intersection - Hold Normal to Cylinder Axis", WorkerDynamicPlaneModeValue.OffsetPlaneFromPlane => "Offset Plane From Plane", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerDynamicPointModeValue value) => value switch { WorkerDynamicPointModeValue.IntersectionLinePlane => "Intersection of Line and Plane", WorkerDynamicPointModeValue.IntersectionCylinderPlane => "Intersection of Cylinder and Plane", WorkerDynamicPointModeValue.IntersectionConePlane => "Intersection of Cone and Plane", WorkerDynamicPointModeValue.IntersectionThreePlanes => "Intersection of Three Planes", WorkerDynamicPointModeValue.MidPointPerpendicularTwoLines => "Mid-Point of Perpendicular to Two Lines", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerEdgeModeValue value) => value switch { WorkerEdgeModeValue.IncludeEdges => "Include Edges", WorkerEdgeModeValue.ExcludeEdges => "Exclude Edges", WorkerEdgeModeValue.EdgesOnly => "Edges Only", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerExportDataDelimiterTypeValue value) => value switch { WorkerExportDataDelimiterTypeValue.Space => "Space", WorkerExportDataDelimiterTypeValue.Comma => "Comma", WorkerExportDataDelimiterTypeValue.Tab => "Tab", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerExportTargetNameFormatValue value) => value switch { WorkerExportTargetNameFormatValue.CollectionGroupTarget => "Collection Group Target", WorkerExportTargetNameFormatValue.GroupTarget => "Group Target", WorkerExportTargetNameFormatValue.Target => "Target", WorkerExportTargetNameFormatValue.None => "None", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerExportVectorNameFormatValue value) => value switch { WorkerExportVectorNameFormatValue.CollectionGroupVector => "Collection Group Vector", WorkerExportVectorNameFormatValue.GroupVector => "Group Vector", WorkerExportVectorNameFormatValue.Vector => "Vector", WorkerExportVectorNameFormatValue.None => "None", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerGeometryTypeValue value) => value switch { WorkerGeometryTypeValue.Line => "Line", WorkerGeometryTypeValue.Plane => "Plane", WorkerGeometryTypeValue.Circle => "Circle", WorkerGeometryTypeValue.Sphere => "Sphere", WorkerGeometryTypeValue.Cylinder => "Cylinder", WorkerGeometryTypeValue.Cone => "Cone", WorkerGeometryTypeValue.Paraboloid => "Paraboloid", WorkerGeometryTypeValue.Ellipse => "Ellipse", WorkerGeometryTypeValue.Slot => "Slot", WorkerGeometryTypeValue.Torus => "Torus", _ => throw Unknown(value) };

    public static string ToSdkString(WorkerInstrumentTypeValue value) => value switch
    {
        WorkerInstrumentTypeValue.AiconDpa => "AICON DPA",
        WorkerInstrumentTypeValue.AiconMoveInspect => "AICON MoveInspect",
        WorkerInstrumentTypeValue.ApiIlt => "API iLT",
        WorkerInstrumentTypeValue.AssemblyGuidanceLaserProjector => "Assembly Guidance Laser Projector",
        WorkerInstrumentTypeValue.CreaformVxElements => "Creaform VXelements",
        WorkerInstrumentTypeValue.DigitalNetworkLevel => "Digital Network Level",
        WorkerInstrumentTypeValue.FaroArm15m6Dof => "FARO Arm 1.5m 6 dof (QuantumS, QuantumM)",
        WorkerInstrumentTypeValue.FaroArm25m6Dof => "FARO Arm 2.5m 6 dof (QuantumS, QuantumM)",
        WorkerInstrumentTypeValue.FaroArm25m7Dof => "FARO Arm 2.5m 7 dof (QuantumS, QuantumM, Quantum Max)",
        WorkerInstrumentTypeValue.FaroArm2m6Dof => "FARO Arm 2m 6 dof (QuantumS, QuantumM)",
        WorkerInstrumentTypeValue.FaroArm2m7Dof => "FARO Arm 2m 7 dof (QuantumS, QuantumM, Quantum Max)",
        WorkerInstrumentTypeValue.FaroArm35m6Dof => "FARO Arm 3.5m 6 dof (QuantumS, QuantumM)",
        WorkerInstrumentTypeValue.FaroArm35m7Dof => "FARO Arm 3.5m 7 dof (QuantumS, QuantumM, Quantum Max)",
        WorkerInstrumentTypeValue.FaroArm3m6Dof => "FARO Arm 3m 6 dof (QuantumS, QuantumM)",
        WorkerInstrumentTypeValue.FaroArm3m7Dof => "FARO Arm 3m 7 dof (QuantumS, QuantumM, Quantum Max)",
        WorkerInstrumentTypeValue.FaroArm4m6Dof => "FARO Arm 4m 6 dof (QuantumS, QuantumM)",
        WorkerInstrumentTypeValue.FaroArm4m7Dof => "FARO Arm 4m 7 dof (QuantumS, QuantumM, Quantum Max)",
        WorkerInstrumentTypeValue.FaroScannerPhotonLsFocus3d => "Faro Scanner Photon/LS/Focus 3D",
        WorkerInstrumentTypeValue.GenericAuxDevice => "Generic Aux Device",
        WorkerInstrumentTypeValue.GenericAuxDevice2 => "Generic Aux Device 2",
        WorkerInstrumentTypeValue.GenericPhotogrammetrySystem => "Generic Photogrammetry System",
        WorkerInstrumentTypeValue.GenericPhotogrammetrySystem2 => "Generic Photogrammetry System 2",
        WorkerInstrumentTypeValue.LapCadProLaserProjector => "LAP CAD-Pro Laser Projector",
        WorkerInstrumentTypeValue.LeicaGeosystemsRtc360 => "Leica Geosystems RTC360",
        WorkerInstrumentTypeValue.LeicaGeosystemsScanStationPxx => "Leica Geosystems ScanStation PXX",
        WorkerInstrumentTypeValue.LeicaT1200TotalStation => "Leica T1200 Total Station",
        WorkerInstrumentTypeValue.LeicaTm6100aTheodolite => "Leica TM6100A Theodolite",
        WorkerInstrumentTypeValue.LeicaTs09TotalStation => "Leica TS09 Total Station",
        WorkerInstrumentTypeValue.LeicaTs15TotalStation => "Leica TS15 Total Station",
        WorkerInstrumentTypeValue.LeicaTs16TotalStation => "Leica TS16 Total Station",
        WorkerInstrumentTypeValue.LeicaTs20TotalStation => "Leica TS20 Total Station",
        WorkerInstrumentTypeValue.LeicaTs30TotalStation => "Leica TS30 Total Station",
        WorkerInstrumentTypeValue.LptLaserProjector => "LPT Laser Projector",
        WorkerInstrumentTypeValue.SaOpenAuxiliaryInstrument => "SA Open Auxiliary Instrument",
        WorkerInstrumentTypeValue.SaOpenInstrument => "SA Open Instrument",
        WorkerInstrumentTypeValue.SaPipeline => "SA Pipeline",
        WorkerInstrumentTypeValue.Surphaser10Scanner => "Surphaser 10 Scanner",
        WorkerInstrumentTypeValue.SurphaserScanner => "Surphaser Scanner",
        WorkerInstrumentTypeValue.ViconTracker => "Vicon Tracker",
        WorkerInstrumentTypeValue.XyzReferenceFrame => "XYZ Reference Frame",
        WorkerInstrumentTypeValue.AiconProCam3DProbe => "AICON ProCam 3D Probe",
        WorkerInstrumentTypeValue.ApiLadar => "API Ladar",
        WorkerInstrumentTypeValue.ApiLaserRail => "API Laser Rail",
        WorkerInstrumentTypeValue.ApiOmniTrac => "API OmniTrac",
        WorkerInstrumentTypeValue.ApiOmniTrac2 => "API OmniTrac2",
        WorkerInstrumentTypeValue.ApiRadian => "API Radian",
        WorkerInstrumentTypeValue.ApiRadianPlusCore => "API Radian Plus/Core",
        WorkerInstrumentTypeValue.ApiRadianPro => "API Radian Pro",
        WorkerInstrumentTypeValue.ApiTrackerDeviceInterface => "API Tracker Device Interface",
        WorkerInstrumentTypeValue.ApiTrackerIi => "API Tracker II",
        WorkerInstrumentTypeValue.ApiTrackerIii => "API Tracker III",
        WorkerInstrumentTypeValue.Axxis6100Arm26m6Dof => "Axxis 6-100 Arm (2.6m 6 dof)",
        WorkerInstrumentTypeValue.Axxis6200Arm32m6Dof => "Axxis 6-200 Arm (3.2m 6 dof)",
        WorkerInstrumentTypeValue.Axxis7100ArmProbe26m7Dof => "Axxis 7-100 Arm Probe (2.6m 7 dof)",
        WorkerInstrumentTypeValue.Axxis7100ArmScanner26m7Dof => "Axxis 7-100 Arm Scanner (2.6m 7 dof)",
        WorkerInstrumentTypeValue.CimCoreArm1024 => "CimCore Arm 1024",
        WorkerInstrumentTypeValue.CimCoreArm1028 => "CimCore Arm 1028",
        WorkerInstrumentTypeValue.CimCoreArm1030 => "CimCore Arm 1030",
        WorkerInstrumentTypeValue.CimCoreArm2200 => "CimCore Arm 2200",
        WorkerInstrumentTypeValue.CimCoreArm2500 => "CimCore Arm 2500",
        WorkerInstrumentTypeValue.CimCoreArm6Dof3012i501212m => "CimCore Arm 6DOF: 3012i, 5012, 1.2m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof3018i501818m => "CimCore Arm 6DOF: 3018i, 5018, 1.8m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof3024i502424m => "CimCore Arm 6DOF: 3024i, 5024, 2.4m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof3028i502828m => "CimCore Arm 6DOF: 3028i, 5028, 2.8m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof3036i503636m => "CimCore Arm 6DOF: 3036i, 5036, 3.6m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof511212m => "CimCore Arm 6DOF: 5112, 1.2m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof511818m => "CimCore Arm 6DOF: 5118, 1.8m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof512424m => "CimCore Arm 6DOF: 5124, 2.4m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof512828m => "CimCore Arm 6DOF: 5128, 2.8m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof513030m => "CimCore Arm 6DOF: 5130, 3.0m",
        WorkerInstrumentTypeValue.CimCoreArm6Dof513636m => "CimCore Arm 6DOF: 5136, 3.6m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5012Sc301212m => "CimCore Arm 7DOF: 5012Sc, 3012, 1.2m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5018Sc301818m => "CimCore Arm 7DOF: 5018Sc, 3018, 1.8m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5024Sc302424m => "CimCore Arm 7DOF: 5024Sc, 3024, 2.4m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5028Sc302828m => "CimCore Arm 7DOF: 5028Sc, 3028, 2.8m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5030Sc303030m => "CimCore Arm 7DOF: 5030Sc, 3030, 3.0m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5036Sc303636m => "CimCore Arm 7DOF: 5036Sc, 3036, 3.6m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5112Sc12m => "CimCore Arm 7DOF: 5112Sc, 1.2m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5118Sc18m => "CimCore Arm 7DOF: 5118Sc, 1.8m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5124Sc24m => "CimCore Arm 7DOF: 5124Sc, 2.4m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5128Sc28m => "CimCore Arm 7DOF: 5128Sc, 2.8m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5130Sc30m => "CimCore Arm 7DOF: 5130Sc, 3.0m",
        WorkerInstrumentTypeValue.CimCoreArm7Dof5136Sc36m => "CimCore Arm 7DOF: 5136Sc, 3.6m",
        WorkerInstrumentTypeValue.CubicKitTheodolite => "Cubic KIT Theodolite",
        WorkerInstrumentTypeValue.DavisPerceptionIiWeatherStation => "Davis Perception II Weather Station",
        WorkerInstrumentTypeValue.FaroArm => "FARO Arm",
        WorkerInstrumentTypeValue.FaroArmG04 => "FARO Arm G04",
        WorkerInstrumentTypeValue.FaroArmG04057Dof => "FARO Arm G04-05 (7dof)",
        WorkerInstrumentTypeValue.FaroArmG08 => "FARO Arm G08",
        WorkerInstrumentTypeValue.FaroArmG08057Dof => "FARO Arm G08-05 (7dof)",
        WorkerInstrumentTypeValue.FaroArmG12 => "FARO Arm G12",
        WorkerInstrumentTypeValue.FaroArmG12057Dof => "FARO Arm G12-05 (7dof)",
        WorkerInstrumentTypeValue.FaroArmS08 => "FARO Arm S08",
        WorkerInstrumentTypeValue.FaroArmS12 => "FARO Arm S12",
        WorkerInstrumentTypeValue.FaroArmUsb10FtQuantumFusionPrimePlatinum => "FARO Arm USB 10 ft. (Quantum, Fusion, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb10Ft7DofQuantumFusionPrimePlatinum => "FARO Arm USB 10 ft. 7 dof (Quantum, Fusion, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb12FtQuantumFusionPrimePlatinum => "FARO Arm USB 12 ft. (Quantum, Fusion, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb12Ft7DofEdgeQuantumFusionPrimePlatinum => "FARO Arm USB 12 ft. 7 dof (Edge, Quantum, Fusion, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb4FtQuantumPrimePlatinum => "FARO Arm USB 4 ft. (Quantum, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb4Ft7DofQuantumPrimePlatinum => "FARO Arm USB 4 ft. 7 dof (Quantum, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb6FtQuantumFusionPrimePlatinum => "FARO Arm USB 6 ft. (Quantum, Fusion, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb6Ft7DofEdgeQuantumFusionPrimePlatinum => "FARO Arm USB 6 ft. 7 dof (Edge, Quantum, Fusion, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb8FtQuantumFusionPrimePlatinum => "FARO Arm USB 8 ft.  (Quantum, Fusion, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb8Ft7DofQuantumFusionPrimePlatinum => "FARO Arm USB 8 ft. 7 dof (Quantum, Fusion, Prime, Platinum)",
        WorkerInstrumentTypeValue.FaroArmUsb9Ft7DofEdge => "FARO Arm USB 9 ft. 7 dof (Edge)",
        WorkerInstrumentTypeValue.FaroIonTracker => "Faro Ion Tracker",
        WorkerInstrumentTypeValue.FaroTracker => "Faro Tracker",
        WorkerInstrumentTypeValue.FaroVantage => "Faro Vantage",
        WorkerInstrumentTypeValue.GsiVStarsPhotogrammetrySystem => "GSI V-STARS Photogrammetry System",
        WorkerInstrumentTypeValue.HexagonAbsolute86Dof12mCompact => "Hexagon Absolute 8 6dof-1.2m Compact",
        WorkerInstrumentTypeValue.HexagonAbsolute86Dof25m => "Hexagon Absolute 8 6dof-2.5m",
        WorkerInstrumentTypeValue.HexagonAbsolute86Dof2m => "Hexagon Absolute 8 6dof-2m",
        WorkerInstrumentTypeValue.HexagonAbsolute86Dof35m => "Hexagon Absolute 8 6dof-3.5m",
        WorkerInstrumentTypeValue.HexagonAbsolute86Dof3m => "Hexagon Absolute 8 6dof-3m",
        WorkerInstrumentTypeValue.HexagonAbsolute86Dof45m => "Hexagon Absolute 8 6dof-4.5m",
        WorkerInstrumentTypeValue.HexagonAbsolute86Dof4m => "Hexagon Absolute 8 6dof-4m",
        WorkerInstrumentTypeValue.HexagonAbsolute87Dof25m => "Hexagon Absolute 8 7dof-2.5m",
        WorkerInstrumentTypeValue.HexagonAbsolute87Dof2m => "Hexagon Absolute 8 7dof-2m",
        WorkerInstrumentTypeValue.HexagonAbsolute87Dof35m => "Hexagon Absolute 8 7dof-3.5m",
        WorkerInstrumentTypeValue.HexagonAbsolute87Dof3m => "Hexagon Absolute 8 7dof-3m",
        WorkerInstrumentTypeValue.HexagonAbsolute87Dof45m => "Hexagon Absolute 8 7dof-4.5m",
        WorkerInstrumentTypeValue.HexagonAbsolute87Dof4m => "Hexagon Absolute 8 7dof-4m",
        WorkerInstrumentTypeValue.HexagonHandheld3DScanner => "Hexagon Handheld 3D Scanner",
        WorkerInstrumentTypeValue.ImportedMeasurementsWithUncertainty => "Imported Measurements with Uncertainty",
        WorkerInstrumentTypeValue.KernE2Theodolite => "Kern E2 Theodolite",
        WorkerInstrumentTypeValue.KreonApiAce620 => "Kreon/API Ace-6-20",
        WorkerInstrumentTypeValue.KreonApiAce625 => "Kreon/API Ace-6-25",
        WorkerInstrumentTypeValue.KreonApiAce630 => "Kreon/API Ace-6-30",
        WorkerInstrumentTypeValue.KreonApiAce635 => "Kreon/API Ace-6-35",
        WorkerInstrumentTypeValue.KreonApiAce640 => "Kreon/API Ace-6-40",
        WorkerInstrumentTypeValue.KreonApiAce645 => "Kreon/API Ace-6-45",
        WorkerInstrumentTypeValue.KreonApiAce720 => "Kreon/API Ace-7-20",
        WorkerInstrumentTypeValue.KreonApiAce725 => "Kreon/API Ace-7-25",
        WorkerInstrumentTypeValue.KreonApiAce730 => "Kreon/API Ace-7-30",
        WorkerInstrumentTypeValue.KreonApiAce735 => "Kreon/API Ace-7-35",
        WorkerInstrumentTypeValue.KreonApiAce740 => "Kreon/API Ace-7-40",
        WorkerInstrumentTypeValue.KreonApiAce745 => "Kreon/API Ace-7-45",
        WorkerInstrumentTypeValue.LeicaAt500 => "Leica AT500",
        WorkerInstrumentTypeValue.LeicaAt960930 => "Leica AT960/930",
        WorkerInstrumentTypeValue.LeicaAts600 => "Leica ATS600",
        WorkerInstrumentTypeValue.LeicaAts800 => "Leica ATS800",
        WorkerInstrumentTypeValue.LeicaEmSconAbsoluteTrackerAt901Series => "Leica emScon Absolute Tracker (AT901 Series)",
        WorkerInstrumentTypeValue.LeicaEmSconAt401 => "Leica emScon AT401",
        WorkerInstrumentTypeValue.LeicaEmSconAt402 => "Leica emScon AT402",
        WorkerInstrumentTypeValue.LeicaEmSconAt403 => "Leica emScon AT403",
        WorkerInstrumentTypeValue.LeicaEmSconTrackerLt500800Series => "Leica emScon Tracker (LT500-800 Series)",
        WorkerInstrumentTypeValue.LeicaNovaMs50TotalStation => "Leica Nova MS50 Total Station",
        WorkerInstrumentTypeValue.LeicaNovaMs60TotalStation => "Leica Nova MS60 Total Station",
        WorkerInstrumentTypeValue.LeicaTda5005TotalStationGeoCOM => "Leica TDA5005 Total Station (GeoCOM)",
        WorkerInstrumentTypeValue.LeicaTdra6000TotalStation => "Leica TDRA6000 Total Station",
        WorkerInstrumentTypeValue.LeicaTotalStationTc2000Tc2002 => "Leica Total Station TC2000, TC2002",
        WorkerInstrumentTypeValue.LeicaTpsTheodolite1800 => "Leica TPS Theodolite (1800)",
        WorkerInstrumentTypeValue.LeicaTpsTheodolite5100 => "Leica TPS Theodolite (5100)",
        WorkerInstrumentTypeValue.LeicaTpsTotalStation200350005005 => "Leica TPS Total Station (2003,5000,5005)",
        WorkerInstrumentTypeValue.LeicaTrackerTpLink => "Leica Tracker TP-LINK",
        WorkerInstrumentTypeValue.LeicaWildTheodolitesT2000T2002T3000 => "Leica/Wild Theodolites T2000,T2002,T3000",
        WorkerInstrumentTypeValue.MetronorPortableMeasurementSystem => "METRONOR Portable Measurement System",
        WorkerInstrumentTypeValue.MitutoyoSpaceTracA => "Mitutoyo SpaceTrac-A",
        WorkerInstrumentTypeValue.MitutoyoSpaceTracAi => "Mitutoyo SpaceTrac-AI",
        WorkerInstrumentTypeValue.MitutoyoSpaceTracAp => "Mitutoyo SpaceTrac-AP",
        WorkerInstrumentTypeValue.NikonMetrologyApdisMv400 => "Nikon Metrology APDIS MV400",
        WorkerInstrumentTypeValue.NikonMetrologyLaserRadarMv200 => "Nikon Metrology Laser Radar MV200",
        WorkerInstrumentTypeValue.NikonMetrologyLaserRadarMv300 => "Nikon Metrology Laser Radar MV300",
        WorkerInstrumentTypeValue.NikonMetrologySurveyorV2 => "Nikon Metrology Surveyor v2",
        WorkerInstrumentTypeValue.Nivel20TwoAxisLevel => "Nivel 20 Two Axis Level",
        WorkerInstrumentTypeValue.OnTrakLaserLineSystemOt4040Ot6000 => "On-Trak Laser Line System (OT-4040, OT-6000)",
        WorkerInstrumentTypeValue.RomerAbsolute7315 => "Romer Absolute 7315",
        WorkerInstrumentTypeValue.RomerAbsolute7x20 => "Romer Absolute 7x20",
        WorkerInstrumentTypeValue.RomerAbsolute7x20SiSe => "Romer Absolute 7x20SI/SE",
        WorkerInstrumentTypeValue.RomerAbsolute7x25 => "Romer Absolute 7x25",
        WorkerInstrumentTypeValue.RomerAbsolute7x25SiSe => "Romer Absolute 7x25SI/SE",
        WorkerInstrumentTypeValue.RomerAbsolute7x30 => "Romer Absolute 7x30",
        WorkerInstrumentTypeValue.RomerAbsolute7x30SiSe => "Romer Absolute 7x30SI/SE",
        WorkerInstrumentTypeValue.RomerAbsolute7x35 => "Romer Absolute 7x35",
        WorkerInstrumentTypeValue.RomerAbsolute7x35SiSe => "Romer Absolute 7x35SI/SE",
        WorkerInstrumentTypeValue.RomerAbsolute7x40 => "Romer Absolute 7x40",
        WorkerInstrumentTypeValue.RomerAbsolute7x40SiSe => "Romer Absolute 7x40SI/SE",
        WorkerInstrumentTypeValue.RomerAbsolute7x45 => "Romer Absolute 7x45",
        WorkerInstrumentTypeValue.RomerAbsolute7x45SiSe => "Romer Absolute 7x45SI/SE",
        WorkerInstrumentTypeValue.RomerMultiGage => "Romer Multi-Gage",
        WorkerInstrumentTypeValue.SokkiaNet1TotalStation => "Sokkia Net-1 Total Station",
        WorkerInstrumentTypeValue.SokkiaNet2TotalStation => "Sokkia Net-2 Total Station",
        WorkerInstrumentTypeValue.SokkiaNet05AXTotalStation => "Sokkia Net05AX Total Station",
        WorkerInstrumentTypeValue.SokkiaNet05XTotalStation => "Sokkia Net05X Total Station",
        WorkerInstrumentTypeValue.SokkiaSetxTotalStation => "Sokkia SETX Total Station",
        WorkerInstrumentTypeValue.ThommenHm30WeatherStation => "Thommen HM30 Weather Station",
        WorkerInstrumentTypeValue.TopconMsAxSeriesTotalStation => "Topcon MS AX Series Total Station",
        WorkerInstrumentTypeValue.UltrasonicThicknessGaugeCl400 => "Ultrasonic Thickness Gauge (CL400)",
        WorkerInstrumentTypeValue.VirtekLaserProjector => "Virtek Laser Projector",
        WorkerInstrumentTypeValue.ZeissETh2Theodolite => "Zeiss ETh 2 Theodolite",
        _ => throw Unknown(value)
    };

    private static readonly FrozenDictionary<WorkerObjectTypeValue, string> ObjectNames =
        new Dictionary<WorkerObjectTypeValue, string>
        {
            [WorkerObjectTypeValue.Any] = "Any",
            [WorkerObjectTypeValue.BSpline] = "B-Spline",
            [WorkerObjectTypeValue.Circle] = "Circle",
            [WorkerObjectTypeValue.Cloud] = "Cloud",
            [WorkerObjectTypeValue.EnhancedCloud] = "Enhanced Cloud",
            [WorkerObjectTypeValue.ScanStripeCloud] = "Scan Stripe Cloud",
            [WorkerObjectTypeValue.CrossSectionCloud] = "Cross Section Cloud",
            [WorkerObjectTypeValue.Cone] = "Cone",
            [WorkerObjectTypeValue.Cylinder] = "Cylinder",
            [WorkerObjectTypeValue.Datum] = "Datum",
            [WorkerObjectTypeValue.Ellipse] = "Ellipse",
            [WorkerObjectTypeValue.Frame] = "Frame",
            [WorkerObjectTypeValue.FrameSet] = "Frame Set",
            [WorkerObjectTypeValue.Line] = "Line",
            [WorkerObjectTypeValue.Paraboloid] = "Paraboloid",
            [WorkerObjectTypeValue.Perimeter] = "Perimeter",
            [WorkerObjectTypeValue.Plane] = "Plane",
            [WorkerObjectTypeValue.PointGroup] = "Point Group",
            [WorkerObjectTypeValue.PointSet] = "Point Set",
            [WorkerObjectTypeValue.PolySurface] = "Poly Surface",
            [WorkerObjectTypeValue.ScanStripeMesh] = "Scan Stripe Mesh",
            [WorkerObjectTypeValue.Slot] = "Slot",
            [WorkerObjectTypeValue.Sphere] = "Sphere",
            [WorkerObjectTypeValue.Surface] = "Surface",
            [WorkerObjectTypeValue.Torus] = "Torus",
            [WorkerObjectTypeValue.VectorGroup] = "Vector Group",
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, WorkerObjectTypeValue> ObjectValues =
        ObjectNames.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public static string ToSdkString(WorkerObjectTypeValue value) =>
        ObjectNames.TryGetValue(value, out var name) ? name : throw Unknown(value);

    private static readonly FrozenDictionary<WorkerItemTypeValue, string> ItemNames =
        new Dictionary<WorkerItemTypeValue, string>
        {
            [WorkerItemTypeValue.Any] = "Any",
            [WorkerItemTypeValue.Alignment] = "Alignment",
            [WorkerItemTypeValue.Annotation] = "Annotation",
            [WorkerItemTypeValue.BSpline] = "B-Spline",
            [WorkerItemTypeValue.CalibrationApplianceNode] = "Calibration Appliance Node",
            [WorkerItemTypeValue.CalloutView] = "Callout View",
            [WorkerItemTypeValue.Chart] = "Chart",
            [WorkerItemTypeValue.Circle] = "Circle",
            [WorkerItemTypeValue.Cloud] = "Cloud",
            [WorkerItemTypeValue.EnhancedCloud] = "Enhanced Cloud",
            [WorkerItemTypeValue.ScanStripeCloud] = "Scan Stripe Cloud",
            [WorkerItemTypeValue.CrossSectionCloud] = "Cross Section Cloud",
            [WorkerItemTypeValue.Cone] = "Cone",
            [WorkerItemTypeValue.Cylinder] = "Cylinder",
            [WorkerItemTypeValue.Datum] = "Datum",
            [WorkerItemTypeValue.Dimension] = "Dimension",
            [WorkerItemTypeValue.Ellipse] = "Ellipse",
            [WorkerItemTypeValue.Event] = "Event",
            [WorkerItemTypeValue.FeatureCheck] = "Feature Check",
            [WorkerItemTypeValue.Frame] = "Frame",
            [WorkerItemTypeValue.FrameSet] = "Frame Set",
            [WorkerItemTypeValue.Line] = "Line",
            [WorkerItemTypeValue.Paraboloid] = "Paraboloid",
            [WorkerItemTypeValue.Perimeter] = "Perimeter",
            [WorkerItemTypeValue.Picture] = "Picture",
            [WorkerItemTypeValue.Plane] = "Plane",
            [WorkerItemTypeValue.PointGroup] = "Point Group",
            [WorkerItemTypeValue.PointSet] = "Point Set",
            [WorkerItemTypeValue.PolySurface] = "Poly Surface",
            [WorkerItemTypeValue.Relationship] = "Relationship",
            [WorkerItemTypeValue.SaDoc] = "SA Doc",
            [WorkerItemTypeValue.SaReport] = "SA Report",
            [WorkerItemTypeValue.SaReportTemplate] = "SA Report Template",
            [WorkerItemTypeValue.ScaleBar] = "Scale Bar",
            [WorkerItemTypeValue.ScanStripeMesh] = "Scan Stripe Mesh",
            [WorkerItemTypeValue.Slot] = "Slot",
            [WorkerItemTypeValue.Sphere] = "Sphere",
            [WorkerItemTypeValue.Surface] = "Surface",
            [WorkerItemTypeValue.Table] = "Table",
            [WorkerItemTypeValue.TcpFixture] = "TCP Fixture",
            [WorkerItemTypeValue.Torus] = "Torus",
            [WorkerItemTypeValue.VectorGroup] = "Vector Group",
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, WorkerItemTypeValue> ItemValues =
        ItemNames.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public static string ToSdkString(WorkerItemTypeValue value) =>
        ItemNames.TryGetValue(value, out var name) ? name : throw Unknown(value);

    public static bool TryParseObjectType(string value, out WorkerObjectTypeValue result) =>
        ObjectValues.TryGetValue(value, out result);

    public static bool TryParseItemType(string value, out WorkerItemTypeValue result) =>
        ItemValues.TryGetValue(value, out result);

    public static string ToSdkString(WorkerOffsetDirectionTypeValue value) => value switch { WorkerOffsetDirectionTypeValue.Both => "Both", WorkerOffsetDirectionTypeValue.PositiveOnly => "Positive only", WorkerOffsetDirectionTypeValue.NegativeOnly => "Negative only", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerPointFilterInputTypeValue value) => value switch { WorkerPointFilterInputTypeValue.CardinalPoints => "Cardinal Points", WorkerPointFilterInputTypeValue.InputPoints => "Input Points", WorkerPointFilterInputTypeValue.NominalCardinalPoints => "Nominal Cardinal Points", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerRelationshipWeightingModeValue value) => value switch { WorkerRelationshipWeightingModeValue.NormalizeEquationCount => "Normalize on equation count", WorkerRelationshipWeightingModeValue.NormalizeEquationCountAndToleranceWidth => "Normalize on equation count AND tolerance width", WorkerRelationshipWeightingModeValue.ResetAllWeights => "Reset All weights to 1.0", WorkerRelationshipWeightingModeValue.NormalizeSquareRootEquationCount => "Normalize on square root of equation count", WorkerRelationshipWeightingModeValue.NormalizeSquareRootAndToleranceWidth => "Normalize on square root AND tolerance width", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerRenderModeTypeValue value) => value switch { WorkerRenderModeTypeValue.Wireframe => "Wireframe", WorkerRenderModeTypeValue.HiddenLineRemoved => "Hidden Line Removed", WorkerRenderModeTypeValue.SolidAndEdges => "Solid+Edges", WorkerRenderModeTypeValue.Solid => "Solid", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerReportPageOrientationValue value) => value switch { WorkerReportPageOrientationValue.Portrait => "Portrait", WorkerReportPageOrientationValue.Landscape => "Landscape", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerSaturationLimitTypeValue value) => value switch { WorkerSaturationLimitTypeValue.Deviation => "Deviation", WorkerSaturationLimitTypeValue.SigmaRule => "Sigma Rule", WorkerSaturationLimitTypeValue.Custom => "Custom", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerShowUsmnDialogTypeValue value) => value switch { WorkerShowUsmnDialogTypeValue.No => "No", WorkerShowUsmnDialogTypeValue.Yes => "Yes", WorkerShowUsmnDialogTypeValue.OnToleranceViolation => "On Tolerance Violation", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerSurfaceAnalysisModeValue value) => value switch { WorkerSurfaceAnalysisModeValue.None => "None", WorkerSurfaceAnalysisModeValue.Relationship => "Relationship", WorkerSurfaceAnalysisModeValue.Normals => "Normals", WorkerSurfaceAnalysisModeValue.Curvature => "Curvature", WorkerSurfaceAnalysisModeValue.DeviationRms => "Deviation RMS", WorkerSurfaceAnalysisModeValue.DeviationMax => "Deviation MAX", WorkerSurfaceAnalysisModeValue.DeviationAverage => "Deviation AVG", WorkerSurfaceAnalysisModeValue.DeviationMin => "Deviation MIN", WorkerSurfaceAnalysisModeValue.DeviationMaxAbsolute => "Deviation MAX ABS", WorkerSurfaceAnalysisModeValue.DeviationMaxDelta => "Deviation MAX DELTA", WorkerSurfaceAnalysisModeValue.PseudoSurface => "Pseudo Surface", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerSurfaceDissectionModeTypeValue value) => value switch { WorkerSurfaceDissectionModeTypeValue.EntireSolid => "Entire Solid", WorkerSurfaceDissectionModeTypeValue.SelectFaces => "Select Faces", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerTargetComputationMethodValue value) => value switch { WorkerTargetComputationMethodValue.UseMostRecentShotFromEachFace => "Use most recent shot from each face", WorkerTargetComputationMethodValue.UseOnlyMostRecentShot => "Use only most recent shot", WorkerTargetComputationMethodValue.DoNotChangePriorMeasurements => "Do not change prior measurements at all", WorkerTargetComputationMethodValue.ForceNewPointForEachMeasurement => "Force a new point for each measurement", WorkerTargetComputationMethodValue.RemoveAllPriorShots => "Remove all prior shots", WorkerTargetComputationMethodValue.DeactivateAllPriorShots => "Deactivate all prior shots", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerTranslucencyTypeValue value) => value switch { WorkerTranslucencyTypeValue.Solid => "Solid", WorkerTranslucencyTypeValue.Translucent => "Translucent", WorkerTranslucencyTypeValue.Wireframe => "Wireframe", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerCompTechniqueValue value) => value switch { WorkerCompTechniqueValue.Standard => "Standard", WorkerCompTechniqueValue.MaxInscribed => "Max Inscribed", WorkerCompTechniqueValue.MinCircumscribed => "Min Circumscribed", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerDegreeOfFreedomValue value) => value switch { WorkerDegreeOfFreedomValue.Any => "Any", WorkerDegreeOfFreedomValue.LockFocusLocation => "Lock Focus Location", WorkerDegreeOfFreedomValue.LockVertexLocation => "Lock Vertex Location", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerFitMethodValue value) => value switch { WorkerFitMethodValue.MinimumRms => "Minimum RMS", WorkerFitMethodValue.BestAxis => "Best Axis", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerMeasuredSideForPlanarOffsetValue value) => value switch { WorkerMeasuredSideForPlanarOffsetValue.AbovePlane => "Above Plane", WorkerMeasuredSideForPlanarOffsetValue.ProbeCenter => "Probe Center", WorkerMeasuredSideForPlanarOffsetValue.BelowPlane => "Below Plane", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerMeasuredSideForRadialOffsetValue value) => value switch { WorkerMeasuredSideForRadialOffsetValue.Inside => "Inside", WorkerMeasuredSideForRadialOffsetValue.ProbeCenter => "Probe Center", WorkerMeasuredSideForRadialOffsetValue.Outside => "Outside", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerMpDialogInteractionModeValue value) => value switch { WorkerMpDialogInteractionModeValue.BlockApplicationInteraction => "Block Application Interaction", WorkerMpDialogInteractionModeValue.AllowApplicationInteraction => "Allow Application Interaction", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerMpInteractionModeValue value) => value switch { WorkerMpInteractionModeValue.HaltOnFailureOnly => "Halt on Failure Only", WorkerMpInteractionModeValue.HaltOnFailureOrPartialSuccess => "Halt on Failure or Partial Success", WorkerMpInteractionModeValue.NeverHalt => "Never Halt", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerNormalDirectionValue value) => value switch { WorkerNormalDirectionValue.ProbingDirection => "Probing Direction", WorkerNormalDirectionValue.WorkingOriginPositive => "Working Origin Positive", WorkerNormalDirectionValue.RightHandRule => "Right Hand Rule", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerSaInteractionModeValue value) => value switch { WorkerSaInteractionModeValue.Manual => "Manual", WorkerSaInteractionModeValue.Automatic => "Automatic", WorkerSaInteractionModeValue.Silent => "Silent", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerSlotTypeValue value) => value switch { WorkerSlotTypeValue.Round => "Round", WorkerSlotTypeValue.Square => "Square", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerSphereFitComputationModeValue value) => value switch { WorkerSphereFitComputationModeValue.Standard => "Standard", WorkerSphereFitComputationModeValue.MaxInscribed => "Max Inscribed", WorkerSphereFitComputationModeValue.MinCircumscribed => "Min Circumscribed", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerWindowStateValue value) => value switch { WorkerWindowStateValue.Maximize => "Maximize", WorkerWindowStateValue.Minimize => "Minimize", WorkerWindowStateValue.Restore => "Restore", WorkerWindowStateValue.Show => "Show", WorkerWindowStateValue.Hide => "Hide", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerSystemStringValue value) => value switch
    {
        WorkerSystemStringValue.SaVersion => "SA Version",
        WorkerSystemStringValue.XitFilename => "XIT Filename",
        WorkerSystemStringValue.MpFilename => "MP Filename",
        WorkerSystemStringValue.MpFilenameFullPath => "MP Filename Full Path",
        WorkerSystemStringValue.DateAndTime => "Date and Time",
        WorkerSystemStringValue.Date => "Date",
        WorkerSystemStringValue.DateShort => "Date Short",
        WorkerSystemStringValue.Time => "Time",
        WorkerSystemStringValue.KeySerialNumber => "Key Serial Number",
        WorkerSystemStringValue.CompanyName => "Company Name",
        WorkerSystemStringValue.UserName => "User Name",
        WorkerSystemStringValue.LicenseUserName => "License User Name",
        WorkerSystemStringValue.WindowsUserName => "Windows User Name",
        WorkerSystemStringValue.ComputerName => "Computer Name",
        _ => throw Unknown(value)
    };
    public static string ToSdkString(WorkerGdtDistanceBetweenModeValue value) => value switch
    {
        WorkerGdtDistanceBetweenModeValue.Centroid => "Centroid",
        WorkerGdtDistanceBetweenModeValue.MinMax => "Min/Max",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(WorkerGdtEvaluationMethodValue value) => value switch
    {
        WorkerGdtEvaluationMethodValue.None => "None",
        WorkerGdtEvaluationMethodValue.Asme1994 => "ASME 1994",
        WorkerGdtEvaluationMethodValue.Asme2009 => "ASME 2009",
        WorkerGdtEvaluationMethodValue.Asme2018 => "ASME 2018",
        WorkerGdtEvaluationMethodValue.Iso1983 => "ISO 1983",
        WorkerGdtEvaluationMethodValue.Iso2004 => "ISO 2004",
        WorkerGdtEvaluationMethodValue.Iso2017 => "ISO 2017",
        _ => throw Unknown(value)
    };

    public static string ToSdkString(WorkerCloudThinningModeValue value) => value switch { WorkerCloudThinningModeValue.None => "None", WorkerCloudThinningModeValue.Random => "Random", WorkerCloudThinningModeValue.NthPoint => "Nth Point", _ => throw Unknown(value) };

    public static string ToSdkString(WorkerReportOutputTypeValue value) => value switch { WorkerReportOutputTypeValue.None => "None", WorkerReportOutputTypeValue.SaReport => "SAReport", WorkerReportOutputTypeValue.SaDocument => "SADoc", WorkerReportOutputTypeValue.Pdf => "PDF", WorkerReportOutputTypeValue.Rtf => "RTF", _ => throw Unknown(value) };
    public static string ToSdkString(WorkerReportViewTypeValue value) => value switch { WorkerReportViewTypeValue.None => "None", WorkerReportViewTypeValue.CurrentView => "Current View", WorkerReportViewTypeValue.CalloutView => "Callout View", _ => throw Unknown(value) };

    public static int ToSdkOffsetMode(WorkerOffsetDirectionTypeValue value) => value switch
    {
        WorkerOffsetDirectionTypeValue.Both => 0,
        WorkerOffsetDirectionTypeValue.PositiveOnly => 1,
        WorkerOffsetDirectionTypeValue.NegativeOnly => 2,
        _ => throw Unknown(value)
    };

    private static ArgumentOutOfRangeException Unknown<T>(T value) where T : struct, Enum =>
        new(nameof(value), value, $"Unknown {typeof(T).Name} value.");
}
