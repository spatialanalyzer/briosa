using System.Diagnostics.CodeAnalysis;

namespace Briosa.Server.Workers;

[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "This internal exception must always retain both generation values.")]
internal sealed class WorkerGenerationConflictException(
    int expectedGeneration,
    int actualGeneration) : InvalidOperationException(
        $"Expected SDK generation '{expectedGeneration}', but the current generation is '{actualGeneration}'.")
{
    public int ExpectedGeneration { get; } = expectedGeneration;

    public int ActualGeneration { get; } = actualGeneration;
}
