using global::Briosa;
using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Briosa.Server.Tests;

public sealed class OperationPolicyTests
{
    private const string OperationId = "file_operations.get_working_directory";

    [Fact]
    public void MissingAllowListDeniesEveryImplementedOperation()
    {
        var policy = CreatePolicy();

        foreach (var operation in SpatialAnalyzerApi.Operations)
        {
            var decision = policy.Evaluate(Command(
                operation.OperationId,
                operation.MpStep));
            Assert.Equal(OperationPolicyDecisionKind.Denied, decision.Kind);
            Assert.Equal("operation-policy-denied", decision.DiagnosticCode);
        }
        Assert.Empty(policy.AllowedOperations);
    }

    [Fact]
    public void EveryImplementedOperationRequiresAnExactExplicitAllowEntry()
    {
        var operationIds = SpatialAnalyzerApi.Operations
            .Select(operation => operation.OperationId)
            .ToArray();
        var policy = CreatePolicy(allow: operationIds);

        foreach (var operation in SpatialAnalyzerApi.Operations)
        {
            Assert.Equal(
                OperationPolicyDecisionKind.Allowed,
                policy.Evaluate(Command(operation.OperationId, operation.MpStep)).Kind);
        }
        Assert.Equal(operationIds.Order(), policy.AllowedOperations
            .Select(operation => operation.OperationId).Order());
    }

    [Fact]
    public void ExactAllowListEnablesTheImplementedBinding()
    {
        var policy = CreatePolicy(allow: [OperationId]);

        var decision = policy.Evaluate(Command());

        Assert.Equal(OperationPolicyDecisionKind.Allowed, decision.Kind);
        Assert.Equal(OperationId, Assert.Single(policy.AllowedOperations).OperationId);
    }

    [Fact]
    public void DenyListWinsWhenAnOperationAppearsInBothLists()
    {
        var policy = CreatePolicy(allow: [OperationId], deny: [OperationId]);

        Assert.Equal(OperationPolicyDecisionKind.Denied, policy.Evaluate(Command()).Kind);
        Assert.Empty(policy.AllowedOperations);
    }

    [Theory]
    [InlineData("unsupported.operation", "Get Working Directory", "operation-unsupported")]
    [InlineData(OperationId, "Different MP Step", "operation-binding-mismatch")]
    public void UnsupportedOrMismatchedBindingsNeverBecomeAllowed(
        string operationId,
        string stepName,
        string diagnosticCode)
    {
        var policy = CreatePolicy(allow: [OperationId]);

        var decision = policy.Evaluate(Command(operationId, stepName));

        Assert.Equal(OperationPolicyDecisionKind.Unsupported, decision.Kind);
        Assert.Equal(diagnosticCode, decision.DiagnosticCode);
    }

    [Fact]
    public void UnknownConfiguredOperationFailsClosedAtStartup()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreatePolicy(allow: ["unsupported.operation"]));

        Assert.Contains("unsupported operation ID", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OperationExecutionScope.Unspecified, "operation-isolation-unreviewed")]
    [InlineData(OperationExecutionScope.Unknown, "operation-isolation-unreviewed")]
    [InlineData(OperationExecutionScope.ExclusiveWorkflow, "operation-isolation-unsupported")]
    public void UnsupportedIsolationScopesFailClosed(
        OperationExecutionScope executionScope,
        string diagnosticCode)
    {
        var operation = WorkingDirectoryOperation() with
        {
            ExecutionScope = executionScope
        };
        var policy = CreatePolicy(allow: [OperationId], operations: [operation]);

        var decision = policy.Evaluate(Command());

        Assert.Equal(OperationPolicyDecisionKind.Denied, decision.Kind);
        Assert.Equal(diagnosticCode, decision.DiagnosticCode);
        Assert.Empty(policy.AllowedOperations);
    }

    [Fact]
    public async Task CompetingOrdinaryCallersCannotEnterAnExclusiveWorkflow()
    {
        var factory = new CountingProcessFactory();
        var supervisor = new WorkerProcessSupervisor(factory, RestartPolicy());
        await using var configuredSupervisor = supervisor.ConfigureAwait(true);
        var operation = WorkingDirectoryOperation() with
        {
            ExecutionScope = OperationExecutionScope.ExclusiveWorkflow
        };
        var executor = new PolicyEnforcingWorkerCommandExecutor(
            supervisor,
            CreatePolicy(allow: [OperationId], operations: [operation]),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));

        var outcomes = await Task.WhenAll(
            executor.ExecuteAsync(Command(), Guid.NewGuid()),
            executor.ExecuteAsync(Command(), Guid.NewGuid())).ConfigureAwait(true);

        Assert.All(outcomes, outcome =>
        {
            Assert.Equal(WorkerExecutionStatus.PolicyDenied, outcome.Status);
            Assert.Equal("operation-isolation-unsupported", outcome.DiagnosticCode);
            Assert.Equal(WorkerExecutionDisposition.NotStarted, outcome.ExecutionDisposition);
        });
        Assert.Equal(0, factory.StartCount);
    }

    [Fact]
    public async Task RejectionOccursBeforeTheWorkerSupervisorIsStarted()
    {
        var factory = new CountingProcessFactory();
        var supervisor = new WorkerProcessSupervisor(factory, RestartPolicy());
        await using var configuredSupervisor = supervisor.ConfigureAwait(true);
        var executor = new PolicyEnforcingWorkerCommandExecutor(
            supervisor,
            CreatePolicy(),
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance));
        var correlationId = Guid.NewGuid();

        var outcome = await executor.ExecuteAsync(Command(), correlationId)
            .ConfigureAwait(true);

        Assert.Equal(WorkerExecutionStatus.PolicyDenied, outcome.Status);
        Assert.Equal("operation-policy-denied", outcome.DiagnosticCode);
        Assert.Equal(correlationId, outcome.CorrelationId);
        Assert.Equal(0, factory.StartCount);
    }

    private static OperationPolicy CreatePolicy(
        IReadOnlyList<string>? allow = null,
        IReadOnlyList<string>? deny = null,
        IReadOnlyList<OperationDescriptor>? operations = null)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        Add(values, OperationPolicy.AllowKey, allow);
        Add(values, OperationPolicy.DenyKey, deny);
        return OperationPolicy.Create(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
            operations ?? SpatialAnalyzerApi.Operations);
    }

    private static OperationDescriptor WorkingDirectoryOperation() =>
        GetWorkingDirectoryOperation.Descriptor;

    private static void Add(
        Dictionary<string, string?> values,
        string key,
        IReadOnlyList<string>? operationIds)
    {
        if (operationIds is null)
        {
            return;
        }

        for (var index = 0; index < operationIds.Count; index++)
        {
            values.Add($"{key}:{index}", operationIds[index]);
        }
    }

    private static WorkerMpCommand Command(
        string operationId = OperationId,
        string stepName = "Get Working Directory") =>
        new(operationId, stepName, [], []);

    private static WorkerRestartPolicy RestartPolicy() =>
        new(
            maximumRestarts: 0,
            restartWindow: TimeSpan.FromSeconds(1),
            heartbeatInterval: TimeSpan.FromSeconds(1),
            heartbeatTimeout: TimeSpan.FromSeconds(1),
            startupTimeout: TimeSpan.FromSeconds(1),
            shutdownTimeout: TimeSpan.FromSeconds(1),
            restartDelay: TimeSpan.Zero);

    private sealed class CountingProcessFactory : IWorkerProcessFactory
    {
        public int StartCount { get; private set; }

        public ValueTask<IWorkerProcess> StartAsync(
            int generation,
            CancellationToken cancellationToken = default)
        {
            StartCount++;
            throw new InvalidOperationException("The policy test must not start a worker.");
        }
    }
}
