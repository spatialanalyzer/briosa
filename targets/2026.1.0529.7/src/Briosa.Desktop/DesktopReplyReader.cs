using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Briosa.Desktop;

/// <summary>
/// Reads the protobuf JSON state in a desktop reply (#400). A property that a later
/// server adds is ignored. A value that this Control Center does not recognize in a
/// field it knows is never read as if the field were omitted: the protobuf parser that
/// ignores unknown properties also drops an unrecognized enum value, so each payload is
/// also checked against this reader's own descriptors, and a reply with such a value
/// is reported as not recognized.
/// </summary>
internal static class DesktopReplyReader
{
    private static readonly JsonParser Parser =
        new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

    // Configuration that Control Center only reports: an unrecognized mode stays unknown.
    private static readonly string ReportedOnly = GetServerInfoResponse.Descriptor
        .FindFieldByNumber(GetServerInfoResponse.ConfiguredSpatialAnalyzerInteractionModeFieldNumber).FullName;

    /// <summary>Parses the payload; <paramref name="recognized"/> is false when a known field has a value this reader does not know.</summary>
    public static T Read<T>(string json, out bool recognized) where T : IMessage<T>, new()
    {
        var message = Parser.Parse<T>(json);
        using var document = JsonDocument.Parse(json);
        recognized = IsRecognized(document.RootElement, message.Descriptor);
        return message;
    }

    private static bool IsRecognized(JsonElement element, MessageDescriptor descriptor)
    {
        // Well-known types have their own JSON forms and carry no Briosa enum.
        if (element.ValueKind != JsonValueKind.Object ||
            descriptor.File.Name.StartsWith("google/protobuf/", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            var field = descriptor.FindFieldByName(property.Name) ??
                descriptor.Fields.InDeclarationOrder().FirstOrDefault(
                    candidate => candidate.JsonName == property.Name);
            // A property that a later server adds is ignored.
            if (field is not null && !IsRecognized(property.Value, field)) return false;
        }

        return true;
    }

    private static bool IsRecognized(JsonElement value, FieldDescriptor field)
    {
        if (value.ValueKind == JsonValueKind.Null) return true;
        if (field.IsMap)
        {
            var mapValue = field.MessageType.FindFieldByNumber(2);
            return value.ValueKind != JsonValueKind.Object ||
                value.EnumerateObject().All(entry => IsRecognizedValue(entry.Value, mapValue));
        }

        if (field.IsRepeated && value.ValueKind == JsonValueKind.Array)
            return value.EnumerateArray().All(item => IsRecognizedValue(item, field));
        return IsRecognizedValue(value, field);
    }

    private static bool IsRecognizedValue(JsonElement value, FieldDescriptor field) =>
        field.FieldType switch
        {
            FieldType.Enum => field.FullName == ReportedOnly || IsKnownEnumValue(value, field.EnumType),
            FieldType.Message => IsRecognized(value, field.MessageType),
            _ => true
        };

    // By name or by number, as protobuf JSON allows both.
    private static bool IsKnownEnumValue(JsonElement value, EnumDescriptor type) =>
        value.ValueKind switch
        {
            JsonValueKind.String => type.FindValueByName(value.GetString()!) is not null,
            JsonValueKind.Number => value.TryGetInt32(out var number) && type.FindValueByNumber(number) is not null,
            _ => false
        };
}
