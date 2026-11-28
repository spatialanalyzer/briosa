using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "xUnit owns the test methods' synchronization context; the CLI code under test configures its own awaits.",
    Scope = "module")]
