using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GdtCollectionNameMapper
{
    public static WorkerTextValue Required(Api.CollectionName? collection)
    {
        if (collection is null || string.IsNullOrWhiteSpace(collection.Name))
            throw new ArgumentException("Request field 'collection' is required.", nameof(collection));
        return new(collection.Name);
    }
}
