using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "Portable test-support code does not run under a synchronization context.",
    Scope = "namespaceanddescendants",
    Target = "~N:Briosa.Worker.Testing")]
