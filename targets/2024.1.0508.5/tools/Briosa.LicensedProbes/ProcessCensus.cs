using System.Diagnostics;

namespace Briosa.LicensedProbes;

/// <summary>Counts processes by name. It reads no paths, arguments, or window titles.</summary>
internal interface IProcessCensus
{
    int Count(string processName);
}

internal sealed class ProcessCensus : IProcessCensus
{
    public const string SpatialAnalyzer = "Spatial Analyzer64";
    public const string SpatialAnalyzerSdk = "SpatialAnalyzerSDK";
    public const string BriosaServer = "Briosa.Server";
    public const string BriosaWorker = "Briosa.Worker";

    public static ProcessCensus Instance { get; } = new();

    public int Count(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Length;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}
