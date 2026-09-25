using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Worker.Sdk;

internal sealed record SpatialAnalyzerSdkActivation(
    ISpatialAnalyzerSdkCalls Sdk,
    ISpatialAnalyzerSdkProcessMonitor ProcessMonitor);
