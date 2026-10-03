namespace Briosa.Worker.Control;

/// <summary>
/// Value-free diagnostic codes for a per-call SDK fault (an exception from one SDK
/// call while the worker STA stays healthy). Each code names the proven MP phase;
/// exception text is never carried.
/// </summary>
public static class WorkerSdkFaultDiagnosticCodes
{
    /// <summary>SetStep or an input setter faulted; ExecuteStep was never called.</summary>
    public const string BeforeExecute = "sdk-call-faulted-before-execute";

    /// <summary>ExecuteStep faulted; the MP may have started.</summary>
    public const string ExecuteStep = "sdk-execute-step-faulted";

    /// <summary>GetMPStepResult faulted after ExecuteStep returned true.</summary>
    public const string MpResultRetrieval = "sdk-mp-result-retrieval-faulted";

    /// <summary>An output getter faulted after retrieved MP code 2.</summary>
    public const string OutputGetter = "sdk-output-getter-faulted";
}
