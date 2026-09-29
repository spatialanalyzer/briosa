using Briosa.Worker.Control;

namespace Briosa.Server.Workers;

internal static class WorkerCommandExchange
{
    public static async Task<WorkerExecutionResponse> RunAsync(
        IWorkerProcess worker,
        WorkerMpCommand command,
        Guid correlationId,
        WorkerRuntimeIdentitySnapshot? expectedRuntimeIdentity,
        CancellationToken cancellationToken)
    {
        await worker.SendAsync(WorkerControlMessage.Execute(correlationId, command),
            cancellationToken).ConfigureAwait(false);
        // The response frame has its own 64 KiB wire limit and one exchange is
        // active per worker. Its transient receive buffer is separate from the
        // request/command reservation held by the execution queue.
        var message = await worker.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        if (message.Kind != WorkerControlMessageKind.ExecutionResult ||
            message.CorrelationId != correlationId ||
            message.ExecutionResponse is not { } response)
        {
            throw new InvalidDataException("The worker returned an invalid execution response.");
        }

        if (response.Connection.RuntimeIdentity != expectedRuntimeIdentity ||
            response.Execution is WorkerMpResultAvailable { ResultCode: 2 } execution &&
            !OutputsMatch(command.OutputArguments, execution.OutputValues))
        {
            throw new InvalidDataException("The worker execution response has an invalid result shape.");
        }

        return response;
    }

    private static bool OutputsMatch(
        IReadOnlyList<WorkerMpOutputArgument> requested,
        IReadOnlyList<WorkerMpOutputValue> returned)
    {
        if (requested.Count != returned.Count) return false;
        for (var index = 0; index < requested.Count; index++)
        {
            if (requested[index].Name != returned[index].Name ||
                requested[index].Kind != returned[index].Kind)
            {
                return false;
            }
        }

        return true;
    }
}
