namespace Briosa.Server.Services;

internal sealed class SpatialAnalyzerModalDialogProbe(
    SpatialAnalyzerApplicationOptions options,
    ISpatialAnalyzerProcessPlatform processPlatform,
    ISpatialAnalyzerWindowSource windowSource) : ISpatialAnalyzerModalDialogProbe
{
    private readonly SpatialAnalyzerApplicationOptions _options = options;
    private readonly ISpatialAnalyzerProcessPlatform _processPlatform = processPlatform;
    private readonly ISpatialAnalyzerWindowSource _windowSource = windowSource;

    public SpatialAnalyzerModalDialogState Observe()
    {
        _ = (_options, _processPlatform, _windowSource);
        return SpatialAnalyzerModalDialogState.NotObserved;
    }
}
