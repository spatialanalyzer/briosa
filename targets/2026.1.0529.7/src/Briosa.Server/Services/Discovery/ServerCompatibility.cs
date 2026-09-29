using System.Text.Json;

namespace Briosa.Server.Services;

internal static class ServerCompatibility
{
    private static readonly global::Briosa.CompatibilityContract Contract = Read();

    internal static global::Briosa.CompatibilityContract Create() => Contract.Clone();

    private static global::Briosa.CompatibilityContract Read()
    {
        using var stream = typeof(ServerCompatibility).Assembly
            .GetManifestResourceStream("Briosa.Compatibility") ??
            throw new InvalidOperationException("Missing behavioral contract declaration.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var major = root.GetProperty("major").GetUInt32();
        if (root.GetProperty("schemaVersion").GetInt32() != 1 || major == 0)
        {
            throw new InvalidOperationException("Invalid behavioral contract declaration.");
        }

        return new global::Briosa.CompatibilityContract
        {
            Major = major,
            Revision = root.GetProperty("revision").GetUInt32()
        };
    }
}
