namespace Briosa.Worker.Control;

/// <summary>
/// Builds the bounded execution result that replaces one the private channel
/// rejected before writing any frame bytes, for example because an output value
/// is oversized or non-finite. Completion evidence is never discarded: an SDK
/// call fault stays an SDK call fault, and only the values that cannot be
/// encoded are dropped.
/// </summary>
public static class WorkerExecutionDelivery
{
    /// <summary>Value-free code for output values the private channel could not encode.</summary>
    public const string OutputEncodingRejected = "worker-output-encoding-rejected";

    /// <summary>
    /// Returns an encodable replacement for <paramref name="response"/>, or
    /// throws <see cref="WorkerMessageRejectedException"/> when the result
    /// carries no completion evidence that a smaller frame could preserve.
    /// </summary>
    public static WorkerControlMessage Undeliverable(Guid correlationId, WorkerExecutionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.Execution switch
        {
            WorkerSdkCallFaulted fault => BoundSdkCallFault(correlationId, response, fault),
            { MpSucceeded: true } execution => WorkerControlMessage.ExecutionResult(correlationId,
                new WorkerExecutionResponse(
                    WorkerExecutionResponseStatus.Completed,
                    new WorkerMpOutputsUnavailable(execution.DurationMilliseconds, OutputEncodingRejected),
                    response.Connection,
                    OutputEncodingRejected)),
            _ => throw new WorkerMessageRejectedException("The worker execution result could not be encoded.")
        };
    }

    // Keep the phase, diagnostic code and every output's status. First mark each
    // output value that cannot be encoded on its own (non-finite, invalid, or
    // alone over the frame bound); then, while the frame is still over the bound,
    // drop the largest remaining value. If even a frame with every output marked
    // unavailable does not fit, send the fault with its per-output evidence
    // withheld: that frame has a fixed size, so the SDK call fault always arrives.
    private static WorkerControlMessage BoundSdkCallFault(
        Guid correlationId,
        WorkerExecutionResponse response,
        WorkerSdkCallFaulted fault)
    {
        var outputs = fault.Outputs.ToArray();
        var sizes = new int[outputs.Length];
        for (var index = 0; index < outputs.Length; index++)
        {
            if (outputs[index].Retrieved &&
                (!WorkerControlChannel.TryMeasureOutput(outputs[index], out sizes[index]) ||
                    sizes[index] > WorkerControlProtocol.MaximumMessageBytes))
            {
                outputs[index] = EncodingRejected(outputs[index]);
            }
        }

        while (true)
        {
            var candidate = Message(correlationId, response, fault, outputs);
            if (WorkerControlChannel.TryEncode(candidate))
            {
                return candidate;
            }

            var largest = -1;
            for (var index = 0; index < outputs.Length; index++)
            {
                if (outputs[index].Retrieved && (largest < 0 || sizes[index] > sizes[largest]))
                {
                    largest = index;
                }
            }

            if (largest < 0)
            {
                break;
            }

            outputs[largest] = EncodingRejected(outputs[largest]);
        }

        var withheld = Message(correlationId, response, fault, []);
        return WorkerControlChannel.TryEncode(withheld)
            ? withheld
            : throw new WorkerMessageRejectedException("The SDK call fault could not be encoded.");
    }

    private static WorkerUnavailableOutput EncodingRejected(WorkerMpOutputValue output) =>
        new(output.Name, output.Kind, OutputEncodingRejected, WorkerUnavailableOutputReason.EncodingRejected);

    private static WorkerControlMessage Message(
        Guid correlationId,
        WorkerExecutionResponse response,
        WorkerSdkCallFaulted fault,
        IReadOnlyList<WorkerMpOutputValue> outputs) =>
        WorkerControlMessage.ExecutionResult(correlationId, new WorkerExecutionResponse(
            WorkerExecutionResponseStatus.Completed,
            new WorkerSdkCallFaulted(fault.Phase, fault.DurationMilliseconds, outputs, fault.DiagnosticCode!),
            response.Connection,
            response.DiagnosticCode));
}
