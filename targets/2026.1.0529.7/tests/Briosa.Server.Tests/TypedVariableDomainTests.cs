using System.Net;
using Briosa.Server.Operations;
using Briosa.Server.Operations.Values;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Operations.WaveA;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedVariableDomainTests
{
    [Fact]
    public async Task GeneratedClientExercisesEveryVariableRpcThroughThePrivateValueContract()
    {
        var worker = new VariableWorker();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(new OperationExecutor(worker,
            new OperationAuditLogger(NullLogger<OperationAuditLogger>.Instance), TimeProvider.System));
        await using var app = builder.Build();
        app.MapGrpcService<VariablesService>();
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress(Assert.Single(app.Urls));
        var client = new Api.Variables.VariablesClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(30));

        await client.SetBooleanVariableAsync(new() { Name = "boolean" }, options);
        var boolean = await client.GetBooleanVariableAsync(new() { Name = "boolean" }, options);
        Assert.True(boolean.HasValue);
        Assert.False(boolean.Value);
        await client.SetIntegerVariableAsync(new() { Name = "integer", Value = int.MinValue }, options);
        var integer = await client.GetIntegerVariableAsync(new() { Name = "integer" }, options);
        Assert.True(integer.HasValue);
        Assert.Equal(int.MinValue, integer.Value);
        await client.SetStringVariableAsync(new() { Name = "string" }, options);
        var text = await client.GetStringVariableAsync(new() { Name = "string" }, options);
        Assert.True(text.HasValue);
        Assert.Equal("", text.Value);
        await client.SetDoubleVariableAsync(new() { Name = "double", Value = -1.5 }, options);
        Assert.Equal(-1.5, (await client.GetDoubleVariableAsync(new() { Name = "double" }, options)).Value);

        var objectName = new Api.CollectionObjectName { CollectionName = "c", ObjectName = "object", ObjectType = Api.ObjectType.Any };
        await client.SetCollectionObjectNameVariableAsync(new() { Name = "object", Value = objectName }, options);
        Assert.Equal(objectName, (await client.GetCollectionObjectNameVariableAsync(new() { Name = "object" }, options)).Value);
        var point = new Api.PointName { CollectionName = "c", GroupName = "g", TargetName = "p" };
        await client.SetPointNameVariableAsync(new() { Name = "point", Value = point }, options);
        Assert.Equal(point, (await client.GetPointNameVariableAsync(new() { Name = "point" }, options)).Value);
        var vector = new Api.Vector { X = 0, Y = -2.5, Z = 8 };
        await client.SetVectorVariableAsync(new() { Name = "vector", Value = vector }, options);
        Assert.Equal(vector, (await client.GetVectorVariableAsync(new() { Name = "vector" }, options)).Value);
        var transform = new Api.Transform { Values = { Enumerable.Range(0, 16).Select(value => value / 2d) } };
        await client.SetTransformVariableAsync(new() { Name = "transform", Value = transform }, options);
        Assert.Equal(transform, (await client.GetTransformVariableAsync(new() { Name = "transform" }, options)).Value);

        var objectList = new Api.SetCollectionObjectRefListVariableRequest { Name = "objects", Value = { objectName, objectName.Clone() } };
        await client.SetCollectionObjectRefListVariableAsync(objectList, options);
        Assert.Equal(objectList.Value, (await client.GetCollectionObjectRefListVariableAsync(new() { Name = "objects" }, options)).Value);
        var pointList = new Api.SetPointNameRefListVariableRequest { Name = "points", Value = { point, point.Clone() } };
        await client.SetPointNameRefListVariableAsync(pointList, options);
        Assert.Equal(pointList.Value, (await client.GetPointNameRefListVariableAsync(new() { Name = "points" }, options)).Value);
        var vectorList = new Api.SetVectorNameRefListVariableRequest
        {
            Name = "vectors", Value = { new Api.VectorName { CollectionName = "c", GroupName = "vg", Name = "v" } }
        };
        await client.SetVectorNameRefListVariableAsync(vectorList, options);
        Assert.Equal(vectorList.Value, (await client.GetVectorNameRefListVariableAsync(new() { Name = "vectors" }, options)).Value);

        // Exercise a choice in the item domain beyond the object-domain range.
        var itemType = Enum.GetValues<Api.ItemType>().Max();
        var item = new Api.CollectionItemName { CollectionName = "c", ItemName = "item", ItemType = itemType };
        var relationships = new Api.SetRelationshipRefListVariableRequest { Name = "relationships", Value = { item } };
        await client.SetRelationshipRefListVariableAsync(relationships, options);
        Assert.Equal(relationships.Value, (await client.GetRelationshipRefListVariableAsync(new() { Name = "relationships" }, options)).Value);
        var reports = new Api.SetReportItemsReferenceListVariableRequest { Name = "reports", Value = { item, item.Clone() } };
        await client.SetReportItemsReferenceListVariableAsync(reports, options);
        Assert.Equal(reports.Value, (await client.GetReportItemsReferenceListVariableAsync(new() { Name = "reports" }, options)).Value);
        var strings = new Api.SetStringRefListVariableRequest { Name = "strings", Value = { "", "two", "two" } };
        await client.SetStringRefListVariableAsync(strings, options);
        Assert.Equal(strings.Value, (await client.GetStringRefListVariableAsync(new() { Name = "strings" }, options)).Value);
        await client.SetFontVariableAsync(new() { Name = "font" }, options);
        Assert.Equal(new WorkerFontValue("MS Shell Dlg", 8, new(0, 0, 0)), worker.Values["font"]);

        await client.SetNamedDoubleListVariableAsync(new() { Name = "numbers", DoubleListVariable = { -3, 2 } }, options);
        await client.AddDoubleToNamedDoubleListVariableAsync(new() { Name = "numbers", DoubleValue = 9 }, options);
        var numbers = await client.GetNamedDoubleListVariableAsync(new() { Name = "numbers" }, options);
        Assert.Equal([-3d, 2d, 9d], numbers.DoubleListVariable);
        var limits = await client.GetNamedDoubleListVariableMinMaxAsync(new() { Name = "numbers" }, options);
        Assert.True(limits.HasMinimumValue);
        Assert.True(limits.HasMaximumValue);
        Assert.Equal(-3, limits.MinimumValue);
        Assert.Equal(9, limits.MaximumValue);
        Assert.Equal(["minimum_value", "maximum_value"], limits.Execution.OutputRetrievals.Select(output => output.FieldName));
        await client.ClearNamedDoubleListVariableAsync(new() { Name = "numbers" }, options);
        Assert.Empty((await client.GetNamedDoubleListVariableAsync(new() { Name = "numbers" }, options)).DoubleListVariable);
        await client.DeleteVariableAsync(new() { Name = "string" }, options);
        Assert.False(worker.Values.ContainsKey("string"));
        await client.DeleteVariablesWildcardMatchAsync(new() { VariableWildcardCriteria = "*" }, options);
        Assert.Empty(worker.Values);

        Assert.Equal(Api.Variables.Descriptor.Methods.Count, worker.SeenOperations.Count);
        Assert.DoesNotContain(MpOperationCatalog.Operations, operation => operation.Descriptor.GrpcService == "briosa.Variables");
        Assert.Equal(Api.Variables.Descriptor.Methods.Count,
            SpatialAnalyzerApi.Operations.Count(operation => operation.GrpcService == "briosa.Variables"));

        var callsBeforeInvalid = worker.Calls;
        Func<Task>[] invalidCalls =
        [
            async () => await client.SetPointNameVariableAsync(new(), options),
            async () => await client.SetCollectionObjectNameVariableAsync(new(), options),
            async () => await client.SetVectorVariableAsync(new(), options),
            async () => await client.SetTransformVariableAsync(new() { Value = new() }, options),
            async () => await client.SetCollectionObjectRefListVariableAsync(new(), options),
            async () => await client.SetPointNameRefListVariableAsync(new(), options),
            async () => await client.SetVectorNameRefListVariableAsync(new(), options),
            async () => await client.SetRelationshipRefListVariableAsync(new(), options),
            async () => await client.SetReportItemsReferenceListVariableAsync(new(), options),
            async () => await client.SetStringRefListVariableAsync(new(), options),
            async () => await client.SetFontVariableAsync(new() { Value = new() { Size = 256 } }, options)
        ];
        foreach (var call in invalidCalls)
        {
            var error = await Assert.ThrowsAsync<RpcException>(call);
            Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
            var details = Api.OperationError.Parser.ParseFrom(Assert.Single(error.Trailers,
                entry => entry.Key == "briosa-operation-error-bin").ValueBytes);
            Assert.Equal(Api.ExecutionDisposition.NotStarted, details.ExecutionDisposition);
        }
        Assert.Equal(callsBeforeInvalid, worker.Calls);
        await app.StopAsync();
    }

    [Fact]
    public void TypedDefaultsPreservePresenceAndTargetChoiceRules()
    {
        Assert.Equal(SetBooleanVariableOperation.CreateCommand(new()).InputArguments,
            SetBooleanVariableOperation.CreateCommand(new() { Name = "", Value = false }).InputArguments);
        Assert.Equal(SetIntegerVariableOperation.CreateCommand(new()).InputArguments,
            SetIntegerVariableOperation.CreateCommand(new() { Name = "", Value = 0 }).InputArguments);
        Assert.Equal(SetStringVariableOperation.CreateCommand(new()).InputArguments,
            SetStringVariableOperation.CreateCommand(new() { Name = "", Value = "" }).InputArguments);
        Assert.Equal(AddDoubleToNamedDoubleListVariableOperation.CreateCommand(new()).InputArguments,
            AddDoubleToNamedDoubleListVariableOperation.CreateCommand(new() { Name = "", DoubleValue = 0 }).InputArguments);
        Assert.Equal(FontMapper.WithDefaults(null), FontMapper.WithDefaults(new()));
        Assert.Equal(new WorkerFontValue("", 0, new(0, 255, 0)),
            FontMapper.WithDefaults(new() { FontName = "", Size = 0, Color = new() { Green = 255 } }));
        Assert.Throws<ArgumentOutOfRangeException>(() => FontMapper.WithDefaults(new() { Color = new() { Blue = 256 } }));

        Assert.Equal(WorkerObjectTypeValue.Any, CollectionObjectNameMapper.Required(new() { ObjectName = "object" }, "value").ObjectType);
        Assert.Equal(WorkerItemTypeValue.Any, CollectionItemNameMapper.RequiredList([new() { ItemName = "item" }], "value").Values[0].ItemType);
        Assert.Throws<ArgumentException>(() => CollectionItemNameMapper.RequiredList([new() { ItemName = "item", ItemType = Api.ItemType.Unspecified }], "value"));
        foreach (var type in new[] { -1, 9999 })
        {
            Assert.Throws<ArgumentException>(() => CollectionObjectNameMapper.Required(new() { ObjectName = "object", ObjectType = (Api.ObjectType)type }, "value"));
            Assert.Throws<ArgumentException>(() => CollectionItemNameMapper.RequiredList([new() { ItemName = "item", ItemType = (Api.ItemType)type }], "value"));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void InvalidTransformLengthsAreRejectedBeforeDispatch(int count)
    {
        var request = new Api.SetTransformVariableRequest { Value = new() { Values = { new double[count] } } };
        Assert.Throws<ArgumentException>(() => SetTransformVariableOperation.CreateCommand(request));
    }

    [Fact]
    public void StructuredValuesOwnTheirDataAndPreserveEmptyListMemberFields()
    {
        var request = new Api.SetPointNameRefListVariableRequest { Value = { new Api.PointName(), new Api.PointName { TargetName = "original" } } };
        var command = SetPointNameRefListVariableOperation.CreateCommand(request);
        request.Value[1].TargetName = "changed";
        request.Value.Clear();
        var points = command.InputArguments[1].RequireValue<WorkerPointNameListValue>().Values;
        Assert.Equal(["", "original"], points.Select(point => point.TargetName));
        var transform = new Api.Transform { Values = { Enumerable.Range(0, 16).Select(value => (double)value) } };
        var owned = TransformMapper.Required(transform, "value");
        transform.Values[0] = 99;
        Assert.Equal(0, owned.Values[0]);
    }

    private sealed class VariableWorker : IWorkerCommandExecutor
    {
        public Dictionary<string, WorkerMpValue> Values { get; } = new(StringComparer.Ordinal);
        public HashSet<string> SeenOperations { get; } = new(StringComparer.Ordinal);
        public int Calls { get; private set; }

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Calls++;
            SeenOperations.Add(command.OperationId);
            var name = command.InputArguments[0].RequireValue<WorkerTextValue>().Value;
            Assert.Equal("SetStringArg", command.InputArguments[0].SdkBinding);
            IReadOnlyList<WorkerMpOutputValue> outputs = [];
            switch (command.OperationId)
            {
                case "variables.add_double_to_named_double_list_variable":
                    Assert.Equal("Double Value", command.InputArguments[1].Name);
                    Assert.Equal("SetDoubleArg", command.InputArguments[1].SdkBinding);
                    Values[name] = new WorkerDoubleArrayValue([.. ((WorkerDoubleArrayValue)Values[name]).Values,
                        command.InputArguments[1].RequireValue<WorkerDoubleValue>().Value]);
                    break;
                case "variables.clear_named_double_list_variable":
                    Assert.Single(command.InputArguments);
                    Values[name] = new WorkerDoubleArrayValue([]);
                    break;
                case "variables.delete_variable":
                    Assert.Single(command.InputArguments);
                    Values.Remove(name);
                    break;
                case "variables.delete_variables_wildcard_match":
                    Assert.Equal("Variable Wildcard Criteria", Assert.Single(command.InputArguments).Name);
                    Assert.Equal("*", name);
                    Assert.Equal("Delete Variables -- Wildcard Match", command.StepName);
                    Values.Clear();
                    break;
                case "variables.get_named_double_list_variable_min_max":
                    Assert.Equal(["Minimum Value", "Maximum Value"], command.OutputArguments.Select(output => output.Name));
                    Assert.All(command.OutputArguments, output => Assert.Equal("GetDoubleArg", output.SdkBinding));
                    var numbers = ((WorkerDoubleArrayValue)Values[name]).Values;
                    outputs = [new WorkerRetrievedOutput("Minimum Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(numbers.Min())),
                        new WorkerRetrievedOutput("Maximum Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(numbers.Max()))];
                    break;
                default:
                    if (command.OperationId.StartsWith("variables.set_", StringComparison.Ordinal))
                    {
                        Assert.Equal(2, command.InputArguments.Count);
                        var input = command.InputArguments[1];
                        Assert.Equal(Bindings(input.Kind).Set, input.SdkBinding);
                        Values[name] = input.Value;
                        Assert.Empty(command.OutputArguments);
                    }
                    else
                    {
                        Assert.StartsWith("variables.get_", command.OperationId, StringComparison.Ordinal);
                        Assert.Single(command.InputArguments);
                        var output = Assert.Single(command.OutputArguments);
                        Assert.Equal(Bindings(output.Kind).Get, output.SdkBinding);
                        outputs = [new WorkerRetrievedOutput(output.Name, output.Kind, Values[name])];
                    }
                    break;
            }
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed, new WorkerMpResultAvailable(2, 1, outputs, null),
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "test", DateTimeOffset.UnixEpoch), null)));
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, response.ExecutionResponse!.Execution, null, "completed", 1));
        }

        private static (string Set, string Get) Bindings(WorkerMpValueKind kind) => kind switch
        {
            WorkerMpValueKind.Logical => ("SetBoolArg", "GetBoolArg"),
            WorkerMpValueKind.WholeNumber => ("SetIntegerArg", "GetIntegerArg"),
            WorkerMpValueKind.FloatingPoint => ("SetDoubleArg", "GetDoubleArg"),
            WorkerMpValueKind.Text => ("SetStringArg", "GetStringArg"),
            WorkerMpValueKind.CollectionObjectName => ("SetCollectionObjectNameArg2", "GetCollectionObjectNameArg"),
            WorkerMpValueKind.CollectionObjectNameList or WorkerMpValueKind.CollectionItemNameList => ("SetCollectionObjectNameRefListArg", "GetCollectionObjectNameRefListArg"),
            WorkerMpValueKind.PointName => ("SetPointNameArg", "GetPointNameArg"),
            WorkerMpValueKind.PointNameList => ("SetPointNameRefListArg", "GetPointNameRefListArg"),
            WorkerMpValueKind.VectorNameList => ("SetVectorNameRefListArg", "GetVectorNameRefListArg"),
            WorkerMpValueKind.Vector => ("SetVectorArg", "GetVectorArg"),
            WorkerMpValueKind.Transform => ("SetTransformArg", "GetTransformArg"),
            WorkerMpValueKind.StringList => ("SetStringRefListArg", "GetStringRefListArg"),
            WorkerMpValueKind.DoubleArray => ("SetDoubleArrayArg", "GetDoubleArrayArg"),
            WorkerMpValueKind.Font => ("SetFontTypeArg", ""),
            _ => throw new InvalidOperationException("Unexpected variable value family.")
        };

        private static WorkerControlMessage RoundTrip(WorkerControlMessage message)
        {
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream);
            channel.Send(message);
            stream.Position = 0;
            return channel.Receive();
        }
    }
}
