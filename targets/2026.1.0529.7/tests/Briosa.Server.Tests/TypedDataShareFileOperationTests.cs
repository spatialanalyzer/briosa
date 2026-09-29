using Briosa.Server.Operations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedDataShareFileOperationTests
{
    private static readonly string[] Ids =
    [
        "file_operations.get_boolean_from_data_share_file", "file_operations.get_double_from_data_share_file",
        "file_operations.get_integer_from_data_share_file", "file_operations.get_string_from_data_share_file",
        "file_operations.get_transform_from_data_share_file", "file_operations.get_vector_from_data_share_file",
        "file_operations.set_boolean_in_data_share_file", "file_operations.set_double_in_data_share_file",
        "file_operations.set_integer_in_data_share_file", "file_operations.set_string_in_data_share_file",
        "file_operations.set_transform_in_data_share_file", "file_operations.set_vector_in_data_share_file"
    ];

    [Fact]
    public void DataShareOperationsHaveTypedRegistrationsAndExactEffects()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Equal(Api.ReplaySafety.Safe, GetBooleanFromDataShareFileOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.ReplaySafety.Safe, GetVectorFromDataShareFileOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.ReplaySafety.Unsafe, SetBooleanInDataShareFileOperation.Descriptor.ReplaySafety);
        Assert.Equal(Api.ReplaySafety.Unsafe, SetTransformInDataShareFileOperation.Descriptor.ReplaySafety);
    }

    [Fact]
    public void DataShareCommandsPreserveBindingsDefaultsAndRequiredValues()
    {
        var file = new Api.FileReference { Path = "shared.data" };
        var getters = new WorkerMpCommand[]
        {
            GetBooleanFromDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            GetDoubleFromDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            GetIntegerFromDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            GetStringFromDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            GetTransformFromDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            GetVectorFromDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file })
        };
        Assert.Equal(Ids.Take(6), getters.Select(command => command.OperationId));
        Assert.All(getters, command =>
        {
            Assert.Equal("DataShare File Path", command.InputArguments[0].Name);
            Assert.Equal("SetFilePathArg", command.InputArguments[0].SdkBinding);
            Assert.Equal("shared.data", command.InputArguments[0].RequireValue<WorkerFileReferenceValue>().Path);
            Assert.Equal(string.Empty, command.InputArguments[1].RequireValue<WorkerTextValue>().Value);
            Assert.Equal("SetStringArg", command.InputArguments[1].SdkBinding);
            Assert.Single(command.OutputArguments);
        });
        Assert.Equal("GetBoolArg", getters[0].OutputArguments[0].SdkBinding);
        Assert.Equal("GetDoubleArg", getters[1].OutputArguments[0].SdkBinding);
        Assert.Equal("GetIntegerArg", getters[2].OutputArguments[0].SdkBinding);
        Assert.Equal("GetStringArg", getters[3].OutputArguments[0].SdkBinding);
        Assert.Equal("GetTransformArg", getters[4].OutputArguments[0].SdkBinding);
        Assert.Equal("GetVectorArg", getters[5].OutputArguments[0].SdkBinding);

        var setters = new WorkerMpCommand[]
        {
            SetBooleanInDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            SetDoubleInDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            SetIntegerInDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            SetStringInDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }),
            SetTransformInDataShareFileOperation.CreateCommand(new()
            {
                DataShareFilePath = file,
                TransformValue = new Api.Transform { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } }
            }),
            SetVectorInDataShareFileOperation.CreateCommand(new()
            {
                DataShareFilePath = file, VectorValue = new Api.Vector { X = 1, Y = 2, Z = 3 }
            })
        };
        Assert.Equal(Ids.Skip(6), setters.Select(command => command.OperationId));
        Assert.All(setters, command =>
        {
            Assert.Equal("SetFilePathArg", command.InputArguments[0].SdkBinding);
            Assert.Equal(string.Empty, command.InputArguments[1].RequireValue<WorkerTextValue>().Value);
            Assert.Empty(command.OutputArguments);
        });
        Assert.False(setters[0].InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetBoolArg", setters[0].InputArguments[2].SdkBinding);
        Assert.Equal(0d, setters[1].InputArguments[2].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal("SetDoubleArg", setters[1].InputArguments[2].SdkBinding);
        Assert.Equal(0, setters[2].InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal("SetIntegerArg", setters[2].InputArguments[2].SdkBinding);
        Assert.Equal(string.Empty, setters[3].InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetStringArg", setters[3].InputArguments[2].SdkBinding);
        Assert.Equal(16, setters[4].InputArguments[2].RequireValue<WorkerTransformValue>().Values.Count);
        Assert.Equal(new WorkerVectorValue(1, 2, 3), setters[5].InputArguments[2].RequireValue<WorkerVectorValue>());

        Assert.Throws<ArgumentException>(() => GetBooleanFromDataShareFileOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetBooleanInDataShareFileOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetTransformInDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }));
        Assert.Throws<ArgumentException>(() => SetVectorInDataShareFileOperation.CreateCommand(new() { DataShareFilePath = file }));
        Assert.Throws<ArgumentException>(() => SetTransformInDataShareFileOperation.CreateCommand(new()
        {
            DataShareFilePath = file, TransformValue = new Api.Transform { Values = { 1, 2 } }
        }));
    }

    [Fact]
    public async Task GeneratedClientRoutesAllDataShareCallsToTypedCommands()
    {
        var worker = new DataShareWorker();
        var grpcHost = await GrpcTestHost.StartAsync<FileOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.FileOperations.FileOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var file = new Api.FileReference { Path = "shared.data" };

        var boolean = await client.GetBooleanFromDataShareFileAsync(new() { DataShareFilePath = file }, options);
        var doubleValue = await client.GetDoubleFromDataShareFileAsync(new() { DataShareFilePath = file }, options);
        var integer = await client.GetIntegerFromDataShareFileAsync(new() { DataShareFilePath = file }, options);
        var text = await client.GetStringFromDataShareFileAsync(new() { DataShareFilePath = file }, options);
        var transform = await client.GetTransformFromDataShareFileAsync(new() { DataShareFilePath = file }, options);
        var vector = await client.GetVectorFromDataShareFileAsync(new() { DataShareFilePath = file }, options);
        await client.SetBooleanInDataShareFileAsync(new() { DataShareFilePath = file, BooleanName = "flag", BooleanValue = true }, options);
        await client.SetDoubleInDataShareFileAsync(new() { DataShareFilePath = file, DoubleName = "gain", DoubleValue = 2.5 }, options);
        await client.SetIntegerInDataShareFileAsync(new() { DataShareFilePath = file, IntegerName = "count", IntegerValue = 7 }, options);
        await client.SetStringInDataShareFileAsync(new() { DataShareFilePath = file, StringName = "label", StringValue = "sample" }, options);
        await client.SetTransformInDataShareFileAsync(new()
        {
            DataShareFilePath = file, TransformName = "pose",
            TransformValue = new Api.Transform { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } }
        }, options);
        await client.SetVectorInDataShareFileAsync(new()
        {
            DataShareFilePath = file, VectorName = "axis", VectorValue = new Api.Vector { X = 1, Y = 2, Z = 3 }
        }, options);

        Assert.True(boolean.BooleanValue);
        Assert.Equal(2.5, doubleValue.DoubleValue);
        Assert.Equal(7, integer.IntegerValue);
        Assert.Equal("sample", text.StringValue);
        Assert.Equal(Enumerable.Range(0, 16).Select(index => (double)index), transform.TransformValue.Values);
        Assert.Equal(new Api.Vector { X = 1, Y = 2, Z = 3 }, vector.VectorValue);
        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("SetFilePathArg", worker.Commands[0].InputArguments[0].SdkBinding);
        Assert.Equal("flag", worker.Commands[6].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("pose", worker.Commands[10].InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetVectorArg", worker.Commands[11].InputArguments[2].SdkBinding);
    }

    private sealed class DataShareWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "file_operations.get_boolean_from_data_share_file" =>
                    [new WorkerRetrievedOutput("Boolean Value", WorkerMpValueKind.Logical, new WorkerBooleanValue(true))],
                "file_operations.get_double_from_data_share_file" =>
                    [new WorkerRetrievedOutput("Double Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2.5))],
                "file_operations.get_integer_from_data_share_file" =>
                    [new WorkerRetrievedOutput("Integer Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(7))],
                "file_operations.get_string_from_data_share_file" =>
                    [new WorkerRetrievedOutput("String Value", WorkerMpValueKind.Text, new WorkerTextValue("sample"))],
                "file_operations.get_transform_from_data_share_file" =>
                    [new WorkerRetrievedOutput("Transform Value", WorkerMpValueKind.Transform,
                        new WorkerTransformValue(Enumerable.Range(0, 16).Select(index => (double)index).ToArray()))],
                "file_operations.get_vector_from_data_share_file" =>
                    [new WorkerRetrievedOutput("Vector Value", WorkerMpValueKind.Vector, new WorkerVectorValue(1, 2, 3))],
                _ => []
            };
            var result = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, result, null, "completed", 1));
        }
    }
}
