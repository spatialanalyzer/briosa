using System.Diagnostics;

var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "desktop", "Briosa.ControlCenter.App.exe"))
{
    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
    WorkingDirectory = AppContext.BaseDirectory
};
foreach (var argument in args) start.ArgumentList.Add(argument);
try { using var process = Process.Start(start); return process is null ? 2 : 0; }
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { return 2; }
