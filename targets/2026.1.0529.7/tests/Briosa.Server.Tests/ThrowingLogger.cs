using Microsoft.Extensions.Logging;

namespace Briosa.Server.Tests;

// A logger that fails on the events a test selects, and optionally on IsEnabled, so
// tests can prove that a diagnostics record never changes the operation it describes.
internal sealed class ThrowingLogger<T>(Func<EventId, bool> throwsFor, bool throwOnIsEnabled = false) : ILogger<T>
{
    private int _failures;

    public const string FailureMessage = "injected-logger-failure";

    public int Failures => Volatile.Read(ref _failures);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
    {
        if (!throwOnIsEnabled) return true;
        Interlocked.Increment(ref _failures);
        throw new InvalidOperationException(FailureMessage);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!throwsFor(eventId)) return;
        Interlocked.Increment(ref _failures);
        throw new InvalidOperationException(FailureMessage);
    }
}
