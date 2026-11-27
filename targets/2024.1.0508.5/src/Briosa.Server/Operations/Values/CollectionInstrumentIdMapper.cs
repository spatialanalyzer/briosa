using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CollectionInstrumentIdMapper
{
    public static WorkerCollectionInstrumentIdValue Required(Api.CollectionInstrumentId? value, string fieldName)
    {
        if (value is null)
        {
            throw RequestFieldViolation.Required(fieldName);
        }

        return ToWorker(value, fieldName);
    }

    public static WorkerCollectionInstrumentIdListValue RequiredList(
        IReadOnlyList<Api.CollectionInstrumentId> values,
        string fieldName)
    {
        if (values.Count == 0)
        {
            throw RequestFieldViolation.NotEmpty(fieldName);
        }

        return new(values.Select(value => ToWorker(value, fieldName)).ToImmutableArray());
    }

    // Both members are required; an element of a list reports the list's path.
    private static WorkerCollectionInstrumentIdValue ToWorker(Api.CollectionInstrumentId value, string fieldName)
    {
        if (!value.HasCollectionName)
        {
            throw RequestFieldViolation.Required(RequestFieldViolation.Path(fieldName, "collection_name"));
        }

        if (string.IsNullOrWhiteSpace(value.CollectionName))
        {
            throw RequestFieldViolation.NotBlank(RequestFieldViolation.Path(fieldName, "collection_name"));
        }

        if (!value.HasInstrumentId)
        {
            throw RequestFieldViolation.Required(RequestFieldViolation.Path(fieldName, "instrument_id"));
        }

        return new(value.CollectionName, value.InstrumentId);
    }
}
