using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Briosa.Server.Services;

// Each query reads window state that the window manager holds. None of them sends a
// message to the window, so a SpatialAnalyzer UI thread blocked in a dialog cannot
// stall the probe, and nothing here can close, answer or focus a window.
internal sealed class WindowsSpatialAnalyzerWindowSource : ISpatialAnalyzerWindowSource
{
    private const uint OwnerRelation = 4; // GW_OWNER

    public IReadOnlyList<SpatialAnalyzerTopLevelWindow> GetTopLevelWindows(IReadOnlySet<int> processIds)
    {
        ArgumentNullException.ThrowIfNull(processIds);
        var windows = new List<SpatialAnalyzerTopLevelWindow>();
        var className = new char[256];
        bool Visit(IntPtr window, IntPtr parameter)
        {
            var processId = ProcessOf(window);
            if (processId is not { } id || !processIds.Contains(id)) return true;
            var owner = NativeMethods.GetWindow(window, OwnerRelation);
            var length = NativeMethods.GetClassNameW(window, className, className.Length);
            windows.Add(new SpatialAnalyzerTopLevelWindow(
                id,
                NativeMethods.IsWindowVisible(window),
                new string(className, 0, Math.Max(length, 0)),
                owner == IntPtr.Zero ? null : ProcessOf(owner),
                owner != IntPtr.Zero && NativeMethods.IsWindowEnabled(owner)));
            return true;
        }

        NativeMethods.EnumWindowsProc callback = Visit;
        if (!NativeMethods.EnumWindows(callback, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        GC.KeepAlive(callback);
        return windows;
    }

    private static int? ProcessOf(IntPtr window) =>
        NativeMethods.GetWindowThreadProcessId(window, out var processId) == 0 ? null : (int)processId;

    private static class NativeMethods
    {
        internal delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr window);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowEnabled(IntPtr window);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr window, uint relation);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int GetClassNameW(IntPtr window, [Out] char[] className, int maxCount);
    }
}
