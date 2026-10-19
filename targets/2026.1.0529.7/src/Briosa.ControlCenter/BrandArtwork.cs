using WpfColor = System.Windows.Media.Color;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace Briosa.ControlCenter;

/// <summary>Draws the brand's outlined SVG artwork as WPF vector geometry, so the header logo
/// stays crisp at every scale instead of downsampling a raster export.</summary>
internal static class BrandArtwork
{
    private static readonly Dictionary<string, DrawingImage> images = [];

    // The brand build writes a small, fixed SVG subset: groups that translate and scale, and
    // filled paths and circles. Anything else fails loudly rather than drawing incorrectly.
    public static DrawingImage Load(string path)
    {
        if (images.TryGetValue(path, out var image)) return image;
        var resource = Application.GetResourceStream(new Uri($"pack://application:,,,/Briosa.ControlCenter.App;component/Assets/{path}"))
            ?? throw new FileNotFoundException("Missing brand artwork.", path);
        XElement svg;
        using (var stream = resource.Stream) svg = XElement.Load(stream);
        var box = Numbers(svg.Attribute("viewBox")?.Value);
        var drawing = new DrawingGroup();
        // A transparent canvas keeps the artwork's built-in clear space in the layout.
        drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(box[0], box[1], box[2], box[3]))));
        foreach (var element in svg.Elements()) Add(drawing, element);
        image = new DrawingImage(drawing);
        image.Freeze();
        return images[path] = image;
    }

    private static void Add(DrawingGroup parent, XElement element)
    {
        switch (element.Name.LocalName)
        {
            case "title" or "desc":
                return;
            case "g":
                var group = new DrawingGroup { Transform = Transform(element.Attribute("transform")?.Value) };
                foreach (var child in element.Elements()) Add(group, child);
                parent.Children.Add(group);
                return;
            case "path":
                // F1 selects SVG's nonzero fill rule; WPF path markup defaults to even-odd.
                parent.Children.Add(new GeometryDrawing(Fill(element), null, Geometry.Parse("F1 " + Attribute(element, "d"))));
                return;
            case "circle":
                var radius = Number(Attribute(element, "r"));
                parent.Children.Add(new GeometryDrawing(Fill(element), null, new EllipseGeometry(
                    new Point(Number(Attribute(element, "cx")), Number(Attribute(element, "cy"))), radius, radius)));
                return;
            default:
                throw new InvalidDataException($"Unsupported brand SVG element: {element.Name.LocalName}.");
        }
    }

    // SVG lists transforms outermost first; WPF matrices compose innermost first.
    private static MatrixTransform Transform(string? value)
    {
        var matrix = Matrix.Identity;
        foreach (var part in (value ?? "").Split(')', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var open = part.IndexOf('(', StringComparison.Ordinal);
            var name = part[..open].Trim();
            var arguments = Numbers(part[(open + 1)..]);
            var step = name switch
            {
                "translate" => new Matrix(1, 0, 0, 1, arguments[0], arguments.Length > 1 ? arguments[1] : 0),
                "scale" => new Matrix(arguments[0], 0, 0, arguments.Length > 1 ? arguments[1] : arguments[0], 0, 0),
                _ => throw new InvalidDataException($"Unsupported brand SVG transform: {name}."),
            };
            matrix = Matrix.Multiply(step, matrix);
        }
        return new MatrixTransform(matrix);
    }

    private static SolidColorBrush Fill(XElement element) => new((WpfColor)ColorConverter.ConvertFromString(Attribute(element, "fill")));
    private static string Attribute(XElement element, string name) =>
        element.Attribute(name)?.Value ?? throw new InvalidDataException($"Brand SVG {element.Name.LocalName} lacks {name}.");
    private static double Number(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static double[] Numbers(string? value) =>
        (value ?? throw new InvalidDataException("Brand SVG lacks a viewBox."))
        .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Select(Number).ToArray();
}
