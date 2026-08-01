using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations;

internal static class SpatialAnalyzerValueMapper
{
    public static Api.CollectionObjectName ToProtocol(
        WorkerCollectionObjectNameValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Api.CollectionObjectName
        {
            CollectionName = value.CollectionName,
            ObjectName = value.ObjectName,
            ObjectType = value.ObjectType switch
            {
                WorkerObjectTypeValue.Any => Api.ObjectType.Any,
                WorkerObjectTypeValue.BSpline => Api.ObjectType.BSpline,
                WorkerObjectTypeValue.Circle => Api.ObjectType.Circle,
                WorkerObjectTypeValue.Cloud => Api.ObjectType.Cloud,
                WorkerObjectTypeValue.EnhancedCloud => Api.ObjectType.EnhancedCloud,
                WorkerObjectTypeValue.ScanStripeCloud => Api.ObjectType.ScanStripeCloud,
                WorkerObjectTypeValue.CrossSectionCloud => Api.ObjectType.CrossSectionCloud,
                WorkerObjectTypeValue.Cone => Api.ObjectType.Cone,
                WorkerObjectTypeValue.Cylinder => Api.ObjectType.Cylinder,
                WorkerObjectTypeValue.Datum => Api.ObjectType.Datum,
                WorkerObjectTypeValue.Ellipse => Api.ObjectType.Ellipse,
                WorkerObjectTypeValue.Frame => Api.ObjectType.Frame,
                WorkerObjectTypeValue.FrameSet => Api.ObjectType.FrameSet,
                WorkerObjectTypeValue.Line => Api.ObjectType.Line,
                WorkerObjectTypeValue.Paraboloid => Api.ObjectType.Paraboloid,
                WorkerObjectTypeValue.Perimeter => Api.ObjectType.Perimeter,
                WorkerObjectTypeValue.Plane => Api.ObjectType.Plane,
                WorkerObjectTypeValue.PointGroup => Api.ObjectType.PointGroup,
                WorkerObjectTypeValue.PointSet => Api.ObjectType.PointSet,
                WorkerObjectTypeValue.PolySurface => Api.ObjectType.PolySurface,
                WorkerObjectTypeValue.ScanStripeMesh => Api.ObjectType.ScanStripeMesh,
                WorkerObjectTypeValue.Slot => Api.ObjectType.Slot,
                WorkerObjectTypeValue.Sphere => Api.ObjectType.Sphere,
                WorkerObjectTypeValue.Surface => Api.ObjectType.Surface,
                WorkerObjectTypeValue.Torus => Api.ObjectType.Torus,
                WorkerObjectTypeValue.VectorGroup => Api.ObjectType.VectorGroup,
                _ => throw new InvalidOperationException(
                    "SpatialAnalyzer returned an unsupported object type.")
            }
        };
    }
}
