using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "The xUnit test synchronization context owns asynchronous test continuations.",
    Scope = "namespaceanddescendants",
    Target = "~N:Briosa.LicensedProbes.Tests")]
[assembly: SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "The in-memory fake transport owns no resources; tests inspect it after the session.",
    Scope = "namespaceanddescendants",
    Target = "~N:Briosa.LicensedProbes.Tests")]
