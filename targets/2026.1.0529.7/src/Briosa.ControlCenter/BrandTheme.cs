using WpfColor = System.Windows.Media.Color;
using System.ComponentModel;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Briosa.ControlCenter;

/// <summary>Maps Briosa v1 colors to native Fluent controls without replacing their templates.</summary>
public sealed class BrandTheme : IDisposable
{
    private readonly Application app;
    private static readonly HashSet<string> nativeOverrides = [];
    private static readonly Dictionary<string, BitmapSource> images = [];
    private static readonly WpfColor Blue = WpfColor.FromRgb(0, 56, 117);
    private static readonly WpfColor Cyan = WpfColor.FromRgb(0, 186, 241);
    private static readonly WpfColor Graphite = WpfColor.FromRgb(88, 91, 98);
    private static readonly WpfColor Silver = WpfColor.FromRgb(242, 242, 242);
    private bool disposed;

    public BrandTheme(Application app)
    {
        this.app = app;
        ApplySystem(app);
        SystemEvents.UserPreferenceChanged += PreferenceChanged;
        SystemParameters.StaticPropertyChanged += SystemParameterChanged;
    }

    private void PreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Refresh();
    private void SystemParameterChanged(object? sender, PropertyChangedEventArgs e) => Refresh();
    private void Refresh()
    {
        if (!disposed && !app.Dispatcher.HasShutdownStarted)
            app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { if (!disposed) ApplySystem(app); }));
    }

    public static void ApplySystem(Application app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var dark = false;
#pragma warning disable WPF0001 // WPF's selected theme; no OS setting is changed.
        if (app.ThemeMode == ThemeMode.Dark) dark = true;
        else if (app.ThemeMode == ThemeMode.System)
#pragma warning restore WPF0001
        {
            // Matches WPF's system-theme detection; reading is optional and fails to light.
            try
            {
                using var preferences = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                dark = preferences?.GetValue("AppsUseLightTheme") is int value && value == 0;
            }
            catch (Exception e) when (e is SecurityException or UnauthorizedAccessException or System.IO.IOException) { }
        }
        Apply(app, dark, SystemParameters.HighContrast);
    }

    public static void ApplyPreference(Application app, string theme)
    {
        ArgumentNullException.ThrowIfNull(app);
#pragma warning disable WPF0001 // App-wide Fluent appearance; never writes a Windows preference.
        app.ThemeMode = theme switch
        {
            "system" => ThemeMode.System,
            "light" => ThemeMode.Light,
            "dark" => ThemeMode.Dark,
            _ => throw new ArgumentOutOfRangeException(nameof(theme)),
        };
#pragma warning restore WPF0001
        ApplySystem(app);
    }

    // Explicit appearance parameters also let the control-tree harness validate both
    // palettes and high-contrast delegation without changing the user's Windows settings.
    public static void Apply(Application app, bool dark, bool highContrast)
    {
        ArgumentNullException.ThrowIfNull(app);
        var resources = app.Resources;
        foreach (var key in nativeOverrides) resources.Remove(key);
        nativeOverrides.Clear();
        var white = Colors.White;
        // A deeper graphite shade for long desktop sessions; the brand color
        // remains the light-mode text color and the source for neutral tints.
        var darkBase = Mix(Graphite, Colors.Black, .66); // #1E1F21
        var text = dark ? white : Graphite;
        var background = dark ? darkBase : white;
        var surface = dark ? Mix(darkBase, white, .045) : Silver;
        var control = dark ? Mix(darkBase, white, .07) : white;
        var border = dark ? Mix(darkBase, Silver, .43) : Mix(Graphite, white, .25);
        var accent = dark ? Cyan : Blue;
        var onAccent = dark ? Blue : white;
        // Pair every accent fill with its foreground. Light-theme interaction
        // darkens blue; dark-theme interaction lifts cyan without losing contrast.
        var accentHover = Mix(accent, dark ? white : Colors.Black, .08);
        var accentPressed = Mix(accent, dark ? white : Colors.Black, .16);
        var selected = Mix(background, accent, dark ? .12 : .10);
        var selectedHover = Mix(background, accent, dark ? .20 : .16);
        var hover = dark ? Mix(darkBase, white, .12) : Silver;
        var pressed = dark ? Mix(darkBase, white, .17) : Mix(Silver, Graphite, .10);
        var navBackground = highContrast ? SystemColors.WindowColor : dark ? Mix(darkBase, Colors.Black, .14) : Silver;
        var navText = highContrast ? SystemColors.WindowTextColor : text;
        var navSelected = highContrast ? SystemColors.HighlightColor : accent;
        var navSelectedText = highContrast ? SystemColors.HighlightTextColor : dark ? navBackground : white;
        resources["BriosaNavigationBackgroundColor"] = navBackground;
        resources["BriosaNavigationTextColor"] = navText;
        resources["BriosaNavigationSelectedColor"] = navSelected;
        resources["BriosaNavigationSelectedHoverColor"] = highContrast ? SystemColors.HighlightColor : accentHover;
        resources["BriosaNavigationSelectedTextColor"] = navSelectedText;
        resources["BriosaNavigationHoverColor"] = highContrast ? SystemColors.ControlColor : dark ? Mix(darkBase, white, .10) : Mix(Silver, Graphite, .06);
        resources["BriosaNavigationBrush"] = Brush(navBackground);
        resources["BriosaNavigationTextBrush"] = Brush(navText);
        resources["BriosaHeadingBrush"] = Brush(highContrast ? SystemColors.WindowTextColor : dark ? white : Blue);
        // Semantic amber complements blue while staying readable on each card surface.
        resources["BriosaWarningBrush"] = Brush(highContrast ? SystemColors.WindowTextColor :
            dark ? WpfColor.FromRgb(255, 196, 107) : WpfColor.FromRgb(156, 87, 0));
        resources["BriosaSelectionBorderBrush"] = Brush(highContrast ? SystemColors.HighlightColor : accent);
        resources["BriosaSectionSelectedTextBrush"] = Brush(highContrast ? SystemColors.WindowTextColor : accent);
        // WPF otherwise blends SelectionBrush with the input surface and inherits
        // a system foreground that may be intended for a different highlight.
        resources["BriosaTextSelectionBrush"] = Brush(highContrast ? SystemColors.HighlightColor : accent);
        resources["BriosaTextSelectionTextBrush"] = Brush(highContrast ? SystemColors.HighlightTextColor : onAccent);
        var logo = highContrast ? (Luminance(navBackground) < .5 ? "white" : "black") : dark ? "inverse" : "color";
        resources["BriosaLogo"] = Bitmap($"Brand/png/logos/briosa-horizontal-{logo}.png");
        // Native vector geometry is non-interactive and completely removed in high contrast.
        resources["BriosaWorkspaceBrush"] = highContrast ? Brush(SystemColors.WindowColor) : LayeredPlanes.Create(dark, darkBase, Silver, Cyan);
        // Let WPF's high-contrast resource dictionary own all native control colors.
        if (highContrast) return;

        void Paint(WpfColor color, params string[] keys)
        {
            var brush = Brush(color);
            foreach (var key in keys) { resources[key] = brush; nativeOverrides.Add(key); }
        }
        Paint(background, "ApplicationBackgroundBrush", "WindowBackground");
        Paint(surface, "CardBackgroundFillColorDefaultBrush", "CardBackgroundFillColorSecondaryBrush", "LayerFillColorDefaultBrush",
            "DataGridHeaderBackground", "DataGridColumnHeaderBackground", "ExpanderHeaderBackground", "ExpanderContentBackground");
        Paint(text, "TextFillColorPrimaryBrush", "TextFillColorSecondaryBrush", "ListBoxItemForeground", "ButtonForeground",
            "ButtonForegroundPointerOver", "ButtonForegroundPressed", "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused",
            "ComboBoxForeground", "ComboBoxForegroundPointerOver", "ComboBoxForegroundPressed", "ComboBoxForegroundFocused", "ComboBoxForegroundFocusedPressed",
            "ComboBoxDropDownForeground", "ComboBoxItemForeground", "ComboBoxItemForegroundSelected", "CheckBoxForegroundUnchecked",
            "CheckBoxForegroundChecked", "DataGridColumnHeaderForeground", "ExpanderHeaderForeground", "TabViewForeground");
        Paint(control, "ControlFillColorDefaultBrush", "ControlFillColorInputActiveBrush", "ButtonBackground", "TextControlBackground",
            "TextControlBackgroundFocused", "ComboBoxBackground", "ComboBoxBackgroundFocused", "ComboBoxDropDownBackground");
        Paint(hover, "ButtonBackgroundPointerOver", "TextControlBackgroundPointerOver",
            "ComboBoxBackgroundPointerOver", "ComboBoxDropDownBackgroundPointerOver",
            "ListBoxItemUnselectedBackgroundPointerOverThemeBrush");
        Paint(pressed, "ButtonBackgroundPressed", "ComboBoxBackgroundPressed");
        Paint(dark ? Mix(darkBase, white, .18) : Mix(Graphite, white, .65), "ControlElevationBorderBrush");
        Paint(border, "ButtonBorderBrush", "TextControlBorderBrush", "TextControlElevationBorderBrush",
            "ComboBoxBorderBrush", "ComboBoxDropDownBorderBrush");
        Paint(dark ? Mix(darkBase, Silver, .56) : Mix(Graphite, white, .15),
            "ButtonBorderBrushPointerOver", "ButtonBorderBrushPressed", "TextControlBorderBrushPointerOver",
            "ComboBoxBorderBrushPointerOver", "ComboBoxBorderBrushPressed");
        Paint(accent, "AccentFillColorDefaultBrush", "AccentButtonBackground", "AccentButtonBorderBrush",
            "AccentTextFillColorPrimaryBrush", "AccentTextFillColorSecondaryBrush", "AccentTextFillColorTertiaryBrush",
            "TextControlBorderBrushFocused", "TextControlFocusedBorderBrush", "ComboBoxBorderBrushFocused", "ComboBoxItemPillFillBrush",
            "CheckBoxCheckBackgroundFillChecked", "CheckBoxCheckBackgroundStrokeChecked",
            "ProgressBarForeground", "KeyboardFocusBorderColorBrush", "TabViewItemForegroundSelected", "TabViewSelectedItemBorderBrush");
        Paint(accentHover, "AccentFillColorSecondaryBrush", "AccentButtonBackgroundPointerOver", "AccentButtonBorderBrushPointerOver",
            "CheckBoxCheckBackgroundFillCheckedPointerOver", "CheckBoxCheckBackgroundStrokeCheckedPointerOver");
        Paint(accentPressed, "AccentFillColorTertiaryBrush", "AccentButtonBackgroundPressed", "AccentButtonBorderBrushPressed",
            "CheckBoxCheckBackgroundFillCheckedPressed", "CheckBoxCheckBackgroundStrokeCheckedPressed");
        Paint(onAccent, "TextOnAccentFillColorPrimaryBrush", "TextOnAccentFillColorSecondaryBrush", "AccentButtonForeground",
            "AccentButtonForegroundPointerOver", "AccentButtonForegroundPressed", "CheckBoxCheckGlyphForeground", "CheckBoxCheckGlyphForegroundPressed");
        Paint(selected, "ListBoxItemSelectedBackgroundThemeBrush", "DataGridRowSelectedBackgroundThemeBrush",
            "ComboBoxItemBackgroundSelected", "TabViewItemHeaderBackgroundSelected");
        Paint(selectedHover, "ListBoxItemSelectedBackgroundPointerOverThemeBrush", "ListBoxItemSelectedBackgroundPressedThemeBrush");
        Paint(dark ? white : Blue, "ListBoxItemSelectedForegroundThemeBrush", "DataGridRowSelectedForegroundThemeBrush");
        Paint(accent, "AccentFillColorSelectedTextBackgroundBrush", "TextControlSelectionHighlightColor");
    }

    private static BitmapSource Bitmap(string path)
    {
        if (!images.TryGetValue(path, out var image))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri($"pack://application:,,,/Briosa.ControlCenter.App;component/Assets/{path}");
            bitmap.EndInit(); bitmap.Freeze();
            images[path] = image = bitmap;
        }
        return image;
    }

    private static SolidColorBrush Brush(WpfColor color) { var brush = new SolidColorBrush(color); brush.Freeze(); return brush; }
    private static WpfColor Mix(WpfColor background, WpfColor foreground, double amount) => WpfColor.FromRgb(
        (byte)Math.Round(background.R + (foreground.R - background.R) * amount),
        (byte)Math.Round(background.G + (foreground.G - background.G) * amount),
        (byte)Math.Round(background.B + (foreground.B - background.B) * amount));
    private static double Luminance(WpfColor color) => (.2126 * color.R + .7152 * color.G + .0722 * color.B) / 255;
    public void Dispose()
    {
        disposed = true;
        SystemEvents.UserPreferenceChanged -= PreferenceChanged;
        SystemParameters.StaticPropertyChanged -= SystemParameterChanged;
    }
}
