using global::Briosa;
using Google.Protobuf;
using Api = global::Briosa;

namespace Briosa.Protocol.Tests;

public sealed partial class ProtocolSchemaTests
{
    [Fact]
    public void OperationResultsCarrySharedExecutionDetailsWithoutRenumberingOutputs()
    {
        var directory = Api.GetWorkingDirectoryResult.Descriptor
            .FindFieldByName("directory");
        var execution = Api.GetWorkingDirectoryResult.Descriptor
            .FindFieldByName("execution");

        Assert.Equal(1, directory.FieldNumber);
        Assert.Equal(1000, execution.FieldNumber);
        Assert.Equal(MpExecutionDetails.Descriptor, execution.MessageType);
    }

    [Fact]
    public void SuccessDetailsDistinguishRetrievedEmptyValuesFromMissingValues()
    {
        var result = new Api.GetWorkingDirectoryResult
        {
            Directory = string.Empty,
            Execution = new MpExecutionDetails
            {
                State = MpExecutionState.Succeeded,
                MpResultCode = 2
            }
        };
        result.Execution.OutputRetrievals.Add(new OutputRetrievalDetails
        {
            FieldName = "directory",
            State = OutputRetrievalState.Retrieved
        });

        Assert.True(result.HasDirectory);
        Assert.NotNull(result.Execution);
        Assert.True(result.Execution.HasMpResultCode);
        Assert.Equal(
            OutputRetrievalState.Retrieved,
            Assert.Single(result.Execution.OutputRetrievals).State);

        result.ClearDirectory();
        Assert.False(result.HasDirectory);
        Assert.NotNull(result.Execution);
    }

    [Fact]
    public void TypedErrorDetailContainsOutcomesButNoOperationValues()
    {
        var error = new OperationError
        {
            OperationId = "file_operations.get_working_directory",
            Kind = OperationFailureKind.OutputRetrievalFailure,
            DiagnosticCode = "sdk-output-retrieval-failed",
            ExecutionDisposition = ExecutionDisposition.Completed,
            RecoveryGuidance = RecoveryGuidance.None,
            ReplayGuidance = ReplayGuidance.DoNotReplay,
            ReplaySafety = ReplaySafety.Safe,
            WorkerGeneration = 2,
            MpExecution = new MpExecutionDetails
            {
                State = MpExecutionState.Succeeded,
                MpResultCode = 2
            }
        };
        error.MpExecution.OutputRetrievals.Add(new OutputRetrievalDetails
        {
            FieldName = "directory",
            State = OutputRetrievalState.Failed,
            DiagnosticCode = "sdk-output-retrieval-failed"
        });

        var roundTrip = OperationError.Parser.ParseFrom(error.ToByteArray());

        Assert.Equal(error, roundTrip);
        Assert.DoesNotContain("directory_value", OperationError.Descriptor.Fields
            .InFieldNumberOrder()
            .Select(field => field.Name));
    }
}
