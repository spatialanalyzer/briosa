using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Briosa.LicensedProbes;

/// <summary>
/// One process as a snapshot lists it: its image name without extension, never a path or
/// arguments. The start time and session are read only for the tracked roles.
/// </summary>
internal sealed record ProcessEntry(int Id, int ParentId, string Name, DateTimeOffset? StartedAt, int? SessionId = null);

/// <summary>Whether a snapshot listed every process with the identity the tracker needs.</summary>
internal enum SnapshotStatus
{
    // Every process was listed, and every tracked one with its start time.
    Complete,

    // The enumeration ended early, or a tracked process's start time could not be read.
    Incomplete,

    // No list could be taken at all.
    Failed
}

internal sealed record ProcessSnapshot(SnapshotStatus Status, IReadOnlyList<ProcessEntry> Entries)
{
    public static ProcessSnapshot Failed { get; } = new(SnapshotStatus.Failed, []);

    public static ProcessSnapshot Of(params ProcessEntry[] entries) => new(SnapshotStatus.Complete, entries);
}

/// <summary>Lists processes for the SDK-ownership probes (#252, #402). It reads names, parents, start times and sessions only.</summary>
internal interface IProcessTable
{
    ProcessSnapshot Snapshot();
}

/// <summary>
/// The Windows process list from one Toolhelp snapshot. A snapshot that cannot be taken is
/// <see cref="SnapshotStatus.Failed"/>; one whose enumeration ends with an error other than
/// "no more entries", or in which a tracked process's start time or session cannot be read,
/// is <see cref="SnapshotStatus.Incomplete"/>. Neither is ever read as an absence.
/// </summary>
internal sealed class WindowsProcessTable : IProcessTable
{
    private const uint SnapProcesses = 0x00000002;
    private const int NoMoreFiles = 18;

    public static WindowsProcessTable Instance { get; } = new();

    public ProcessSnapshot Snapshot()
    {
        using var snapshot = NativeMethods.CreateToolhelp32Snapshot(SnapProcesses, 0);
        if (snapshot.IsInvalid)
        {
            return ProcessSnapshot.Failed;
        }

        var entries = new List<ProcessEntry>();
        var status = SnapshotStatus.Complete;
        var entry = new NativeMethods.ProcessEntry32 { Size = (uint)Marshal.SizeOf<NativeMethods.ProcessEntry32>() };
        if (!NativeMethods.Process32FirstW(snapshot, ref entry))
        {
            return ProcessSnapshot.Failed;
        }

        do
        {
            var name = Path.GetFileNameWithoutExtension(entry.ExeFile ?? string.Empty);
            var id = unchecked((int)entry.ProcessId);
            DateTimeOffset? started = null;
            int? session = null;
            if (ProcessRoles.IsTracked(name) && !TryIdentity(id, out started, out session))
            {
                status = SnapshotStatus.Incomplete;
            }

            entries.Add(new ProcessEntry(id, unchecked((int)entry.ParentProcessId), name, started, session));
        }
        while (NativeMethods.Process32NextW(snapshot, ref entry));

        if (Marshal.GetLastWin32Error() != NoMoreFiles)
        {
            status = SnapshotStatus.Incomplete;
        }

        return new ProcessSnapshot(status, entries);
    }

    // Read through a query-only handle, as a binding reads them, so both compare the same values.
    private static bool TryIdentity(int id, out DateTimeOffset? started, out int? session)
    {
        session = ProcessHandles.SessionOf(id);
        using var handle = ProcessHandles.Open(id, terminate: false);
        started = handle is null ? null : ProcessHandles.StartTime(handle);
        return started is not null && session is not null;
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

/// <summary>
/// Opens processes with the least rights a read needs: query and wait, plus terminate only
/// for a binding that a scenario's script terminates. It reads start times, sessions and image
/// paths through the handle; a path is compared in memory only and never recorded.
/// </summary>
internal static class ProcessHandles
{
    private const uint QueryLimitedInformation = 0x1000;
    private const uint Synchronize = 0x00100000;
    private const uint TerminateRight = 0x0001;
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 0x102;

    /// <summary>A handle that can query and wait, and terminate only when asked; null when it cannot be opened.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The handle is returned to the caller, or disposed when it is invalid.")]
    public static SafeProcessHandle? Open(int processId, bool terminate)
    {
        var handle = NativeMethods.OpenProcess(QueryLimitedInformation | Synchronize | (terminate ? TerminateRight : 0), 0, processId);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            return null;
        }

        return handle;
    }

    /// <summary>The creation time, as <see cref="Process.StartTime"/> reports it, in UTC; null when unreadable.</summary>
    public static DateTimeOffset? StartTime(SafeProcessHandle handle) =>
        NativeMethods.GetProcessTimes(handle, out var creation, out _, out _, out _)
            ? new DateTimeOffset(DateTime.FromFileTime(creation).ToUniversalTime(), TimeSpan.Zero)
            : null;

    public static int? SessionOf(int processId) =>
        NativeMethods.ProcessIdToSessionId(unchecked((uint)processId), out var session) ? unchecked((int)session) : null;

    public static string? ImagePath(SafeProcessHandle handle)
    {
        var buffer = new char[32768];
        var size = buffer.Length;
        return NativeMethods.QueryFullProcessImageNameW(handle, 0, buffer, ref size) ? new string(buffer, 0, size) : null;
    }

    /// <summary>True once exited, false while running, null when the state cannot be read.</summary>
    public static bool? HasExited(SafeProcessHandle handle) => NativeMethods.WaitForSingleObject(handle, 0) switch
    {
        WaitObject0 => true,
        WaitTimeout => false,
        _ => null
    };

    public static bool WaitForExit(SafeProcessHandle handle, TimeSpan timeout) =>
        NativeMethods.WaitForSingleObject(handle, (uint)timeout.TotalMilliseconds) == WaitObject0;

    /// <summary>Ends that one process; its children are never touched.</summary>
    public static bool TerminateOnly(SafeProcessHandle handle) => NativeMethods.TerminateProcess(handle, 1);

    private static class NativeMethods
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeProcessHandle OpenProcess(uint access, int inheritHandle, int processId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetProcessTimes(SafeProcessHandle process, out long creation, out long exit, out long kernel, out long user);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, [Out] char[] name, ref int size);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(SafeProcessHandle process, uint exitCode);
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

    // The only processes that may descend from the bound worker before watchdog-kill's stall:
    // the console host Windows starts for a console process. The product's watchdog ends the
    // worker's whole tree, so a stall is refused when anything else descends from it.
    public static IReadOnlyList<string> ApprovedWorkerDescendantNames { get; } = ["conhost"];

    private static readonly HashSet<string> ApprovedWorkerDescendants = new(ApprovedWorkerDescendantNames, StringComparer.OrdinalIgnoreCase);

    // Image names compare case-insensitively, as Windows does.
    public static string? RoleOf(string name) => name switch
    {
        _ when string.Equals(name, ProcessCensus.SpatialAnalyzer, StringComparison.OrdinalIgnoreCase) => SpatialAnalyzer,
        _ when string.Equals(name, ProcessCensus.SpatialAnalyzerSdk, StringComparison.OrdinalIgnoreCase) => Sdk,
        _ when string.Equals(name, ProcessCensus.BriosaServer, StringComparison.OrdinalIgnoreCase) => Server,
        _ when string.Equals(name, ProcessCensus.BriosaWorker, StringComparison.OrdinalIgnoreCase) => Worker,
        _ => null
    };

    public static bool IsTracked(string name) => RoleOf(name) is not null;

    public static bool IsApprovedWorkerDescendant(string name) => ApprovedWorkerDescendants.Contains(name);

    // A parent outside the tracked roles is named only as the harness, a Windows service host,
    // or "other"; no other process name is ever recorded. A service-host parent is a fact of the
    // snapshot, not a claim about how the process was started.
    public static string ParentRoleOf(int parentId, ProcessEntry? parent, int harnessProcessId) => parent switch
    {
        _ when parentId == harnessProcessId => Harness,
        null => Exited,
        _ when RoleOf(parent.Name) is { } role => role,
        _ when string.Equals(parent.Name, "svchost", StringComparison.OrdinalIgnoreCase) => ServiceHost,
        _ => Other
    };
}

/// <summary>
/// One tracked process over a probe: its label, never its id. Exit is never stated as a time:
/// it happened after <see cref="LastSeenMilliseconds"/> and no later than
/// <see cref="FirstAbsentMilliseconds"/>, the first complete snapshot without it.
/// </summary>
internal sealed record TrackedProcess(string Label, string Role, DateTimeOffset? StartedAt, string ParentRole, long FirstSeenMilliseconds, bool PresentAtStart)
{
    public long LastSeenMilliseconds { get; init; }

    public long? FirstAbsentMilliseconds { get; init; }

    // A process that a complete snapshot missed and a later one listed again: the earlier
    // absence was not an exit.
    public int Reappearances { get; init; }

    // How the run knows this process is its own: "bound-server", "child-of-bound-server", or
    // "appeared-during-start" (an SDK process first seen while the run's own start was in
    // flight). Null when the run cannot attribute it.
    public string? Attribution { get; init; }

    public bool Alive => FirstAbsentMilliseconds is null;
}

/// <summary>
/// Labels each Spatial Analyzer, SDK, server and worker process the first time a snapshot
/// shows it with its start time (for example <c>sdk-1</c>). A process is known by its id and
/// start time, so a reused id is a new process. Only a complete snapshot can mark a process
/// absent; an incomplete or failed one is counted as a gap. Ids are kept in memory only.
/// </summary>
internal sealed class ProcessTracker(int harnessProcessId)
{
    private readonly Dictionary<(int Id, DateTimeOffset StartedAt), TrackedProcess> _processes = [];
    private readonly Dictionary<string, (int Id, DateTimeOffset StartedAt, int ParentId, int? SessionId)> _identities = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _ordinals = new(StringComparer.Ordinal);
    private bool _observed;

    public int CompleteSnapshots { get; private set; }

    public int IncompleteSnapshots { get; private set; }

    public int FailedSnapshots { get; private set; }

    /// <summary>The status of the most recent snapshot; null before the first.</summary>
    public SnapshotStatus? LastStatus { get; private set; }

    public ProcessSnapshot? LastSnapshot { get; private set; }

    public IReadOnlyList<TrackedProcess> Processes => [.. _processes.Values
        .OrderBy(static process => process.FirstSeenMilliseconds).ThenBy(static process => process.Label, StringComparer.Ordinal)];

    public IReadOnlyList<TrackedProcess> Alive(string role) =>
        [.. Processes.Where(process => process.Alive && string.Equals(process.Role, role, StringComparison.Ordinal))];

    /// <summary>True only when the last snapshot was complete and showed none of these roles.</summary>
    public bool NoneConfirmed(params string[] roles) =>
        LastStatus == SnapshotStatus.Complete && roles.All(role => Alive(role).Count == 0);

    /// <summary>The id, start time, parent and session behind a label, for binding; never recorded.</summary>
    public (int Id, DateTimeOffset StartedAt, int ParentId, int? SessionId)? Identity(string label) =>
        _identities.TryGetValue(label, out var identity) ? identity : null;

    public void Attribute(string label, string attribution)
    {
        foreach (var (key, process) in _processes)
        {
            if (string.Equals(process.Label, label, StringComparison.Ordinal))
            {
                _processes[key] = process with { Attribution = attribution };
            }
        }
    }

    /// <summary>
    /// Labels and counts of every process descended from <paramref name="rootId"/> in the last
    /// snapshot, through parent ids. Only tracked descendants have labels; how many are not on
    /// the approved list is counted, and no other name is ever returned.
    /// </summary>
    public (IReadOnlyList<string> Labels, int Count) Descendants(int rootId)
    {
        var (labels, count, _) = DescendantCensus(rootId);
        return (labels, count);
    }

    public (IReadOnlyList<string> Labels, int Count, int Unapproved) DescendantCensus(int rootId)
    {
        if (LastSnapshot is not { } snapshot)
        {
            return ([], 0, 0);
        }

        var children = snapshot.Entries.Where(static entry => entry.Id != entry.ParentId)
            .GroupBy(static entry => entry.ParentId).ToDictionary(static group => group.Key, static group => group.ToList());
        var seen = new HashSet<int> { rootId };
        var queue = new Queue<int>([rootId]);
        var labels = new List<string>();
        var count = 0;
        var unapproved = 0;
        while (queue.Count > 0)
        {
            foreach (var child in children.GetValueOrDefault(queue.Dequeue(), []))
            {
                if (!seen.Add(child.Id))
                {
                    continue;
                }

                count++;
                queue.Enqueue(child.Id);
                if (!ProcessRoles.IsApprovedWorkerDescendant(child.Name))
                {
                    unapproved++;
                }

                if (child.StartedAt is { } started && _processes.TryGetValue((child.Id, started), out var tracked))
                {
                    labels.Add(tracked.Label);
                }
            }
        }

        return ([.. labels.Order(StringComparer.Ordinal)], count, unapproved);
    }

    public void Observe(ProcessSnapshot snapshot, long offsetMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        LastStatus = snapshot.Status;
        switch (snapshot.Status)
        {
            case SnapshotStatus.Failed:
                FailedSnapshots++;
                return;
            case SnapshotStatus.Incomplete:
                IncompleteSnapshots++;
                break;
            default:
                CompleteSnapshots++;
                break;
        }

        LastSnapshot = snapshot;
        var byId = new Dictionary<int, ProcessEntry>();
        foreach (var entry in snapshot.Entries)
        {
            byId.TryAdd(entry.Id, entry);
        }

        var present = new HashSet<(int, DateTimeOffset)>();
        foreach (var entry in snapshot.Entries)
        {
            // A tracked process without its start time cannot be told from a reused id.
            if (ProcessRoles.RoleOf(entry.Name) is not { } role || entry.StartedAt is not { } started)
            {
                continue;
            }

            var key = (entry.Id, started);
            present.Add(key);
            if (_processes.TryGetValue(key, out var known))
            {
                _processes[key] = known with
                {
                    LastSeenMilliseconds = offsetMilliseconds,
                    FirstAbsentMilliseconds = null,
                    Reappearances = known.Alive ? known.Reappearances : known.Reappearances + 1
                };
                continue;
            }

            var ordinal = _ordinals[role] = _ordinals.GetValueOrDefault(role) + 1;
            var label = $"{role}-{ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            _processes[key] = new TrackedProcess(label, role, started,
                ProcessRoles.ParentRoleOf(entry.ParentId, byId.GetValueOrDefault(entry.ParentId), harnessProcessId), offsetMilliseconds, PresentAtStart: !_observed)
            {
                LastSeenMilliseconds = offsetMilliseconds
            };
            _identities[label] = (entry.Id, started, entry.ParentId, entry.SessionId);
        }

        // Only a complete snapshot can show that a process is gone.
        if (snapshot.Status == SnapshotStatus.Complete)
        {
            foreach (var key in _processes.Keys.ToList())
            {
                if (_processes[key].Alive && !present.Contains(key))
                {
                    _processes[key] = _processes[key] with { FirstAbsentMilliseconds = offsetMilliseconds };
                }
            }
        }

        _observed = true;
    }
}
