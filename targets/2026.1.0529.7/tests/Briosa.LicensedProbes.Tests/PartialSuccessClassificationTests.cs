using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.LicensedProbes.Tests;

// #351: both probe paths classify MP code 4 as the shipped server reports it.
public sealed class PartialSuccessClassificationTests
{
    private static readonly WorkerRetrievedOutput RetrievedCount =
        new("Total Count", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(4));

    [Fact]
    public void PublicPartialSuccessIsASuccessThatKeepsItsCode()
    {
        var execution = new Api.MpExecutionDetails { State = Api.MpExecutionState.PartialSuccess, MpResultCode = 4 };
        execution.OutputRetrievals.Add(new Api.OutputRetrievalDetails { FieldName = "points_count", State = Api.OutputRetrievalState.Retrieved });

        var outcome = PublicOutcomes.FromSuccess(execution, Observations.Of(("observed", "true")));

        Assert.Equal(ProbeOutcomeKind.Succeeded, outcome.Kind);
        Assert.Equal(4, outcome.MpResultCode);
        Assert.True(outcome.ExecuteStepReturned);
        Assert.True(outcome.MpResultRetrieved);
        Assert.Equal("true", outcome.Observations["observed"]);
    }

    [Fact]
    public void PublicOutputFailureAfterPartialSuccessKeepsTheRetrievalEvidence()
    {
        var error = new Api.OperationError
        {
            Kind = Api.OperationFailureKind.OutputRetrievalFailure,
            ExecutionDisposition = Api.ExecutionDisposition.Completed,
            DiagnosticCode = "sdk-output-retrieval-failed",
            MpExecution = new Api.MpExecutionDetails { State = Api.MpExecutionState.PartialSuccess, MpResultCode = 4 }
        };
        var trailers = new Metadata { { PublicOutcomes.ErrorTrailerName, error.ToByteArray() } };

        var outcome = PublicOutcomes.FromRpcException(new RpcException(new Status(StatusCode.DataLoss, "detail"), trailers));

        Assert.Equal(ProbeOutcomeKind.OutputRetrievalFailed, outcome.Kind);
        Assert.Equal(4, outcome.MpResultCode);
        Assert.True(outcome.ExecuteStepReturned);
        Assert.True(outcome.MpResultRetrieved);
    }

    [Fact]
    public void WorkerPartialSuccessIsASuccessThatKeepsItsCode()
    {
        var outcome = WorkerOutcomes.FromResponse(
            Completed(new WorkerMpResultAvailable(4, 1, [RetrievedCount], null)),
            static _ => Observations.Of(("observed", "true")),
            "analysis_operations.fit_geometry_to_point_group");

        Assert.Equal(ProbeOutcomeKind.Succeeded, outcome.Kind);
        Assert.Equal("Completed", outcome.ExecutionDisposition);
        Assert.Equal(4, outcome.MpResultCode);
        Assert.Equal("true", outcome.Observations["observed"]);
    }

    [Fact]
    public void WorkerCodeFourFromAListedOperationIsAnMpFailure()
    {
        var outcome = WorkerOutcomes.FromResponse(
            Completed(new WorkerMpResultAvailable(4, 1, [], null)),
            static _ => Observations.Of(("observed", "true")),
            "file_operations.open_sa_file");

        Assert.Equal(ProbeOutcomeKind.MpFailed, outcome.Kind);
        Assert.Equal("Completed", outcome.ExecutionDisposition);
        Assert.Equal(4, outcome.MpResultCode);
        Assert.Empty(outcome.Observations);
    }

    [Fact]
    public void WorkerOutputFailureAfterPartialSuccessIsAnOutputRetrievalFailure()
    {
        var outcome = WorkerOutcomes.FromResponse(
            Completed(new WorkerMpResultAvailable(4, 1,
                [RetrievedCount, new WorkerUnavailableOutput("Other", WorkerMpValueKind.WholeNumber)], "sdk-output-retrieval-failed")),
            static _ => Observations.Of(("observed", "true")),
            "analysis_operations.fit_geometry_to_point_group");

        Assert.Equal(ProbeOutcomeKind.OutputRetrievalFailed, outcome.Kind);
        Assert.Equal(4, outcome.MpResultCode);
    }

    private static WorkerExecutionResponse Completed(WorkerMpExecutionResult execution) => new(
        WorkerExecutionResponseStatus.Completed, execution,
        new WorkerConnectionSnapshot(WorkerConnectionState.Connected, WorkerExecutionReadinessState.ExecutionReady, 0, 1, 1,
            "connect-ex-connected", DateTimeOffset.UnixEpoch),
        null);
}
