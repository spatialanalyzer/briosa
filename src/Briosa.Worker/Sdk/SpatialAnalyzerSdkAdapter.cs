using System.Diagnostics;
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
        ObjectDisposedException.ThrowIf(_sdk is null, this);

        var started = Stopwatch.GetTimestamp();
        _sdk.SetStep(command.StepName);
        foreach (var argument in command.Arguments)
        {
            if (!SetArgument(_sdk, argument))
            {
                return new SdkExecutionResult(
                    ExecuteStepReturned: false,
                    new SdkMpResult(false, -1, "sdk-argument-rejected"),
                    Stopwatch.GetElapsedTime(started));
            }
        }

        var executeStepReturned = _sdk.ExecuteStep();
        var resultCode = 0;
        var mpSucceeded = _sdk.GetMPStepResult(ref resultCode);
        return new SdkExecutionResult(
            executeStepReturned,
            new SdkMpResult(
                mpSucceeded,
                resultCode,
                mpSucceeded ? null : "mp-command-failed"),
            Stopwatch.GetElapsedTime(started));
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

    private static bool SetArgument(ComSdk sdk, SdkArgument argument) =>
        argument.Kind switch
        {
            SdkArgumentKind.Logical when argument.BooleanValue is { } value =>
                sdk.SetBoolArg(argument.Name, value),
            SdkArgumentKind.WholeNumber when argument.IntegerValue is { } value =>
                sdk.SetIntegerArg(argument.Name, value),
            SdkArgumentKind.FloatingPoint when argument.DoubleValue is { } value =>
                sdk.SetDoubleArg(argument.Name, value),
            SdkArgumentKind.Text when argument.StringValue is { } value =>
                sdk.SetStringArg(argument.Name, value),
            _ => false
        };
}
