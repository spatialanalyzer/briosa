using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class FileReferenceMapper
{
    public static WorkerFileReferenceValue Required(Api.FileReference? value, string fieldName)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.Path))
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        return new(value.Path, value.EmbeddedFile);
    }
}
