namespace Briosa.Worker.Control;

/// <summary>
/// The raw MP result codes after which the MP command did its work: SUCCESS (2)
/// and PARTIAL SUCCESS (4). Only these permit output retrieval. The host decides
/// whether a code 4 is reported as a partial success or, for an operation whose
/// target documentation says code 4 leaves the work undone, as an MP failure.
/// </summary>
public static class WorkerMpResultCodes
{
    public const int Success = 2;
    public const int PartialSuccess = 4;

    public static bool PermitsOutputRetrieval(int resultCode) =>
        resultCode is Success or PartialSuccess;
}
