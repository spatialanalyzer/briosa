using Briosa.Server.Operations.WaveB;

namespace Briosa.Server.Operations.WaveA;

internal static class MpOperationCatalog
{
    public static IReadOnlyList<MpOperationContract> Operations { get; } =
    [
        .. WaveAOperationCatalog.Operations,
        .. WaveBOperationCatalog.Operations
    ];

    private static readonly Dictionary<string, MpOperationContract> ById =
        Operations.ToDictionary(
            operation => operation.Descriptor.OperationId,
            StringComparer.Ordinal);

    public static MpOperationContract Get(string operationId) =>
        ById.TryGetValue(operationId, out var operation)
            ? operation
            : throw new InvalidOperationException($"Unknown MP operation '{operationId}'.");
}
