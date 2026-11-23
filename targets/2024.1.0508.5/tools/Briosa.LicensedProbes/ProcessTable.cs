using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Briosa.LicensedProbes;

/// <summary>One process as a snapshot lists it: its image name without extension, never a path or arguments.</summary>
internal sealed record ProcessEntry(int Id, int ParentId, string Name, DateTimeOffset? StartedAt);

/// <summary>Lists processes for the SDK-ownership probes (#252, #402). It reads names, parents and start times only.</summary>
internal interface IProcessTable
{
    IReadOnlyList<ProcessEntry> Snapshot();
}

/// <summary>
/// The Windows process list from one Toolhelp snapshot. Start times are read only for the
/// processes the ownership probes track, through <see cref="Process.StartTime"/>; a process
/// that exits or denies access in between keeps a null start time.
/// </summary>
internal sealed class WindowsProcessTable : IProcessTable
{
    private const uint SnapProcesses = 0x00000002;

    public static WindowsProcessTable Instance { get; } = new();

    public IReadOnlyList<ProcessEntry> Snapshot()
    {
        using var snapshot = NativeMethods.CreateToolhelp32Snapshot(SnapProcesses, 0);
        if (snapshot.IsInvalid)
        {
            return [];
        }

        var entries = new List<ProcessEntry>();
        var entry = new NativeMethods.ProcessEntry32 { Size = (uint)Marshal.SizeOf<NativeMethods.ProcessEntry32>() };
        for (var more = NativeMethods.Process32FirstW(snapshot, ref entry); more; more = NativeMethods.Process32NextW(snapshot, ref entry))
        {
            var name = Path.GetFileNameWithoutExtension(entry.ExeFile ?? string.Empty);
            var id = unchecked((int)entry.ProcessId);
            entries.Add(new ProcessEntry(id, unchecked((int)entry.ParentProcessId), name,
                ProcessRoles.IsTracked(name) ? StartTime(id) : null));
        }

        return entries;
    }

    private static DateTimeOffset? StartTime(int id)
    {
        try
        {
            using var process = Process.GetProcessById(id);
            return new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return null;
        }
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct ProcessEntry32
        {
            public uint Size;
            public uint Usage;
            public uint ProcessId;
            public IntPtr DefaultHeapId;
            public uint ModuleId;
            public uint Threads;
            public uint ParentProcessId;
            public int PriorityClassBase;
            public uint Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string? ExeFile;
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeFileHandle CreateToolhelp32Snapshot(uint flags, uint processId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32FirstW(SafeFileHandle snapshot, ref ProcessEntry32 entry);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32NextW(SafeFileHandle snapshot, ref ProcessEntry32 entry);
    }
}

/// <summary>The closed vocabulary of process roles a record may name.</summary>
internal static class ProcessRoles
{
    public const string SpatialAnalyzer = "spatial-analyzer";
    public const string Sdk = "sdk";
    public const string Server = "server";
    public const string Worker = "worker";

    // Parent roles only.
    public const string Harness = "harness";
    public const string ServiceHost = "svchost";
    public const string Other = "other";
    public const string Exited = "exited";

    public static string? RoleOf(string name) => name switch
    {
        ProcessCensus.SpatialAnalyzer => SpatialAnalyzer,
        ProcessCensus.SpatialAnalyzerSdk => Sdk,
        ProcessCensus.BriosaServer => Server,
        ProcessCensus.BriosaWorker => Worker,
        _ => null
    };

    public static bool IsTracked(string name) => RoleOf(name) is not null;

    // A parent outside the tracked roles is named only as the harness, the service host that
    // DCOM activation uses, or "other"; no other process name is ever recorded.
    public static string ParentRoleOf(int parentId, ProcessEntry? parent, int harnessProcessId) => parent switch
    {
        _ when parentId == harnessProcessId => Harness,
        null => Exited,
        _ when RoleOf(parent.Name) is { } role => role,
        _ when string.Equals(parent.Name, "svchost", StringComparison.OrdinalIgnoreCase) => ServiceHost,
        _ => Other
    };
}

/// <summary>One tracked process over a probe: its label, never its id, and when it was seen.</summary>
internal sealed record TrackedProcess(string Label, string Role, DateTimeOffset? StartedAt, string ParentRole, long FirstSeenMilliseconds, bool PresentAtStart)
{
    public long? ExitedMilliseconds { get; init; }

    public bool Alive => ExitedMilliseconds is null;
}

/// <summary>
/// Labels each Spatial Analyzer, SDK, server and worker process the first time a snapshot
/// shows it (for example <c>sdk-1</c>) and records when it disappears. A process is known by
/// its id and start time, so a reused id is a new process. Ids are kept in memory only.
/// </summary>
internal sealed class ProcessTracker(int harnessProcessId)
{
    private readonly Dictionary<(int Id, DateTimeOffset? StartedAt), TrackedProcess> _processes = [];
    private readonly Dictionary<(int Id, DateTimeOffset? StartedAt), int> _ids = [];
    private readonly Dictionary<string, int> _ordinals = new(StringComparer.Ordinal);
    private bool _observed;

    public IReadOnlyList<TrackedProcess> Processes => [.. _processes.Values.OrderBy(static process => process.FirstSeenMilliseconds).ThenBy(static process => process.Label, StringComparer.Ordinal)];

    public IReadOnlyList<TrackedProcess> Alive(string role) =>
        [.. Processes.Where(process => process.Alive && string.Equals(process.Role, role, StringComparison.Ordinal))];

    /// <summary>The live process with this label, for a termination that must hit exactly it.</summary>
    public (int Id, DateTimeOffset? StartedAt)? Identity(string label)
    {
        foreach (var (key, process) in _processes)
        {
            if (process.Alive && string.Equals(process.Label, label, StringComparison.Ordinal))
            {
                return (_ids[key], key.StartedAt);
            }
        }

        return null;
    }

    public void Observe(IReadOnlyList<ProcessEntry> snapshot, long offsetMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var byId = new Dictionary<int, ProcessEntry>();
        foreach (var entry in snapshot)
        {
            byId.TryAdd(entry.Id, entry);
        }

        var present = new HashSet<(int, DateTimeOffset?)>();
        foreach (var entry in snapshot)
        {
            if (ProcessRoles.RoleOf(entry.Name) is not { } role)
            {
                continue;
            }

            var key = (entry.Id, entry.StartedAt);
            present.Add(key);
            if (_processes.ContainsKey(key))
            {
                continue;
            }

            var ordinal = _ordinals[role] = _ordinals.GetValueOrDefault(role) + 1;
            var parent = byId.GetValueOrDefault(entry.ParentId);
            _processes[key] = new TrackedProcess(
                $"{role}-{ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture)}", role, entry.StartedAt,
                ProcessRoles.ParentRoleOf(entry.ParentId, parent, harnessProcessId), offsetMilliseconds, PresentAtStart: !_observed);
            _ids[key] = entry.Id;
        }

        foreach (var key in _processes.Keys.ToList())
        {
            if (_processes[key].Alive && !present.Contains(key))
            {
                _processes[key] = _processes[key] with { ExitedMilliseconds = offsetMilliseconds };
            }
        }

        _observed = true;
    }
}
