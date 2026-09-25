using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CollectionObjectNameMapper
{
    public static WorkerCollectionObjectNameValue Required(Api.CollectionObjectName? value, string fieldName)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ObjectName))
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        }

        if (!Enum.IsDefined(value.ObjectType))
        {
            throw new ArgumentException("Object type is not supported by this SA target.", nameof(value));
        }

        // The reviewed collection-object binding treats an omitted type as Any.
        var type = value.ObjectType == Api.ObjectType.Unspecified
            ? WorkerObjectTypeValue.Any : (WorkerObjectTypeValue)value.ObjectType;
        return new(value.CollectionName, value.ObjectName, type);
    }
}
