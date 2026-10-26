using System.Reflection;
using global::Briosa;
using Briosa.Server.Operations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;

namespace Briosa.Server.Tests;

// #351: MP code 4 (PARTIAL SUCCESS) is a completed success that says it was
// partial, except for the reviewed operations where code 4 leaves the work undone.
public sealed class MpPartialSuccessTests
{
    private const string OperationId = "file_operations.get_working_directory";
    private static readonly OperationOutputContract[] Outputs =
    [
        new("directory", "Directory", WorkerMpValueKind.Text)
    ];

    private static readonly string[] ReviewedExceptions =
    [
        "construction_operations.construct_point_from_survey_target_center",
        "file_operations.open_sa_file",
        "file_operations.open_template_file",
        "reporting_operations.set_report_options_for_object"
    ];

    public static TheoryData<string> Exceptions() => [.. ReviewedExceptions];

    [Fact]
    public void PartialSuccessReturnsItsOutputsAsACompletedSuccess()
    {
        var execution = OverPrivateChannel(new WorkerMpResultAvailable(4, 5, [Directory()], null));

        var completed = GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(execution), OperationId, ReplaySafety.Safe, Outputs, callerDeadlineExceeded: false);

        Assert.Equal(MpExecutionState.PartialSuccess, completed.Details.State);
        Assert.True(completed.Details.HasMpResultCode);
        Assert.Equal(4, completed.Details.MpResultCode);
        var retrieval = Assert.Single(completed.Details.OutputRetrievals);
        Assert.Equal("directory", retrieval.FieldName);
        Assert.Equal(OutputRetrievalState.Retrieved, retrieval.State);
        Assert.Equal("C:\\work", Assert.IsType<WorkerTextValue>(Assert.Single(completed.Execution.OutputValues).ReadValue()).Value);
    }

    [Fact]
    public void PartialSuccessWithoutOutputsIsACompletedSuccess()
    {
        var completed = GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(new WorkerMpResultAvailable(4, 5, [], null)),
            "cloud_and_mesh_operations.consolidate_mesh",
            ReplaySafety.Unsafe,
            [],
            callerDeadlineExceeded: false);

        Assert.Equal(MpExecutionState.PartialSuccess, completed.Details.State);
        Assert.Equal(4, completed.Details.MpResultCode);
        Assert.Empty(completed.Details.OutputRetrievals);
    }

    [Fact]
    public void FullSuccessStillReportsSucceeded()
    {
        var completed = GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(new WorkerMpResultAvailable(2, 5, [Directory()], null)),
            OperationId, ReplaySafety.Safe, Outputs, callerDeadlineExceeded: false);

        Assert.Equal(MpExecutionState.Succeeded, completed.Details.State);
        Assert.Equal(2, completed.Details.MpResultCode);
    }

    [Theory]
    [MemberData(nameof(Exceptions))]
    public void ListedOperationsKeepTheMpFailureMappingForCodeFour(string operationId)
    {
        var exception = Assert.Throws<RpcException>(() => GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(new WorkerMpResultAvailable(4, 5, [], null)),
            operationId, ReplaySafety.Unsafe, [], callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.FailedPrecondition, exception.StatusCode);
        Assert.Equal(OperationFailureKind.MpFailure, error.Kind);
        Assert.Equal("mp-command-failed", error.DiagnosticCode);
        Assert.Equal(ExecutionDisposition.Completed, error.ExecutionDisposition);
        Assert.Equal(RecoveryGuidance.None, error.RecoveryGuidance);
        Assert.Equal(ReplayGuidance.DoNotReplay, error.ReplayGuidance);
        Assert.Equal(MpExecutionState.Failed, error.MpExecution.State);
        Assert.Equal(4, error.MpExecution.MpResultCode);
        Assert.Empty(error.MpExecution.OutputRetrievals);
    }

    [Theory]
    [MemberData(nameof(Exceptions))]
    public void ListedOperationsStillSucceedWithCodeTwo(string operationId)
    {
        var completed = GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(new WorkerMpResultAvailable(2, 5, [], null)),
            operationId, ReplaySafety.Unsafe, [], callerDeadlineExceeded: false);

        Assert.Equal(MpExecutionState.Succeeded, completed.Details.State);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(42)]
    public void OtherNonSuccessCodesStillFail(int resultCode)
    {
        var exception = Assert.Throws<RpcException>(() => GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(new WorkerMpResultAvailable(resultCode, 5, [], "mp-command-failed")),
            OperationId, ReplaySafety.Safe, Outputs, callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(OperationFailureKind.MpFailure, error.Kind);
        Assert.Equal(MpExecutionState.Failed, error.MpExecution.State);
        Assert.Equal(resultCode, error.MpExecution.MpResultCode);
        Assert.Equal(OutputRetrievalState.NotAttempted, Assert.Single(error.MpExecution.OutputRetrievals).State);
    }

    [Fact]
    public void TheExceptionListIsTheReviewedTable()
    {
        Assert.Equal(ReviewedExceptions, MpPartialSuccessExceptions.OperationIds.Order(StringComparer.Ordinal));
        Assert.All(ReviewedExceptions, id => Assert.True(MpPartialSuccessExceptions.ReportsFailure(id)));
        Assert.False(MpPartialSuccessExceptions.ReportsFailure(OperationId));
        Assert.False(MpPartialSuccessExceptions.ReportsFailure("instrument_operations.verify_instrument_connection"));
    }

    // The worker retrieves outputs after code 2 or 4 without knowing the operation,
    // so an operation that reports code 4 as a failure must have none to retrieve.
    [Theory]
    [MemberData(nameof(Exceptions))]
    public void EveryListedOperationIsRegisteredAndHasNoOutputs(string operationId)
    {
        Assert.Contains(SpatialAnalyzerApi.Operations, operation => operation.OperationId == operationId);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var operationType = Assert.Single(
            typeof(SpatialAnalyzerApi).Assembly.GetTypes(),
            type => type.GetProperty("Descriptor", flags)?.GetValue(null) is OperationDescriptor descriptor &&
                descriptor.OperationId == operationId);
        var outputs = Assert.IsAssignableFrom<IEnumerable<OperationOutputContract>>(
            operationType.GetProperty("OutputContracts", flags)!.GetValue(null));
        Assert.Empty(outputs);
    }

    [Fact]
    public void OutputRetrievalFailureAfterPartialSuccessKeepsTheCode()
    {
        var exception = Assert.Throws<RpcException>(() => GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(new WorkerMpResultAvailable(4, 5,
                [new WorkerUnavailableOutput("Directory", WorkerMpValueKind.Text)], "sdk-output-retrieval-failed")),
            OperationId, ReplaySafety.Safe, Outputs, callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        Assert.Equal(OperationFailureKind.OutputRetrievalFailure, error.Kind);
        Assert.Equal(ExecutionDisposition.Completed, error.ExecutionDisposition);
        Assert.Equal(ReplayGuidance.DoNotReplay, error.ReplayGuidance);
        Assert.Equal(MpExecutionState.PartialSuccess, error.MpExecution.State);
        Assert.Equal(4, error.MpExecution.MpResultCode);
        Assert.Equal(OutputRetrievalState.Failed, Assert.Single(error.MpExecution.OutputRetrievals).State);
    }

    [Fact]
    public void UndeliverableOutputsAfterPartialSuccessKeepTheCode()
    {
        var correlationId = Guid.NewGuid();
        var response = new WorkerExecutionResponse(
            WorkerExecutionResponseStatus.Completed,
            new WorkerMpResultAvailable(4, 5,
                [new WorkerRetrievedOutput("Directory", WorkerMpValueKind.Text,
                    new WorkerTextValue(new string('x', WorkerControlProtocol.MaximumMessageBytes + 1)))], null),
            Connection(),
            DiagnosticCode: null);
        var delivered = Deliver(WorkerExecutionDelivery.Undeliverable(correlationId, response));
        Assert.Equal(4, Assert.IsType<WorkerMpOutputsUnavailable>(delivered).ResultCode);

        var exception = Assert.Throws<RpcException>(() => GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(delivered), OperationId, ReplaySafety.Safe, Outputs, callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        Assert.Equal(OperationFailureKind.OutputRetrievalFailure, error.Kind);
        Assert.Equal("worker-output-encoding-rejected", error.DiagnosticCode);
        Assert.Equal(MpExecutionState.PartialSuccess, error.MpExecution.State);
        Assert.Equal(4, error.MpExecution.MpResultCode);
    }

    [Theory]
    [InlineData(2, MpExecutionState.Succeeded)]
    [InlineData(4, MpExecutionState.PartialSuccess)]
    public void OutputGetterFaultKeepsTheRetrievedCode(int resultCode, MpExecutionState expectedState)
    {
        var fault = OverPrivateChannel(new WorkerSdkCallFaulted(
            WorkerSdkCallPhase.OutputGetter,
            5,
            [new WorkerUnavailableOutput("Directory", WorkerMpValueKind.Text,
                WorkerSdkFaultDiagnosticCodes.OutputGetter, WorkerUnavailableOutputReason.SdkCallFaulted)],
            WorkerSdkFaultDiagnosticCodes.OutputGetter,
            resultCode));

        var exception = Assert.Throws<RpcException>(() => GrpcOperationOutcomeMapper.RequireSuccess(
            Completed(fault), OperationId, ReplaySafety.Safe, Outputs, callerDeadlineExceeded: false));
        var error = Error(exception);

        Assert.Equal(StatusCode.DataLoss, exception.StatusCode);
        Assert.Equal(OperationFailureKind.SdkCallFaulted, error.Kind);
        Assert.Equal(ExecutionDisposition.Completed, error.ExecutionDisposition);
        Assert.Equal(expectedState, error.MpExecution.State);
        Assert.Equal(resultCode, error.MpExecution.MpResultCode);
    }

    [Fact]
    public void AuditReportsPartialSuccessSeparately()
    {
        var partial = OperationAuditSummary.Create(Completed(new WorkerMpResultAvailable(4, 5, [Directory()], null)), OperationId);
        var listed = OperationAuditSummary.Create(
            Completed(new WorkerMpResultAvailable(4, 5, [], null)), "file_operations.open_sa_file");
        var getterFault = OperationAuditSummary.Create(Completed(new WorkerSdkCallFaulted(
            WorkerSdkCallPhase.OutputGetter, 5, [], WorkerSdkFaultDiagnosticCodes.OutputGetter, 4)), OperationId);
        var succeeded = OperationAuditSummary.Create(Completed(new WorkerMpResultAvailable(2, 5, [Directory()], null)), OperationId);

        Assert.Equal(("partial_success", "retrieved", 4), (partial.MpOutcome, partial.OutputRetrievalOutcome, partial.MpResultCode));
        Assert.Equal(("failed", "not_attempted", 4), (listed.MpOutcome, listed.OutputRetrievalOutcome, listed.MpResultCode));
        Assert.Equal(("partial_success", "sdk_call_faulted", 4),
            (getterFault.MpOutcome, getterFault.OutputRetrievalOutcome, getterFault.MpResultCode));
        Assert.Equal(("succeeded", "retrieved", 2), (succeeded.MpOutcome, succeeded.OutputRetrievalOutcome, succeeded.MpResultCode));
    }

    private static WorkerRetrievedOutput Directory() =>
        new("Directory", WorkerMpValueKind.Text, new WorkerTextValue("C:\\work"));

    private static WorkerExecutionOutcome Completed(WorkerMpExecutionResult execution) =>
        WorkerExecutionOutcome.FromWorkerResponse(
            new WorkerExecutionResponse(WorkerExecutionResponseStatus.Completed, execution, Connection(), DiagnosticCode: null),
            generation: 7,
            Guid.NewGuid());

    private static WorkerMpExecutionResult OverPrivateChannel(WorkerMpExecutionResult execution) =>
        Deliver(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new WorkerExecutionResponse(
            WorkerExecutionResponseStatus.Completed, execution, Connection(), DiagnosticCode: null)));

    private static WorkerMpExecutionResult Deliver(WorkerControlMessage message)
    {
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(message);
        stream.Position = 0;
        return channel.Receive().ExecutionResponse!.Execution!;
    }

    private static WorkerConnectionSnapshot Connection() =>
        new(
            WorkerConnectionState.Connected,
            WorkerExecutionReadinessState.ExecutionReady,
            StatusCode: 0,
            Attempt: 1,
            MaximumAttempts: 3,
            DiagnosticCode: "sdk-connected",
            DateTimeOffset.UnixEpoch);

    private static OperationError Error(RpcException exception)
    {
        var entry = Assert.Single(
            exception.Trailers,
            item => item.Key == GrpcOperationOutcomeMapper.ErrorTrailerName);
        return OperationError.Parser.ParseFrom(entry.ValueBytes);
    }
}
