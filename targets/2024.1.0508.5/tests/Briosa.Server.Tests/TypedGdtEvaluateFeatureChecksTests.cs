using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtEvaluateFeatureChecksTests
{
    [Fact]
    public void MapsObjectListBindingDefaultsAndTypedCounts()
    {
        var request = new Api.EvaluateFeatureChecksRequest();
        request.FeatureCheckList.Add(new Api.CollectionItemName { CollectionName = "C", ItemName = "FC" });
        var command = EvaluateFeatureChecksOperation.CreateCommand(request);

        Assert.Equal(["SetCollectionObjectNameRefListArg", "SetBoolArg", "SetBoolArg"],
            command.InputArguments.Select(x => x.SdkBinding));
        var item = Assert.Single(command.InputArguments[0]
            .RequireValue<WorkerCollectionObjectNameListValue>().Values);
        Assert.Equal("FC", item.ObjectName);
        Assert.Equal(WorkerObjectTypeValue.Any, item.ObjectType);
        Assert.Equal([false, false], command.InputArguments.Skip(1)
            .Select(x => x.RequireValue<WorkerBooleanValue>().Value));
        Assert.Equal(["GetIntegerArg", "GetIntegerArg", "GetIntegerArg"],
            command.OutputArguments.Select(x => x.SdkBinding));

        WorkerRetrievedOutput[] outputs =
        [
            new("Total Passed", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(3)),
            new("Total Failed", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(2)),
            new("Total Incomplete", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(1))
        ];
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
        var details = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 };
        var result = EvaluateFeatureChecksOperation.CreateResult(new SuccessfulOperationExecution(execution, details));
        Assert.Equal([3, 2, 1], [result.TotalPassed, result.TotalFailed, result.TotalIncomplete]);
        Assert.Same(details, result.Execution);
        Assert.Throws<ArgumentException>(() => EvaluateFeatureChecksOperation.CreateCommand(new()));

        var id = EvaluateFeatureChecksOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }
}
