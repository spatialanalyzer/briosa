using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGetGdtExtendedOptionsTests
{
    [Fact]
    public void MapsBooleanOutputAndRemovesDynamicContract()
    {
        var command = GetGdtExtendedOptionsOperation.CreateCommand(new Api.GetGdtExtendedOptionsRequest());
        Assert.Empty(command.InputArguments);
        Assert.Equal("GetBoolArg", Assert.Single(command.OutputArguments).SdkBinding);
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1,
            [new WorkerRetrievedOutput("Use Extended Options", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(true))], "completed");
        var details = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 };
        var result = GetGdtExtendedOptionsOperation.CreateResult(new SuccessfulOperationExecution(execution, details));
        Assert.True(result.UseExtendedOptions);
        Assert.Same(details, result.Execution);

        var id = GetGdtExtendedOptionsOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }
}
