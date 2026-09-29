using Briosa.Server.Operations;
using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedCloudPointAndSphereExtractionTests
{
    [Fact]
    public async Task GeneratedClientMapsCloudPointSelectionAndSphereExtractionWithDefaultsAndOutputs()
    {
        var worker = new CloudPointAndSphereExtractionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var cloudSelection = await client.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectAsync(new()
        {
            CloudName = Object("Scans", "Input")
        }, options);
        var pointSelection = await client.ConstructPointFromCloudPointRuntimeSelectAsync(new()
        {
            ConstructedPointName = new() { TargetName = "SelectedPoint" }
        }, options);
        var sphereExtraction = await client.ExtractSphereCentersFromPointCloudAsync(new()
        {
            CloudName = Object("Scans", "Input"),
            GroupNameForPoints = Object("Results", "SphereCenters")
        }, options);

        Assert.Equal(Api.MpExecutionState.Succeeded, cloudSelection.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, pointSelection.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, sphereExtraction.Execution.State);
        Assert.Equal(12, sphereExtraction.NumberOfPointsExtracted);
        Assert.Equal(3, worker.Commands.Count);

        var cloudCommand = worker.Commands[0];
        Assert.Equal("Construct Point Clouds from Existing Cloud Points - Runtime Select", cloudCommand.StepName);
        Assert.Equal(new WorkerCollectionObjectNameValue("Scans", "Input", WorkerObjectTypeValue.Cloud),
            cloudCommand.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>());
        Assert.Empty(cloudCommand.OutputArguments);

        var pointCommand = worker.Commands[1];
        Assert.Equal("Construct Point From Cloud Point - Runtime Select", pointCommand.StepName);
        Assert.Equal("Select cloud point", pointCommand.InputArguments[0].RequireValue<WorkerTextValue>().Value);
        Assert.False(pointCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerPointNameValue("", "", "SelectedPoint"), pointCommand.InputArguments[2].RequireValue<WorkerPointNameValue>());
        Assert.Equal(("Selection Cloud Point Coordinates", "GetVectorArg"),
            (Assert.Single(pointCommand.OutputArguments).Name, Assert.Single(pointCommand.OutputArguments).SdkBinding));
        Assert.Equal(1, pointSelection.SelectionCloudPointCoordinates.X);
        Assert.Equal(2, pointSelection.SelectionCloudPointCoordinates.Y);
        Assert.Equal(3, pointSelection.SelectionCloudPointCoordinates.Z);

        var extractionCommand = worker.Commands[2];
        Assert.Equal("Extract Sphere Centers from Point Cloud", extractionCommand.StepName);
        Assert.Equal(new WorkerCollectionObjectNameValue("Scans", "Input", WorkerObjectTypeValue.Cloud),
            extractionCommand.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>());
        Assert.Equal(0, extractionCommand.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, extractionCommand.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(50, extractionCommand.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(new WorkerCollectionObjectNameValue("Results", "SphereCenters", WorkerObjectTypeValue.PointGroup),
            extractionCommand.InputArguments[4].RequireValue<WorkerCollectionObjectNameValue>());
        Assert.True(extractionCommand.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(120, extractionCommand.InputArguments[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(("Number of Points Extracted", "GetIntegerArg"),
            (Assert.Single(extractionCommand.OutputArguments).Name, Assert.Single(extractionCommand.OutputArguments).SdkBinding));

        var configuredExtraction = await client.ExtractSphereCentersFromPointCloudAsync(new()
        {
            CloudName = Object("Scans", "Input"),
            DesiredDiameter = 0.25,
            ExtractionTolerance = 0.01,
            MinimumPointCount = 24,
            GroupNameForPoints = Object("Results", "CustomCenters"),
            PerformFinalFit = false,
            FinalFitConeAngle = 95
        }, options);
        Assert.Equal(Api.MpExecutionState.Succeeded, configuredExtraction.Execution.State);
        Assert.Equal(4, worker.Commands.Count);
        var configured = worker.Commands[3];
        Assert.Equal(0.25, configured.InputArguments[1].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0.01, configured.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(24, configured.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(configured.InputArguments[5].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(95, configured.InputArguments[6].RequireValue<WorkerDoubleValue>().Value);
    }

    [Fact]
    public async Task MissingRequiredCloudPointAndPointGroupNamesAreRejectedBeforeWorkerExecution()
    {
        var worker = new CloudPointAndSphereExtractionWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ConstructionOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ConstructionOperations.ConstructionOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var missingSelectedCloud = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointCloudsFromExistingCloudPointsRuntimeSelectAsync(new(), options));
        var missingConstructedPoint = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ConstructPointFromCloudPointRuntimeSelectAsync(new(), options));
        var missingExtractionCloud = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ExtractSphereCentersFromPointCloudAsync(new()
            {
                GroupNameForPoints = Object("Results", "SphereCenters")
            }, options));
        var missingPointGroup = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.ExtractSphereCentersFromPointCloudAsync(new()
            {
                CloudName = Object("Scans", "Input")
            }, options));

        Assert.Equal(StatusCode.InvalidArgument, missingSelectedCloud.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, missingConstructedPoint.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, missingExtractionCloud.StatusCode);
        Assert.Equal(StatusCode.InvalidArgument, missingPointGroup.StatusCode);
        Assert.Empty(worker.Commands);
    }

    [Fact]
    public void CloudPointAndSphereExtractionOperationsAreTypedUnsafeMutations()
    {
        foreach (var operation in new[]
        {
            ConstructPointCloudsFromExistingCloudPointsRuntimeSelectOperation.Descriptor,
            ConstructPointFromCloudPointRuntimeSelectOperation.Descriptor,
            ExtractSphereCentersFromPointCloudOperation.Descriptor
        })
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == operation.OperationId);
            Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation, operation.ExecutionScope);
            Assert.Equal(Api.ReplaySafety.Unsafe, operation.ReplaySafety);
            Assert.Empty(operation.RiskFlags);
        }
    }

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private sealed class CloudPointAndSphereExtractionWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "construction_operations.construct_point_from_cloud_point_runtime_select" =>
                [new WorkerRetrievedOutput("Selection Cloud Point Coordinates", WorkerMpValueKind.Vector,
                    new WorkerVectorValue(1, 2, 3))],
                "construction_operations.extract_sphere_centers_from_point_cloud" =>
                [new WorkerRetrievedOutput("Number of Points Extracted", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(12))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
