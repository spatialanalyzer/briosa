using System.Reflection;
using Briosa.Server.Operations.WaveA;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class WaveAOperationCatalogTests
{
    private static readonly Dictionary<string, ServiceDescriptor> Services =
        new[]
        {
            Api.AnalysisOperations.Descriptor,
            Api.DimensionOperations.Descriptor,
            Api.EventOperations.Descriptor,
            Api.FileOperations.Descriptor,
            Api.MpSubroutines.Descriptor,
            Api.MpTaskOverview.Descriptor,
            Api.ProcessFlowOperations.Descriptor,
            Api.RelationshipOperations.Descriptor,
            Api.ReportingOperations.Descriptor,
            Api.ScaleBarOperations.Descriptor,
            Api.UtilityOperations.Descriptor,
            Api.Variables.Descriptor,
            Api.VectorOperations.Descriptor,
            Api.ViewControl.Descriptor
        }.ToDictionary(service => service.FullName, StringComparer.Ordinal);

    private static readonly int[] ExpectedDecimalDigits = [4, 4, 6, 6, 3];

    [Fact]
    public void EveryWaveAContractMatchesItsProtobufMethodAndBuildsAWorkerCommand()
    {
        Assert.NotEmpty(WaveAOperationCatalog.Operations);
        Assert.Equal(
            WaveAOperationCatalog.Operations.Count,
            WaveAOperationCatalog.Operations
                .Select(operation => operation.Descriptor.OperationId)
                .Distinct(StringComparer.Ordinal)
                .Count());

        foreach (var operation in WaveAOperationCatalog.Operations)
        {
            var descriptor = operation.Descriptor;
            var service = Services[descriptor.GrpcService];
            var method = Assert.Single(
                service.Methods,
                candidate => candidate.Name == descriptor.Rpc);

            Assert.Equal(
                operation.Inputs.Select(input => input.FieldName).Order(StringComparer.Ordinal),
                method.InputType.Fields.InFieldNumberOrder()
                    .Select(field => field.Name)
                    .Order(StringComparer.Ordinal));
            Assert.Equal(
                operation.Outputs.Select(output => output.FieldName).Order(StringComparer.Ordinal),
                method.OutputType.Fields.InFieldNumberOrder()
                    .Where(field => field.Name != "execution")
                    .Select(field => field.Name)
                    .Order(StringComparer.Ordinal));

            var request = method.InputType.Parser.ParseFrom(Array.Empty<byte>());
            PopulateRequiredFields(request, operation.Inputs);
            var command = operation.CreateCommand(request);
            using var stream = new MemoryStream();
            using var channel = new WorkerControlChannel(stream);
            var encodingFailure = Record.Exception(() => channel.Send(WorkerControlMessage.Execute(Guid.NewGuid(), command)));
            Assert.True(encodingFailure is null, $"{descriptor.OperationId} produced an invalid worker command: {encodingFailure}");

            Assert.Equal(descriptor.OperationId, command.OperationId);
            Assert.Equal(descriptor.MpStep, command.StepName);
            Assert.Equal(
                operation.Inputs.Select(input => (input.MpName, input.Kind, input.SdkBinding)),
                command.InputArguments.Select(input =>
                    (input.Name, input.Kind, input.SdkBinding!)));
            Assert.Equal(
                operation.Outputs.Select(output => (output.MpName, output.Kind, output.SdkBinding)),
                command.OutputArguments.Select(output =>
                    (output.Name, output.Kind, output.SdkBinding!)));
        }
    }

    [Fact]
    public void EveryWaveAResultKindMapsToItsTypedProtobufField()
    {
        foreach (var operation in WaveAOperationCatalog.Operations.Where(
                     candidate => candidate.Outputs.Count > 0))
        {
            var service = Services[operation.Descriptor.GrpcService];
            var method = Assert.Single(
                service.Methods,
                candidate => candidate.Name == operation.Descriptor.Rpc);
            var outputs = operation.Outputs.Select(CreateOutput).ToArray();
            var completed = new SuccessfulOperationExecution(
                WorkerMpExecutionResult.FromEvidence(
                    executeStepReturned: true,
                    mpResultRetrieved: true,
                    mpSucceeded: true,
                    mpResultCode: 2,
                    durationMilliseconds: 1,
                    outputs,
                    diagnosticCode: null),
                new Api.MpExecutionDetails());
            var createResult = typeof(MpOperationContract)
                .GetMethod(nameof(MpOperationContract.CreateResult))!
                .MakeGenericMethod(method.OutputType.ClrType);

            var result = Assert.IsAssignableFrom<IMessage>(
                createResult.Invoke(operation, [completed]));

            foreach (var output in operation.Outputs)
            {
                var field = result.Descriptor.FindFieldByName(output.FieldName)!;
                Assert.True(
                    field.IsRepeated
                        ? ((System.Collections.ICollection)field.Accessor.GetValue(result)).Count > 0
                        : field.HasPresence
                            ? field.Accessor.HasValue(result)
                            : IsNonDefaultScalar(field.Accessor.GetValue(result)),
                    $"Result field '{result.Descriptor.FullName}.{field.Name}' was not populated.");
            }
        }
    }

    [Fact]
    public void RenamedAngleTolerancePreservesTheSdkLabelAndZeroSentinel()
    {
        var operation = WaveAOperationCatalog.Get("analysis_operations.angle_between_line_and_plane");
        var request = new Api.AngleBetweenLineAndPlaneRequest
        {
            SelectedLine = new Api.CollectionObjectName
            {
                CollectionName = "Collection",
                ObjectName = "Line",
                ObjectType = Api.ObjectType.Line
            },
            SelectedPlane = new Api.CollectionObjectName
            {
                CollectionName = "Collection",
                ObjectName = "Plane",
                ObjectType = Api.ObjectType.Plane
            },
            AngleTolerance = 0
        };

        var command = operation.CreateCommand(request);
        var tolerance = Assert.Single(command.InputArguments, value => value.Name == "Angle Tolerance (0.0 for none)");
        Assert.Equal("SetDoubleArg", tolerance.SdkBinding);
        Assert.Equal(0, ((tolerance.Value as WorkerDoubleValue)?.Value));
        request.AngleTolerance = 0.25;
        Assert.Equal(0.25, ((Assert.Single(operation.CreateCommand(request).InputArguments,
            value => value.Name == "Angle Tolerance (0.0 for none)").Value as WorkerDoubleValue)?.Value));
    }

    [Fact]
    public void SetDecimalDigitsUsesTheReviewedSpatialAnalyzerDefaults()
    {
        var command = WaveAOperationCatalog
            .Get("utility_operations.set_decimal_digits_for_display")
            .CreateCommand(new Api.SetDecimalDigitsForDisplayRequest());

        Assert.Equal(
            ExpectedDecimalDigits,
            command.InputArguments.Select(argument => ((argument.Value as WorkerIntegerValue)?.Value)!.Value));
    }

    private static void PopulateRequiredFields(
        IMessage request,
        IReadOnlyList<MpArgumentContract> contracts)
    {
        foreach (var contract in contracts.Where(contract => contract.Required))
        {
            var field = request.Descriptor.FindFieldByName(contract.FieldName)!;
            if (field.IsRepeated)
            {
                AddRepeatedValue(request, field, CreateFieldValue(field));
            }
            else
            {
                field.Accessor.SetValue(request, CreateFieldValue(field));
            }
        }
    }

    private static bool IsNonDefaultScalar(object value) => value switch
    {
        bool item => item,
        int item => item != 0,
        long item => item != 0,
        float item => item != 0,
        double item => item != 0,
        string item => item.Length > 0,
        _ => true
    };

    private static object CreateFieldValue(FieldDescriptor field) =>
        field.FieldType switch
        {
            FieldType.Bool => true,
            FieldType.Int32 => 1,
            FieldType.Double => 1d,
            FieldType.String => "value",
            FieldType.Enum => field.EnumType.Values[1].Number,
            FieldType.Message => CreateMessage(field.MessageType),
            _ => throw new InvalidOperationException(
                $"No test value exists for {field.FullName} ({field.FieldType}).")
        };

    private static IMessage CreateMessage(MessageDescriptor descriptor)
    {
        var message = descriptor.Parser.ParseFrom(Array.Empty<byte>());
        foreach (var field in descriptor.Fields.InFieldNumberOrder())
        {
            if (field.IsRepeated)
            {
                var count = descriptor.FullName == "briosa.Transform" ? 16 : 1;
                for (var index = 0; index < count; index++)
                {
                    AddRepeatedValue(message, field, CreateFieldValue(field));
                }
            }
            else
            {
                field.Accessor.SetValue(message, CreateFieldValue(field));
            }
        }

        return message;
    }

    private static void AddRepeatedValue(
        IMessage message,
        FieldDescriptor field,
        object value)
    {
        var collection = field.Accessor.GetValue(message);
        var elementType = collection.GetType().GetGenericArguments().Single();
        var add = collection.GetType().GetMethods()
            .Single(method =>
                method.Name == "Add" &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == elementType);
        _ = add.Invoke(collection, [value]);
    }

    private static WorkerMpOutputValue CreateOutput(MpArgumentContract contract)
    {
        var limit = new WorkerToleranceLimit(true, 1);
        var scalarLimit = new WorkerToleranceLimit(true, 1);
        return contract.Kind switch
        {
            WorkerMpValueKind.Logical => Output(contract, new WorkerBooleanValue(true)),
            WorkerMpValueKind.WholeNumber => Output(contract, new WorkerIntegerValue(1)),
            WorkerMpValueKind.FloatingPoint => Output(contract, new WorkerDoubleValue(1)),
            WorkerMpValueKind.Text or WorkerMpValueKind.CollectionName =>
                Output(contract, new WorkerTextValue("value")),
            WorkerMpValueKind.DoubleArray =>
                Output(contract, new WorkerDoubleArrayValue([1d])),
            WorkerMpValueKind.EditText or WorkerMpValueKind.StringList =>
                Output(contract, new WorkerStringListValue(["value"])),
            WorkerMpValueKind.PointName =>
                Output(contract, new WorkerPointNameValue("Collection", "Group", "Point")),
            WorkerMpValueKind.PointNameList =>
                Output(contract, new WorkerPointNameListValue(
                    [new WorkerPointNameValue("Collection", "Group", "Point")])),
            WorkerMpValueKind.Vector =>
                Output(contract, new WorkerVectorValue(1, 2, 3)),
            WorkerMpValueKind.ToleranceVectorOptions =>
                Output(contract, new WorkerToleranceVectorOptionsValue(
                    limit, limit, limit, limit, limit, limit, limit, limit)),
            WorkerMpValueKind.Transform =>
                Output(contract, new WorkerTransformValue(Enumerable.Repeat(1d, 16).ToArray())),
            WorkerMpValueKind.WorldTransform =>
                Output(contract, new WorkerWorldTransformValue(
                    new WorkerTransformValue(Enumerable.Repeat(1d, 16).ToArray()),
                    1)),
            WorkerMpValueKind.FileReference =>
                Output(contract, new WorkerFileReferenceValue("C:\\file.txt", false)),
            WorkerMpValueKind.CollectionObjectName =>
                Output(contract, new WorkerCollectionObjectNameValue(
                    "Collection", "Object", WorkerObjectTypeValue.Any)),
            WorkerMpValueKind.CollectionObjectNameList =>
                Output(contract, new WorkerCollectionObjectNameListValue(
                    [new WorkerCollectionObjectNameValue(
                        "Collection", "Object", WorkerObjectTypeValue.Any)])),
            WorkerMpValueKind.CollectionItemName =>
                Output(contract, new WorkerCollectionItemNameValue(
                    "Collection", "Item", WorkerItemTypeValue.Any)),
            WorkerMpValueKind.CollectionItemNameList =>
                Output(contract, new WorkerCollectionItemNameListValue(
                    [new WorkerCollectionItemNameValue(
                        "Collection", "Item", WorkerItemTypeValue.Any)])),
            WorkerMpValueKind.VectorNameList =>
                Output(contract, new WorkerVectorNameListValue(
                    [new WorkerVectorNameValue("Collection", "Group", "Vector")])),
            WorkerMpValueKind.FitConstraintScalarOptions =>
                Output(contract, new WorkerFitConstraintScalarOptionsValue(
                    scalarLimit, scalarLimit)),
            WorkerMpValueKind.ToleranceScalarOptions =>
                Output(contract, new WorkerToleranceScalarOptionsValue(
                    scalarLimit, scalarLimit)),
            _ => throw new InvalidOperationException(
                $"No test output exists for {contract.Kind}.")
        };
    }

    private static WorkerRetrievedOutput Output(MpArgumentContract contract, WorkerMpValue value) =>
        new(contract.MpName, contract.Kind, value);
}
