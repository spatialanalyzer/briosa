namespace Briosa.Server.Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
internal sealed class OperationTestAttribute(string operationId) : Attribute
{
    public string OperationId { get; } =
        string.IsNullOrWhiteSpace(operationId)
            ? throw new ArgumentException("An operation ID is required.", nameof(operationId))
            : operationId;
}
