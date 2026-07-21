using System.Diagnostics;
using System.IO.Pipes;
using Briosa.Worker.Control;

namespace Briosa.Worker;

internal static class WorkerControlHost
{
    public static int Run(string pipeName, int? parentProcessId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        if (parentProcessId is > 0)
        {
            StartParentMonitor(parentProcessId.Value);
        }

        var completion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => completion.SetResult(RunOnSta(pipeName)))
        {
            IsBackground = false,
            Name = "Briosa worker control STA"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task.GetAwaiter().GetResult();
    }

    private static int RunOnSta(string pipeName)
    {
        try
        {
            using var lifetime = new WorkerStaLifetime();
            using var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.None);
            pipe.Connect(15_000);
            using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
            channel.Send(WorkerControlMessage.Ready(Environment.ProcessId));

            while (true)
            {
                var message = channel.Receive();
                switch (message.Kind)
                {
                    case WorkerControlMessageKind.Ping:
                        channel.Send(WorkerControlMessage.Pong(message.CorrelationId));
                        break;
                    case WorkerControlMessageKind.Stop:
                        channel.Send(WorkerControlMessage.Stopped(message.CorrelationId));
                        return 0;
                    default:
                        return 4;
                }
            }
        }
        catch (TimeoutException)
        {
            return 2;
        }
        catch (IOException)
        {
            return 3;
        }
        catch (InvalidDataException)
        {
            return 4;
        }
    }

    private static void StartParentMonitor(int parentProcessId)
    {
        var monitor = new Thread(() =>
        {
            try
            {
                using var parent = Process.GetProcessById(parentProcessId);
                parent.WaitForExit();
            }
            catch (ArgumentException)
            {
            }

            Environment.Exit(20);
        })
        {
            IsBackground = true,
            Name = "Briosa worker parent monitor"
        };
        monitor.Start();
    }

    private sealed class WorkerStaLifetime : IDisposable
    {
        private readonly int _threadId;

        public WorkerStaLifetime()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("The worker SDK lifetime requires an STA.");
            }

            _threadId = Environment.CurrentManagedThreadId;
            _ = InteropMetadata.AssemblyName;
        }

        public void Dispose()
        {
            if (Environment.CurrentManagedThreadId != _threadId ||
                Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException(
                    "The worker SDK lifetime must be released on its owning STA.");
            }
        }
    }
}
