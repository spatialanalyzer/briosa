using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Api = global::Briosa;

namespace Briosa.Server.Operations;

/// <summary>
/// A request validation failure that belongs to one request field (#442). It names
/// the field by its protobuf path and the constraint the field did not meet, and it
/// never holds a request value: every path is a literal from the request schema, and
/// the message is built only from the path and the constraint.
/// <c>OperationExecutor</c> reports it as <see cref="Api.RequestValidationDetails"/>
/// on an otherwise unchanged validation failure. Any other
/// <see cref="ArgumentException"/> still fails validation without detail.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "Every instance must name a schema field path and a constraint, and nothing else.")]
internal sealed partial class RequestFieldViolation : ArgumentException
{
    private RequestFieldViolation(string fieldPath, Api.RequestFieldConstraint constraint)
        : base($"Request field '{fieldPath}' does not meet its {constraint} constraint.")
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
    /// The public detail, or null when the path is not a dotted list of lowercase
    /// protobuf field names. The check keeps anything other than a schema path, such
    /// as text built from a request value by mistake, out of the error.
    /// </summary>
    public Api.RequestValidationDetails? ToDetails() =>
        FieldPathPattern().IsMatch(FieldPath) &&
        Constraint is not Api.RequestFieldConstraint.Unspecified &&
        Enum.IsDefined(Constraint)
            ? new() { FieldPath = FieldPath, Constraint = Constraint }
            : null;

    [GeneratedRegex("\\A[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)*\\z", RegexOptions.CultureInvariant)]
    private static partial Regex FieldPathPattern();
}
