using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Worker.Sdk;

internal sealed class AlwaysAliveSpatialAnalyzerSdkProcessMonitor
    : ISpatialAnalyzerSdkProcessMonitor
{
    public static AlwaysAliveSpatialAnalyzerSdkProcessMonitor Instance { get; } = new();

    public SdkLivenessStatus GetLiveness() => SdkLivenessStatus.Alive;

    public void Dispose()
    {
    }
}
