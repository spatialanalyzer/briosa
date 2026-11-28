using Briosa.Worker.Control;
using Grpc.Core;

namespace Briosa.Cli.Tests;

/// <summary>
/// One typed failure of each kind and path the server can return for an MP call to
/// <see cref="OperationId"/>, produced by the server's own mapper and executor, with
/// the operation's real three output contracts, rather than written by hand. These
/// go through the real CLI; <see cref="MapperEnumeration"/> covers every combination
/// in-process.
/// </summary>
internal static class ServerMapperShapes
{
    public const string OperationId = "analysis_operations.get_ith_point_from_group";

    /// <summary>The shape's label, its expected CLI outcome, and the server's exception.</summary>
    public static IReadOnlyList<(string Label, string Outcome, int ExitCode, RpcException Exception)> All { get; } = Build();

    private static List<(string, string, int, RpcException)> Build()
    {
        var operation = GenuineResponses.Operation(OperationId);
        var contracts = operation.Contracts;
        var retrieved = GenuineResponses.Retrieved(operation);
        WorkerMpOutputValue[] FirstUnavailable(WorkerUnavailableOutputReason reason = WorkerUnavailableOutputReason.None) =>
            [GenuineResponses.Unavailable(contracts[0], reason), .. retrieved.Skip(1)];
        var wrongShape = new WorkerRetrievedOutput("Another Output", WorkerMpValueKind.Text, new WorkerTextValue("P1"));
        RpcException Completed(WorkerMpExecutionResult execution) => GenuineResponses.Map(operation, execution).Error!;
        RpcException Transport(string status, string disposition, string code, WorkerConnectionSnapshot? connection = null,
            bool deadline = false, object? denial = null) =>
            GenuineResponses.MapTransport(operation, status, disposition, connection, code, deadline, denial)!.Error!;
        var success = GenuineResponses.Complete(operation, 2).Details!;
        return
        [
            ("caller-restriction-invalid", "invalid_request", 7,
                GenuineResponses.Validation(operation, "caller-restriction-invalid", ReplaySafety.Safe, null).Error!),
            ("request-validation-with-field", "invalid_request", 7,
                GenuineResponses.Validation(operation, "request-validation-failed", ReplaySafety.Safe,
                    new RequestValidationDetails { FieldPath = "group_name.object_name", Constraint = RequestFieldConstraint.NotBlank }).Error!),
            ("request-violation-after-mapping", "started_outcome_unknown", 11,
                GenuineResponses.RequestViolationAfterMapping(operation, ReplaySafety.Safe).Error!),
            ("result-mapping-failed", "outputs_incomplete", 9,
                GenuineResponses.ResultMappingFailure(operation, ReplaySafety.Safe, success).Error!),
            ("overloaded", "not_started", 6, Transport("Overloaded", "NotStarted", "worker-admission-full")),
            ("request-rejected", "invalid_request", 7, Transport("RequestRejected", "NotStarted", "request-encoding-rejected")),
            ("policy-denied", "not_started", 6,
                Transport("PolicyDenied", "NotStarted", "operation-policy-denied", denial: GenuineResponses.PolicyDenials[0])),
            ("unsupported-from-worker", "not_started", 6, Transport("Unsupported", "NotStarted", "operation-unsupported")),
            ("caller-cancelled-queued", "not_started", 6, Transport("ClientCancelled", "NotStarted", "queued-request-abandoned")),
            ("caller-cancelled-claimed", "started_outcome_unknown", 11,
                Transport("ClientCancelled", "StartedOutcomeUnknown", "client-wait-cancelled")),
            ("caller-deadline-queued", "not_started", 6,
                Transport("ClientCancelled", "NotStarted", "queued-request-abandoned", deadline: true)),
            ("caller-deadline-claimed", "started_outcome_unknown", 11,
                Transport("ClientCancelled", "StartedOutcomeUnknown", "client-wait-cancelled", deadline: true)),
            ("watchdog-timeout", "started_outcome_unknown", 11,
                Transport("WatchdogTimeout", "StartedOutcomeUnknown", "worker-watchdog-timeout")),
            ("watchdog-modal-dialog", "started_outcome_unknown", 11,
                Transport("WatchdogTimeout", "StartedOutcomeUnknown", "sa-modal-dialog-open")),
            ("worker-failure", "started_outcome_unknown", 11, Transport("WorkerFailure", "StartedOutcomeUnknown", "worker-process-exited")),
            ("worker-unavailable-not-started", "not_started", 6, Transport("Unavailable", "NotStarted", "worker-not-ready")),
            ("worker-unavailable-unknown", "started_outcome_unknown", 11,
                Transport("Unavailable", "StartedOutcomeUnknown", "worker-response-lost")),
            ("sa-unavailable-not-started", "not_started", 6,
                Transport("Unavailable", "NotStarted", "spatial-analyzer-disconnected", Disconnected())),
            ("sa-unavailable-unknown", "started_outcome_unknown", 11,
                Transport("Unavailable", "StartedOutcomeUnknown", "spatial-analyzer-disconnected", Disconnected())),
            ("sa-operator-recovery", "not_started", 6,
                Transport("Unavailable", "NotStarted", "spatial-analyzer-operator-recovery", OperatorRecovery())),
            ("arguments-rejected", "not_started", 6, Completed(new WorkerArgumentsRejected(1, "sdk-argument-rejected"))),
            ("execute-step-rejected", "started_outcome_unknown", 11, Completed(new WorkerExecuteRejected(1, "execute-step-rejected"))),
            ("mp-result-unavailable", "started_outcome_unknown", 11, Completed(new WorkerMpResultUnavailable(1, null))),
            ("mp-failure", "mp_failed", 8, Completed(new WorkerMpResultAvailable(0, 1, [], null))),
            ("mp-failure-code-3", "mp_failed", 8, Completed(new WorkerMpResultAvailable(3, 1, [], null))),
            ("outputs-unavailable", "outputs_incomplete", 9, Completed(new WorkerMpOutputsUnavailable(2, 1, null))),
            ("outputs-unavailable-partial", "outputs_incomplete", 9, Completed(new WorkerMpOutputsUnavailable(4, 1, null))),
            ("output-shape-invalid", "outputs_incomplete", 9, Completed(new WorkerMpResultAvailable(2, 1, [wrongShape], null))),
            ("output-retrieval-failed", "outputs_incomplete", 9, Completed(new WorkerMpResultAvailable(2, 1, FirstUnavailable(), null))),
            ("output-retrieval-failed-partial", "outputs_incomplete", 9,
                Completed(new WorkerMpResultAvailable(4, 1, FirstUnavailable(), null))),
            ("sdk-fault-before-execute", "not_started", 6,
                Completed(new WorkerSdkCallFaulted(WorkerSdkCallPhase.BeforeExecute, 1, [], "sdk-call-faulted-before-execute"))),
            ("sdk-fault-execute-step", "started_outcome_unknown", 11,
                Completed(new WorkerSdkCallFaulted(WorkerSdkCallPhase.ExecuteStep, 1, [], "sdk-execute-step-faulted"))),
            ("sdk-fault-result-retrieval", "started_outcome_unknown", 11,
                Completed(new WorkerSdkCallFaulted(WorkerSdkCallPhase.MpResultRetrieval, 1, [], "sdk-mp-result-retrieval-faulted"))),
            ("sdk-fault-output-getter", "outputs_incomplete", 9,
                Completed(new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1,
                    FirstUnavailable(WorkerUnavailableOutputReason.SdkCallFaulted), "sdk-output-getter-faulted", 2))),
            ("sdk-fault-output-getter-partial", "outputs_incomplete", 9,
                Completed(new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1, [], "sdk-output-getter-faulted", 4))),
            ("sdk-fault-output-shape-invalid", "outputs_incomplete", 9,
                Completed(new WorkerSdkCallFaulted(WorkerSdkCallPhase.OutputGetter, 1,
                    [new WorkerUnavailableOutput("Another Output", WorkerMpValueKind.Text)], "sdk-output-getter-faulted", 2)))
        ];
    }

    internal static WorkerConnectionSnapshot Disconnected() => new(
        WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified, null, 1, 1,
        "spatial-analyzer-disconnected", DateTimeOffset.UnixEpoch);

    internal static WorkerConnectionSnapshot OperatorRecovery() => new(
        WorkerConnectionState.Connected, WorkerExecutionReadinessState.OperatorRecoveryRequired, null, 1, 1,
        "spatial-analyzer-operator-recovery", DateTimeOffset.UnixEpoch);
}
