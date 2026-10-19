using System.Windows;
using System.IO;
using System.Text.Json;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Briosa.ControlCenter;
using Briosa.Desktop;
using Google.Protobuf;

namespace Briosa.ControlCenter.Smoke;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args is ["--package", var package, var worker]) return PackageSmoke.RunAsync(package, worker).GetAwaiter().GetResult();
        if (args is not [var output]) return 2;
        Directory.CreateDirectory(output);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Briosa.ControlCenter.App;component/Theme.xaml") });
        // An isolated, nonexistent package path prevents attachment to user servers.
        using var session = new DesktopSession(Path.Combine(output, "inert-package"));
        var window = new MainWindow(session);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -20000; window.Top = -20000;
        window.ShowActivated = false; window.ShowInTaskbar = false;
        window.Show();
        var info = new GetServerInfoResponse { ReadyForMp = true,
            Version = new() { BriosaVersion = "0.2.0-review", SpatialAnalyzerTarget = DesktopProtocol.Target, ProtocolPackage = "briosa" },
            ActivatedSdkIdentity = new() { Version = DesktopProtocol.Target, Source = RuntimeIdentityEvidenceSource.OperatorAttestation, MatchState = RuntimeIdentityMatchState.ExactMatch },
            ConnectedSpatialAnalyzerIdentity = new() { Version = DesktopProtocol.Target, Source = RuntimeIdentityEvidenceSource.OperatorAttestation, MatchState = RuntimeIdentityMatchState.ExactMatch } };
        var sdk = new SpatialAnalyzerSdkLifecycleState { ReadyForMp = true, SdkState = SpatialAnalyzerSdkState.Ready,
            SdkGeneration = 1, ConnectionState = SpatialAnalyzerConnectionState.Connected,
            ExecutionReadinessState = SpatialAnalyzerExecutionReadinessState.ExecutionReady,
            RecoveryState = SpatialAnalyzerSdkRecoveryState.NotRequired };
        var state = DesktopState.FromReply(new() { Instance = DesktopProtocol.NewInstance(), Target = DesktopProtocol.Target,
            Endpoint = "http://127.0.0.1:50051", HostState = "Running", CanManage = true,
            ServerInfoJson = JsonFormatter.Default.Format(info), SdkStateJson = JsonFormatter.Default.Format(sdk),
            ApplicationStateJson = JsonFormatter.Default.Format(new SpatialAnalyzerLifecycleState { ApplicationState = SpatialAnalyzerApplicationState.Running,
                Ownership = SpatialAnalyzerOwnership.External }) });
        window.Render(state);
        var stop = (Button)window.FindName("StopButton");
        if (!stop.IsEnabled || new ButtonAutomationPeer(stop).GetName().Length == 0) throw new InvalidOperationException("Owned stop control is inaccessible.");
        BrandTheme.ApplyPreference(app, "light");
        CheckTheme(app, "light");
        Save(window, Path.Combine(output, "overview.png"), 1140, 800, 1);
        var brand = (Image)window.FindName("BrandImage");
        if (brand.ActualWidth < 170 || AutomationProperties.GetName(brand) != "Briosa Control Center")
            throw new InvalidOperationException("The header logo is below its 170 px minimum or lacks its accessible name.");
        BrandTheme.ApplyPreference(app, "dark");
        CheckTheme(app, "dark");
        Save(window, Path.Combine(output, "overview-dark.png"), 1140, 800, 1);
        window.Render(state.Unavailable());
        if (stop.IsEnabled) throw new InvalidOperationException("Stale status enabled management.");
        Save(window, Path.Combine(output, "stale-150-percent.png"), 800, 680, 1.5);
        ((ListBox)window.FindName("Navigation")).SelectedIndex = 1;
        ((DataGrid)window.FindName("ActivityGrid")).ItemsSource = new[]
        {
            new ActivityEntry(DateTimeOffset.Now, "Error", "Execution", "Command execution observation recorded.", "", "GetWorkingFrameProperties", "ExecutionDisposition: StartedOutcomeUnknown"),
            new ActivityEntry(DateTimeOffset.Now.AddMinutes(-2), "Information", "Server", "Server started.", "", "", "")
        };
        Save(window, Path.Combine(output, "activity.png"), 1000, 760, 1);
        ((ListBox)window.FindName("Navigation")).SelectedIndex = 2;
        Save(window, Path.Combine(output, "support.png"), 1000, 760, 1);
        ((ListBox)window.FindName("Navigation")).SelectedIndex = 3;
        Save(window, Path.Combine(output, "connection-setup.png"), 1140, 800, 1);
        BrandTheme.Apply(app, dark: false, highContrast: true);
        if (app.Resources["BriosaWorkspaceBrush"] is not SolidColorBrush contrast || contrast.Color != SystemColors.WindowColor)
            throw new InvalidOperationException("High contrast retained the decorative grid.");
        // Uses the current system colors, so a non-contrast session shows the black logo on a white window.
        ((ListBox)window.FindName("Navigation")).SelectedIndex = 0;
        Save(window, Path.Combine(output, "overview-high-contrast.png"), 1140, 800, 1);
        window.Exit();
        app.Shutdown();
        Console.WriteLine("WPF layout, state gating, and automation labels passed. No server or SDK was started.");
        return 0;
    }

    // Brand v2 tokens apply unchanged (light accents use link blue because the light jaw is
    // only 3.08:1 on clean room); text keeps 4.5:1 and boundaries 3:1 in each palette.
    private static void CheckTheme(Application app, string theme)
    {
        using var tokens = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "colors.json")));
        var palette = tokens.RootElement.GetProperty("themes").GetProperty(theme);
        foreach (var (key, token) in new[] { ("ApplicationBackgroundBrush", "ground"), ("TextFillColorPrimaryBrush", "ink"),
            ("TextFillColorSecondaryBrush", "ink-2"), ("AccentFillColorDefaultBrush", theme == "dark" ? "accent" : "link") })
            if (Resolve(app, key) != (System.Windows.Media.Color)ColorConverter.ConvertFromString(palette.GetProperty(token).GetString()!))
                throw new InvalidOperationException($"{theme} {key} is not the brand's {token} token.");
        foreach (var surface in new[] { "ApplicationBackgroundBrush", "CardBackgroundFillColorDefaultBrush" })
            foreach (var text in new[] { "TextFillColorPrimaryBrush", "TextFillColorSecondaryBrush", "AccentTextFillColorPrimaryBrush", "BriosaWarningBrush" })
                CheckContrast(app, theme, text, surface, 4.5);
        foreach (var state in new[] { "", "PointerOver", "Pressed" })
        {
            CheckContrast(app, theme, "AccentButtonForeground" + state, "AccentButtonBackground" + state, 4.5);
            CheckContrast(app, theme, "ButtonForeground" + state, "ButtonBackground" + state, 4.5);
            CheckContrast(app, theme, "ButtonBorderBrush" + state, "ButtonBackground" + state, 3);
        }
        CheckContrast(app, theme, "BriosaNavigationSelectedTextColor", "BriosaNavigationSelectedColor", 4.5);
        CheckContrast(app, theme, "BriosaNavigationSelectedColor", "BriosaNavigationBrush", 3);
        CheckContrast(app, theme, "DataGridRowSelectedForegroundThemeBrush", "DataGridRowSelectedBackgroundThemeBrush", 4.5);
        CheckContrast(app, theme, "KeyboardFocusBorderColorBrush", "CardBackgroundFillColorDefaultBrush", 3);
        // The measurement grid keeps its pitch and 1-pixel lines instead of stretching with the window.
        if (app.Resources["BriosaWorkspaceBrush"] is not DrawingBrush { Stretch: Stretch.None, IsFrozen: true, Drawing: DrawingGroup grid } ||
            grid.Children.Count == 0 || !grid.Children.All(d => d is GeometryDrawing))
            throw new InvalidOperationException($"{theme} background is not an unscaled native vector grid.");
        // The header draws the brand's Control Center compact logo from its outlined SVG, never a raster.
        if (app.Resources["BriosaLogo"] is not DrawingImage { IsFrozen: true, Width: 511, Drawing: DrawingGroup logo } || !Vector(logo))
            throw new InvalidOperationException($"{theme} header logo is not the brand's vector artwork.");
        static bool Vector(DrawingGroup group) => group.Children.All(d => d is GeometryDrawing || d is DrawingGroup child && Vector(child));
    }

    private static void CheckContrast(Application app, string theme, string foreground, string background, double minimum)
    {
        static double Luminance(System.Windows.Media.Color color)
        {
            static double Channel(byte value) { var c = value / 255d; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
            return .2126 * Channel(color.R) + .7152 * Channel(color.G) + .0722 * Channel(color.B);
        }
        var a = Luminance(Resolve(app, foreground));
        var b = Luminance(Resolve(app, background));
        var contrast = (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
        if (contrast < minimum) throw new InvalidOperationException($"{theme} {foreground} on {background}: {contrast:F2}:1 is below {minimum}:1.");
    }

    private static System.Windows.Media.Color Resolve(Application app, string key) => app.Resources[key] switch
    {
        SolidColorBrush { Color.A: 255 } brush => brush.Color,
        System.Windows.Media.Color { A: 255 } color => color,
        _ => throw new InvalidOperationException($"{key} is not an opaque color.")
    };

    private static void Save(MainWindow window, string path, int width, int height, double scale)
    {
        window.Width = width; window.Height = height;
        window.Measure(new Size(width, height));
        window.Arrange(new Rect(0, 0, width, height));
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        var selected = ((ListBox)window.FindName("Navigation")).SelectedIndex;
        // This render harness deliberately places its window outside the desktop.
        // ScrollViewer's automation child projection depends on screen visibility and
        // peer caches, so check each selected page's visible control and label directly.
        var (name, expected) = selected switch
        {
            0 => ("StartButton", "Start server"),
            1 => ("SearchBox", "Search activity"),
            2 => ("VersionText", "Distribution and runtime details"),
            _ => ("ApplicationVersionBox", "Connected SA version")
        };
        var control = (FrameworkElement)window.FindName(name);
        var peer = UIElementAutomationPeer.CreatePeerForElement(control);
        if (!control.IsVisible || control.ActualWidth <= 0 || control.ActualHeight <= 0 || peer.GetName() != expected)
            throw new InvalidOperationException("The selected page control is hidden, unarranged, or missing its automation label: " + expected);
        var visual = (Visual)window.Content;
        var bitmap = new RenderTargetBitmap((int)(width * scale), (int)(height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        var canvas = new DrawingVisual();
        using (var drawing = canvas.RenderOpen())
        {
            drawing.DrawRectangle(SystemColors.WindowBrush, null, new Rect(0, 0, width, height));
            drawing.DrawRectangle(new VisualBrush(visual), null, new Rect(0, 0, width, height));
        }
        bitmap.Render(canvas);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        if (pixels.Distinct().Count() < 10) throw new InvalidOperationException("The rendered window is blank.");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }
}
