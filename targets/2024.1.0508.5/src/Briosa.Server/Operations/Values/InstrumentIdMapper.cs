using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class InstrumentIdMapper
{
    public static WorkerCollectionInstrumentIdValue Required(Api.CollectionInstrumentId? value, string fieldName)
    {
        if (value is null)
            throw RequestFieldViolation.Required(fieldName);
        if (string.IsNullOrWhiteSpace(value.CollectionName))
            throw RequestFieldViolation.NotBlank(RequestFieldViolation.Path(fieldName, "collection_name"));

        return new(value.CollectionName, value.InstrumentId);
    }

    public static WorkerCollectionInstrumentIdListValue RequiredList(
        IReadOnlyList<Api.CollectionInstrumentId> values,
        string fieldName)
    {
        if (values.Count == 0)
            throw RequestFieldViolation.NotEmpty(fieldName);

        return new(values.Select(value => new WorkerCollectionInstrumentIdValue(
            value.CollectionName, value.InstrumentId)).ToArray());
    }

    public static Api.CollectionInstrumentId ToProtocol(WorkerCollectionInstrumentIdValue value) => new()
    {
        CollectionName = value.CollectionName,
        InstrumentId = value.InstrumentId
    };

    public static IEnumerable<Api.CollectionInstrumentId> ToProtocolList(WorkerCollectionInstrumentIdListValue value) =>
        value.Values.Select(ToProtocol);
}