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
        Justification = "A synchronous launch failure must release the pipe; child ownership transfers immediately on success.")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Pipe ownership transfers to the returned worker or is disposed in the failure path.")]
    public async ValueTask<IWorkerProcess> StartAsync(
        int generation,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var launch = _launchFactory(generation);
        var pipeName = $"briosa-{Environment.ProcessId}-{Guid.NewGuid():N}";
        var pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);

        try
        {
            var startInfo = CreateStartInfo(launch, pipeName);
            return new NamedPipeWorkerProcess(startInfo, pipe);
        }
        catch (Exception)
        {
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
        RemoveIdentityConfiguration(startInfo.Environment);

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

    internal static void RemoveIdentityConfiguration(IDictionary<string, string?> environment)
    {
        // Operator evidence is server policy, not an input to the SDK process.
        foreach (var key in environment.Keys.Where(key => key.Replace("__", ":", StringComparison.Ordinal)
            .StartsWith("Briosa:SpatialAnalyzer:Identity:", StringComparison.OrdinalIgnoreCase)).ToArray())
            environment.Remove(key);
    }

}
