using Briosa.Worker.Control;

namespace Briosa.Server.Operations.Values;

/// <summary>
/// Required request scalars (#243 D2). An absent field, or an empty or
/// whitespace-only required string, fails request validation before dispatch
/// (InvalidArgument, NotStarted) instead of being sent as the protobuf zero value.
/// </summary>
internal static class RequiredValueMapper
{
    public static WorkerDoubleValue Double(bool present, double value, string fieldName) =>
        present ? new(value) : throw Missing(fieldName);

    public static WorkerIntegerValue Integer(bool present, int value, string fieldName) =>
        present ? new(value) : throw Missing(fieldName);

    public static WorkerBooleanValue Boolean(bool present, bool value, string fieldName) =>
        present ? new(value) : throw Missing(fieldName);

    public static WorkerTextValue NonEmptyText(bool present, string value, string fieldName) =>
        present && !string.IsNullOrWhiteSpace(value) ? new(value) : throw Missing(fieldName);

    private static ArgumentException Missing(string fieldName) =>
        new($"Request field '{fieldName}' is required.", fieldName);
}
