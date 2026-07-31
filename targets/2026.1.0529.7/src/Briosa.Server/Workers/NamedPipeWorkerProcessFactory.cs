using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal sealed class NamedPipeWorkerProcessFactory(
    Func<int, WorkerProcessLaunch> launchFactory) : IWorkerProcessFactory
{
    private readonly Func<int, WorkerProcessLaunch> _launchFactory =
        launchFactory ?? throw new ArgumentNullException(nameof(launchFactory));

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Any launch failure must tear down a partially created child process and pipe.")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Pipe ownership transfers to the returned worker or is disposed in the failure path.")]
    public async ValueTask<IWorkerProcess> StartAsync(
        int generation,
        CancellationToken cancellationToken = default)
    {
        var launch = _launchFactory(generation);
        var pipeName = $"briosa-{Environment.ProcessId}-{Guid.NewGuid():N}";
        var pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);
        Process? process = null;

        try
        {
            var startInfo = CreateStartInfo(launch, pipeName);
            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("The worker process could not be started.");
            await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            return new NamedPipeWorkerProcess(process, pipe);
        }
        catch (Exception)
        {
            if (process is not null)
            {
                await TerminateProcess(process).ConfigureAwait(false);
                process.Dispose();
            }

            await pipe.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static ProcessStartInfo CreateStartInfo(WorkerProcessLaunch launch, string pipeName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = launch.FileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = launch.WorkingDirectory ?? Environment.CurrentDirectory
        };

        foreach (var argument in launch.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("--control-pipe");
        startInfo.ArgumentList.Add(pipeName);
        startInfo.ArgumentList.Add("--parent-process-id");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        return startInfo;
    }

    private static async Task TerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }

        try
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private sealed class NamedPipeWorkerProcess(
        Process process,
        NamedPipeServerStream pipe) : IWorkerProcess
    {
        private readonly WorkerControlChannel _channel = new(pipe);
        private readonly Process _process = process;
        private int _disposeState;

        public bool HasExited => _process.HasExited;

        public int? ExitCode => _process.HasExited ? _process.ExitCode : null;

        public ValueTask SendAsync(
            WorkerControlMessage message,
            CancellationToken cancellationToken = default) =>
            _channel.SendAsync(message, cancellationToken);

        public ValueTask<WorkerControlMessage> ReceiveAsync(
            CancellationToken cancellationToken = default) =>
            _channel.ReceiveAsync(cancellationToken);

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            _process.WaitForExitAsync(cancellationToken);

        public async ValueTask TerminateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }

            if (!_process.HasExited)
            {
                await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            {
                return;
            }

            await TerminateAsync().ConfigureAwait(false);
            _channel.Dispose();
            _process.Dispose();
        }
    }
}
