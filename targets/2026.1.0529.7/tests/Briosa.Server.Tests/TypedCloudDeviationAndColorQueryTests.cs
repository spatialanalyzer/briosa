using Briosa.Server.Operations;
using Briosa.Server.Operations.CloudAndMeshOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Tests retain xUnit synchronization behavior.")]
public sealed class TypedCloudDeviationAndColorQueryTests
{
    [Fact]
    public async Task GeneratedClientMapsDeviationAndRgbQueriesWithDefaultsAndOutputs()
    {
        var worker = new CloudDeviationAndColorWorker();
        var grpcHost = await GrpcTestHost.StartAsync<CloudAndMeshOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.CloudAndMeshOperations.CloudAndMeshOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var cloud = Object("Scans", "CloudA");

        await client.ClearCloudPointDeviationsAsync(new() { CloudName = cloud }, options);
        var defaultRgb = await client.GetCloudRGBValuesAsync(new() { SourceCloudName = cloud }, options);
        var blueRgb = await client.GetCloudRGBValuesAsync(new()
        {
            SourceCloudName = cloud,
            RgbColorChannel = Api.RGBColorChannel.Blue
        }, options);
        var nearPoint = new Api.PointName { CollectionName = "Targets", GroupName = "Inspection", TargetName = "P1" };
        var defaultNearPoint = await client.GetCloudRGBValuesNearPointAsync(new()
        {
            SourceCloudName = cloud,
            SinglePoint = nearPoint
        }, options);
        var configuredNearPoint = await client.GetCloudRGBValuesNearPointAsync(new()
        {
            SourceCloudName = cloud,
            SinglePoint = nearPoint,
            Diameter = 0.5,
            RgbColorChannel = Api.RGBColorChannel.Green
        }, options);

        Assert.Equal(5, defaultRgb.LowValue);
        Assert.Equal(210, defaultRgb.HighValue);
        Assert.Equal(64, defaultRgb.AverageValue);
        Assert.Equal(12, defaultRgb.StandardDeviation);
        Assert.Equal(defaultRgb.LowValue, blueRgb.LowValue);
        Assert.Equal(defaultRgb.HighValue, defaultNearPoint.HighValue);
        Assert.Equal(defaultRgb.AverageValue, configuredNearPoint.AverageValue);
        Assert.Equal(5, worker.Commands.Count);

        var clear = worker.Commands[0];
        Assert.Equal("Clear Cloud Point Deviations", clear.StepName);
        Assert.Equal(WorkerObjectTypeValue.Cloud,
            clear.InputArguments[0].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Empty(clear.OutputArguments);

        var defaultRgbCommand = worker.Commands[1];
        Assert.Equal("Intensity", defaultRgbCommand.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetStringArg", defaultRgbCommand.InputArguments[1].SdkBinding);
        Assert.Equal(4, defaultRgbCommand.OutputArguments.Count);
        Assert.Equal("Standard Deviation", defaultRgbCommand.OutputArguments[3].Name);
        Assert.Equal("GetIntegerArg", defaultRgbCommand.OutputArguments[3].SdkBinding);
        Assert.Equal("Blue", worker.Commands[2].InputArguments[1].RequireValue<WorkerTextValue>().Value);

        var defaultNear = worker.Commands[3];
        Assert.Equal(new WorkerPointNameValue("Targets", "Inspection", "P1"),
            defaultNear.InputArguments[1].RequireValue<WorkerPointNameValue>());
        Assert.Equal(10, defaultNear.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("Intensity", defaultNear.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        var configuredNear = worker.Commands[4];
        Assert.Equal(0.5, configuredNear.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("Green", configuredNear.InputArguments[3].RequireValue<WorkerTextValue>().Value);
    }

    [Fact]
    public async Task MissingNamesAndUnsupportedRgbChannelsAreRejectedBeforeWorkerExecution()
    {
        var worker = new CloudDeviationAndColorWorker();
        var grpcHost = await GrpcTestHost.StartAsync<CloudAndMeshOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.CloudAndMeshOperations.CloudAndMeshOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var cloud = Object("Scans", "CloudA");

        await AssertInvalid(() => client.ClearCloudPointDeviationsAsync(new(), options).ResponseAsync);
        await AssertInvalid(() => client.GetCloudRGBValuesAsync(new(), options).ResponseAsync);
        await AssertInvalid(() => client.GetCloudRGBValuesAsync(new()
        {
            SourceCloudName = cloud,
            RgbColorChannel = Api.RGBColorChannel.Unspecified
        }, options).ResponseAsync);
        await AssertInvalid(() => client.GetCloudRGBValuesAsync(new()
        {
            SourceCloudName = cloud,
            RgbColorChannel = (Api.RGBColorChannel)99
        }, options).ResponseAsync);
        await AssertInvalid(() => client.GetCloudRGBValuesNearPointAsync(new()
        {
            SourceCloudName = cloud
        }, options).ResponseAsync);
        await AssertInvalid(() => client.GetCloudRGBValuesNearPointAsync(new()
        {
            SourceCloudName = cloud,
            SinglePoint = new() { TargetName = "P1" },
            RgbColorChannel = (Api.RGBColorChannel)99
        }, options).ResponseAsync);
        Assert.Empty(worker.Commands);
    }

    [Fact]
    public void CloudDeviationAndRgbOperationsAreRegisteredWithCorrectExecutionSafety()
    {
        var operations = new[]
        {
            ClearCloudPointDeviationsOperation.Descriptor,
            GetCloudRGBValuesOperation.Descriptor,
            GetCloudRGBValuesNearPointOperation.Descriptor
        };

        foreach (var operation in operations)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, candidate => candidate.OperationId == operation.OperationId);
            Assert.Empty(operation.RiskFlags);
        }

        Assert.Equal(Api.OperationExecutionScope.GlobalStateMutation,
            ClearCloudPointDeviationsOperation.Descriptor.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Unsafe, ClearCloudPointDeviationsOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.OperationExecutionScope.GlobalStateRead, GetCloudRGBValuesOperation.Descriptor.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Safe, GetCloudRGBValuesOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.OperationExecutionScope.GlobalStateRead, GetCloudRGBValuesNearPointOperation.Descriptor.ExecutionScope);
        Assert.Equal(Api.ReplaySafety.Safe, GetCloudRGBValuesNearPointOperation.Descriptor.ReplaySafety);
    }



    private static async Task AssertInvalid(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(action).ConfigureAwait(false);
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    private static Api.CollectionObjectName Object(string collection, string name) => new()
    {
        CollectionName = collection,
        ObjectName = name
    };

    private sealed class CloudDeviationAndColorWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId is
                "cloud_and_mesh_operations.get_cloud_rgb_values" or
                "cloud_and_mesh_operations.get_cloud_rgb_values_near_point"
                ?
                [
                    new WorkerRetrievedOutput("Low Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(5)),
                    new WorkerRetrievedOutput("High Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(210)),
                    new WorkerRetrievedOutput("Average Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(64)),
                    new WorkerRetrievedOutput("Standard Deviation", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(12))
                ]
                : [];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
