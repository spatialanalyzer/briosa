using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Briosa.Desktop;
using Forms = System.Windows.Forms;

namespace Briosa.ControlCenter;

internal sealed class SingleWindow : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _show;
    private readonly RegisteredWaitHandle? _wait;
    public string Key { get; }
    public bool IsFirst { get; }
    public SingleWindow(string key, Action show)
    {
        Key = key;
        _mutex = new Mutex(false, "Local\\Briosa.ControlCenter." + key, out var created);
        IsFirst = created;
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\Briosa.ControlCenter.Show." + key);
        if (created) _wait = ThreadPool.RegisterWaitForSingleObject(_show, (_, _) => show(), null, Timeout.Infinite, false);
    }
    public void ShowExisting() => _show.Set();
    public void Dispose() { _wait?.Unregister(null); _show.Dispose(); _mutex.Dispose(); }
}
