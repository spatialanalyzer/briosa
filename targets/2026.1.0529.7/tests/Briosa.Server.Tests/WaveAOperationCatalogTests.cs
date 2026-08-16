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
                new WorkerMpExecutionResult(
                    ExecuteStepReturned: true,
                    MpResultRetrieved: true,
                    MpSucceeded: true,
                    MpResultCode: 2,
                    DurationMilliseconds: 1,
                    outputs,
                    DiagnosticCode: null),
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
    public void SetDecimalDigitsUsesTheReviewedSpatialAnalyzerDefaults()
    {
        var command = WaveAOperationCatalog
            .Get("utility_operations.set_decimal_digits_for_display")
            .CreateCommand(new Api.SetDecimalDigitsForDisplayRequest());

        Assert.Equal(
            ExpectedDecimalDigits,
            command.InputArguments.Select(argument => argument.IntegerValue!.Value));
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
        var scalarLimit = new WorkerScalarToleranceLimit(true, 1);
        return contract.Kind switch
        {
            WorkerMpValueKind.Logical => Output(contract, BooleanValue: true),
            WorkerMpValueKind.WholeNumber => Output(contract, IntegerValue: 1),
            WorkerMpValueKind.FloatingPoint => Output(contract, DoubleValue: 1),
            WorkerMpValueKind.Text or WorkerMpValueKind.CollectionName =>
                Output(contract, StringValue: "value"),
            WorkerMpValueKind.DoubleArray =>
                Output(contract, DoubleArrayValue: new([1d])),
            WorkerMpValueKind.EditText or WorkerMpValueKind.StringList =>
                Output(contract, StringListValue: new(["value"])),
            WorkerMpValueKind.PointName =>
                Output(contract, PointNameValue: new("Collection", "Group", "Point")),
            WorkerMpValueKind.PointNameList =>
                Output(contract, PointNameListValue: new(
                    [new WorkerPointNameValue("Collection", "Group", "Point")])),
            WorkerMpValueKind.Vector =>
                Output(contract, VectorValue: new(1, 2, 3)),
            WorkerMpValueKind.ToleranceVectorOptions =>
                Output(contract, ToleranceVectorOptionsValue: new(
                    limit, limit, limit, limit, limit, limit, limit, limit)),
            WorkerMpValueKind.Transform =>
                Output(contract, TransformValue: new(Enumerable.Repeat(1d, 16).ToArray())),
            WorkerMpValueKind.WorldTransform =>
                Output(contract, WorldTransformValue: new(
                    new WorkerTransformValue(Enumerable.Repeat(1d, 16).ToArray()),
                    1)),
            WorkerMpValueKind.FileReference =>
                Output(contract, FileReferenceValue: new("C:\\file.txt", false)),
            WorkerMpValueKind.CollectionObjectName =>
                Output(contract, CollectionObjectNameValue: new(
                    "Collection", "Object", WorkerObjectTypeValue.Any)),
            WorkerMpValueKind.CollectionObjectNameList =>
                Output(contract, CollectionObjectNameListValue: new(
                    [new WorkerCollectionObjectNameValue(
                        "Collection", "Object", WorkerObjectTypeValue.Any)])),
            WorkerMpValueKind.CollectionItemName =>
                Output(contract, CollectionItemNameValue: new(
                    "Collection", "Item", WorkerItemTypeValue.Any)),
            WorkerMpValueKind.CollectionItemNameList =>
                Output(contract, CollectionItemNameListValue: new(
                    [new WorkerCollectionItemNameValue(
                        "Collection", "Item", WorkerItemTypeValue.Any)])),
            WorkerMpValueKind.VectorNameList =>
                Output(contract, VectorNameListValue: new(
                    [new WorkerVectorNameValue("Collection", "Group", "Vector")])),
            WorkerMpValueKind.FitConstraintScalarOptions =>
                Output(contract, FitConstraintScalarOptionsValue: new(
                    scalarLimit, scalarLimit)),
            WorkerMpValueKind.ToleranceScalarOptions =>
                Output(contract, ToleranceScalarOptionsValue: new(
                    scalarLimit, scalarLimit)),
            _ => throw new InvalidOperationException(
                $"No test output exists for {contract.Kind}.")
        };
    }

    private static WorkerMpOutputValue Output(
        MpArgumentContract contract,
        bool? BooleanValue = null,
        int? IntegerValue = null,
        double? DoubleValue = null,
        string? StringValue = null,
        WorkerPointNameValue? PointNameValue = null,
        WorkerVectorValue? VectorValue = null,
        WorkerToleranceVectorOptionsValue? ToleranceVectorOptionsValue = null,
        WorkerCollectionItemNameValue? CollectionItemNameValue = null,
        WorkerCollectionItemNameListValue? CollectionItemNameListValue = null,
        WorkerCollectionObjectNameValue? CollectionObjectNameValue = null,
        WorkerCollectionObjectNameListValue? CollectionObjectNameListValue = null,
        WorkerPointNameListValue? PointNameListValue = null,
        WorkerStringListValue? StringListValue = null,
        WorkerVectorNameListValue? VectorNameListValue = null,
        WorkerDoubleArrayValue? DoubleArrayValue = null,
        WorkerTransformValue? TransformValue = null,
        WorkerWorldTransformValue? WorldTransformValue = null,
        WorkerFileReferenceValue? FileReferenceValue = null,
        WorkerFitConstraintScalarOptionsValue? FitConstraintScalarOptionsValue = null,
        WorkerToleranceScalarOptionsValue? ToleranceScalarOptionsValue = null) =>
        new(
            contract.MpName,
            contract.Kind,
            Retrieved: true,
            BooleanValue,
            IntegerValue,
            DoubleValue,
            StringValue,
            PointNameValue,
            VectorValue,
            ToleranceVectorOptionsValue,
            CollectionItemNameValue: CollectionItemNameValue,
            CollectionItemNameListValue: CollectionItemNameListValue,
            CollectionObjectNameValue: CollectionObjectNameValue,
            CollectionObjectNameListValue: CollectionObjectNameListValue,
            PointNameListValue: PointNameListValue,
            StringListValue: StringListValue,
            VectorNameListValue: VectorNameListValue,
            DoubleArrayValue: DoubleArrayValue,
            TransformValue: TransformValue,
            WorldTransformValue: WorldTransformValue,
            FileReferenceValue: FileReferenceValue,
            FitConstraintScalarOptionsValue: FitConstraintScalarOptionsValue,
            ToleranceScalarOptionsValue: ToleranceScalarOptionsValue);
}
