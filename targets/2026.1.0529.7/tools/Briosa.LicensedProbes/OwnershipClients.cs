using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Briosa.Worker.Control;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Win32.SafeHandles;
using Api = global::Briosa;

namespace Briosa.LicensedProbes;

/// <summary>
/// The packaged server through generated clients, on loopback HTTP only. Every call has its
/// own deadline and is sent once; a failure becomes a value-free result, never an exception
/// with server text. Every value the server returns is checked before it can reach a record.
/// Nothing is sent before the run binds the server, and every connection is proven, from the
/// TCP connection table, to reach that process before its first byte is written; a connection
/// that cannot be proven is closed unused and the call is recorded as never sent.
/// </summary>
internal sealed class OwnershipServer : IOwnershipServer
{
    public const string SdkLifecycleErrorTrailer = "briosa-spatial-analyzer-sdk-lifecycle-error-bin";

    private static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(15);

    private readonly GrpcChannel _channel;
    private readonly TimeSpan _callTimeout;
    private readonly Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient _sdk;
    private readonly Func<IPEndPoint, IPEndPoint, int, CancellationToken, Task<bool>> _verifyPeer;
    private int _peer;
    private int _verified;
    private int _refused;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The channel owns the handler (DisposeHttpClient) and disposes it with itself.")]
    public OwnershipServer(Uri address, TimeSpan callTimeout, Func<IPEndPoint, IPEndPoint, int, CancellationToken, Task<bool>>? verifyPeer = null)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
            throw new ArgumentException("The ownership probe address must be loopback HTTP.", nameof(address));
        _verifyPeer = verifyPeer ?? PeerConnection.IsOwnedByAsync;
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = ConnectAsync,
            UseProxy = false,
            EnableMultipleHttp2Connections = false
        };
        _channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = handler, DisposeHttpClient = true });
        _callTimeout = callTimeout;
        _sdk = new Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient(_channel);
    }

    public int VerifiedConnections => Volatile.Read(ref _verified);

    public int RefusedConnections => Volatile.Read(ref _refused);

    public void BindPeer(int serverProcessId)
    {
        if (serverProcessId <= 0)
            throw new ArgumentOutOfRangeException(nameof(serverProcessId), "A bound server has a process id.");
        Volatile.Write(ref _peer, serverProcessId);
    }

    public async Task<ServerDescription> DescribeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var info = await new Api.DiscoveryService.DiscoveryServiceClient(_channel)
                .GetServerInfoAsync(new Api.GetServerInfoRequest(), deadline: DateTime.UtcNow.Add(DiscoveryTimeout), cancellationToken: cancellationToken)
                .ResponseAsync.ConfigureAwait(false);
            return Describe(info);
        }
        catch (RpcException exception) when (IsPeerRefusal(exception))
        {
            return ServerDescription.PeerRefused;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            // The operator's cancellation, never an unreachable server: the run records it as such.
            throw new OperationCanceledException("Discovery was cancelled.", exception, cancellationToken);
        }
        catch (RpcException)
        {
            return ServerDescription.Unreachable;
        }
    }

    internal static ServerDescription Describe(Api.GetServerInfoResponse info)
    {
        ArgumentNullException.ThrowIfNull(info);
        return new ServerDescription(
            Reachable: true,
            TargetMatches: string.Equals(info.Version?.SpatialAnalyzerTarget, ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal),
            ServerVersion: info.Version is { HasBriosaVersion: true } version ? ExternalValues.Version(version.BriosaVersion) : null,
            SourceRevision: info.Version is { HasSourceRevision: true } source ? ExternalValues.Revision(source.SourceRevision) : null,
            LifecycleBoundsMilliseconds: Bounds(info.LifecycleBounds));
    }

    public Task<LifecycleResult> GetSdkStateAsync(CancellationToken cancellationToken) =>
        CallAsync(options => _sdk.GetSpatialAnalyzerSdkStateAsync(new Api.GetSpatialAnalyzerSdkStateRequest(), options).ResponseAsync,
            static response => response.State, cancellationToken);

    public Task<LifecycleResult> StartSdkAsync(CancellationToken cancellationToken) =>
        CallAsync(options => _sdk.StartSpatialAnalyzerSdkAsync(new Api.StartSpatialAnalyzerSdkRequest(), options).ResponseAsync,
            static response => response.State, cancellationToken);

    public Task<LifecycleResult> ConnectAsync(int generation, CancellationToken cancellationToken) =>
        CallAsync(options => _sdk.ConnectToSpatialAnalyzerAsync(
                new Api.ConnectToSpatialAnalyzerRequest { ExpectedSdkGeneration = generation }, options).ResponseAsync,
            static response => response.State, cancellationToken);

    public Task<LifecycleResult> StopSdkAsync(int generation, CancellationToken cancellationToken) =>
        CallAsync(options => _sdk.StopSpatialAnalyzerSdkAsync(
                new Api.StopSpatialAnalyzerSdkRequest { ExpectedSdkGeneration = generation }, options).ResponseAsync,
            static response => response.State, cancellationToken);

    public async Task<ProbeOutcome> ExecuteAsync(ProbeStep step, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(step);
        var options = new CallOptions(deadline: DateTime.UtcNow.Add(step.ClientTimeout ?? _callTimeout), cancellationToken: cancellationToken);
        var refused = RefusedConnections;
        try
        {
            var outcome = await step.Operation.InvokePublicAsync(_channel.CreateCallInvoker(), step.Request, options).ConfigureAwait(false);

            // Calls are sequential, so a refusal during this call refused this call's connection:
            // the step was never sent.
            return RefusedConnections != refused ? PeerUnverifiedOutcome : outcome;
        }
        catch (OperationCanceledException)
        {
            return ProbeOutcome.Unknown(LifecycleResult.ClientCancelledTransport, "client-cancelled");
        }
    }

    internal static ProbeOutcome PeerUnverifiedOutcome { get; } = new(ProbeOutcomeKind.NotStarted, LifecycleResult.PeerUnverifiedTransport,
        null, null, null, [], null, null, "NotStarted", ProbeOutcome.EmptyObservations);

    public ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }

    internal static LifecycleResult FromState(Api.SpatialAnalyzerSdkLifecycleState? state, string transport) => state is null
        ? LifecycleResult.Failed(transport, null)
        : new LifecycleResult(
            transport,
            state.SdkState.ToString(),
            state.HasSdkGeneration ? state.SdkGeneration : null,
            state.ReadyForMp,
            state.ConnectionState.ToString(),
            state.HasDiagnosticCode ? ExternalValues.DiagnosticCode(state.DiagnosticCode) : null,
            state.LastIncident?.TerminationKind.ToString());

    internal static LifecycleResult FromRpcException(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var transport = "grpc:" + exception.StatusCode.ToString();
        var trailer = exception.Trailers.FirstOrDefault(static entry => entry.Key == SdkLifecycleErrorTrailer);
        if (trailer is null)
        {
            return LifecycleResult.Failed(transport, null);
        }

        try
        {
            var error = Api.SpatialAnalyzerSdkLifecycleError.Parser.ParseFrom(trailer.ValueBytes);
            return FromState(error.State, transport) with { DiagnosticCode = ExternalValues.DiagnosticCode(error.DiagnosticCode) };
        }
        catch (Google.Protobuf.InvalidProtocolBufferException)
        {
            return LifecycleResult.Failed(transport, null);
        }
    }

    internal static IReadOnlyDictionary<string, long> Bounds(Api.LifecycleBounds? bounds)
    {
        var map = new SortedDictionary<string, long>(StringComparer.Ordinal);
        if (bounds is null)
        {
            return map;
        }

        void Add(string name, bool present, uint value)
        {
            if (present && value > 0)
            {
                map[name] = value;
            }
        }

        Add("start_sdk_milliseconds", bounds.HasStartSdkMilliseconds, bounds.StartSdkMilliseconds);
        Add("connect_spatial_analyzer_milliseconds", bounds.HasConnectSpatialAnalyzerMilliseconds, bounds.ConnectSpatialAnalyzerMilliseconds);
        Add("stop_sdk_milliseconds", bounds.HasStopSdkMilliseconds, bounds.StopSdkMilliseconds);
        Add("recover_sdk_milliseconds", bounds.HasRecoverSdkMilliseconds, bounds.RecoverSdkMilliseconds);
        Add("startup_without_launch_milliseconds", bounds.HasStartupWithoutLaunchMilliseconds, bounds.StartupWithoutLaunchMilliseconds);
        return map;
    }

    /// <summary>The call failed because its connection's peer was not proven, so it was never sent.</summary>
    internal static bool IsPeerRefusal(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        for (var inner = exception.Status.DebugException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PeerUnverifiedException)
            {
                return true;
            }
        }

        return false;
    }

    // Opens every connection the channel uses. Only a loopback connection whose other end the
    // TCP connection table attributes to the bound server is handed to the channel; any other is
    // closed before a byte is written.
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Each socket is disposed in its finally unless the returned stream, which owns it, took it.")]
    private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var peer = Volatile.Read(ref _peer);
        if (peer <= 0)
        {
            Interlocked.Increment(ref _refused);
            throw new PeerUnverifiedException();
        }

        var host = context.DnsEndPoint.Host.Trim('[', ']');
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        SocketException? unreachable = null;
        foreach (var address in addresses.Where(IPAddress.IsLoopback))
        {
            Socket? socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.NoDelay = true;
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken).ConfigureAwait(false);
                }
                catch (SocketException exception)
                {
                    // Nothing listens on this address; nothing was sent.
                    unreachable = exception;
                    continue;
                }

                if (socket.LocalEndPoint is IPEndPoint local && socket.RemoteEndPoint is IPEndPoint remote &&
                    await _verifyPeer(local, remote, peer, cancellationToken).ConfigureAwait(false))
                {
                    Interlocked.Increment(ref _verified);
                    var stream = new NetworkStream(socket, ownsSocket: true);
                    socket = null;
                    return stream;
                }

                // Closed in the finally below, before a byte is written.
                Interlocked.Increment(ref _refused);
                throw new PeerUnverifiedException();
            }
            finally
            {
                socket?.Dispose();
            }
        }

        throw unreachable ?? new SocketException((int)SocketError.AddressNotAvailable);
    }

    private async Task<LifecycleResult> CallAsync<TResponse>(
        Func<CallOptions, Task<TResponse>> call,
        Func<TResponse, Api.SpatialAnalyzerSdkLifecycleState?> state,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await call(new CallOptions(deadline: DateTime.UtcNow.Add(_callTimeout), cancellationToken: cancellationToken))
                .ConfigureAwait(false);
            return FromState(state(response), "grpc:OK");
        }
        catch (RpcException exception) when (IsPeerRefusal(exception))
        {
            return LifecycleResult.Failed(LifecycleResult.PeerUnverifiedTransport, null);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
        {
            return LifecycleResult.Failed(LifecycleResult.ClientCancelledTransport, "client-cancelled");
        }
        catch (RpcException exception)
        {
            return FromRpcException(exception);
        }
        catch (OperationCanceledException)
        {
            return LifecycleResult.Failed(LifecycleResult.ClientCancelledTransport, "client-cancelled");
        }
    }
}

/// <summary>A connection whose peer could not be proven to be the bound server; nothing was sent over it.</summary>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors",
    Justification = "Raised only by the connection check, with one fixed message.")]
internal sealed class PeerUnverifiedException() : Exception("The connection's peer is not the bound server.");

/// <summary>
/// Reads, from the TCP connection table, which process owns the other end of one loopback
/// connection. It reads addresses, ports and owning process ids only, and records none of them.
/// </summary>
internal static class PeerConnection
{
    private const int Attempts = 3;
    private static readonly TimeSpan Retry = TimeSpan.FromMilliseconds(20);

    /// <summary>
    /// True only when the table lists this harness's side of the connection and exactly one other
    /// side, with the endpoints reversed, owned by <paramref name="serverProcessId"/>. A table that
    /// cannot be read, or a side that is missing or ambiguous, is never a match.
    /// </summary>
    public static async Task<bool> IsOwnedByAsync(IPEndPoint local, IPEndPoint remote, int serverProcessId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(remote);

        // A dual-mode socket reports an IPv4 connection as IPv4-mapped; the IPv4 table lists it.
        if (local.Address.IsIPv4MappedToIPv6 && remote.Address.IsIPv4MappedToIPv6)
        {
            (local, remote) = (new IPEndPoint(local.Address.MapToIPv4(), local.Port), new IPEndPoint(remote.Address.MapToIPv4(), remote.Port));
        }

        if (!IPAddress.IsLoopback(remote.Address) || local.AddressFamily != remote.AddressFamily)
        {
            return false;
        }

        var family = remote.AddressFamily == AddressFamily.InterNetwork ? TcpTable.AddressFamilyIPv4 : TcpTable.AddressFamilyIPv6;
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (TcpTable.Rows(family, TcpTable.OwnerPidConnections) is not { } rows)
            {
                return false;
            }

            var ours = rows.Count(row => row.Local.Equals(local) && row.Remote.Equals(remote) && row.Owner == Environment.ProcessId);
            var theirs = rows.Where(row => row.Local.Equals(remote) && row.Remote.Equals(local)).Select(static row => row.Owner).ToList();
            if (ours == 1 && theirs.Count == 1)
            {
                return theirs[0] == serverProcessId;
            }

            // The other side can appear a moment after the connect completes.
            await Task.Delay(Retry, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}

/// <summary>Reads the IPv4 or IPv6 TCP table with owning process ids (iphlpapi).</summary>
internal static class TcpTable
{
    public const int AddressFamilyIPv4 = 2;
    public const int AddressFamilyIPv6 = 23;
    public const int OwnerPidListener = 3;
    public const int OwnerPidConnections = 4;

    private const uint InsufficientBuffer = 122;

    internal readonly record struct Row(IPEndPoint Local, IPEndPoint Remote, int Owner);

    /// <summary>Every row of one table, or null when it cannot be read.</summary>
    public static IReadOnlyList<Row>? Rows(int family, int tableClass)
    {
        var size = 0;
        _ = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref size, false, family, tableClass, 0);
        for (var attempt = 0; attempt < 3 && size > 0; attempt++)
        {
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                var result = NativeMethods.GetExtendedTcpTable(buffer, ref size, false, family, tableClass, 0);
                if (result == InsufficientBuffer)
                {
                    continue;
                }

                return result == 0 ? Parse(buffer, family) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return size == 0 ? [] : null;
    }

    private static List<Row> Parse(IntPtr table, int family)
    {
        var count = Marshal.ReadInt32(table);
        var rows = new List<Row>(count);
        var rowSize = family == AddressFamilyIPv4 ? 24 : 56;
        for (var index = 0; index < count; index++)
        {
            var row = table + 4 + (index * rowSize);
            if (family == AddressFamilyIPv4)
            {
                // MIB_TCPROW_OWNER_PID: state, local address, local port, remote address, remote port, owner.
                rows.Add(new Row(
                    new IPEndPoint(new IPAddress(unchecked((uint)Marshal.ReadInt32(row + 4))), NetworkPort(Marshal.ReadInt32(row + 8))),
                    new IPEndPoint(new IPAddress(unchecked((uint)Marshal.ReadInt32(row + 12))), NetworkPort(Marshal.ReadInt32(row + 16))),
                    Marshal.ReadInt32(row + 20)));
            }
            else
            {
                // MIB_TCP6ROW_OWNER_PID: local address (16), scope, local port, remote address (16),
                // scope, remote port, state, owner.
                rows.Add(new Row(
                    new IPEndPoint(Address6(row, 0), NetworkPort(Marshal.ReadInt32(row + 20))),
                    new IPEndPoint(Address6(row, 24), NetworkPort(Marshal.ReadInt32(row + 44))),
                    Marshal.ReadInt32(row + 52)));
            }
        }

        return rows;
    }

    private static IPAddress Address6(IntPtr row, int offset)
    {
        var bytes = new byte[16];
        Marshal.Copy(row + offset, bytes, 0, 16);
        return new IPAddress(bytes, unchecked((uint)Marshal.ReadInt32(row + offset + 16)));
    }

    // The port is in network byte order in the low two bytes.
    private static int NetworkPort(int value) => ((value & 0xFF) << 8) | ((value >> 8) & 0xFF);

    private static class NativeMethods
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr table, ref int size, [MarshalAs(UnmanagedType.Bool)] bool order, int family, int tableClass, uint reserved);
    }
}

/// <summary>
/// Binds Briosa worker and server processes through handles with the least rights a scenario
/// needs, reads the loopback endpoint's listener owners, and compares the second worker's
/// folder with the bound server's. It never opens SpatialAnalyzer or an SDK process.
/// </summary>
internal sealed class ProcessControl : IProcessControl
{
    public static ProcessControl Instance { get; } = new();

    public int HarnessSessionId { get; } = ProcessHandles.SessionOf(Environment.ProcessId) ?? -1;

    public IBoundProcess? Bind(int processId, DateTimeOffset startedAt, string expectedName, bool terminable)
    {
        if (expectedName is not (ProcessCensus.BriosaWorker or ProcessCensus.BriosaServer))
        {
            throw new ArgumentException("Only a Briosa worker or server may be bound.", nameof(expectedName));
        }

        return Open(processId, startedAt, expectedName, terminable);
    }

    /// <summary>
    /// Opens the process for query and wait, and for termination only when
    /// <paramref name="terminable"/>. The handle is opened first; the name and start time are
    /// then read through it, so they describe the process it names, not whatever holds the id later.
    /// </summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The handle is owned by the returned binding, or disposed on every refusal.")]
    internal static BoundProcess? Open(int processId, DateTimeOffset startedAt, string expectedName, bool terminable)
    {
        var handle = ProcessHandles.Open(processId, terminable);
        if (handle is null)
        {
            return null;
        }

        var bound = new BoundProcess(handle, terminable);
        if (bound.Check() != BindingCheck.Running ||
            ProcessHandles.StartTime(handle) != startedAt ||
            !string.Equals(Path.GetFileNameWithoutExtension(ProcessHandles.ImagePath(handle)), expectedName, StringComparison.OrdinalIgnoreCase))
        {
            bound.Dispose();
            return null;
        }

        return bound;
    }

    public IReadOnlyList<int>? EndpointListeners(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        var literal = IPAddress.TryParse(address.Host.Trim('[', ']'), out var parsed) ? parsed : null;
        var owners = new List<int>();
        foreach (var family in new[] { TcpTable.AddressFamilyIPv4, TcpTable.AddressFamilyIPv6 })
        {
            var wildcard = family == TcpTable.AddressFamilyIPv4 ? IPAddress.Any : IPAddress.IPv6Any;

            // An address literal reaches only listeners of its own family.
            if (literal is not null && literal.AddressFamily != wildcard.AddressFamily)
            {
                continue;
            }

            if (TcpTable.Rows(family, TcpTable.OwnerPidListener) is not { } listeners)
            {
                return null;
            }

            owners.AddRange(listeners
                .Where(listener => listener.Local.Port == address.Port && (listener.Local.Address.Equals(wildcard) ||
                    (literal is null ? IPAddress.IsLoopback(listener.Local.Address) : listener.Local.Address.Equals(literal))))
                .Select(static listener => listener.Owner));
        }

        return owners;
    }

    public bool? WorkerFromServerPackage(IBoundProcess server, string workerPath)
    {
        ArgumentNullException.ThrowIfNull(server);
        if (server is not BoundProcess bound || string.IsNullOrWhiteSpace(workerPath))
        {
            return null;
        }

        // Paths are compared in memory only and never recorded.
        try
        {
            var serverFolder = Path.GetDirectoryName(ProcessHandles.ImagePath(bound.Handle));
            var workerFolder = Path.GetDirectoryName(Path.GetFullPath(workerPath));
            return serverFolder is not null && workerFolder is not null &&
                string.Equals(Path.GetFullPath(serverFolder), workerFolder, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Path.GetFileName(workerPath), "Briosa.Worker.exe", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(workerPath);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Every TCP listener of one address family, or null when the table cannot be read.</summary>
    internal static IReadOnlyList<TcpTable.Row>? TcpListeners(int family) => TcpTable.Rows(family, TcpTable.OwnerPidListener);

    internal sealed class BoundProcess(SafeProcessHandle handle, bool terminable) : IBoundProcess
    {
        private static readonly TimeSpan ExitWait = TimeSpan.FromSeconds(10);

        public SafeProcessHandle Handle { get; } = handle;

        public bool Terminable { get; } = terminable;

        public BindingCheck Check() => ProcessHandles.HasExited(Handle) switch
        {
            true => BindingCheck.Exited,
            false => BindingCheck.Running,
            _ => BindingCheck.Unverifiable
        };

        public string Terminate()
        {
            // A binding without the right to terminate never ends the process.
            if (!Terminable)
            {
                return "not-authorized";
            }

            switch (Check())
            {
                case BindingCheck.Exited:
                    return "exited";
                case BindingCheck.Unverifiable:
                    return "failed";
            }

            // That process only, through the held handle; never its children.
            if (!ProcessHandles.TerminateOnly(Handle))
            {
                return Check() == BindingCheck.Exited ? "exited" : "failed";
            }

            return ProcessHandles.WaitForExit(Handle, ExitWait) ? "terminated" : "failed";
        }

        public void Dispose() => Handle.Dispose();
    }
}

/// <summary>
/// The competing client: a second, unchanged Briosa worker from the same package, hosted by
/// the harness over the private control protocol while the server's worker is connected. It
/// activates the SDK and attaches with one ConnectEx attempt; it never runs an MP. A worker
/// whose activated SDK is not runtime-verified as this exact target is stopped at once. Every
/// exchange is bounded, and a worker that does not stop in time is terminated (that process
/// only).
/// </summary>
internal sealed class SecondWorker : ISecondWorker
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AttachTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(15);

    private readonly string _workerPath;
    private NamedPipeServerStream? _pipe;
    private WorkerControlChannel? _channel;
    private Process? _process;
    private bool _faulted;
    private bool _abandoned;

    public SecondWorker(string workerPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerPath);
        _workerPath = workerPath;
    }

    public async Task<SecondWorkerResult> StartAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_workerPath) ||
            !string.Equals(Path.GetFileName(_workerPath), "Briosa.Worker.exe", StringComparison.OrdinalIgnoreCase))
            return new SecondWorkerResult("worker-executable-invalid", null, null, null);

        var pipeName = $"briosa-ownership-{Environment.ProcessId}-{Guid.NewGuid():N}";
        _pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);
        var startInfo = new ProcessStartInfo
        {
            FileName = _workerPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(_workerPath))!
        };
        startInfo.ArgumentList.Add("--control-pipe");
        startInfo.ArgumentList.Add(pipeName);
        startInfo.ArgumentList.Add("--parent-process-id");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
        try
        {
            _process = Process.Start(startInfo);
        }
        catch (Win32Exception)
        {
            return new SecondWorkerResult("worker-start-failed", null, null, null);
        }

        if (_process is null)
            return new SecondWorkerResult("worker-start-failed", null, null, null);

        try
        {
            using (var connect = Linked(ConnectTimeout, cancellationToken))
            {
                await _pipe.WaitForConnectionAsync(connect.Token).ConfigureAwait(false);
            }

            if (!NativeMethods.GetNamedPipeClientProcessId(_pipe.SafePipeHandle, out var clientProcessId) || clientProcessId != (uint)_process.Id)
            {
                await FaultAsync().ConfigureAwait(false);
                return new SecondWorkerResult("worker-pipe-client-mismatch", null, null, null);
            }

            _channel = new WorkerControlChannel(_pipe, leaveOpen: true);
            WorkerControlMessage ready;
            using (var bounded = Linked(StartupTimeout, cancellationToken))
            {
                ready = await _channel.ReceiveAsync(bounded.Token).ConfigureAwait(false);
            }

            if (ready.Kind != WorkerControlMessageKind.Ready || ready.ProcessId != _process.Id)
            {
                await FaultAsync().ConfigureAwait(false);
                return new SecondWorkerResult("worker-ready-invalid", null, null, null);
            }

            var activated = ready.Connection?.RuntimeIdentity?.ActivatedSdk;
            var matches = activated is { Source: WorkerRuntimeIdentityEvidenceSource.RuntimeVerified } &&
                string.Equals(activated.Version, ProbeTarget.SpatialAnalyzerTarget, StringComparison.Ordinal);
            var state = ready.Connection?.State.ToString();
            var code = ExternalValues.DiagnosticCode(ready.Connection?.DiagnosticCode);
            if (!matches)
            {
                // Never attached: a client whose SDK is not this exact target is stopped at once.
                await FaultAsync().ConfigureAwait(false);
                return new SecondWorkerResult("activated-sdk-identity-mismatch", state, code, false);
            }

            return new SecondWorkerResult(SecondWorkerResult.Ready, state, code, true);
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidDataException or TimeoutException)
        {
            await FaultAsync().ConfigureAwait(false);
            return new SecondWorkerResult(exception is OperationCanceledException ? "startup-timeout" : "worker-startup-failed", null, null, null);
        }
    }

    public async Task<SecondWorkerResult> ConnectAsync(CancellationToken cancellationToken)
    {
        if (_faulted || _channel is null)
            return new SecondWorkerResult("not-run", null, null, null);
        try
        {
            var request = WorkerControlMessage.Connect(Guid.NewGuid());
            using var bounded = Linked(AttachTimeout, cancellationToken);
            await _channel.SendAsync(request, bounded.Token).ConfigureAwait(false);
            var response = await _channel.ReceiveAsync(bounded.Token).ConfigureAwait(false);
            if (response.Kind != WorkerControlMessageKind.ConnectionResult || response.CorrelationId != request.CorrelationId)
            {
                await FaultAsync().ConfigureAwait(false);
                return new SecondWorkerResult("connect-response-invalid", null, null, null);
            }

            return new SecondWorkerResult(
                response.Connection?.State == WorkerConnectionState.Connected ? SecondWorkerResult.Connected : "not-connected",
                response.Connection?.State.ToString(), ExternalValues.DiagnosticCode(response.Connection?.DiagnosticCode), null);
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or InvalidDataException or TimeoutException)
        {
            // A ConnectEx that does not answer in time: the worker is terminated, never reused.
            await FaultAsync().ConfigureAwait(false);
            return new SecondWorkerResult(exception is OperationCanceledException ? "connect-timeout" : "channel-lost", null, null, null);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Best-effort graceful stop; termination is the bounded fallback and the result says which happened.")]
    public async Task<SecondWorkerResult> StopAsync(CancellationToken cancellationToken)
    {
        if (_process is null)
            return new SecondWorkerResult("not-started", null, null, null);
        if (_abandoned)
            return new SecondWorkerResult("not-run", null, null, null);
        if (!_faulted && _channel is not null && !_process.HasExited)
        {
            try
            {
                var request = WorkerControlMessage.Stop(Guid.NewGuid());
                using var bounded = Linked(StopTimeout, cancellationToken);
                await _channel.SendAsync(request, bounded.Token).ConfigureAwait(false);
                var response = await _channel.ReceiveAsync(bounded.Token).ConfigureAwait(false);
                await _process.WaitForExitAsync(bounded.Token).ConfigureAwait(false);
                if (response.Kind == WorkerControlMessageKind.Stopped)
                    return new SecondWorkerResult("stopped", null, null, null);
            }
            catch (Exception)
            {
            }
        }

        var exited = _process.HasExited;
        await TerminateAsync().ConfigureAwait(false);

        // "terminated" only once the process is seen to have exited.
        return new SecondWorkerResult(exited ? "exited" : _process.HasExited ? "terminated" : "termination-unconfirmed", null, null, null);
    }

    public void Abandon() => _abandoned = true;

    public async ValueTask DisposeAsync()
    {
        // After a license prompt the second worker is left as it is: nothing is sent to it and
        // it is not terminated. Only the harness's own handles are released.
        if (!_abandoned)
        {
            await TerminateAsync().ConfigureAwait(false);
        }

        _channel?.Dispose();
        if (_pipe is not null)
        {
            await _pipe.DisposeAsync().ConfigureAwait(false);
        }

        _process?.Dispose();
    }

    private async Task FaultAsync()
    {
        _faulted = true;
        await TerminateAsync().ConfigureAwait(false);
    }

    private async Task TerminateAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            // The second worker itself only, never its process tree.
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: false);
            }
        }
        catch (InvalidOperationException) when (_process.HasExited)
        {
        }
        catch (Win32Exception) when (_process.HasExited)
        {
        }

        using var exit = new CancellationTokenSource(StopTimeout);
        try
        {
            await _process.WaitForExitAsync(exit.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static CancellationTokenSource Linked(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(timeout);
        return source;
    }

    private static class NativeMethods
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);
    }
}
