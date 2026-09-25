using System.ComponentModel;
using System.Diagnostics;

namespace Briosa.Server.Services;

internal interface ISpatialAnalyzerProcessPlatform
{
    IReadOnlyList<SpatialAnalyzerProcessObservation> ObserveEligibleProcesses(
        string executablePath);

    ISpatialAnalyzerOwnedProcess Start(ProcessStartInfo startInfo);
}
