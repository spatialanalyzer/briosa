using System.Reflection;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace Briosa.Cli.Tests;

/// <summary>
/// Every typed failure the server can return for an MP call, produced by the
/// server's own <c>GrpcOperationOutcomeMapper</c> rather than written by hand, so
/// the CLI's classification is checked against the mapper itself. The mapper and
/// its inputs are internal to the server, so they are reached by reflection.
/// </summary>
internal static class ServerMapperShapes
{
    public const string OperationId = "analysis_operations.get_ith_point_from_group";

    private static readonly Assembly Server = Assembly.Load(new AssemblyName("Briosa.Server"));
    private static readonly Type Mapper = Server.GetType("Briosa.Server.Services.GrpcOperationOutcomeMapper", true)!;
    private static readonly Type Outcome = Server.GetType("Briosa.Server.Workers.WorkerExecutionOutcome", true)!;
    private static readonly Type Status = Server.GetType("Briosa.Server.Workers.WorkerExecutionStatus", true)!;
    private static readonly Type Disposition = Server.GetType("Briosa.Server.Workers.WorkerExecutionDisposition", true)!;
    private static readonly Type Contract = Server.GetType("Briosa.Server.Services.OperationOutputContract", true)!;

    /// <summary>The shape's label, its expected CLI outcome, and the server's exception.</summary>
    public static IReadOnlyList<(string Label, string Outcome, int ExitCode, RpcException Exception)> All { get; } = Build();

    private static List<(string, string, int, RpcException)> Build()
    {
        var text = WorkerMpValueKind.Text;
        WorkerMpOutputValue Unavailable(WorkerUnavailableOutputReason reason = WorkerUnavailableOutputReason.None) =>
            new WorkerUnavailableOutput("Point Name Only", text, "sdk-output-retrieval-failed", reason);
        var wrongShape = new WorkerRetrievedOutput("Another Output", text, new WorkerTextValue("P1"));
        var shapes = new List<(string, string, int, RpcException)>
        {
            ("caller-restriction-invalid", "invalid_request", 7,
                Invoke("CreateValidationFailure", OperationId, "caller-restriction-invalid", 0, ReplaySafety.Safe, null)),
            ("request-validation-with-field", "invalid_request", 7,
                Invoke("CreateValidationFailure", OperationId, "request-validation-failed", 0, ReplaySafety.Safe,
                    new RequestValidationDetails { FieldPath = "group_name.object_name", Constraint = RequestFieldConstraint.NotBlank })),
            ("unsupported", "not_started", 6,
                Invoke("CreateUnsupportedFailure", OperationId, "operation-unsupported", 0, ReplaySafety.Safe)),
            ("request-violation-after-mapping", "started_outcome_unknown", 11,
                Invoke("CreateInternalFailure", OperationId, ReplaySafety.Safe, 0, Enum.Parse(Disposition, "StartedOutcomeUnknown"),
                    "request-violation-after-mapping")),
            ("result-mapping-failed", "outputs_incomplete", 9,
                Invoke("CreateResultMappingFailure", OperationId, ReplaySafety.Safe, 3,
                    new MpExecutionDetails
                    {
                        State = MpExecutionState.Succeeded,
                        MpResultCode = 2,
                        OutputRetrievals = { new OutputRetrievalDetails { FieldName = "point_name_only", State = OutputRetrievalState.Retrieved } }
                    })),
            ("overloaded", "not_started", 6, RequireSuccess("Overloaded", "NotStarted", null, null, "worker-admission-full")),
            ("request-rejected", "invalid_request", 7, RequireSuccess("RequestRejected", "NotStarted", null, null, "request-encoding-rejected")),
            ("policy-denied", "not_started", 6, PolicyDenied()),
            ("unsupported-from-worker", "not_started", 6, RequireSuccess("Unsupported", "NotStarted", null, null, "operation-unsupported")),
            ("caller-cancelled-queued", "not_started", 6, RequireSuccess("ClientCancelled", "NotStarted", null, null, "queued-request-abandoned")),
            ("caller-cancelled-claimed", "started_outcome_unknown", 11, RequireSuccess("ClientCancelled", "StartedOutcomeUnknown", null, null, "client-wait-cancelled")),
            ("caller-deadline-queued", "not_started", 6, RequireSuccess("ClientCancelled", "NotStarted", null, null, "queued-request-abandoned", deadline: true)),
            ("caller-deadline-claimed", "started_outcome_unknown", 11, RequireSuccess("ClientCancelled", "StartedOutcomeUnknown", null, null, "client-wait-cancelled", deadline: true)),
            ("watchdog-timeout", "started_outcome_unknown", 11, RequireSuccess("WatchdogTimeout", "StartedOutcomeUnknown", null, null, "worker-watchdog-timeout")),
            ("watchdog-modal-dialog", "started_outcome_unknown", 11, RequireSuccess("WatchdogTimeout", "StartedOutcomeUnknown", null, null, "sa-modal-dialog-open")),
            ("worker-failure", "started_outcome_unknown", 11, RequireSuccess("WorkerFailure", "StartedOutcomeUnknown", null, null, "worker-process-exited")),
            ("worker-unavailable-not-started", "not_started", 6, RequireSuccess("Unavailable", "NotStarted", null, null, "worker-not-ready")),
            ("worker-unavailable-unknown", "started_outcome_unknown", 11, RequireSuccess("Unavailable", "StartedOutcomeUnknown", null, null, "worker-response-lost")),
            ("sa-unavailable-not-started", "not_started", 6, RequireSuccess("Unavailable", "NotStarted", null, Disconnected(), "spatial-analyzer-disconnected")),
            ("sa-unavailable-unknown", "started_outcome_unknown", 11, RequireSuccess("Unavailable", "StartedOutcomeUnknown", null, Disconnected(), "spatial-analyzer-disconnected")),
            ("arguments-rejected", "not_started", 6, RequireSuccess("Completed", null, new WorkerArgumentsRejected(1, "sdk-argument-rejected"), null, "c")),
            ("execute-step-rejected", "started_outcome_unknown", 11, RequireSuccess("Completed", null, new WorkerExecuteRejected(1, "execute-step-rejected"), null, "c")),
            ("mp-result-unavailable", "started_outcome_unknown", 11, RequireSuccess("Completed", null, new WorkerMpResultUnavailable(1, null), null, "c")),
            ("mp-failure", "mp_failed", 8, RequireSuccess("Completed", null, new WorkerMpResultAvailable(0, 1, [], null), null, "c")),
            ("mp-failure-code-3", "mp_failed", 8, RequireSuccess("Completed", null, new WorkerMpResultAvailable(3, 1, [], null), null, "c")),
            ("outputs-unavailable", "outputs_incomplete", 9, RequireSuccess("Completed", null, new WorkerMpOutputsUnavailable(2, 1, null), null, "c")),
            ("outputs-unavailable-partial", "outputs_incomplete", 9, RequireSuccess("Completed", null, new WorkerMpOutputsUnavailable(4, 1, null), null, "c")),
            ("output-shape-invalid", "outputs_incomplete", 9, RequireSuccess("Completed", null, new WorkerMpResultAvailable(2, 1, [wrongShape], null), null, "c")),
            ("output-retrieval-failed", "outputs_incomplete", 9, RequireSuccess("Completed", null, new WorkerMpResultAvailable(2, 1, [Unavailable()], null), null, "c")),
            ("output-retrieval-failed-partial", "outputs_incomplete", 9, RequireSuccess("Completed", null, new WorkerMpResultAvailable(4, 1, [Unavailable()], null), null, "c")),
            ("sdk-fault-before-execute", "not_started", 6, RequireSuccess("Completed", null, new WorkerSdkCallFaulted(WorkerSdkCallPhase.BeforeExecute, 1, [], "sdk-call-faulted-before-execute"), null, "c")),
            ("sdk-fault-execute-step", "started_outcome_unknown", 11, RequireSuccess("Completed", null, new WorkerSdkCallFaulted(WorkerSdkCallPhase.ExecuteStep, 1, [], "sdk-execute-step-faulted"), null, "c")),
            ("sdk-fault-result-retrieval", "started_outcome_unknown", 11, RequireSuccess("Completed", null, new WorkerSdkCallFaulted(WorkerSdkCallPhase.MpResultRetrieval, 1, [], "sdk-mp-result-retrieval-faulted"), null, "c")),
            ("sdk-fault-output-getter", "outputs_incomplete", 9, RequireSuccess("Completed", null, new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1, [Unavailable(WorkerUnavailableOutputReason.SdkCallFaulted)], "sdk-output-getter-faulted", 2), null, "c")),
            ("sdk-fault-output-getter-partial", "outputs_incomplete", 9, RequireSuccess("Completed", null, new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1, [], "sdk-output-getter-faulted", 4), null, "c")),
            ("sdk-fault-output-shape-invalid", "outputs_incomplete", 9, RequireSuccess("Completed", null, new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1, [new WorkerUnavailableOutput("Another Output", text)], "sdk-output-getter-faulted", 2), null, "c"))
        };

        return shapes;
    }

    private static WorkerConnectionSnapshot Disconnected() => new(
        WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified, null, 1, 1,
        "spatial-analyzer-disconnected", DateTimeOffset.UnixEpoch);

    private static RpcException PolicyDenied()
    {
        var api = Server.GetType("Briosa.Server.Operations.SpatialAnalyzerApi", true)!;
        var operations = api.GetProperty("Operations", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        var policyType = Server.GetType("Briosa.Server.Security.OperationPolicy", true)!;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Briosa:Security:Operations:Profile"] = "read-only"
        }).Build();
        var policy = policyType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [configuration, operations, null, null])!;
        var decision = policyType.GetMethod("Evaluate", [typeof(string), Server.GetType("Briosa.Server.Security.OperationCallerRestriction", true)!])!
            .Invoke(policy, ["construction_operations.delete_points", null])!;
        var denial = decision.GetType().GetProperty("Denial")!.GetValue(decision)!;
        return RequireSuccess("PolicyDenied", "NotStarted", null, null, "operation-policy-denied", denial: denial);
    }

    private static RpcException RequireSuccess(string status, string? disposition, WorkerMpExecutionResult? execution,
        WorkerConnectionSnapshot? connection, string diagnosticCode, bool deadline = false, object? denial = null)
    {
        var outcome = disposition is null
            ? CompletedOutcome(execution!, diagnosticCode)
            : Activator.CreateInstance(Outcome, Enum.Parse(Status, status), Enum.Parse(Disposition, disposition),
                execution, connection, diagnosticCode, 3, Guid.Empty, denial)!;
        var contract = Activator.CreateInstance(Contract, "point_name_only", "Point Name Only", WorkerMpValueKind.Text)!;
        var contracts = Array.CreateInstance(Contract, 1);
        contracts.SetValue(contract, 0);
        try
        {
            Mapper.GetMethod("RequireSuccess", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, [outcome, OperationId, ReplaySafety.Safe, contracts, deadline]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is RpcException rpc)
        {
            return rpc;
        }

        throw new InvalidOperationException($"The mapper accepted the {status} shape.");
    }

    // A completed worker outcome must carry the disposition its execution proves.
    private static object CompletedOutcome(WorkerMpExecutionResult execution, string diagnosticCode)
    {
        foreach (var name in new[] { "NotStarted", "StartedOutcomeUnknown", "Completed" })
        {
            try
            {
                return Activator.CreateInstance(Outcome, Enum.Parse(Status, "Completed"), Enum.Parse(Disposition, name),
                    execution, null, diagnosticCode, 3, Guid.Empty, null)!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
            {
            }
        }

        throw new InvalidOperationException("No disposition fits the execution.");
    }

    private static RpcException Invoke(string method, params object?[] arguments)
    {
        var target = Mapper.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == method);
        return (RpcException)target.Invoke(null, arguments)!;
    }
}
