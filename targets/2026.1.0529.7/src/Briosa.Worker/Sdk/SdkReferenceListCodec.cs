using Briosa.Worker.Control;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Briosa.Worker.Sdk;

internal static class SdkReferenceListCodec
{
    private const string Separator = "::";

    public static object ToComValue(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new VariantWrapper(values.Select(value => (object)value).ToArray());
    }

    public static string Format(WorkerCollectionInstrumentIdValue value) =>
        Join(value.CollectionName, value.InstrumentId.ToString(CultureInfo.InvariantCulture));

    public static string Format(WorkerCollectionGroupNameValue value) =>
        Join(value.CollectionName, value.GroupName);

    public static string Format(WorkerCollectionObjectNameValue value) =>
        Join(value.CollectionName, value.ObjectName, SdkSpecializedValueCodec.ToSdkString(value.ObjectType));

    public static string Format(WorkerCollectionItemNameValue value) =>
        Join(value.CollectionName, value.ItemName, SdkSpecializedValueCodec.ToSdkString(value.ItemType));

    public static string Format(WorkerCollectionVectorGroupNameValue value) =>
        Join(value.CollectionName, value.VectorGroupName);

    public static string Format(WorkerPointNameValue value) =>
        Join(value.CollectionName, value.GroupName, value.TargetName);

    public static string Format(WorkerVectorNameValue value) =>
        Join(value.CollectionName, value.GroupName, value.VectorName);

    public static bool TryParseInstrumentIds(
        object value,
        out WorkerCollectionInstrumentIdListValue? result) =>
        TryParseList<WorkerCollectionInstrumentIdValue, WorkerCollectionInstrumentIdListValue>(value, TryParseInstrumentId, values =>
            new WorkerCollectionInstrumentIdListValue(values), out result);

    public static bool TryParseGroupNames(
        object value,
        out WorkerCollectionGroupNameListValue? result) =>
        TryParseList<WorkerCollectionGroupNameValue, WorkerCollectionGroupNameListValue>(value, TryParseGroupName, values =>
            new WorkerCollectionGroupNameListValue(values), out result);

    public static bool TryParseObjectNames(
        object value,
        out WorkerCollectionObjectNameListValue? result) =>
        TryParseList<WorkerCollectionObjectNameValue, WorkerCollectionObjectNameListValue>(
            value,
            TryParseObjectNameReference,
            values => new WorkerCollectionObjectNameListValue(values),
            out result);

    public static bool TryParseItemNames(
        object value,
        out WorkerCollectionItemNameListValue? result) =>
        TryParseList<WorkerCollectionItemNameValue, WorkerCollectionItemNameListValue>(
            value,
            TryParseItemNameReference,
            values => new WorkerCollectionItemNameListValue(values),
            out result);

    public static bool TryParseObjectNameResult(
        string collectionName,
        string value,
        out WorkerCollectionObjectNameValue? result)
    {
        var parts = value.Split(',', StringSplitOptions.None);
        if (parts.Length >= 2 &&
            SdkSpecializedValueCodec.TryParseObjectType(parts[1], out var objectType))
        {
            result = new WorkerCollectionObjectNameValue(collectionName, parts[0], objectType);
            return true;
        }

        result = null;
        return false;
    }

    public static bool TryParseItemNameResult(
        string collectionName,
        string value,
        out WorkerCollectionItemNameValue? result)
    {
        var parts = value.Split(',', StringSplitOptions.None);
        if (parts.Length >= 2 &&
            SdkSpecializedValueCodec.TryParseItemType(parts[1], out var itemType))
        {
            result = new WorkerCollectionItemNameValue(collectionName, parts[0], itemType);
            return true;
        }

        result = null;
        return false;
    }

    public static bool TryParseVectorGroupNames(
        object value,
        out WorkerCollectionVectorGroupNameListValue? result) =>
        TryParseList<WorkerCollectionVectorGroupNameValue, WorkerCollectionVectorGroupNameListValue>(value, TryParseVectorGroupName, values =>
            new WorkerCollectionVectorGroupNameListValue(values), out result);

    public static bool TryParsePointNames(
        object value,
        out WorkerPointNameListValue? result) =>
        TryParseList<WorkerPointNameValue, WorkerPointNameListValue>(value, TryParsePointName, values =>
            new WorkerPointNameListValue(values), out result);

    public static bool TryParseStrings(object value, out WorkerStringListValue? result)
    {
        if (!TryGetStrings(value, out var strings))
        {
            result = null;
            return false;
        }

        result = new WorkerStringListValue(strings);
        return true;
    }

    public static bool TryParseVectorNames(
        object value,
        out WorkerVectorNameListValue? result) =>
        TryParseList<WorkerVectorNameValue, WorkerVectorNameListValue>(value, TryParseVectorName, values =>
            new WorkerVectorNameListValue(values), out result);

    private static string Join(params string[] components)
    {
        if (components.Any(component => component is null ||
            component.Contains(Separator, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "SpatialAnalyzer reference components cannot contain the '::' separator.");
        }

        return string.Join(Separator, components);
    }

    private static bool TryParseInstrumentId(
        string value,
        out WorkerCollectionInstrumentIdValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length is >= 2 and <= 3 &&
            (parts.Length == 2 || string.Equals(parts[2], "Instrument", StringComparison.Ordinal)) &&
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            result = new WorkerCollectionInstrumentIdValue(parts[0], id);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseGroupName(string value, out WorkerCollectionGroupNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            result = new WorkerCollectionGroupNameValue(parts[0], parts[1]);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseObjectNameReference(
        string value,
        out WorkerCollectionObjectNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 3 &&
            SdkSpecializedValueCodec.TryParseObjectType(
                parts[2].Split(',', 2, StringSplitOptions.None)[0],
                out var objectType))
        {
            result = new WorkerCollectionObjectNameValue(parts[0], parts[1], objectType);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseItemNameReference(
        string value,
        out WorkerCollectionItemNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 3 &&
            SdkSpecializedValueCodec.TryParseItemType(
                parts[2].Split(',', 2, StringSplitOptions.None)[0],
                out var itemType))
        {
            result = new WorkerCollectionItemNameValue(parts[0], parts[1], itemType);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseVectorGroupName(
        string value,
        out WorkerCollectionVectorGroupNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            result = new WorkerCollectionVectorGroupNameValue(parts[0], parts[1]);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParsePointName(string value, out WorkerPointNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 3)
        {
            result = new WorkerPointNameValue(parts[0], parts[1], parts[2]);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseVectorName(string value, out WorkerVectorNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 3)
        {
            result = new WorkerVectorNameValue(parts[0], parts[1], parts[2]);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseList<TItem, TResult>(
        object value,
        TryParse<TItem> parser,
        Func<IReadOnlyList<TItem>, TResult> create,
        out TResult? result)
        where TResult : class
    {
        if (!TryGetStrings(value, out var strings))
        {
            result = null;
            return false;
        }

        var parsed = new List<TItem>(strings.Count);
        foreach (var item in strings)
        {
            if (!parser(item, out var parsedItem))
            {
                result = null;
                return false;
            }

            parsed.Add(parsedItem);
        }

        result = create(parsed);
        return true;
    }

    private static bool TryGetStrings(object value, out IReadOnlyList<string> strings)
    {
        if (value is VariantWrapper wrapper)
        {
            if (wrapper.WrappedObject is not { } wrapped)
            {
                strings = [];
                return false;
            }

            value = wrapped;
        }

        if (value is string || value is not IEnumerable sequence)
        {
            strings = [];
            return false;
        }

        var values = new List<string>();
        foreach (var item in sequence)
        {
            if (item is not string text)
            {
                strings = [];
                return false;
            }

            values.Add(text);
        }

        strings = values;
        return true;
    }

    private delegate bool TryParse<T>(string value, out T result);
}