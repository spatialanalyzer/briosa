using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class NamedPipeWorkerProcess : IWorkerProcess
{
    private readonly NamedPipeServerStream _pipe;
    private readonly WorkerControlChannel _channel;
    private readonly Process _process;
    private bool _connected;
    private int _disposeState;

    public NamedPipeWorkerProcess(ProcessStartInfo startInfo, NamedPipeServerStream pipe)
    {
        _pipe = pipe;
        _channel = new WorkerControlChannel(pipe);
        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The worker process could not be started.");
    }

    public bool HasExited => _process.HasExited;
    public int? ExitCode => _process.HasExited ? _process.ExitCode : null;

    public ValueTask SendAsync(WorkerControlMessage message, CancellationToken cancellationToken = default) =>
        _channel.SendAsync(message, cancellationToken);

    public async ValueTask<WorkerControlMessage> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        // Transfer ownership before waiting for startup. Even a child that never
        // connects remains available to the controller's bounded cleanup path.
        if (!_connected)
        {
            await _pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            _connected = true;
        }
        return await _channel.ReceiveAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
        _process.WaitForExitAsync(cancellationToken);

    public async ValueTask TerminateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) when (_process.HasExited)
        {
        }
        catch (Win32Exception) when (_process.HasExited)
        {
        }
        await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        // Disposal releases handles only after confirmed exit. It cannot hide a
        // second, unbounded termination attempt from the process owner.
        if (Volatile.Read(ref _disposeState) != 0) return ValueTask.CompletedTask;
        if (!_process.HasExited)
            throw new InvalidOperationException("A live worker must be terminated before disposal.");
        if (Interlocked.Exchange(ref _disposeState, 1) == 0)
        {
            _channel.Dispose();
            _process.Dispose();
        }
        return ValueTask.CompletedTask;
    }
}
