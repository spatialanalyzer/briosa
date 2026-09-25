using System.ComponentModel;
using System.Diagnostics;

namespace Briosa.Server.Services;

internal sealed record SpatialAnalyzerProcessIdentity(int ProcessId, long StartTimeUtcTicks);
