using System.Text.Json.Nodes;
using Google.Protobuf.Reflection;

namespace Briosa.Cli.Engine;

/// <summary>
/// Builds a JSON Schema (draft 2020-12) for a request message from its compiled
/// descriptor. It describes exactly the forms <see cref="StrictJsonInput"/> accepts,
/// by exact protobuf field name. It deliberately carries no <c>required</c> list:
/// protobuf presence is not requiredness, and the descriptors do not say which
/// fields the server requires.
/// </summary>
internal static class RequestSchema
{
    public const string RequirednessStatement =
        "This schema lists the exact protobuf field names and the JSON form of each value. It does not " +
        "describe which fields the server requires. Protobuf optional (explicit presence) does not mean " +
        "that a field may be omitted, and a field without it can still be required. When the server rejects " +
        "a request, the failure names the first offending field and the constraint it did not meet " +
        "(Server 0.10.2 and later).";

    private const string PresenceDescription =
        "Explicit presence: omitting this field and sending its default value are different requests.";

    public static JsonObject Build(MessageDescriptor request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definitions = new JsonObject();
        AddDefinition(request, definitions);
        return new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = request.FullName,
            ["description"] = RequirednessStatement,
            ["$ref"] = Reference(request),
            ["$defs"] = definitions
        };
    }

    /// <summary>The documented input forms, for the <c>describe</c> output.</summary>
    public static JsonObject InputRules() => new()
    {
        ["field_names"] = "Exact protobuf field names. The camelCase JSON names are rejected.",
        ["null"] = "Rejected everywhere. Omit a field instead.",
        ["duplicate_keys"] = "Rejected at any depth.",
        ["unknown_fields"] = "Rejected.",
        ["oneof"] = "At most one field of each oneof.",
        ["integers_32_bit"] = "A JSON integer without a fraction or exponent, for example 7 or -3.",
        ["integers_64_bit"] = "A decimal JSON string, for example \"18446744073709551615\".",
        ["doubles"] = "A JSON number, or one of the strings \"NaN\", \"Infinity\" and \"-Infinity\".",
        ["enums"] = "The exact value name as a JSON string. Numbers are rejected.",
        ["booleans"] = "true or false.",
        ["max_input_bytes"] = StrictJsonInput.MaximumInputBytes,
        ["max_nesting_depth"] = StrictJsonInput.MaximumDepth,
        ["max_encoded_request_bytes"] = BridgeContract.ServerReceiveLimitBytes
    };

    private static string Reference(MessageDescriptor message) => "#/$defs/" + message.FullName;

    private static void AddDefinition(MessageDescriptor message, JsonObject definitions)
    {
        if (definitions.ContainsKey(message.FullName))
        {
            return;
        }

        // Reserve the name first so a recursive message terminates.
        var definition = new JsonObject { ["type"] = "object", ["additionalProperties"] = false };
        definitions[message.FullName] = definition;
        var properties = new JsonObject();
        foreach (var field in message.Fields.InFieldNumberOrder())
        {
            properties[field.Name] = FieldSchema(field, definitions);
        }

        definition["properties"] = properties;
        var oneofs = message.Oneofs.Where(oneof => !oneof.IsSynthetic).ToArray();
        if (oneofs.Length > 0)
        {
            definition["description"] = "Set at most one field of each oneof listed in x-briosa-oneofs.";
            definition["x-briosa-oneofs"] = new JsonArray(oneofs.Select(oneof => (JsonNode)new JsonObject
            {
                ["name"] = oneof.Name,
                ["fields"] = new JsonArray(oneof.Fields.Select(field => (JsonNode)field.Name).ToArray())
            }).ToArray());
        }
    }

    private static JsonObject FieldSchema(FieldDescriptor field, JsonObject definitions)
    {
        var value = ValueSchema(field, definitions);
        var schema = field.IsRepeated ? new JsonObject { ["type"] = "array", ["items"] = value } : value;
        var note = field.RealContainingOneof is { } oneof
            ? "Member of oneof " + oneof.Name + ": set at most one of its fields."
            : field.HasPresence && field.FieldType != FieldType.Message ? PresenceDescription : null;
        if (note is not null)
        {
            schema["description"] = schema["description"] is { } existing
                ? existing.GetValue<string>() + " " + note
                : note;
        }

        schema["x-protobuf-type"] = (field.IsRepeated ? "repeated " : string.Empty) + TypeName(field);
        return schema;
    }

    private static JsonObject ValueSchema(FieldDescriptor field, JsonObject definitions)
    {
        switch (field.FieldType)
        {
            case FieldType.Message:
                AddDefinition(field.MessageType, definitions);
                return new JsonObject { ["$ref"] = Reference(field.MessageType) };
            case FieldType.Bool:
                return new JsonObject { ["type"] = "boolean" };
            case FieldType.String:
                return new JsonObject { ["type"] = "string" };
            case FieldType.Enum:
                return new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray(field.EnumType.Values.Select(value => (JsonNode)value.Name).ToArray())
                };
            case FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32:
                return Integer(int.MinValue, int.MaxValue);
            case FieldType.UInt32 or FieldType.Fixed32:
                return Integer(0, uint.MaxValue);
            case FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64:
                return new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = "^(0|-?[1-9][0-9]{0,18})$",
                    ["description"] = "A 64-bit integer written as a decimal string."
                };
            case FieldType.UInt64 or FieldType.Fixed64:
                return new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = "^(0|[1-9][0-9]{0,19})$",
                    ["description"] = "An unsigned 64-bit integer written as a decimal string."
                };
            case FieldType.Double:
                return new JsonObject
                {
                    ["anyOf"] = new JsonArray(
                        new JsonObject { ["type"] = "number" },
                        new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray("NaN", "Infinity", "-Infinity")
                        })
                };
            default:
                // StrictJsonInput rejects these kinds, so the schema admits nothing.
                return new JsonObject { ["not"] = new JsonObject() };
        }
    }

    private static JsonObject Integer(long minimum, long maximum) => new()
    {
        ["type"] = "integer",
        ["minimum"] = minimum,
        ["maximum"] = maximum,
        ["description"] = "Written without a fraction or exponent."
    };

    private static string TypeName(FieldDescriptor field) => field.FieldType switch
    {
        FieldType.Message => field.MessageType.FullName,
        FieldType.Enum => field.EnumType.FullName,
        _ => ProtocolJson.ScalarTypeName(field.FieldType)
    };
}
