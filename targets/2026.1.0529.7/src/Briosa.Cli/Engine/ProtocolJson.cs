using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Nodes;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace Briosa.Cli.Engine;

/// <summary>
/// Formats protocol messages and values as proto3 JSON with the exact protobuf
/// field and enum value names. Floating-point values keep the formatter's
/// round-trip form; nothing is rounded.
/// </summary>
internal static class ProtocolJson
{
    private static readonly JsonFormatter Formatter = new(JsonFormatter.Settings.Default
        .WithPreserveProtoFieldNames(true)
        .WithFormatDefaultValues(true));

    private static readonly ConcurrentDictionary<(Type, int), string?> EnumNames = new();

    /// <summary>
    /// Proto3 JSON. Fields without presence are written even at their default value,
    /// so a written field always means a value; a field with presence is written
    /// only when it is set.
    /// </summary>
    public static string Format(IMessage message) => Formatter.Format(message);

    /// <summary>
    /// The same JSON as a node. Numbers keep their exact text when the node is
    /// written again.
    /// </summary>
    public static JsonNode ToNode(IMessage message) => JsonNode.Parse(Format(message))!;

    /// <summary>The protobuf value name, or the number for a value this build does not know.</summary>
    public static JsonNode EnumNode<T>(T value)
        where T : struct, Enum
    {
        var number = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        var name = EnumNames.GetOrAdd((typeof(T), number), static key =>
        {
            var field = key.Item1.GetFields(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(candidate => Convert.ToInt32(candidate.GetValue(null),
                    System.Globalization.CultureInfo.InvariantCulture) == key.Item2);
            return field?.GetCustomAttribute<OriginalNameAttribute>()?.Name;
        });
        return name is null ? JsonValue.Create(number) : JsonValue.Create(name);
    }

    /// <summary>The canonical gRPC status name, for example <c>DEADLINE_EXCEEDED</c>.</summary>
    public static string StatusName(StatusCode code) => code switch
    {
        StatusCode.OK => "OK",
        StatusCode.Cancelled => "CANCELLED",
        StatusCode.Unknown => "UNKNOWN",
        StatusCode.InvalidArgument => "INVALID_ARGUMENT",
        StatusCode.DeadlineExceeded => "DEADLINE_EXCEEDED",
        StatusCode.NotFound => "NOT_FOUND",
        StatusCode.AlreadyExists => "ALREADY_EXISTS",
        StatusCode.PermissionDenied => "PERMISSION_DENIED",
        StatusCode.Unauthenticated => "UNAUTHENTICATED",
        StatusCode.ResourceExhausted => "RESOURCE_EXHAUSTED",
        StatusCode.FailedPrecondition => "FAILED_PRECONDITION",
        StatusCode.Aborted => "ABORTED",
        StatusCode.OutOfRange => "OUT_OF_RANGE",
        StatusCode.Unimplemented => "UNIMPLEMENTED",
        StatusCode.Internal => "INTERNAL",
        StatusCode.Unavailable => "UNAVAILABLE",
        StatusCode.DataLoss => "DATA_LOSS",
        _ => "UNKNOWN"
    };

    /// <summary>
    /// The diagnostic code for a transport failure without a typed trailer:
    /// <c>transport-</c> and the status name in lower kebab case, as the shared
    /// client contract defines it.
    /// </summary>
    public static string TransportCode(StatusCode code) =>
        "transport-" + AsciiLower(StatusName(code)).Replace('_', '-');

    /// <summary>The lowercase risk flag name the policy uses, for example <c>filesystem_read</c>.</summary>
    public static string RiskFlagName(OperationRiskFlag flag)
    {
        var node = EnumNode(flag);
        return node.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? AsciiLower(node.GetValue<string>()["OPERATION_RISK_FLAG_".Length..])
            : node.ToJsonString();
    }

    public static bool TryParseRiskFlag(string name, out OperationRiskFlag flag)
    {
        foreach (var candidate in Enum.GetValues<OperationRiskFlag>())
        {
            if (candidate != OperationRiskFlag.Unspecified &&
                string.Equals(RiskFlagName(candidate), name, StringComparison.Ordinal))
            {
                flag = candidate;
                return true;
            }
        }

        flag = OperationRiskFlag.Unspecified;
        return false;
    }

    public static JsonArray RiskFlags(IEnumerable<OperationRiskFlag> flags) =>
        new(flags.Select(flag => EnumNode(flag)).ToArray());

    public static JsonNode? Optional(IMessage? message) => message is null ? null : ToNode(message);

    /// <summary>The lowercase form of a protocol identifier, which is always ASCII.</summary>
    public static string AsciiLower(string text) => string.Create(text.Length, text, static (span, source) =>
    {
        for (var index = 0; index < source.Length; index++)
        {
            var character = source[index];
            span[index] = character is >= 'A' and <= 'Z' ? (char)(character + ('a' - 'A')) : character;
        }
    });

    /// <summary>The scalar type name of a field kind, for example <c>double</c>.</summary>
    public static string ScalarTypeName(FieldType type) => AsciiLower(type.ToString());
}
