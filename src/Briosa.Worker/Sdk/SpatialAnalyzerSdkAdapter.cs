using System.Runtime.InteropServices;
using ComSdk = Briosa.SpatialAnalyzer.Interop.ISpatialAnalyzerSDK;
using ComSdkClass = Briosa.SpatialAnalyzer.Interop.SpatialAnalyzerSDKClass;

namespace Briosa.Worker.Sdk;

/// <summary>
/// Adapts the generated SpatialAnalyzer COM interface to the worker-owned SDK boundary.
/// </summary>
internal sealed class SpatialAnalyzerSdkAdapter : ISpatialAnalyzerSdk
{
    private ComSdk? _sdk;

    private SpatialAnalyzerSdkAdapter(ComSdk sdk)
    {
        _sdk = sdk;
    }

    public static ISpatialAnalyzerSdk Create() =>
        new SpatialAnalyzerSdkAdapter(new ComSdkClass());

    public SdkConnectionResult Connect(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ObjectDisposedException.ThrowIf(_sdk is null, this);

        var statusCode = 0;
        var connected = _sdk.ConnectEx(host, ref statusCode);
        return connected
            ? new SdkConnectionResult(SdkConnectionStatus.Connected, statusCode, null)
            : new SdkConnectionResult(
                SdkConnectionStatus.Unavailable,
                statusCode,
                "connect-ex-unavailable");
    }

    public SdkExecutionResult Execute(SdkCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        throw new NotSupportedException(
            "SpatialAnalyzer MP execution is not implemented by the connection lifecycle.");
    }

    public void Dispose()
    {
        var sdk = _sdk;
        _sdk = null;
        if (sdk is not null && Marshal.IsComObject(sdk))
        {
            _ = Marshal.FinalReleaseComObject(sdk);
        }
    }
}
