using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class CollectionInstrumentIdMapper
{
    public static WorkerCollectionInstrumentIdValue Required(Api.CollectionInstrumentId? value, string fieldName)
    {
        if (value is null || !value.HasCollectionName || string.IsNullOrWhiteSpace(value.CollectionName) ||
            !value.HasInstrumentId)
        {
            throw new ArgumentException(
                $"Request field '{fieldName}' requires a collection name and instrument ID.", nameof(value));
        }

        return new(value.CollectionName, value.InstrumentId);
    }

    public static WorkerCollectionInstrumentIdListValue RequiredList(
        IReadOnlyList<Api.CollectionInstrumentId> values,
        string fieldName)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));
        }

        return new(values.Select(value =>
        {
            if (!value.HasCollectionName || string.IsNullOrWhiteSpace(value.CollectionName) || !value.HasInstrumentId)
            {
                throw new ArgumentException(
                    $"Every entry in '{fieldName}' requires a collection name and instrument ID.", nameof(values));
            }

            return new WorkerCollectionInstrumentIdValue(value.CollectionName, value.InstrumentId);
        }).ToImmutableArray());
    }
}
