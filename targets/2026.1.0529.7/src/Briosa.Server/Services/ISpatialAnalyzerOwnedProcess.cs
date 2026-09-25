using System.ComponentModel;
using System.Diagnostics;

namespace Briosa.Server.Services;

internal interface ISpatialAnalyzerOwnedProcess : IDisposable
{
    SpatialAnalyzerProcessIdentity Identity { get; }

    bool HasExited { get; }

    bool IsApplicationWindowReady { get; }

    bool RequestClose();

    Task WaitForExitAsync(CancellationToken cancellationToken);

    void Refresh();
}
