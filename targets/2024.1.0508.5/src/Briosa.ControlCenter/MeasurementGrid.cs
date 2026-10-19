using WpfColor = System.Windows.Media.Color;
using System.Windows;
using System.Windows.Media;

namespace Briosa.ControlCenter;

/// <summary>Static Scanline workspace decoration, drawn by WPF at the display DPI.</summary>
internal static class MeasurementGrid
{
    // A fixed logical canvas anchored to the workspace's lower-right corner keeps the
    // 40-pixel pitch and 1-pixel lines at every window size instead of scaling them.
    private const double Width = 1440, Height = 960, Pitch = 40, Minor = 8;

    public static DrawingBrush Create(bool dark, WpfColor line, WpfColor tick, WpfColor jaw)
    {
        var bounds = new Rect(0, 0, Width, Height);
        var drawing = new DrawingGroup
        {
            ClipGeometry = new RectangleGeometry(bounds),
            // Like the homepage hero's grid, the lines fade out from the lower right
            // before they reach the page's denser content.
            OpacityMask = new RadialGradientBrush(new GradientStopCollection
            {
                new(Colors.Black, 0),
                new(WpfColor.FromArgb(140, 0, 0, 0), .5),
                new(Colors.Transparent, 1)
            })
            {
                MappingMode = BrushMappingMode.Absolute,
                Center = new(Width - 60, Height - 40),
                GradientOrigin = new(Width - 60, Height - 40),
                RadiusX = 900,
                RadiusY = 640
            }
        };
        Lines(WpfColor.FromArgb(dark ? (byte)30 : (byte)24, line.R, line.G, line.B), Pitch, 0, rows: true);
        // A vernier rule along the bottom edge: minor ticks in the tick ink and
        // jaw-colored majors on the grid lines, like the mark's beam and ruled wordmark.
        Lines(WpfColor.FromArgb(dark ? (byte)110 : (byte)96, tick.R, tick.G, tick.B), Minor, Height - 5, rows: false);
        Lines(WpfColor.FromArgb(dark ? (byte)170 : (byte)150, jaw.R, jaw.G, jaw.B), Pitch, Height - 11, rows: false);

        var brush = new DrawingBrush(drawing)
        {
            Viewbox = bounds,
            ViewboxUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None,
            AlignmentX = AlignmentX.Right,
            AlignmentY = AlignmentY.Bottom
        };
        brush.Freeze();
        return brush;

        // Columns run from top to the bottom edge, measured from the right; half-pixel
        // centers keep 1-pixel strokes on device pixels at 100% scale.
        void Lines(WpfColor color, double step, double top, bool rows)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                for (var x = Width - .5; x > 0; x -= step)
                {
                    context.BeginFigure(new(x, top), isFilled: false, isClosed: false);
                    context.LineTo(new(x, Height), isStroked: true, isSmoothJoin: false);
                }
                for (var y = Height - .5; rows && y > 0; y -= step)
                {
                    context.BeginFigure(new(0, y), isFilled: false, isClosed: false);
                    context.LineTo(new(Width, y), isStroked: true, isSmoothJoin: false);
                }
            }
            drawing.Children.Add(new GeometryDrawing(null, new Pen(new SolidColorBrush(color), 1), geometry));
        }
    }
}
