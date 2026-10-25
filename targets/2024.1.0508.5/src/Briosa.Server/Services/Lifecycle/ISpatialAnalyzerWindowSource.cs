namespace Briosa.Server.Services;

internal interface ISpatialAnalyzerWindowSource
{
    // Top-level windows of the given processes, with their owner's process and
    // enabled state. Reads no window text.
    IReadOnlyList<SpatialAnalyzerTopLevelWindow> GetTopLevelWindows(IReadOnlySet<int> processIds);
}

internal sealed record SpatialAnalyzerTopLevelWindow(
    int ProcessId,
    bool Visible,
    string ClassName,
    int? OwnerProcessId,
    bool OwnerEnabled);
