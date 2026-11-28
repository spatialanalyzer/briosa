using System.Globalization;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Briosa.Cli.Engine;

/// <summary>
/// Reads a request from strict JSON. Before the protobuf JSON parser runs, the input
/// is checked against the request descriptor so that only one documented form of
/// each value is accepted:
/// <list type="bullet">
/// <item>exact protobuf field names only, never the camelCase JSON names;</item>
/// <item>no <c>null</c> anywhere, no duplicate key at any depth, no unknown field,
/// and at most one field of each oneof;</item>
/// <item>32-bit integers as JSON integers without a fraction or exponent; 64-bit
/// integers as decimal JSON strings; doubles as JSON numbers or the strings
/// <c>NaN</c>, <c>Infinity</c>, <c>-Infinity</c>; enums as their exact value name;</item>
/// <item>at most <see cref="MaximumInputBytes"/> bytes of UTF-8 and
/// <see cref="MaximumDepth"/> levels of nesting.</item>
/// </list>
/// A rejection names a schema path and a position, never the offending text.
/// </summary>
internal static class StrictJsonInput
{
    public const int MaximumInputBytes = 256 * 1024;
    public const int MaximumDepth = 32;

    private static readonly JsonParser Parser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(false));

    public static InputParseResult Parse(ReadOnlySpan<byte> utf8, MessageDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (utf8.Length > MaximumInputBytes)
        {
            return InputParseResult.Reject(InputRejectionCodes.TooLarge, null, null, utf8);
        }

        // A leading byte order mark is not JSON; an editor may still write one.
        if (utf8.StartsWith(Encoding.UTF8.Preamble))
        {
            utf8 = utf8[Encoding.UTF8.Preamble.Length..];
        }

        var rejection = Validate(utf8, descriptor);
        if (rejection is not null)
        {
            return new InputParseResult(null, rejection);
        }

        IMessage message;
        try
        {
            message = Parser.Parse(Encoding.UTF8.GetString(utf8), descriptor);
        }
        catch (Exception exception) when (exception is InvalidProtocolBufferException or InvalidJsonException or
            FormatException or OverflowException or ArgumentException)
        {
            // The parser's message can quote the input, so it is never reported.
            return InputParseResult.Reject(InputRejectionCodes.RejectedByParser, null, null, utf8);
        }

        return message.CalculateSize() > BridgeContract.ServerReceiveLimitBytes
            ? InputParseResult.Reject(InputRejectionCodes.EncodedRequestTooLarge, null, null, utf8)
            : new InputParseResult(message, null);
    }

    /// <summary>
    /// Checks the input against the descriptor without parsing it into a message.
    /// Returns null when every rule holds.
    /// </summary>
    public static InputRejection? Validate(ReadOnlySpan<byte> utf8, MessageDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var walker = new Walker(utf8);
        return walker.Validate(descriptor);
    }

    /// <summary>Joins protobuf field names the way the server's validation detail does.</summary>
    public static string Join(string path, string name) => path.Length == 0 ? name : path + "." + name;

    private ref struct Walker
    {
        private readonly ReadOnlySpan<byte> _input;
        private Utf8JsonReader _reader;

        public Walker(ReadOnlySpan<byte> input)
        {
            _input = input;
            _reader = new Utf8JsonReader(input, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                // One level above the documented limit, so the walker's own check,
                // which names the field, fires first.
                MaxDepth = MaximumDepth + 1
            });
        }

        public InputRejection? Validate(MessageDescriptor descriptor)
        {
            try
            {
                if (!_reader.Read())
                {
                    return Reject(InputRejectionCodes.MalformedJson, null);
                }

                if (_reader.TokenType != JsonTokenType.StartObject)
                {
                    return Reject(InputRejectionCodes.RootNotObject, null);
                }

                var rejection = ValidateObject(descriptor, string.Empty);
                if (rejection is not null)
                {
                    return rejection;
                }

                // Anything after the root value other than whitespace throws.
                return _reader.Read() ? Reject(InputRejectionCodes.MalformedJson, null) : null;
            }
            catch (JsonException exception)
            {
                return InputRejection.AtLine(InputRejectionCodes.MalformedJson, null,
                    exception.LineNumber, exception.BytePositionInLine, _input);
            }
        }

        private InputRejection? ValidateObject(MessageDescriptor descriptor, string path)
        {
            if (_reader.CurrentDepth >= MaximumDepth)
            {
                return Reject(InputRejectionCodes.TooDeep, path.Length == 0 ? null : path);
            }

            HashSet<string> seen = new(StringComparer.Ordinal);
            HashSet<OneofDescriptor> selected = [];
            while (_reader.Read())
            {
                if (_reader.TokenType == JsonTokenType.EndObject)
                {
                    return null;
                }

                if (!TryGetString(out var name))
                {
                    return Reject(InputRejectionCodes.InvalidString, NullIfEmpty(path));
                }

                var field = descriptor.FindFieldByName(name);
                if (field is null)
                {
                    // A JSON name the protobuf parser would accept as an alias gets a
                    // hint with the schema name; the offending key is never repeated.
                    var alias = descriptor.Fields.InDeclarationOrder()
                        .FirstOrDefault(candidate => string.Equals(candidate.JsonName, name, StringComparison.Ordinal));
                    return alias is null
                        ? Reject(InputRejectionCodes.UnknownField, NullIfEmpty(path))
                        : Reject(InputRejectionCodes.FieldNameNotExact, NullIfEmpty(path), alias.Name);
                }

                var fieldPath = Join(path, field.Name);
                if (!seen.Add(field.Name))
                {
                    return Reject(InputRejectionCodes.DuplicateField, fieldPath);
                }

                if (field.RealContainingOneof is { } oneof && !selected.Add(oneof))
                {
                    return Reject(InputRejectionCodes.OneofConflict, Join(path, oneof.Name));
                }

                if (!_reader.Read())
                {
                    return Reject(InputRejectionCodes.MalformedJson, fieldPath);
                }

                var rejection = ValidateField(field, fieldPath);
                if (rejection is not null)
                {
                    return rejection;
                }
            }

            return Reject(InputRejectionCodes.MalformedJson, NullIfEmpty(path));
        }

        private InputRejection? ValidateField(FieldDescriptor field, string path)
        {
            if (_reader.TokenType == JsonTokenType.Null)
            {
                return Reject(InputRejectionCodes.Null, path);
            }

            if (field.IsMap || !IsSupported(field))
            {
                return Reject(InputRejectionCodes.UnsupportedFieldType, path);
            }

            if (!field.IsRepeated)
            {
                return ValidateValue(field, path);
            }

            if (_reader.TokenType != JsonTokenType.StartArray)
            {
                return Reject(InputRejectionCodes.TypeMismatch, path);
            }

            if (_reader.CurrentDepth >= MaximumDepth)
            {
                return Reject(InputRejectionCodes.TooDeep, path);
            }

            while (_reader.Read() && _reader.TokenType != JsonTokenType.EndArray)
            {
                if (_reader.TokenType == JsonTokenType.Null)
                {
                    return Reject(InputRejectionCodes.Null, path);
                }

                var rejection = ValidateValue(field, path);
                if (rejection is not null)
                {
                    return rejection;
                }
            }

            return _reader.TokenType == JsonTokenType.EndArray ? null : Reject(InputRejectionCodes.MalformedJson, path);
        }

        private InputRejection? ValidateValue(FieldDescriptor field, string path)
        {
            var token = _reader.TokenType;
            switch (field.FieldType)
            {
                case FieldType.Message:
                    return token == JsonTokenType.StartObject
                        ? ValidateObject(field.MessageType, path)
                        : Reject(InputRejectionCodes.TypeMismatch, path);
                case FieldType.Bool:
                    return token is JsonTokenType.True or JsonTokenType.False
                        ? null
                        : Reject(InputRejectionCodes.TypeMismatch, path);
                case FieldType.String:
                    if (token != JsonTokenType.String)
                    {
                        return Reject(InputRejectionCodes.TypeMismatch, path);
                    }

                    return TryGetString(out _) ? null : Reject(InputRejectionCodes.InvalidString, path);
                case FieldType.Enum:
                    if (token == JsonTokenType.Number)
                    {
                        return Reject(InputRejectionCodes.EnumNameRequired, path);
                    }

                    if (token != JsonTokenType.String)
                    {
                        return Reject(InputRejectionCodes.TypeMismatch, path);
                    }

                    return TryGetString(out var enumName) && field.EnumType.FindValueByName(enumName) is not null
                        ? null
                        : Reject(InputRejectionCodes.UnknownEnumName, path);
                case FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32:
                    return ValidateInteger32(path, signed: true);
                case FieldType.UInt32 or FieldType.Fixed32:
                    return ValidateInteger32(path, signed: false);
                case FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64:
                    return ValidateInteger64(path, signed: true);
                case FieldType.UInt64 or FieldType.Fixed64:
                    return ValidateInteger64(path, signed: false);
                case FieldType.Double:
                    return ValidateDouble(path);
                default:
                    return Reject(InputRejectionCodes.UnsupportedFieldType, path);
            }
        }

        private InputRejection? ValidateInteger32(string path, bool signed)
        {
            if (_reader.TokenType == JsonTokenType.String)
            {
                return Reject(InputRejectionCodes.NumberForm, path);
            }

            if (_reader.TokenType != JsonTokenType.Number)
            {
                return Reject(InputRejectionCodes.TypeMismatch, path);
            }

            var raw = RawToken();
            if (raw.IndexOfAny((byte)'.', (byte)'e', (byte)'E') >= 0)
            {
                return Reject(InputRejectionCodes.NumberForm, path);
            }

            var inRange = signed ? _reader.TryGetInt32(out _) : _reader.TryGetUInt32(out _);
            return inRange ? null : Reject(InputRejectionCodes.OutOfRange, path);
        }

        private InputRejection? ValidateInteger64(string path, bool signed)
        {
            if (_reader.TokenType == JsonTokenType.Number)
            {
                return Reject(InputRejectionCodes.NumberForm, path);
            }

            if (_reader.TokenType != JsonTokenType.String)
            {
                return Reject(InputRejectionCodes.TypeMismatch, path);
            }

            if (!TryGetString(out var text) || !IsDecimalInteger(text, signed))
            {
                return Reject(InputRejectionCodes.NumberForm, path);
            }

            var inRange = signed
                ? long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)
                : ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _);
            return inRange ? null : Reject(InputRejectionCodes.OutOfRange, path);
        }

        private InputRejection? ValidateDouble(string path)
        {
            if (_reader.TokenType == JsonTokenType.String)
            {
                return TryGetString(out var text) && text is "NaN" or "Infinity" or "-Infinity"
                    ? null
                    : Reject(InputRejectionCodes.NumberForm, path);
            }

            if (_reader.TokenType != JsonTokenType.Number)
            {
                return Reject(InputRejectionCodes.TypeMismatch, path);
            }

            return _reader.TryGetDouble(out var value) && double.IsFinite(value)
                ? null
                : Reject(InputRejectionCodes.OutOfRange, path);
        }

        private bool TryGetString(out string value)
        {
            try
            {
                value = _reader.GetString() ?? string.Empty;
                return true;
            }
            catch (InvalidOperationException)
            {
                // An escaped lone surrogate cannot become a string.
                value = string.Empty;
                return false;
            }
        }

        // The reader runs over one span, so a token is never split into a sequence.
        private ReadOnlySpan<byte> RawToken() => _reader.ValueSpan;

        private InputRejection Reject(string code, string? path, string? suggestion = null) =>
            InputRejection.AtOffset(code, path, _reader.TokenStartIndex, _input, suggestion);

        private static string? NullIfEmpty(string path) => path.Length == 0 ? null : path;
    }

    /// <summary>The field kinds a request may carry. Anything else fails closed.</summary>
    internal static bool IsSupported(FieldDescriptor field) => !field.IsMap && field.FieldType switch
    {
        FieldType.Message => !string.Equals(field.MessageType.File.Package, "google.protobuf", StringComparison.Ordinal),
        FieldType.Bool or FieldType.String or FieldType.Enum or FieldType.Double => true,
        FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 or FieldType.UInt32 or FieldType.Fixed32 => true,
        FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 or FieldType.UInt64 or FieldType.Fixed64 => true,
        _ => false
    };

    private static bool IsDecimalInteger(string text, bool signed)
    {
        var digits = signed && text.StartsWith('-') ? text.AsSpan(1) : text.AsSpan();
        if (digits.IsEmpty || (digits.Length > 1 && digits[0] == '0') || (digits.Length != text.Length && digits is "0"))
        {
            return false;
        }

        foreach (var character in digits)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>The value-free codes of a local input rejection.</summary>
internal static class InputRejectionCodes
{
    public const string TooLarge = "input-too-large";
    public const string TooDeep = "input-too-deep";
    public const string MalformedJson = "input-malformed-json";
    public const string RootNotObject = "input-root-not-object";
    public const string UnknownField = "input-unknown-field";
    public const string FieldNameNotExact = "input-field-name-not-exact";
    public const string DuplicateField = "input-duplicate-field";
    public const string OneofConflict = "input-oneof-conflict";
    public const string Null = "input-null";
    public const string TypeMismatch = "input-type-mismatch";
    public const string NumberForm = "input-number-form";
    public const string OutOfRange = "input-out-of-range";
    public const string EnumNameRequired = "input-enum-name-required";
    public const string UnknownEnumName = "input-unknown-enum-name";
    public const string InvalidString = "input-invalid-string";
    public const string UnsupportedFieldType = "input-unsupported-field-type";
    public const string RejectedByParser = "input-rejected-by-parser";
    public const string EncodedRequestTooLarge = "request-too-large";
}

/// <summary>
/// Why the CLI refused an input before sending anything. The path holds schema
/// field names only; the position is a byte offset with its line and column.
/// </summary>
internal sealed record InputRejection(
    string Code,
    string? FieldPath,
    long? ByteOffset,
    long? Line,
    long? Column,
    string? SuggestedFieldName)
{
    public static InputRejection AtOffset(string code, string? path, long offset, ReadOnlySpan<byte> input,
        string? suggestion = null)
    {
        var (line, column) = LineAndColumn(input, offset);
        return new InputRejection(code, path, offset, line, column, suggestion);
    }

    public static InputRejection AtLine(string code, string? path, long? lineNumber, long? bytePositionInLine,
        ReadOnlySpan<byte> input)
    {
        if (lineNumber is not { } line || bytePositionInLine is not { } column)
        {
            return new InputRejection(code, path, null, null, null, null);
        }

        // The reader reports a zero-based line and byte; the rejection is one-based.
        long offset = 0;
        for (var current = 0L; current < line && offset < input.Length; offset++)
        {
            if (input[(int)offset] == (byte)'\n')
            {
                current++;
            }
        }

        return new InputRejection(code, path, Math.Min(offset + column, input.Length), line + 1, column + 1, null);
    }

    private static (long Line, long Column) LineAndColumn(ReadOnlySpan<byte> input, long offset)
    {
        var end = (int)Math.Clamp(offset, 0, input.Length);
        var before = input[..end];
        var line = before.Count((byte)'\n') + 1L;
        var lastNewline = before.LastIndexOf((byte)'\n');
        return (line, end - lastNewline);
    }
}

internal sealed record InputParseResult(IMessage? Message, InputRejection? Rejection)
{
    public static InputParseResult Reject(string code, string? path, long? offset, ReadOnlySpan<byte> input) =>
        new(null, offset is { } at
            ? InputRejection.AtOffset(code, path, at, input)
            : new InputRejection(code, path, null, null, null, null));
}
