namespace Briosa.Worker.Sdk;

/// <summary>
/// Defines the synchronous SDK operations owned by the worker STA.
/// </summary>
/// <remarks>
/// Implementations must complete an entire command sequence before returning from
/// <see cref="Execute"/>. The interface intentionally contains no COM or public
/// protocol types.
/// </remarks>
internal interface ISpatialAnalyzerSdk : IDisposable
{
    SdkConnectionResult Connect(string host);

    SdkExecutionResult Execute(SdkCommand command);
}
