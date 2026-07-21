using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "The xUnit test synchronization context owns asynchronous process fixtures.",
    Scope = "type",
    Target = "~T:Briosa.Server.Tests.WorkerProcessSupervisorTests")]
