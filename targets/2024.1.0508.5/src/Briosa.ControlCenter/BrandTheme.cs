using WpfColor = System.Windows.Media.Color;
using System.ComponentModel;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Briosa.ControlCenter;

/// <summary>Maps Briosa brand v2 colors to native Fluent controls without replacing their templates.</summary>
public sealed class BrandTheme : IDisposable
{
    private readonly Application app;
    private static readonly HashSet<string> nativeOverrides = [];
    // Brand v2 (Caliper b) tokens, as vendored in Assets/Brand/colors.json.
    private static readonly WpfColor Void = WpfColor.FromRgb(4, 11, 23);
    private static readonly WpfColor CleanRoom = WpfColor.FromRgb(243, 247, 251);
    private static readonly WpfColor DarkInk = WpfColor.FromRgb(234, 243, 255);
    private static readonly WpfColor DarkInk2 = WpfColor.FromRgb(157, 179, 207);
    private static readonly WpfColor DarkInk3 = WpfColor.FromRgb(118, 144, 176);
    private static readonly WpfColor DarkJaw = WpfColor.FromRgb(0, 186, 241);
    private static readonly WpfColor DarkPointSmall = WpfColor.FromRgb(139, 231, 255);
    private static readonly WpfColor DarkEdge = WpfColor.FromRgb(34, 58, 83);
    private static readonly WpfColor LightInk = WpfColor.FromRgb(6, 26, 51);
    private static readonly WpfColor LightInk2 = WpfColor.FromRgb(70, 96, 126);
    private static readonly WpfColor LightInk3 = WpfColor.FromRgb(83, 108, 140);
    private static readonly WpfColor LightJaw = WpfColor.FromRgb(0, 150, 214);
    private static readonly WpfColor LightLink = WpfColor.FromRgb(0, 109, 182);
    private static readonly WpfColor LightLine = WpfColor.FromRgb(185, 201, 219);
    private static readonly WpfColor DarkDeviationPlus1 = WpfColor.FromRgb(255, 210, 63);
    private static readonly WpfColor LightDeviationPlus1 = WpfColor.FromRgb(217, 154, 0);
    // The translucent line colors: dark-edge is this tint at 26% over void and
    // light-line is the light tint at 24% over clean room.
    private static readonly WpfColor DarkEdgeTint = WpfColor.FromRgb(120, 190, 255);
    private static readonly WpfColor LightLineTint = WpfColor.FromRgb(0, 56, 117);
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
        // Dark mode keeps the brand's void ground; light mode uses clean room with white
        // panels. Neutral tints blend the brand's line tint over each ground, so surfaces
        // keep the palette's blue cast.
        var tint = dark ? DarkEdgeTint : LightLineTint;
        var text = dark ? DarkInk : LightInk;
        var background = dark ? Void : CleanRoom;
        var surface = dark ? Mix(Void, tint, .06) : white;
        var control = dark ? Mix(Void, tint, .09) : white;
        var border = dark ? Mix(Void, tint, .56) : Mix(LightInk3, white, .22);
        var strongBorder = dark ? Mix(Void, tint, .72) : Mix(LightInk3, LightInk, .25);
        // light-jaw is only 3.08:1 on clean room, so light-mode accents use link blue.
        var accent = dark ? DarkJaw : LightLink;
        var onAccent = dark ? Void : white;
        // Pair every accent fill with its foreground. Light-theme interaction deepens
        // link blue toward ink; dark-theme interaction lifts the jaw toward its point.
        var accentHover = dark ? Mix(DarkJaw, DarkPointSmall, .2) : Mix(LightLink, LightInk, .14);
        var accentPressed = dark ? Mix(DarkJaw, DarkPointSmall, .4) : Mix(LightLink, LightInk, .28);
        var selected = Mix(surface, accent, dark ? .14 : .09);
        var selectedHover = Mix(surface, accent, dark ? .22 : .15);
        var hover = dark ? Mix(Void, tint, .14) : Mix(white, tint, .06);
        var pressed = dark ? Mix(Void, tint, .19) : Mix(white, tint, .11);
        var navBackground = highContrast ? SystemColors.WindowColor : dark ? Mix(Void, Colors.Black, .14) : white;
        var navText = highContrast ? SystemColors.WindowTextColor : text;
        var navSelected = highContrast ? SystemColors.HighlightColor : accent;
        var navSelectedText = highContrast ? SystemColors.HighlightTextColor : dark ? navBackground : white;
        resources["BriosaNavigationBackgroundColor"] = navBackground;
        resources["BriosaNavigationTextColor"] = navText;
        resources["BriosaNavigationSelectedColor"] = navSelected;
        resources["BriosaNavigationSelectedHoverColor"] = highContrast ? SystemColors.HighlightColor : accentHover;
        resources["BriosaNavigationSelectedTextColor"] = navSelectedText;
        resources["BriosaNavigationHoverColor"] = highContrast ? SystemColors.ControlColor : dark ? Mix(navBackground, tint, .10) : hover;
        resources["BriosaNavigationBrush"] = Brush(navBackground);
        resources["BriosaNavigationTextBrush"] = Brush(navText);
        resources["BriosaHeadingBrush"] = Brush(highContrast ? SystemColors.WindowTextColor : text);
        // Warning accents use the deviation scale's +1 step; the light step is darkened
        // until it reads as text on both the clean-room ground and white panels.
        resources["BriosaWarningBrush"] = Brush(highContrast ? SystemColors.WindowTextColor :
            dark ? DarkDeviationPlus1 : Mix(LightDeviationPlus1, Colors.Black, .36));
        resources["BriosaSelectionBorderBrush"] = Brush(highContrast ? SystemColors.HighlightColor : accent);
        resources["BriosaSectionSelectedTextBrush"] = Brush(highContrast ? SystemColors.WindowTextColor : accent);
        // WPF otherwise blends SelectionBrush with the input surface and inherits
        // a system foreground that may be intended for a different highlight.
        resources["BriosaTextSelectionBrush"] = Brush(highContrast ? SystemColors.HighlightColor : accent);
        resources["BriosaTextSelectionTextBrush"] = Brush(highContrast ? SystemColors.HighlightTextColor : onAccent);
        var logo = highContrast ? (Luminance(navBackground) < .5 ? "white" : "black") : dark ? "inverse" : "color";
        resources["BriosaLogo"] = BrandArtwork.Load($"Brand/suite/briosa-control-center-compact-{logo}.svg");
        // Native vector geometry is non-interactive and completely removed in high contrast.
        resources["BriosaWorkspaceBrush"] = highContrast ? Brush(SystemColors.WindowColor) :
            MeasurementGrid.Create(dark, tint, dark ? DarkInk3 : LightInk3, dark ? DarkJaw : LightJaw);
        // Let WPF's high-contrast resource dictionary own all native control colors.
        if (highContrast) return;

        void Paint(WpfColor color, params string[] keys)
        {
            var brush = Brush(color);
            foreach (var key in keys) { resources[key] = brush; nativeOverrides.Add(key); }
        }
        Paint(background, "ApplicationBackgroundBrush", "WindowBackground");
        Paint(surface, "CardBackgroundFillColorDefaultBrush", "CardBackgroundFillColorSecondaryBrush", "LayerFillColorDefaultBrush",
            "DataGridHeaderBackground", "DataGridColumnHeaderBackground", "ExpanderHeaderBackground", "ExpanderContentBackground",
            "ListBoxBackground");
        Paint(text, "TextFillColorPrimaryBrush", "ListBoxItemForeground", "ButtonForeground",
            "ButtonForegroundPointerOver", "ButtonForegroundPressed", "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused",
            "ComboBoxForeground", "ComboBoxForegroundPointerOver", "ComboBoxForegroundPressed", "ComboBoxForegroundFocused", "ComboBoxForegroundFocusedPressed",
            "ComboBoxDropDownForeground", "ComboBoxItemForeground", "ComboBoxItemForegroundSelected", "CheckBoxForegroundUnchecked",
            "CheckBoxForegroundChecked", "DataGridColumnHeaderForeground", "ExpanderHeaderForeground", "TabViewForeground");
        Paint(dark ? DarkInk2 : LightInk2, "TextFillColorSecondaryBrush");
        Paint(control, "ControlFillColorDefaultBrush", "ControlFillColorInputActiveBrush", "ButtonBackground", "TextControlBackground",
            "TextControlBackgroundFocused", "ComboBoxBackground", "ComboBoxBackgroundFocused", "ComboBoxDropDownBackground");
        Paint(hover, "ButtonBackgroundPointerOver", "TextControlBackgroundPointerOver",
            "ComboBoxBackgroundPointerOver", "ComboBoxDropDownBackgroundPointerOver",
            "ListBoxItemUnselectedBackgroundPointerOverThemeBrush");
        Paint(pressed, "ButtonBackgroundPressed", "ComboBoxBackgroundPressed");
        Paint(dark ? DarkEdge : LightLine, "ControlElevationBorderBrush");
        Paint(border, "ButtonBorderBrush", "TextControlBorderBrush", "TextControlElevationBorderBrush",
            "ComboBoxBorderBrush", "ComboBoxDropDownBorderBrush");
        Paint(strongBorder,
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
        Paint(text, "ListBoxItemSelectedForegroundThemeBrush", "DataGridRowSelectedForegroundThemeBrush");
        Paint(accent, "AccentFillColorSelectedTextBackgroundBrush", "TextControlSelectionHighlightColor");
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
