using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using Briosa.Worker.Control;

namespace Briosa.Worker.TestHost;

/// <summary>A synthetic SDK peer for measuring the real private control channel.</summary>
internal static class PerformanceWorkerProcess
{
    public static async Task<int> RunAsync(string[] arguments)
    {
        using var pipe = new NamedPipeClientStream(".", Option(arguments, "--control-pipe"),
            PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(10000).ConfigureAwait(false);
        using var channel = new WorkerControlChannel(pipe, leaveOpen: true);
        var identity = new WorkerRuntimeIdentitySnapshot(
            new("2024.1.0508.5", WorkerRuntimeIdentityEvidenceSource.RuntimeVerified),
            new("2024.1.0508.5", WorkerRuntimeIdentityEvidenceSource.RuntimeVerified));
        WorkerConnectionSnapshot Snapshot(WorkerExecutionReadinessState readiness) => new(
            WorkerConnectionState.Connected, readiness, 0, 1, 1, "synthetic-performance-worker",
            DateTimeOffset.UtcNow, identity);
        await channel.SendAsync(WorkerControlMessage.Ready(Environment.ProcessId,
            Snapshot(WorkerExecutionReadinessState.Unverified))).ConfigureAwait(false);
        var values = new WorkerDoubleArrayValue([]);
        var executed = 0;
        var pings = 0;
        var delayed = false;
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        while (true)
        {
            var message = await channel.ReceiveAsync().ConfigureAwait(false);
            switch (message.Kind)
            {
                case WorkerControlMessageKind.VerifyExecution:
                    await channel.SendAsync(WorkerControlMessage.ExecutionVerificationResult(message.CorrelationId,
                        Snapshot(WorkerExecutionReadinessState.ExecutionReady))).ConfigureAwait(false);
                    break;
                case WorkerControlMessageKind.Ping:
                    pings++;
                    await channel.SendAsync(WorkerControlMessage.Pong(message.CorrelationId,
                        Snapshot(WorkerExecutionReadinessState.ExecutionReady))).ConfigureAwait(false);
                    break;
                case WorkerControlMessageKind.Stop:
                    using (var process = Process.GetCurrentProcess())
                    {
                        var report = new
                        {
                            executed,
                            pings,
                            allocated_bytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore,
                            retained_managed_bytes = GC.GetTotalMemory(forceFullCollection: true),
                            peak_working_set_bytes = process.PeakWorkingSet64
                        };
                        await File.WriteAllTextAsync(Option(arguments, "--report"),
                            JsonSerializer.Serialize(report)).ConfigureAwait(false);
                    }
                    await channel.SendAsync(WorkerControlMessage.Stopped(message.CorrelationId)).ConfigureAwait(false);
                    return 0;
                case WorkerControlMessageKind.Execute:
                    executed++;
                    var command = message.Command!;
                    if (command.InputArguments.Any(argument => argument.Value is WorkerTextValue { Value: "delay" }))
                    {
                        // Hold the first synthetic request long enough for a burst to fill admission.
                        await Task.Delay(delayed ? 10 : 250).ConfigureAwait(false);
                        delayed = true;
                    }
                    if (command.OperationId == "variables.set_named_double_list_variable")
                        values = command.InputArguments[1].RequireValue<WorkerDoubleArrayValue>();
                    var outputs = command.OutputArguments.Select(output => CreateOutput(output, values)).ToArray();
                    await channel.SendAsync(WorkerControlMessage.ExecutionResult(message.CorrelationId, new(
                        WorkerExecutionResponseStatus.Completed, new WorkerMpResultAvailable(2, 0, outputs, null),
                        Snapshot(WorkerExecutionReadinessState.ExecutionReady), null))).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidDataException("Unexpected performance worker request.");
            }
        }
    }

    private static WorkerRetrievedOutput CreateOutput(WorkerMpOutputArgument output, WorkerDoubleArrayValue values) =>
        new WorkerRetrievedOutput(output.Name, output.Kind, output.Kind switch
        {
            WorkerMpValueKind.FloatingPoint => new WorkerDoubleValue(1.25),
            WorkerMpValueKind.Logical => new WorkerBooleanValue(false),
            WorkerMpValueKind.DoubleArray => values,
            WorkerMpValueKind.FitConstraintScalarOptions => new WorkerFitConstraintScalarOptionsValue(
                new(true, 1.25), new(false, -0.5)),
            _ => throw new InvalidDataException("Unexpected performance output family.")
        });

    private static string Option(string[] arguments, string name)
    {
        var index = Array.IndexOf(arguments, name);
        if (index < 0 || index + 1 >= arguments.Length)
            throw new ArgumentException("Missing performance worker option.", nameof(arguments));
        return arguments[index + 1];
    }
}
