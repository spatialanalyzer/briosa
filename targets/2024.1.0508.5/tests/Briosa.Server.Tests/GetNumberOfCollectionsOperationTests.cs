using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class GetNumberOfCollectionsOperationTests
{
    [Fact]
    public void CommandMappingPreservesTheExactMpContract()
    {
        var command = GetNumberOfCollectionsOperation.CreateCommand(
            new Api.GetNumberOfCollectionsRequest());

        Assert.Equal(GetNumberOfCollectionsOperation.OperationId, command.OperationId);
        Assert.Equal("Get Number of Collections", command.StepName);
        Assert.Empty(command.InputArguments);

        var output = Assert.Single(command.OutputArguments);
        Assert.Equal("Total Count", output.Name);
        Assert.Equal(WorkerMpValueKind.WholeNumber, output.Kind);
        Assert.Equal("GetIntegerArg", output.SdkBinding);

        var outputContract = Assert.Single(
            GetNumberOfCollectionsOperation.OutputContracts);
        Assert.Equal("total_count", outputContract.FieldName);
        Assert.Equal(output.Name, outputContract.ArgumentName);
        Assert.Equal(output.Kind, outputContract.Kind);
    }

    [Fact]
    public void ResultMappingReturnsTheRetrievedCountAndExecutionDetails()
    {
        var execution = new WorkerMpExecutionResult(
            ExecuteStepReturned: true,
            MpResultRetrieved: true,
            MpSucceeded: true,
            MpResultCode: 2,
            DurationMilliseconds: 5,
            OutputValues:
            [
                new WorkerMpOutputValue(
                    "Total Count",
                    WorkerMpValueKind.WholeNumber,
                    Retrieved: true,
                    IntegerValue: 3)
            ],
            DiagnosticCode: "completed");
        var details = new Api.MpExecutionDetails
        {
            State = Api.MpExecutionState.Succeeded,
            MpResultCode = 2
        };

        var result = GetNumberOfCollectionsOperation.CreateResult(
            new SuccessfulOperationExecution(execution, details));

        Assert.True(result.HasTotalCount);
        Assert.Equal(3, result.TotalCount);
        Assert.Same(details, result.Execution);
    }
}
