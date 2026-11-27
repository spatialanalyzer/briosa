using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class FileReferenceMapper
{
    public static WorkerFileReferenceValue Required(Api.FileReference? value, string fieldName)
    {
        if (value is null)
            throw RequestFieldViolation.Required(fieldName);
        if (string.IsNullOrWhiteSpace(value.Path))
            throw RequestFieldViolation.NotBlank(RequestFieldViolation.Path(fieldName, "path"));
        return new(value.Path, value.EmbeddedFile);
    }
}
