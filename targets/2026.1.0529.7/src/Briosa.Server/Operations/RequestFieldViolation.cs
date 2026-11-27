using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Google.Protobuf.Reflection;
using Api = global::Briosa;

namespace Briosa.Server.Operations;

/// <summary>
/// A request validation failure that belongs to one request field (#442). It names
/// the field by its protobuf path and the constraint the field did not meet. Every
/// path is meant to be a literal from the request schema, but nothing is published on
/// that promise: <see cref="ToDetails"/> returns detail only for a path that resolves
/// to a field of the request it is given, and the message is fixed text that holds
/// neither the path nor any value. <c>OperationExecutor</c> reports the detail only
/// for a violation that the operation's request mapping threw; any other
/// <see cref="ArgumentException"/> still fails validation without detail.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Every instance must name a schema field path and a constraint, and nothing else.")]
internal sealed partial class RequestFieldViolation : ArgumentException
{
    // The path is unverified until ToDetails resolves it, so the message, which a
    // generic handler or logger can surface, never contains it.
    private RequestFieldViolation(string fieldPath, Api.RequestFieldConstraint constraint)
        : base("A request field failed validation.")
    {
        FieldPath = fieldPath;
        Constraint = constraint;
    }

    /// <summary>Protobuf field names, separated by dots, from the request message down.</summary>
    public string FieldPath { get; }

    public Api.RequestFieldConstraint Constraint { get; }

    /// <summary>An absent message field, or an absent field with explicit presence.</summary>
    public static RequestFieldViolation Required(string fieldPath) =>
        new(fieldPath, Api.RequestFieldConstraint.Required);

    /// <summary>An empty or whitespace-only required string.</summary>
    public static RequestFieldViolation NotBlank(string fieldPath) =>
        new(fieldPath, Api.RequestFieldConstraint.NotBlank);

    /// <summary>A required repeated field without elements.</summary>
    public static RequestFieldViolation NotEmpty(string fieldPath) =>
        new(fieldPath, Api.RequestFieldConstraint.NotEmpty);

    /// <summary>A repeated field without the exact number of elements its value needs.</summary>
    public static RequestFieldViolation ElementCount(string fieldPath) =>
        new(fieldPath, Api.RequestFieldConstraint.ElementCount);

    /// <summary>A number outside the range the target accepts.</summary>
    public static RequestFieldViolation Range(string fieldPath) =>
        new(fieldPath, Api.RequestFieldConstraint.Range);

    /// <summary>An enum value the operation does not support on this target.</summary>
    public static RequestFieldViolation SupportedChoice(string fieldPath) =>
        new(fieldPath, Api.RequestFieldConstraint.SupportedChoice);

    /// <summary>A oneof with no field set; the path names the message holding it.</summary>
    public static RequestFieldViolation OneofRequired(string fieldPath) =>
        new(fieldPath, Api.RequestFieldConstraint.OneofRequired);

    /// <summary>
    /// Appends the name of a field of <paramref name="parent"/>'s message, for a mapper
    /// that is handed only the path of the message it reads.
    /// </summary>
    public static string Path(string parent, string field) => $"{parent}.{field}";

    /// <summary>
    /// The public detail, or null unless the path names a field of
    /// <paramref name="request"/> and the constraint fits that field. Each segment must
    /// name a field of the message the previous segment holds, crossing a repeated
    /// message field without an index and never a map; an unset oneof is reported on
    /// the message that holds it. Anything else, such as text built from a request
    /// value by mistake, leaves the error without detail.
    /// </summary>
    public Api.RequestValidationDetails? ToDetails(MessageDescriptor request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!FieldPathPattern().IsMatch(FieldPath) || Constraint is Api.RequestFieldConstraint.Unspecified ||
            !Enum.IsDefined(Constraint))
        {
            return null;
        }

        MessageDescriptor? message = request;
        FieldDescriptor? field = null;
        foreach (var segment in FieldPath.Split('.'))
        {
            field = message?.FindFieldByName(segment);
            if (field is null || field.IsMap)
            {
                return null;
            }

            message = field.FieldType == FieldType.Message ? field.MessageType : null;
        }

        return field is not null && Fits(field, Constraint)
            ? new() { FieldPath = FieldPath, Constraint = Constraint }
            : null;
    }

    private static bool Fits(FieldDescriptor field, Api.RequestFieldConstraint constraint) => constraint switch
    {
        Api.RequestFieldConstraint.Required => !field.IsRepeated && field.HasPresence,
        Api.RequestFieldConstraint.NotBlank => !field.IsRepeated && field.FieldType == FieldType.String,
        Api.RequestFieldConstraint.NotEmpty or Api.RequestFieldConstraint.ElementCount => field.IsRepeated,
        Api.RequestFieldConstraint.Range => !field.IsRepeated && field.FieldType is FieldType.Int32 or
            FieldType.UInt32 or FieldType.Int64 or FieldType.UInt64 or FieldType.SInt32 or FieldType.SInt64 or
            FieldType.Fixed32 or FieldType.Fixed64 or FieldType.SFixed32 or FieldType.SFixed64 or
            FieldType.Double or FieldType.Float,
        Api.RequestFieldConstraint.SupportedChoice => !field.IsRepeated &&
            field.FieldType is FieldType.Enum or FieldType.String,
        Api.RequestFieldConstraint.OneofRequired => !field.IsRepeated && field.FieldType == FieldType.Message &&
            field.MessageType.Oneofs.Any(oneof => !oneof.IsSynthetic),
        _ => false
    };

    [GeneratedRegex("\\A[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)*\\z", RegexOptions.CultureInvariant)]
    private static partial Regex FieldPathPattern();
}
