namespace Briosa.Server.Services;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class OperationImplementationAttribute(string operationId) : Attribute
{
    public string OperationId { get; } =
        string.IsNullOrWhiteSpace(operationId)
            ? throw new ArgumentException("An operation ID is required.", nameof(operationId))
            : operationId;
}
