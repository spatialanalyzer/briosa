using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GdtCollectionNameMapper
{
    public static WorkerTextValue Required(Api.CollectionName? collection, string fieldName)
    {
        if (collection is null)
            throw RequestFieldViolation.Required(fieldName);
        if (string.IsNullOrWhiteSpace(collection.Name))
            throw RequestFieldViolation.NotBlank(RequestFieldViolation.Path(fieldName, "name"));
        return new(collection.Name);
    }
}
