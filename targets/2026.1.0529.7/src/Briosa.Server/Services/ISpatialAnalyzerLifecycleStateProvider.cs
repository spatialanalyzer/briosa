using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Server.Services;

internal interface ISpatialAnalyzerLifecycleStateProvider
{
    Task<global::Briosa.SpatialAnalyzerLifecycleState> GetCurrentAsync(
        CancellationToken cancellationToken);
}
