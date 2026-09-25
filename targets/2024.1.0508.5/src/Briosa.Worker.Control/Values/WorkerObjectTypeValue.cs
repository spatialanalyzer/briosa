namespace Briosa.Worker.Control;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "Discrete protocol values retain the reserved 2026-only numeric slot.")]
public enum WorkerObjectTypeValue
{
    Unspecified,
    Any,
    BSpline,
    Circle,
    Cloud,
    ScanStripeCloud = 6,
    CrossSectionCloud,
    Cone,
    Cylinder,
    Datum,
    Ellipse,
    Frame,
    FrameSet,
    Line,
    Paraboloid,
    Perimeter,
    Plane,
    PointGroup,
    PointSet,
    PolySurface,
    ScanStripeMesh,
    Slot,
    Sphere,
    Surface,
    Torus,
    VectorGroup
}
