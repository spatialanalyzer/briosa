using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
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
/// </summary>
internal sealed class OwnershipServer : IOwnershipServer
{
    public const string SdkLifecycleErrorTrailer = "briosa-spatial-analyzer-sdk-lifecycle-error-bin";

    private static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(15);

    private readonly GrpcChannel _channel;
    private readonly TimeSpan _callTimeout;
    private readonly Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient _sdk;

    public OwnershipServer(Uri address, TimeSpan callTimeout)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
            throw new ArgumentException("The ownership probe address must be loopback HTTP.", nameof(address));
        _channel = GrpcChannel.ForAddress(address);
        _callTimeout = callTimeout;
        _sdk = new Api.SpatialAnalyzerSdkLifecycle.SpatialAnalyzerSdkLifecycleClient(_channel);
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
        try
        {
            return await step.Operation.InvokePublicAsync(_channel.CreateCallInvoker(), step.Request, options).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ProbeOutcome.Unknown(LifecycleResult.ClientCancelledTransport, "client-cancelled");
        }
    }

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

/// <summary>
/// Binds Briosa worker and server processes through handles, reads the loopback endpoint's
/// listener owners, and compares the second worker's folder with the bound server's. It never
/// opens SpatialAnalyzer or an SDK process.
/// </summary>
internal sealed class ProcessControl : IProcessControl
{
    public static ProcessControl Instance { get; } = new();

    public int HarnessSessionId { get; } = CurrentSession();

    public IBoundProcess? Bind(int processId, DateTimeOffset startedAt, string expectedName)
    {
        if (expectedName is not (ProcessCensus.BriosaWorker or ProcessCensus.BriosaServer))
        {
            throw new ArgumentException("Only a Briosa worker or server may be bound.", nameof(expectedName));
        }

        Process process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return null;
        }

        try
        {
            // The handle is opened first; the name and start time are then read through it, so
            // they describe the process the handle names, not whatever holds the id later.
            _ = process.SafeHandle;
            if (process.HasExited ||
                !string.Equals(process.ProcessName, expectedName, StringComparison.OrdinalIgnoreCase) ||
                new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero) != startedAt)
            {
                process.Dispose();
                return null;
            }

            return new BoundProcess(process);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            process.Dispose();
            return null;
        }
    }

    public IReadOnlyList<int>? EndpointListeners(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        var owners = new List<int>();
        foreach (var family in new[] { NativeMethods.AddressFamilyIPv4, NativeMethods.AddressFamilyIPv6 })
        {
            if (TcpListeners(family) is not { } listeners)
            {
                return null;
            }

            owners.AddRange(listeners.Where(listener => listener.Port == address.Port && listener.LoopbackOrAny).Select(static listener => listener.OwnerId));
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
            var serverFolder = Path.GetDirectoryName(bound.Process.MainModule?.FileName);
            var workerFolder = Path.GetDirectoryName(Path.GetFullPath(workerPath));
            return serverFolder is not null && workerFolder is not null &&
                string.Equals(Path.GetFullPath(serverFolder), workerFolder, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Path.GetFileName(workerPath), "Briosa.Worker.exe", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(workerPath);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException or ArgumentException or IOException)
        {
            return null;
        }
    }

    private static int CurrentSession()
    {
        using var self = Process.GetCurrentProcess();
        return self.SessionId;
    }

    // Every TCP listener of one address family, or null when the table cannot be read.
    internal static IReadOnlyList<(int Port, bool LoopbackOrAny, int OwnerId)>? TcpListeners(int family)
    {
        var size = 0;
        _ = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref size, false, family, NativeMethods.TcpTableOwnerPidListener, 0);
        for (var attempt = 0; attempt < 3 && size > 0; attempt++)
        {
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                var result = NativeMethods.GetExtendedTcpTable(buffer, ref size, false, family, NativeMethods.TcpTableOwnerPidListener, 0);
                if (result == NativeMethods.InsufficientBuffer)
                {
                    continue;
                }

                if (result != 0)
                {
                    return null;
                }

                return Parse(buffer, family);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        return size == 0 ? [] : null;
    }

    private static List<(int Port, bool LoopbackOrAny, int OwnerId)> Parse(IntPtr table, int family)
    {
        var rows = Marshal.ReadInt32(table);
        var listeners = new List<(int, bool, int)>(rows);
        var rowSize = family == NativeMethods.AddressFamilyIPv4 ? 24 : 56;
        for (var index = 0; index < rows; index++)
        {
            var row = table + 4 + (index * rowSize);
            if (family == NativeMethods.AddressFamilyIPv4)
            {
                // MIB_TCPROW_OWNER_PID: state, local address, local port, remote address, remote port, owner.
                var address = new IPAddress(unchecked((uint)Marshal.ReadInt32(row + 4)));
                var port = NetworkPort(Marshal.ReadInt32(row + 8));
                listeners.Add((port, IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any), Marshal.ReadInt32(row + 20)));
            }
            else
            {
                // MIB_TCP6ROW_OWNER_PID: local address (16), scope, local port, remote address (16),
                // scope, remote port, state, owner.
                var bytes = new byte[16];
                Marshal.Copy(row, bytes, 0, 16);
                var address = new IPAddress(bytes);
                var port = NetworkPort(Marshal.ReadInt32(row + 20));
                listeners.Add((port, IPAddress.IsLoopback(address) || address.Equals(IPAddress.IPv6Any), Marshal.ReadInt32(row + 52)));
            }
        }

        return listeners;
    }

    // The port is in network byte order in the low two bytes.
    private static int NetworkPort(int value) => ((value & 0xFF) << 8) | ((value >> 8) & 0xFF);

    private sealed class BoundProcess(Process process) : IBoundProcess
    {
        private static readonly TimeSpan ExitWait = TimeSpan.FromSeconds(10);

        public Process Process { get; } = process;

        public BindingCheck Check()
        {
            try
            {
                return Process.HasExited ? BindingCheck.Exited : BindingCheck.Running;
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                return BindingCheck.Unverifiable;
            }
        }

        public string Terminate()
        {
            try
            {
                if (Process.HasExited)
                {
                    return "exited";
                }

                // That process only, through the held handle; never its children.
                Process.Kill(entireProcessTree: false);
                return Process.WaitForExit(ExitWait) ? "terminated" : "failed";
            }
            catch (InvalidOperationException)
            {
                return "exited";
            }
            catch (Win32Exception)
            {
                return "failed";
            }
        }

        public void Dispose() => Process.Dispose();
    }

    private static class NativeMethods
    {
        public const int AddressFamilyIPv4 = 2;
        public const int AddressFamilyIPv6 = 23;
        public const int TcpTableOwnerPidListener = 3;
        public const uint InsufficientBuffer = 122;

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr table, ref int size, [MarshalAs(UnmanagedType.Bool)] bool order, int family, int tableClass, uint reserved);
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
        return new SecondWorkerResult(exited ? "exited" : "terminated", null, null, null);
    }

    public async ValueTask DisposeAsync()
    {
        await TerminateAsync().ConfigureAwait(false);
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
