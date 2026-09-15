using WpfColor = System.Windows.Media.Color;
using System.Windows;
using System.Windows.Media;

namespace Briosa.ControlCenter;

/// <summary>Resolution-independent workspace decoration, drawn by WPF at the display DPI.</summary>
internal static class LayeredPlanes
{
    public static DrawingBrush Create(bool dark, WpfColor graphite, WpfColor silver, WpfColor cyan)
    {
        const double width = 932, height = 800;
        var bounds = new Rect(0, 0, width, height);
        var drawing = new DrawingGroup { ClipGeometry = new RectangleGeometry(bounds) };
        var cross = FrontEdge(400);
        var accentStart = FrontEdge(756);
        var risingEdge = new Point(width, 332);
        var accentEnd = new Point(width, 568);

        // Flat, controlled fills avoid baked-in grain, compression noise and gradient banding.
        drawing.Children.Add(new GeometryDrawing(Fill(dark ? graphite : Colors.White), null, new RectangleGeometry(bounds)));
        Plane(dark ? Tone(graphite, 6) : silver, cross, risingEdge, new(width, height), new(0, height));
        Plane(dark ? Tone(graphite, 1) : Tone(silver, -3), accentStart, accentEnd, new(width, 720));
        Plane(dark ? Tone(graphite, -3) : Tone(silver, 7), new(0, 492), new(width, 720), new(width, height), new(0, height));

        Edge(cross, risingEdge, new SolidColorBrush(dark ? WpfColor.FromArgb(24, 255, 255, 255) : WpfColor.FromArgb(22, 0, 56, 117)));
        Edge(new(0, 492), new(width, 720), new SolidColorBrush(WpfColor.FromArgb(dark ? (byte)18 : (byte)210, 255, 255, 255)));
        // Only the short cyan reflection fades; the plane surfaces are solid colors.
        Edge(accentStart, accentEnd, new LinearGradientBrush(
            WpfColor.FromArgb(0, cyan.R, cyan.G, cyan.B),
            WpfColor.FromArgb(dark ? (byte)95 : (byte)55, cyan.R, cyan.G, cyan.B),
            new Point(0, 1), new Point(1, 0)));

        var brush = new DrawingBrush(drawing)
        {
            Viewbox = bounds,
            ViewboxUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Right,
            AlignmentY = AlignmentY.Bottom
        };
        brush.Freeze();
        return brush;

        Point FrontEdge(double x) => new(x, 492 + (720 - 492) * x / width);

        void Plane(WpfColor color, params Point[] points)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(points[0], isFilled: true, isClosed: true);
                context.PolyLineTo(points.Skip(1).ToArray(), isStroked: true, isSmoothJoin: false);
            }
            drawing.Children.Add(new GeometryDrawing(Fill(color), null, geometry));
        }

        void Edge(Point start, Point end, Brush stroke) =>
            drawing.Children.Add(new GeometryDrawing(null, new Pen(stroke, 1), new LineGeometry(start, end)));
    }

    private static SolidColorBrush Fill(WpfColor color) => new(color);
    private static WpfColor Tone(WpfColor color, int offset) => WpfColor.FromRgb(
        (byte)Math.Clamp(color.R + offset, 0, 255),
        (byte)Math.Clamp(color.G + offset, 0, 255),
        (byte)Math.Clamp(color.B + offset, 0, 255));
}
