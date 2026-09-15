using System.Diagnostics;
using Briosa.Desktop;

namespace Briosa.Server.Services;

/// <summary>Announces before host validation/binding, so startup failure is visible in the companion.</summary>
internal sealed class DesktopAnnouncement : IDisposable
{
    private readonly DesktopHostOptions _options;
    private bool _clean;
    public DesktopAnnouncement(DesktopHostOptions options)
    {
        _options = options;
        if (!options.Enabled) return;
        try
        {
            using var current = Process.GetCurrentProcess();
            DesktopStore.Register(new(options.Instance, AppContext.BaseDirectory, current.Id, current.StartTime.ToUniversalTime().Ticks));
            if (!options.AutoLaunch) return;
            var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Briosa.ControlCenter.exe"))
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = AppContext.BaseDirectory };
            start.ArgumentList.Add("--monitor"); start.ArgumentList.Add(options.Instance);
            start.Environment.Remove(DesktopProtocol.CredentialVariable);
            start.Environment.Remove(DesktopProtocol.InstanceVariable);
            using var process = Process.Start(start);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }
    public void Complete() => _clean = true;
    public void Dispose()
    {
        if (!_options.Enabled) return;
        try { DesktopStore.MarkExit(_options.Instance, _clean); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }
}
