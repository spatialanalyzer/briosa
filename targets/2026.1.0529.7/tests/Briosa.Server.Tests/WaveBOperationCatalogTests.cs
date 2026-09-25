using System.Collections;
using System.Reflection;
using Briosa.Server.Operations.WaveA;
using Briosa.Server.Operations.WaveB;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class WaveBOperationCatalogTests
{
    private static readonly Dictionary<string, ServiceDescriptor> Services =
        new[]
        {
            Api.CloudAndMeshOperations.Descriptor,
            Api.ConstructionOperations.Descriptor,
            Api.GdtOperations.Descriptor,
            Api.InstrumentOperations.Descriptor,
            Api.RelationshipOperations.Descriptor,
            Api.RobotCalibrationApplianceNodeOperations.Descriptor,
            Api.RobotOperations.Descriptor
        }.ToDictionary(service => service.FullName, StringComparer.Ordinal);

    [Fact]
    public void EveryWaveBContractMatchesItsProtobufMethodAndBuildsAWorkerCommand()
    {
        Assert.Equal(557, WaveBOperationCatalog.Operations.Count);
        Assert.Equal(
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["briosa.CloudAndMeshOperations"] = 28,
                ["briosa.ConstructionOperations"] = 214,
                ["briosa.GdtOperations"] = 35,
                ["briosa.InstrumentOperations"] = 168,
                ["briosa.RelationshipOperations"] = 54,
                ["briosa.RobotCalibrationApplianceNodeOperations"] = 25,
                ["briosa.RobotOperations"] = 33
            },
            WaveBOperationCatalog.Operations
                .GroupBy(operation => operation.Descriptor.GrpcService, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal));
        Assert.Equal(
            WaveBOperationCatalog.Operations.Count,
            WaveBOperationCatalog.Operations
                .Select(operation => operation.Descriptor.OperationId)
                .Distinct(StringComparer.Ordinal)
                .Count());

        foreach (var operation in WaveBOperationCatalog.Operations)
        {
            var descriptor = operation.Descriptor;
            var service = Services[descriptor.GrpcService];
            var method = Assert.Single(
                service.Methods,
                candidate => candidate.Name == descriptor.Rpc);
            var contractRequestFields = operation.Inputs
                .Select(input => input.FieldName)
                .Concat(operation.Outputs
                    .Select(output => output.ArraySizeFieldName)
                    .OfType<string>())
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);
            var contractResultFields = operation.Outputs
                .Select(output => output.FieldName)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);

            Assert.Equal(
                contractRequestFields,
                method.InputType.Fields.InFieldNumberOrder()
                    .Select(field => field.Name)
                    .Order(StringComparer.Ordinal));
            Assert.Equal(
                contractResultFields,
                method.OutputType.Fields.InFieldNumberOrder()
                    .Where(field => field.Name != "execution")
                    .Select(field => field.Name)
                    .Order(StringComparer.Ordinal));

            var request = method.InputType.Parser.ParseFrom(Array.Empty<byte>());
            PopulateRequiredFields(request, operation);
            WorkerMpCommand? command = null;
            var exception = Record.Exception(() => command = operation.CreateCommand(request));
            Assert.True(
                exception is null,
                $"{descriptor.OperationId} could not build a worker command: {exception}");
            Assert.NotNull(command);

            Assert.Equal(descriptor.OperationId, command.OperationId);
            Assert.Equal(descriptor.MpStep, command.StepName);
            Assert.Equal(
                operation.Inputs
                    .Where(input => input.Required || !input.OmitWhenAbsent)
                    .Select(input => (input.MpName, input.Kind, input.SdkBinding)),
                command.InputArguments.Select(input =>
                    (input.Name, input.Kind, input.SdkBinding!)));
            Assert.Equal(
                operation.Outputs.Select(output =>
                    (output.MpName, output.Kind, output.SdkBinding)),
                command.OutputArguments.Select(output =>
                    (output.Name, output.Kind, output.SdkBinding!)));
            Assert.All(
                operation.Outputs.Where(output => output.ArraySizeFieldName is not null),
                output => Assert.Equal(
                    1,
                    Assert.Single(command.OutputArguments,
                        argument => argument.Name == output.MpName).ArraySize));
        }
    }

    [Fact]
    public void EveryWaveBContractMapsACompleteWorkerResultToItsProtobufResult()
    {
        var createResult = typeof(MpOperationContract)
            .GetMethod(nameof(MpOperationContract.CreateResult))!;
        var failures = new List<string>();

        foreach (var operation in WaveBOperationCatalog.Operations)
        {
            var method = Services[operation.Descriptor.GrpcService].Methods.Single(candidate =>
                candidate.Name == operation.Descriptor.Rpc);
            var completed = new SuccessfulOperationExecution(
                WorkerMpExecutionResult.FromEvidence(
                    executeStepReturned: true,
                    mpResultRetrieved: true,
                    mpSucceeded: true,
                    mpResultCode: 2,
                    durationMilliseconds: 1,
                    operation.Outputs.Select(CreateOutputValue).ToArray(),
                    diagnosticCode: null),
                new Api.MpExecutionDetails());

            var exception = Record.Exception(() =>
                createResult.MakeGenericMethod(method.OutputType.ClrType)
                    .Invoke(operation, [completed]));

            if (exception is not null)
            {
                failures.Add(
                    $"{operation.Descriptor.OperationId}: " +
                    $"{exception.InnerException?.Message ?? exception.Message}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void RobotInterfaceArgumentsUseInstrumentIdsAndRejectRetiredWireFields()
    {
        IMessage[] requests =
        [
            new Api.GetRobotMachineParameterRequest { MachineId = new() { CollectionName = "Robot", InstrumentId = 7 } },
            new Api.StartRobotMachineInterfaceRequest { MachineId = new() { CollectionName = "Robot", InstrumentId = 7 } },
            new Api.StopRobotMachineInterfaceRequest { MachineId = new() { CollectionName = "Robot", InstrumentId = 7 } }
        ];
        string[] ids = ["get_robot_machine_parameter", "start_robot_machine_interface", "stop_robot_machine_interface"];
        for (var index = 0; index < requests.Length; index++)
        {
            var operation = MpOperationCatalog.Get("robot_operations." + ids[index]);
            var input = Assert.Single(operation.CreateCommand(requests[index]).InputArguments,
                argument => argument.Name == "Machine ID");
            Assert.Equal(WorkerMpValueKind.CollectionInstrumentId, input.Kind);
            Assert.Equal("SetColInstIdArg", input.SdkBinding);
            Assert.Equal(new WorkerCollectionInstrumentIdValue("Robot", 7), input.CollectionInstrumentIdValue);
            Assert.Null(input.CollectionMachineIdValue);

            // Field 1 was a CollectionMachineId. It must never be reinterpreted as an instrument.
            var retired = requests[index].Descriptor.Parser.ParseFrom(new byte[] { 10, 5, 10, 1, 67, 16, 7 });
            Assert.Throws<ArgumentException>(() => operation.CreateCommand(retired));
        }
    }

    [Theory]
    [InlineData("create_point_callout", "notes")]
    [InlineData("create_point_comparison_callout", "additional_notes")]
    [InlineData("create_relationship_callout", "additional_notes")]
    [InlineData("create_text_callout", "text")]
    [InlineData("create_vector_callout", "additional_notes")]
    public void CalloutNotesUseEditTextForSuppliedAndDefaultValues(string id, string fieldName)
    {
        var operation = MpOperationCatalog.Get("construction_operations." + id);
        var method = Services[operation.Descriptor.GrpcService].Methods.Single(candidate =>
            candidate.Name == operation.Descriptor.Rpc);
        var request = method.InputType.Parser.ParseFrom(Array.Empty<byte>());
        PopulateRequiredFields(request, operation);
        var defaults = operation.CreateCommand(request).InputArguments.Where(input => input.SdkBinding == "SetEditTextArg");
        if (id == "create_text_callout") Assert.Empty(defaults);
        else
        {
            var input = Assert.Single(defaults);
            Assert.Equal(WorkerMpValueKind.EditText, input.Kind);
            Assert.Empty(input.StringListValue!.Values);
        }
        var field = request.Descriptor.FindFieldByName(fieldName);
        AddRepeatedValue(request, field, "first line");
        AddRepeatedValue(request, field, "second line");
        var command = operation.CreateCommand(request);
        using var stream = new MemoryStream();
        using var channel = new WorkerControlChannel(stream, leaveOpen: true);
        channel.Send(WorkerControlMessage.Execute(Guid.NewGuid(), command));
        stream.Position = 0;
        var text = Assert.Single(channel.Receive().Command!.InputArguments,
            input => input.SdkBinding == "SetEditTextArg");
        Assert.Equal(WorkerMpValueKind.EditText, text.Kind);
        Assert.Equal(["first line", "second line"], text.StringListValue!.Values);
    }

    [Fact]
    public void TcpUncertaintyResultNotesUseEditText()
    {
        var operation = MpOperationCatalog.Get("instrument_operations.calculate_tcp_fixture_uncertainties");
        var notes = Assert.Single(operation.Outputs, output => output.MpName == "Result Notes");
        Assert.Equal(WorkerMpValueKind.EditText, notes.Kind);
        Assert.Equal("GetEditTextArg", notes.SdkBinding);
        var outputs = operation.Outputs.Select(CreateOutputValue).ToArray();
        var result = operation.CreateResult<Api.CalculateTcpFixtureUncertaintiesResult>(Completed(outputs));
        Assert.Equal(["value"], result.Uncertainties.ResultNotes);
    }

    [Fact]
    public void GdtExtendedOptionsUseExactPerArgumentChoices()
    {
        var operation = WaveBOperationCatalog.Operations.Single(candidate =>
            candidate.Descriptor.OperationId == "gdt_operations.set_gdt_extended_options");
        var request = new Api.SetGdtExtendedOptionsRequest
        {
            PlaneExtendedOptions = Api.GdtExtendedEvaluationMethod.EqualizedLsqHighPoint
        };

        var command = operation.CreateCommand(request);
        var plane = Assert.Single(command.InputArguments, argument =>
            argument.Name == "Plane Extended Options");
        Assert.Equal("Equalized LSQ High Point", plane.StringValue);

        request.ConeExtendedOptions = Api.GdtExtendedEvaluationMethod.HighPoint;
        Assert.Throws<ArgumentException>(() => operation.CreateCommand(request));
    }

    [Fact]
    public void InstrumentTypeNameRemainsAnExactOpenStringWrapper()
    {
        var operation = WaveBOperationCatalog.Operations.Single(candidate =>
            candidate.Descriptor.OperationId == "instrument_operations.add_new_instrument");
        var command = operation.CreateCommand(new Api.AddNewInstrumentRequest
        {
            InstrumentType = new Api.InstrumentTypeName { Value = "Future Instrument Type" }
        });

        var instrumentType = Assert.Single(command.InputArguments);
        Assert.Equal(WorkerMpValueKind.InstrumentTypeName, instrumentType.Kind);
        Assert.Equal("Future Instrument Type", instrumentType.StringValue);
    }

    [Fact]
    public void RelationshipWatchWindowMapsUdpSettingsWithoutAmbientState()
    {
        var operation = WaveBOperationCatalog.Operations.Single(candidate =>
            candidate.Descriptor.OperationId == "relationship_operations.relationship_watch_window_template");
        var command = operation.CreateCommand(new Api.RelationshipWatchWindowTemplateRequest
        {
            WatchWindowTemplateName = new Api.CollectionObjectName
            {
                CollectionName = "Templates",
                ObjectName = "Default"
            },
            UdpNetworkTransmitSettings = new Api.RelationshipWatchWindowUdpSettings
            {
                Enabled = true,
                Broadcast = false,
                IpAddress = "127.0.0.1",
                Port = 12000
            }
        });

        var udp = Assert.Single(command.InputArguments, argument =>
            argument.Kind == WorkerMpValueKind.UdpTransmitSettings)
            .UdpTransmitSettingsValue!;
        Assert.True(udp.Enabled);
        Assert.False(udp.Broadcast);
        Assert.Equal("127.0.0.1", udp.IpAddress);
        Assert.Equal(12000, udp.Port);
    }

    [Fact]
    public void WaveBRelationshipIdentitiesUseTheRelationshipItemDomain()
    {
        var operation = WaveBOperationCatalog.Operations.Single(candidate =>
            candidate.Descriptor.OperationId ==
                "relationship_operations.compute_geometry_relationship_uncertainties");
        var command = operation.CreateCommand(
            new Api.ComputeGeometryRelationshipUncertaintiesRequest
            {
                RelationshipName = new Api.CollectionItemName
                {
                    CollectionName = "Relationships",
                    ItemName = "Best Fit"
                }
            });

        var relationship = Assert.Single(command.InputArguments, argument =>
            argument.Name == "Relationship Name");
        Assert.Equal(WorkerMpValueKind.CollectionItemName, relationship.Kind);
        Assert.Equal(
            WorkerItemTypeValue.Relationship,
            relationship.CollectionItemNameValue!.ItemType);
    }

    [Fact]
    public void RobotArrayRetrievalMetadataNeverBecomesAnMpInput()
    {
        var operation = WaveBOperationCatalog.Operations.Single(candidate =>
            candidate.Descriptor.OperationId == "robot_operations.get_robot_pose_for_a_frame");
        var command = operation.CreateCommand(new Api.GetRobotPoseForAFrameRequest
        {
            MachineId = new Api.CollectionMachineId
            {
                CollectionName = "Robots",
                MachineId = 1
            },
            GoalFrame = new Api.CollectionObjectName
            {
                CollectionName = "Frames",
                ObjectName = "Goal"
            },
            GoalPoseCount = 6
        });

        Assert.DoesNotContain(command.InputArguments, argument =>
            argument.Name == "Goal Pose" || argument.Name == "Integer Values");
        var output = Assert.Single(command.OutputArguments);
        Assert.Equal("Goal Pose", output.Name);
        Assert.Equal(6, output.ArraySize);
    }

    [Fact]
    public void RobotCalibrationMetricsPopulateTheNestedResult()
    {
        var operation = WaveBOperationCatalog.Operations.Single(candidate =>
            candidate.Descriptor.OperationId == "robot_operations.perform_robot_calibration");
        var outputs = operation.Outputs
            .Select((output, index) => new WorkerMpOutputValue(
                output.MpName,
                output.Kind,
                Retrieved: true,
                DoubleValue: index + 1))
            .ToArray();
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

        var result = operation.CreateResult<Api.PerformRobotCalibrationResult>(completed);

        Assert.NotNull(result.Metrics);
        Assert.Equal(1, result.Metrics.XyzMax);
        Assert.Equal(7, result.Metrics.Robustness);
    }

    [Fact]
    public void RepeatedMpOutputLabelsRemainPositionallyDistinct()
    {
        var operation = WaveBOperationCatalog.Operations.Single(candidate =>
            candidate.Descriptor.OperationId ==
                "construction_operations.decompose_transform_into_doubles_euler_zxz");
        var outputs = operation.Outputs.Select((output, index) =>
            CreateOutputValue(output) with { DoubleValue = index + 1 }).ToArray();
        var result = operation.CreateResult<Api.DecomposeTransformIntoDoublesEulerZxzResult>(
            Completed(outputs));

        Assert.Equal(4, result.FirstRz);
        Assert.Equal(6, result.SecondRz);
    }

    [Fact]
    public void RobotTextEnumsUseExactSdkValuesAndFailClosed()
    {
        var operation = WaveBOperationCatalog.Operations.Single(candidate =>
            candidate.Descriptor.OperationId ==
                "robot_operations.get_robot_machine_model_link_parameters");
        var outputs = operation.Outputs.Select(CreateOutputValue).ToArray();
        outputs[0] = outputs[0] with { StringValue = "6DOF" };
        outputs[14] = outputs[14] with { StringValue = "THETA" };

        var result = operation.CreateResult<Api.GetRobotMachineModelLinkParametersResult>(
            Completed(outputs));

        Assert.Equal(Api.RobotModelLinkType.SixDof, result.LinkType);
        Assert.Equal(Api.RobotActiveJointComponent.Theta, result.ActiveJointComponent);

        outputs[0] = outputs[0] with { StringValue = "Six Dof" };
        Assert.Throws<InvalidOperationException>(() =>
            operation.CreateResult<Api.GetRobotMachineModelLinkParametersResult>(
                Completed(outputs)));
    }

    private static void PopulateRequiredFields(IMessage request, MpOperationContract operation)
    {
        foreach (var contract in operation.Inputs.Where(contract => contract.Required))
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

        foreach (var fieldName in operation.Outputs
                     .Select(output => output.ArraySizeFieldName)
                     .OfType<string>())
        {
            var field = request.Descriptor.FindFieldByName(fieldName)!;
            field.Accessor.SetValue(request, 1);
        }
    }

    private static object CreateFieldValue(FieldDescriptor field) =>
        field.FieldType switch
        {
            FieldType.Bool => true,
            FieldType.Int32 => 1,
            FieldType.Int64 => 1L,
            FieldType.Double => 1d,
            FieldType.String => "value",
            FieldType.Bytes => ByteString.CopyFrom([1]),
            FieldType.Enum => field.EnumType.Values[1].Number,
            FieldType.Message => CreateMessage(field.MessageType),
            _ => throw new InvalidOperationException(
                $"No test value exists for {field.FullName} ({field.FieldType}).")
        };

    private static IMessage CreateMessage(MessageDescriptor descriptor)
    {
        var message = descriptor.Parser.ParseFrom(Array.Empty<byte>());
        var populatedOneofs = new HashSet<OneofDescriptor>();
        foreach (var field in descriptor.Fields.InFieldNumberOrder())
        {
            if (field.ContainingOneof is { IsSynthetic: false } oneof &&
                !populatedOneofs.Add(oneof))
            {
                continue;
            }

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

    private static WorkerMpOutputValue CreateOutputValue(MpArgumentContract output)
    {
        var tolerance = new WorkerToleranceLimit(true, 1);
        var scalarTolerance = new WorkerToleranceLimit(true, 1);
        return new WorkerMpOutputValue(
            output.MpName,
            output.Kind,
            Retrieved: true,
            BooleanValue: true,
            IntegerValue: 1,
            DoubleValue: 1,
            StringValue: output.EnumTextValues is { Count: > 0 } sdkValues
                ? sdkValues[0]
                : "value",
            PointNameValue: new("Collection", "Group", "Point"),
            VectorValue: new(1, 2, 3),
            ToleranceVectorOptionsValue: new(
                tolerance,
                tolerance,
                tolerance,
                tolerance,
                tolerance,
                tolerance,
                tolerance,
                tolerance),
            CollectionInstrumentIdValue: new("Collection", 1),
            CollectionInstrumentIdListValue: new([new("Collection", 1)]),
            CollectionMachineIdValue: new("Collection", 1),
            CollectionItemNameValue: new("Collection", "Item", WorkerItemTypeValue.Relationship),
            CollectionItemNameListValue: new([
                new("Collection", "Item", WorkerItemTypeValue.Relationship)]),
            CollectionObjectNameValue: new("Collection", "Object", WorkerObjectTypeValue.PointGroup),
            CollectionObjectNameListValue: new([
                new("Collection", "Object", WorkerObjectTypeValue.PointGroup)]),
            CollectionGroupNameListValue: new([new("Collection", "Group")]),
            CollectionVectorGroupNameValue: new("Collection", "Vector Group"),
            CollectionVectorGroupNameListValue: new([new("Collection", "Vector Group")]),
            PointNameListValue: new([new("Collection", "Group", "Point")]),
            StringListValue: new(["value"]),
            VectorNameListValue: new([new("Collection", "Group", "Vector")]),
            DoubleArrayValue: new([1, 2, 3, 4, 5, 6]),
            TransformValue: new(Enumerable.Range(1, 16).Select(Convert.ToDouble).ToArray()),
            WorldTransformValue: new(
                new(Enumerable.Range(1, 16).Select(Convert.ToDouble).ToArray()),
                1),
            FileReferenceValue: new("file.txt", false),
            FitConstraintScalarOptionsValue: new(scalarTolerance, scalarTolerance),
            ToleranceScalarOptionsValue: new(scalarTolerance, scalarTolerance));
    }

    private static SuccessfulOperationExecution Completed(
        IReadOnlyList<WorkerMpOutputValue> outputs) =>
        new(
            WorkerMpExecutionResult.FromEvidence(
                executeStepReturned: true,
                mpResultRetrieved: true,
                mpSucceeded: true,
                mpResultCode: 2,
                durationMilliseconds: 1,
                outputs,
                diagnosticCode: null),
            new Api.MpExecutionDetails());
}
