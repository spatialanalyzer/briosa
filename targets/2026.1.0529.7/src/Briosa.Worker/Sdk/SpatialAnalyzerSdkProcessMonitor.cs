using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Worker.Sdk;

internal interface ISpatialAnalyzerSdkProcessMonitor : IDisposable
{
    SdkLivenessStatus GetLiveness();
}

internal sealed record SpatialAnalyzerSdkActivation(
    ISpatialAnalyzerSdkCalls Sdk,
    ISpatialAnalyzerSdkProcessMonitor ProcessMonitor);

internal static class SpatialAnalyzerSdkProcessMonitor
{
    private const string ProcessName = "SpatialAnalyzerSDK";
    private const int ProcessDiscoveryAttempts = 100;
    private const int ProcessDiscoveryPollMilliseconds = 50;

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Process discovery failures must fail SDK activation closed without exposing machine details.")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned activation transfers ownership of the SDK process monitor to the adapter.")]
    public static SpatialAnalyzerSdkActivation Activate(
        Func<ISpatialAnalyzerSdkCalls> activateSdk)
    {
        ArgumentNullException.ThrowIfNull(activateSdk);
        var before = ObserveIdentities();
        ISpatialAnalyzerSdkCalls? sdk = null;
        try
        {
            sdk = activateSdk();
            var candidates = Array.Empty<Process>();
            for (var attempt = 0; attempt < ProcessDiscoveryAttempts; attempt++)
            {
                candidates = ObserveNewProcesses(before);
                if (candidates.Length != 0)
                {
                    break;
                }

                Thread.Sleep(ProcessDiscoveryPollMilliseconds);
            }

            if (candidates.Length != 1)
            {
                foreach (var candidate in candidates)
                {
                    candidate.Dispose();
                }

                throw new InvalidOperationException(
                    "Briosa could not identify exactly one SDK engine created by this worker generation.");
            }

            return new SpatialAnalyzerSdkActivation(
                sdk,
                new OwnedSpatialAnalyzerSdkProcessMonitor(candidates[0]));
        }
        catch
        {
            sdk?.Dispose();
            throw;
        }
    }

    private static HashSet<ProcessIdentity> ObserveIdentities()
    {
        var identities = new HashSet<ProcessIdentity>();
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            using (process)
            {
                try
                {
                    identities.Add(new ProcessIdentity(
                        process.Id,
                        process.StartTime.ToUniversalTime().Ticks));
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException or
                        Win32Exception or
                        NotSupportedException)
                {
                }
            }
        }

        return identities;
    }

    private static Process[] ObserveNewProcesses(HashSet<ProcessIdentity> before)
    {
        var candidates = new List<Process>();
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            try
            {
                var identity = new ProcessIdentity(
                    process.Id,
                    process.StartTime.ToUniversalTime().Ticks);
                if (!before.Contains(identity))
                {
                    candidates.Add(process);
                    continue;
                }
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or
                    Win32Exception or
                    NotSupportedException)
            {
            }

            process.Dispose();
        }

        return [.. candidates];
    }

    private sealed record ProcessIdentity(int ProcessId, long StartTimeUtcTicks);

    private sealed class OwnedSpatialAnalyzerSdkProcessMonitor(Process process)
        : ISpatialAnalyzerSdkProcessMonitor
    {
        private readonly Process _process = process;

        public SdkLivenessStatus GetLiveness()
        {
            try
            {
                return _process.HasExited
                    ? SdkLivenessStatus.ProcessExited
                    : SdkLivenessStatus.Alive;
            }
            catch (InvalidOperationException)
            {
                return SdkLivenessStatus.Unavailable;
            }

            catch (Win32Exception)
            {
                return SdkLivenessStatus.Unavailable;
            }
        }

        public void Dispose() => _process.Dispose();
    }
}

internal sealed class AlwaysAliveSpatialAnalyzerSdkProcessMonitor
    : ISpatialAnalyzerSdkProcessMonitor
{
    public static AlwaysAliveSpatialAnalyzerSdkProcessMonitor Instance { get; } = new();

    public SdkLivenessStatus GetLiveness() => SdkLivenessStatus.Alive;

    public void Dispose()
    {
    }
}
