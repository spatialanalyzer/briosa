namespace Briosa.Server.Services;

internal sealed class WindowsSpatialAnalyzerWindowSource : ISpatialAnalyzerWindowSource
{
    public IReadOnlyList<SpatialAnalyzerTopLevelWindow> GetTopLevelWindows(IReadOnlySet<int> processIds) => [];
}
