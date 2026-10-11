using System.ComponentModel;
using System.Diagnostics;

namespace Briosa.Server.Services;

internal interface ISpatialAnalyzerProcessPlatform
{
    IReadOnlyList<SpatialAnalyzerProcessObservation> ObserveEligibleProcesses(
        string executablePath);

    // Checks one previously observed process directly, without reading its modules.
    SpatialAnalyzerProcessPresence ObserveProcess(
        string executablePath,
        SpatialAnalyzerProcessIdentity identity);

    ISpatialAnalyzerOwnedProcess Start(ProcessStartInfo startInfo);
}
