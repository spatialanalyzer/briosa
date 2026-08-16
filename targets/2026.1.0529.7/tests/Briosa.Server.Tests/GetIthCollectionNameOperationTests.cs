using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class GetIthCollectionNameOperationTests
{
    [Fact]
    public void CommandMappingPreservesTheExactMpContract()
    {
        var command = GetIthCollectionNameOperation.CreateCommand(
            new Api.GetIthCollectionNameRequest
            {
                CollectionIndex = 0
            });

        Assert.Equal(GetIthCollectionNameOperation.OperationId, command.OperationId);
        Assert.Equal("Get i-th Collection Name", command.StepName);

        var input = Assert.Single(command.InputArguments);
        Assert.Equal("Collection Index", input.Name);
        Assert.Equal(WorkerMpValueKind.WholeNumber, input.Kind);
        Assert.Equal(0, input.IntegerValue);
        Assert.Equal("SetIntegerArg", input.SdkBinding);

        var output = Assert.Single(command.OutputArguments);
        Assert.Equal("Resultant Name", output.Name);
        Assert.Equal(WorkerMpValueKind.CollectionName, output.Kind);
        Assert.Equal("GetCollectionNameArg", output.SdkBinding);

        var outputContract = Assert.Single(
            GetIthCollectionNameOperation.OutputContracts);
        Assert.Equal("resultant_name", outputContract.FieldName);
        Assert.Equal(output.Name, outputContract.ArgumentName);
        Assert.Equal(output.Kind, outputContract.Kind);
    }

    [Fact]
    public void CommandMappingRequiresCollectionIndexPresence()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            GetIthCollectionNameOperation.CreateCommand(
                new Api.GetIthCollectionNameRequest()));

        Assert.Contains(
            "Collection Index",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ResultMappingReturnsTheRetrievedCollectionNameAndExecutionDetails()
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
                    "Resultant Name",
                    WorkerMpValueKind.CollectionName,
                    Retrieved: true,
                    StringValue: "Collection 1")
            ],
            DiagnosticCode: "completed");
        var details = new Api.MpExecutionDetails
        {
            State = Api.MpExecutionState.Succeeded,
            MpResultCode = 2
        };

        var result = GetIthCollectionNameOperation.CreateResult(
            new SuccessfulOperationExecution(execution, details));

        Assert.True(result.HasResultantName);
        Assert.Equal("Collection 1", result.ResultantName);
        Assert.Same(details, result.Execution);
    }
}
