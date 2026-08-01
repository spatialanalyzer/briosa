using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class ActiveContextOperationTests
{
    [Fact]
    public void ActiveContextDescriptorsAreReviewedReadOnlyGlobalStateReads()
    {
        var descriptors = new[]
        {
            GetActiveCollectionNameOperation.Descriptor,
            GetActiveUnitsOperation.Descriptor,
            GetWorkingFramePropertiesOperation.Descriptor
        };

        Assert.Equal(
            [
                "/briosa.ConstructionOperations/GetActiveCollectionName",
                "/briosa.UtilityOperations/GetActiveUnits",
                "/briosa.UtilityOperations/GetWorkingFrameProperties"
            ],
            descriptors.Select(descriptor => descriptor.FullyQualifiedMethod));
        Assert.All(descriptors, descriptor =>
        {
            Assert.Equal("read_only", descriptor.Effect);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateRead, descriptor.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Safe, descriptor.ReplaySafety);
            Assert.Empty(descriptor.RiskFlags);
        });
    }

    [Fact]
    public void GetActiveCollectionNamePreservesTheExactTargetStringGetter()
    {
        var command = GetActiveCollectionNameOperation.CreateCommand(
            new Api.GetActiveCollectionNameRequest());

        Assert.Equal(
            "construction_operations.get_active_collection_name",
            command.OperationId);
        Assert.Equal("Get Active Collection Name", command.StepName);
        Assert.Empty(command.InputArguments);
        var output = Assert.Single(command.OutputArguments);
        Assert.Equal("Currently Active Collection Name", output.Name);
        Assert.Equal(WorkerMpValueKind.Text, output.Kind);
        Assert.Equal("GetStringArg", output.SdkBinding);
        AssertContractMatches(
            GetActiveCollectionNameOperation.OutputContracts,
            command.OutputArguments,
            ["currently_active_collection_name"]);
    }

    [Fact]
    public void GetActiveUnitsPreservesTheExactTargetGetterOrder()
    {
        var command = GetActiveUnitsOperation.CreateCommand(
            new Api.GetActiveUnitsRequest());

        Assert.Equal("utility_operations.get_active_units", command.OperationId);
        Assert.Equal("Get Active Units", command.StepName);
        Assert.Empty(command.InputArguments);
        Assert.Equal(
            ["Length", "Angular", "Temperature"],
            command.OutputArguments.Select(output => output.Name));
        Assert.All(command.OutputArguments, output =>
        {
            Assert.Equal(WorkerMpValueKind.Text, output.Kind);
            Assert.Equal("GetStringArg", output.SdkBinding);
        });
        AssertContractMatches(
            GetActiveUnitsOperation.OutputContracts,
            command.OutputArguments,
            ["length", "angular", "temperature"]);
    }

    [Fact]
    public void GetWorkingFramePropertiesPreservesTheExactTargetGetterOrder()
    {
        var command = GetWorkingFramePropertiesOperation.CreateCommand(
            new Api.GetWorkingFramePropertiesRequest());

        Assert.Equal(
            "utility_operations.get_working_frame_properties",
            command.OperationId);
        Assert.Equal("Get Working Frame Properties", command.StepName);
        Assert.Empty(command.InputArguments);
        Assert.Equal(
            ["Frame Name", "Collection Name", "Working Frame"],
            command.OutputArguments.Select(output => output.Name));
        Assert.Equal(
            [
                WorkerMpValueKind.Text,
                WorkerMpValueKind.Text,
                WorkerMpValueKind.CollectionObjectName
            ],
            command.OutputArguments.Select(output => output.Kind));
        Assert.Equal(
            ["GetStringArg", "GetStringArg", "GetCollectionObjectNameArg"],
            command.OutputArguments.Select(output => output.SdkBinding));
        Assert.Equal(
            WorkerObjectTypeValue.Frame,
            command.OutputArguments[2].ObjectTypeWhenOmitted);
        AssertContractMatches(
            GetWorkingFramePropertiesOperation.OutputContracts,
            command.OutputArguments,
            ["frame_name", "collection_name", "working_frame"]);
    }

    [Fact]
    public void ActiveContextResultsPreserveValuesAndExecutionDetails()
    {
        var details = new Api.MpExecutionDetails
        {
            State = Api.MpExecutionState.Succeeded,
            MpResultCode = 2
        };

        var collectionResult = GetActiveCollectionNameOperation.CreateResult(
            Successful(
                details,
                new WorkerMpOutputValue(
                    "Currently Active Collection Name",
                    WorkerMpValueKind.Text,
                    Retrieved: true,
                    StringValue: "Active")));
        Assert.True(collectionResult.HasCurrentlyActiveCollectionName);
        Assert.Equal("Active", collectionResult.CurrentlyActiveCollectionName);
        Assert.Same(details, collectionResult.Execution);

        var unitsResult = GetActiveUnitsOperation.CreateResult(
            Successful(
                details,
                Text("Length", "millimeters"),
                Text("Angular", "degrees"),
                Text("Temperature", "Celsius")));
        Assert.True(unitsResult.HasLength);
        Assert.True(unitsResult.HasAngular);
        Assert.True(unitsResult.HasTemperature);
        Assert.Equal("millimeters", unitsResult.Length);
        Assert.Equal("degrees", unitsResult.Angular);
        Assert.Equal("Celsius", unitsResult.Temperature);
        Assert.Same(details, unitsResult.Execution);

        var frameResult = GetWorkingFramePropertiesOperation.CreateResult(
            Successful(
                details,
                Text("Frame Name", "World"),
                Text("Collection Name", "Frames"),
                new WorkerMpOutputValue(
                    "Working Frame",
                    WorkerMpValueKind.CollectionObjectName,
                    Retrieved: true,
                    CollectionObjectNameValue: new WorkerCollectionObjectNameValue(
                        "Frames",
                        "World",
                        WorkerObjectTypeValue.Frame))));
        Assert.True(frameResult.HasFrameName);
        Assert.True(frameResult.HasCollectionName);
        Assert.NotNull(frameResult.WorkingFrame);
        Assert.True(frameResult.WorkingFrame.HasCollectionName);
        Assert.True(frameResult.WorkingFrame.HasObjectName);
        Assert.Equal(Api.ObjectType.Frame, frameResult.WorkingFrame.ObjectType);
        Assert.Same(details, frameResult.Execution);
    }

    [Fact]
    public void UnknownObjectTypeFailsClosed()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SpatialAnalyzerValueMapper.ToProtocol(
                new WorkerCollectionObjectNameValue(
                    "Collection",
                    "Object",
                    WorkerObjectTypeValue.Unspecified)));
    }

    private static SuccessfulOperationExecution Successful(
        Api.MpExecutionDetails details,
        params WorkerMpOutputValue[] outputs) =>
        new(
            new WorkerMpExecutionResult(
                ExecuteStepReturned: true,
                MpResultRetrieved: true,
                MpSucceeded: true,
                MpResultCode: 2,
                DurationMilliseconds: 5,
                OutputValues: outputs,
                DiagnosticCode: "completed"),
            details);

    private static WorkerMpOutputValue Text(string name, string value) =>
        new(
            name,
            WorkerMpValueKind.Text,
            Retrieved: true,
            StringValue: value);

    private static void AssertContractMatches(
        IReadOnlyList<OperationOutputContract> contracts,
        IReadOnlyList<WorkerMpOutputArgument> outputs,
        IReadOnlyList<string> expectedFieldNames)
    {
        Assert.Equal(expectedFieldNames, contracts.Select(contract => contract.FieldName));
        Assert.Equal(outputs.Count, contracts.Count);
        for (var index = 0; index < outputs.Count; index++)
        {
            Assert.Equal(outputs[index].Name, contracts[index].ArgumentName);
            Assert.Equal(outputs[index].Kind, contracts[index].Kind);
        }
    }
}
