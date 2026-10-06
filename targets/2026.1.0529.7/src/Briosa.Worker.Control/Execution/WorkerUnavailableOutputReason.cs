namespace Briosa.Worker.Control;

/// <summary>
/// Why an output value is unavailable. It is value-free and crosses the private
/// channel, so the host can report each output precisely after an SDK call fault.
/// </summary>
public enum WorkerUnavailableOutputReason
{
    /// <summary>The getter did not deliver a usable value; no more specific reason.</summary>
    None = 0,

    /// <summary>The output getter threw after MP success.</summary>
    SdkCallFaulted = 1,

    /// <summary>The getter delivered a value that the private channel cannot encode.</summary>
    EncodingRejected = 2
}
