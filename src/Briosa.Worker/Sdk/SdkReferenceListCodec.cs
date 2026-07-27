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

    public static string Format(SdkCollectionInstrumentIdValue value) =>
        Join(value.CollectionName, value.InstrumentId.ToString(CultureInfo.InvariantCulture));

    public static string Format(SdkCollectionGroupNameValue value) =>
        Join(value.CollectionName, value.GroupName);

    public static string Format(SdkCollectionObjectNameValue value) =>
        Join(value.CollectionName, value.ObjectName, value.ObjectType);

    public static string Format(SdkCollectionVectorGroupNameValue value) =>
        Join(value.CollectionName, value.VectorGroupName);

    public static string Format(SdkPointNameValue value) =>
        Join(value.CollectionName, value.GroupName, value.TargetName);

    public static string Format(SdkVectorNameValue value) =>
        Join(value.CollectionName, value.GroupName, value.VectorName);

    public static bool TryParseInstrumentIds(
        object value,
        out SdkCollectionInstrumentIdListValue? result) =>
        TryParseList<SdkCollectionInstrumentIdValue, SdkCollectionInstrumentIdListValue>(value, TryParseInstrumentId, values =>
            new SdkCollectionInstrumentIdListValue(values), out result);

    public static bool TryParseGroupNames(
        object value,
        out SdkCollectionGroupNameListValue? result) =>
        TryParseList<SdkCollectionGroupNameValue, SdkCollectionGroupNameListValue>(value, TryParseGroupName, values =>
            new SdkCollectionGroupNameListValue(values), out result);

    public static bool TryParseObjectNames(
        object value,
        out SdkCollectionObjectNameListValue? result) =>
        TryParseList<SdkCollectionObjectNameValue, SdkCollectionObjectNameListValue>(value, TryParseObjectNameReference, values =>
            new SdkCollectionObjectNameListValue(values), out result);

    public static bool TryParseObjectNameResult(
        string collectionName,
        string value,
        out SdkCollectionObjectNameValue? result)
    {
        var parts = value.Split(',', StringSplitOptions.None);
        if (parts.Length >= 2)
        {
            result = new SdkCollectionObjectNameValue(collectionName, parts[0], parts[1]);
            return true;
        }

        result = null;
        return false;
    }

    public static bool TryParseVectorGroupNames(
        object value,
        out SdkCollectionVectorGroupNameListValue? result) =>
        TryParseList<SdkCollectionVectorGroupNameValue, SdkCollectionVectorGroupNameListValue>(value, TryParseVectorGroupName, values =>
            new SdkCollectionVectorGroupNameListValue(values), out result);

    public static bool TryParsePointNames(
        object value,
        out SdkPointNameListValue? result) =>
        TryParseList<SdkPointNameValue, SdkPointNameListValue>(value, TryParsePointName, values =>
            new SdkPointNameListValue(values), out result);

    public static bool TryParseStrings(object value, out SdkStringListValue? result)
    {
        if (!TryGetStrings(value, out var strings))
        {
            result = null;
            return false;
        }

        result = new SdkStringListValue(strings);
        return true;
    }

    public static bool TryParseVectorNames(
        object value,
        out SdkVectorNameListValue? result) =>
        TryParseList<SdkVectorNameValue, SdkVectorNameListValue>(value, TryParseVectorName, values =>
            new SdkVectorNameListValue(values), out result);

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
        out SdkCollectionInstrumentIdValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length is >= 2 and <= 3 &&
            (parts.Length == 2 || string.Equals(parts[2], "Instrument", StringComparison.Ordinal)) &&
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            result = new SdkCollectionInstrumentIdValue(parts[0], id);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseGroupName(string value, out SdkCollectionGroupNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            result = new SdkCollectionGroupNameValue(parts[0], parts[1]);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseObjectNameReference(
        string value,
        out SdkCollectionObjectNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 3)
        {
            result = new SdkCollectionObjectNameValue(
                parts[0],
                parts[1],
                parts[2].Split(',', 2, StringSplitOptions.None)[0]);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseVectorGroupName(
        string value,
        out SdkCollectionVectorGroupNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            result = new SdkCollectionVectorGroupNameValue(parts[0], parts[1]);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParsePointName(string value, out SdkPointNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 3)
        {
            result = new SdkPointNameValue(parts[0], parts[1], parts[2]);
            return true;
        }

        result = null!;
        return false;
    }

    private static bool TryParseVectorName(string value, out SdkVectorNameValue result)
    {
        var parts = value.Split(Separator, StringSplitOptions.None);
        if (parts.Length == 3)
        {
            result = new SdkVectorNameValue(parts[0], parts[1], parts[2]);
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