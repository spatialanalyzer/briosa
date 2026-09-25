using System.Collections.Immutable;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Values;

internal static class PointNameMapper
{
    public static WorkerPointNameValue Required(Api.PointName? value, string fieldName)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.TargetName))
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(value));
        }

        return new(value.CollectionName, value.GroupName, value.TargetName);
    }

    public static WorkerPointNameListValue RequiredList(IReadOnlyList<Api.PointName> values, string fieldName)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException($"Request field '{fieldName}' is required.", nameof(values));
        }

        return new(values.Select(value => new WorkerPointNameValue(value.CollectionName, value.GroupName, value.TargetName)).ToImmutableArray());
    }

    public static Api.PointName ToProtocol(WorkerPointNameValue value) => new()
    {
        CollectionName = value.CollectionName,
        GroupName = value.GroupName,
        TargetName = value.TargetName
    };
}
