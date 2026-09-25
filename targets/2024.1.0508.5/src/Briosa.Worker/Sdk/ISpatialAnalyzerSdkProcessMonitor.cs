using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Worker.Sdk;

internal interface ISpatialAnalyzerSdkProcessMonitor : IDisposable
{
    SdkLivenessStatus GetLiveness();
    string? GetVersion() => null;
}
