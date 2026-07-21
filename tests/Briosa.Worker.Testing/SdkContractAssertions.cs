using Briosa.Worker.Sdk;

namespace Briosa.Worker.Testing;

/// <summary>
/// Contract checks reusable by the scripted adapter and a future licensed adapter fixture.
/// </summary>
internal static class SdkContractAssertions
{
    public static async Task ConnectedAdapterExecutesOnSta(
        Func<ISpatialAnalyzerSdk> sdkFactory,
        Func<ApartmentState?> apartmentState)
    {
        await using var executor = new SerializedSdkExecutor(sdkFactory);

        var connection = await executor.ConnectAsync("localhost");
        var execution = await executor.ExecuteAsync(new SdkCommand("contract.success"));

        AssertEquivalent(SdkConnectionStatus.Connected, connection.Status, "connection status");
        AssertEquivalent(true, execution.ExecuteStepReturned, "ExecuteStep result");
        AssertEquivalent(true, execution.MpResult.Succeeded, "MP result");
        AssertEquivalent<ApartmentState?>(ApartmentState.STA, apartmentState(), "adapter apartment");
    }

    public static async Task MpFailureIsIndependentFromExecuteStep(
        Func<ISpatialAnalyzerSdk> sdkFactory)
    {
        await using var executor = new SerializedSdkExecutor(sdkFactory);

        var execution = await executor.ExecuteAsync(new SdkCommand("contract.mp-failure"));

        AssertEquivalent(true, execution.ExecuteStepReturned, "ExecuteStep result");
        AssertEquivalent(false, execution.MpResult.Succeeded, "MP result");
    }

    private static void AssertEquivalent<T>(T expected, T actual, string field)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"SDK contract violation for {field}: expected '{expected}', received '{actual}'.");
        }
    }
}
