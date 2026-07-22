using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;
using Briosa.Worker.Control;

namespace Briosa.Server.Security;

internal enum OperationPolicyDecisionKind
{
    Allowed,
    Denied,
    Unsupported
}

internal sealed record OperationPolicyDecision(
    OperationPolicyDecisionKind Kind,
    string DiagnosticCode,
    CatalogOperationDescriptor? Operation);

internal sealed class OperationPolicy
{
    internal const string AllowKey = "Briosa:Security:Operations:Allow";
    internal const string DenyKey = "Briosa:Security:Operations:Deny";

    private readonly FrozenSet<string> _allow;
    private readonly FrozenSet<string> _deny;
    private readonly FrozenDictionary<string, CatalogOperationDescriptor> _operations;

    private OperationPolicy(
        IReadOnlyList<CatalogOperationDescriptor> operations,
        IReadOnlyList<string> allow,
        IReadOnlyList<string> deny)
    {
        _operations = operations.ToFrozenDictionary(
            operation => operation.OperationId,
            StringComparer.Ordinal);
        _allow = allow.ToFrozenSet(StringComparer.Ordinal);
        _deny = deny.ToFrozenSet(StringComparer.Ordinal);
        AllowedOperations = operations
            .Where(operation => _allow.Contains(operation.OperationId) &&
                !_deny.Contains(operation.OperationId))
            .OrderBy(operation => operation.OperationId, StringComparer.Ordinal)
            .ToArray();
        Fingerprint = CreateFingerprint(allow, deny);
    }

    public IReadOnlyList<CatalogOperationDescriptor> AllowedOperations { get; }

    public int AllowCount => _allow.Count;

    public int DenyCount => _deny.Count;

    public string Fingerprint { get; }

    public static OperationPolicy Create(
        IConfiguration configuration,
        IReadOnlyList<CatalogOperationDescriptor> operations)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(operations);

        var duplicateOperation = operations
            .GroupBy(operation => operation.OperationId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateOperation is not null)
        {
            throw new InvalidOperationException(
                $"The generated catalog contains duplicate operation ID '{duplicateOperation.Key}'.");
        }

        var allow = ReadOperationIds(configuration, AllowKey);
        var deny = ReadOperationIds(configuration, DenyKey);
        var supported = operations
            .Select(operation => operation.OperationId)
            .ToHashSet(StringComparer.Ordinal);
        var unknown = allow.Concat(deny)
            .Where(operationId => !supported.Contains(operationId))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unknown.Length > 0)
        {
            throw new InvalidOperationException(
                $"Operation policy references unsupported operation ID '{unknown[0]}'.");
        }

        return new OperationPolicy(operations, allow, deny);
    }

    public OperationPolicyDecision Evaluate(WorkerMpCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_operations.TryGetValue(command.OperationId, out var operation))
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Unsupported,
                "operation-unsupported",
                Operation: null);
        }

        if (!string.Equals(command.StepName, operation.MpStep, StringComparison.Ordinal))
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Unsupported,
                "operation-binding-mismatch",
                operation);
        }

        if (string.Equals(operation.Effect, "unknown", StringComparison.Ordinal) ||
            operation.RiskFlags.Contains("unknown", StringComparer.Ordinal))
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Denied,
                "operation-risk-unreviewed",
                operation);
        }

        if (_deny.Contains(operation.OperationId) || !_allow.Contains(operation.OperationId))
        {
            return new OperationPolicyDecision(
                OperationPolicyDecisionKind.Denied,
                "operation-policy-denied",
                operation);
        }

        return new OperationPolicyDecision(
            OperationPolicyDecisionKind.Allowed,
            "operation-policy-allowed",
            operation);
    }

    private static string[] ReadOperationIds(IConfiguration configuration, string key)
    {
        var section = configuration.GetSection(key);
        if (!string.IsNullOrEmpty(section.Value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' must be an indexed array of operation IDs.");
        }

        var values = section.GetChildren()
            .Select(child => child.Value)
            .ToArray();
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' contains an empty operation ID.");
        }

        var operationIds = values.Select(value => value!).ToArray();
        var duplicate = operationIds
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' contains duplicate operation ID '{duplicate.Key}'.");
        }

        return operationIds;
    }

    private static string CreateFingerprint(
        IReadOnlyList<string> allow,
        IReadOnlyList<string> deny)
    {
        var canonical = string.Join(
            '\n',
            allow.Order(StringComparer.Ordinal).Select(value => $"allow:{value}")
                .Concat(deny.Order(StringComparer.Ordinal).Select(value => $"deny:{value}")));
        return $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";
    }
}
