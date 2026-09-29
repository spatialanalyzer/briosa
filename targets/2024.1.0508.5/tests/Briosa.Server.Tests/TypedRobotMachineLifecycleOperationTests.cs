using Briosa.Server.Operations;
using Briosa.Server.Operations.RobotOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRobotMachineLifecycleOperationTests
{
    [Fact]
    public void LifecycleCommandsHaveOneTypedRegistrationAndNoCatalogEntry()
    {
        string[] ids =
        [
            "robot_operations.add_robot_machine_manip_kin",
            "robot_operations.add_robot_machine_sa_machine",
            "robot_operations.delete_robot_machine",
            "robot_operations.get_robot_machine_parameter",
            "robot_operations.set_robot_machine_parameter",
            "robot_operations.start_robot_machine_interface",
            "robot_operations.stop_robot_machine_interface",
            "robot_operations.set_robot_machine_base_transform",
            "robot_operations.get_robot_machine_model_link_parameters",
            "robot_operations.set_robot_machine_model_link_parameters",
            "robot_operations.create_robot_calibration",
            "robot_operations.delete_robot_calibration",
            "robot_operations.set_active_robot_calibration",
            "robot_operations.set_robot_calibration_tool_frame",
            "robot_operations.set_robot_calibration_measurement_offset_in_tool_frame"
        ];
        foreach (var id in ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
        Assert.Contains("destructive", DeleteRobotMachineOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void AddCommandsOmitAbsentOptionalFilesAndPreserveSuppliedBinding()
    {
        Assert.Empty(AddRobotMachineManipKinOperation.CreateCommand(new()).InputArguments);
        Assert.Empty(AddRobotMachineSaMachineOperation.CreateCommand(new()).InputArguments);
        var manip = AddRobotMachineManipKinOperation.CreateCommand(new()
        {
            ManipKinFile = new() { Path = "robot.ManipKin", EmbeddedFile = true }
        });
        var sa = AddRobotMachineSaMachineOperation.CreateCommand(new()
        {
            SaMachineFile = new() { Path = "robot.SAMachine" }
        });
        Assert.Equal(".ManipKin File", Assert.Single(manip.InputArguments).Name);
        Assert.Equal(".SAMachine File", Assert.Single(sa.InputArguments).Name);
        Assert.All(new[] { manip, sa }, command =>
        {
            var input = Assert.Single(command.InputArguments);
            Assert.Equal(WorkerMpValueKind.FileReference, input.Kind);
            Assert.Equal("SetFilePathArg", input.SdkBinding);
        });
        Assert.True(Assert.Single(manip.InputArguments).RequireValue<WorkerFileReferenceValue>().EmbeddedFile);
    }

    [Fact]
    public void DeleteRequiresMachineIdentityAndUsesMachineIdBinding()
    {
        Assert.Throws<ArgumentException>(() => DeleteRobotMachineOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => DeleteRobotMachineOperation.CreateCommand(new()
        {
            MachineId = new() { MachineId = 7 }
        }));
        var command = DeleteRobotMachineOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 7 }
        });
        var input = Assert.Single(command.InputArguments);
        Assert.Equal("SetColMachineIdArg", input.SdkBinding);
        Assert.Equal(new WorkerCollectionMachineIdValue("Robot", 7), input.RequireValue<WorkerCollectionMachineIdValue>());
    }

    [Fact]
    public void ParameterAndInterfaceCommandsPreserveExactBindingsAndDefaults()
    {
        var get = GetRobotMachineParameterOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", InstrumentId = 7 }
        });
        Assert.Equal("robot_operations.get_robot_machine_parameter", get.OperationId);
        Assert.Equal("", get.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Parameter Value", Assert.Single(get.OutputArguments).Name);
        Assert.Equal("GetDoubleArg", Assert.Single(get.OutputArguments).SdkBinding);

        var set = SetRobotMachineParameterOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 }
        });
        Assert.Equal(new WorkerCollectionMachineIdValue("Robot", 4),
            set.InputArguments[0].RequireValue<WorkerCollectionMachineIdValue>());
        Assert.Equal("", set.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(0d, set.InputArguments[2].RequireValue<WorkerDoubleValue>().Value);

        var start = StartRobotMachineInterfaceOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", InstrumentId = 7 }
        });
        Assert.Equal(new WorkerCollectionInstrumentIdValue("Robot", 7),
            start.InputArguments[0].RequireValue<WorkerCollectionInstrumentIdValue>());
        Assert.Equal(0, start.InputArguments[1].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(start.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        Assert.Throws<ArgumentException>(() => GetRobotMachineParameterOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetRobotMachineParameterOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => StartRobotMachineInterfaceOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => StopRobotMachineInterfaceOperation.CreateCommand(new()));
    }

    [Fact]
    public void RobotBaseTransformUsesReviewedFrameFallbackAndExactArgumentOrder()
    {
        var command = SetRobotMachineBaseTransformOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            DestinationTransform = new() { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } },
            ReferenceFrame = new() { CollectionName = "Collection", ObjectName = "Reference" }
        });

        Assert.Equal(
            ["Machine ID", "Destination Transform", "Reference Frame", "Number of Steps"],
            command.InputArguments.Select(argument => argument.Name));
        Assert.Equal(WorkerObjectTypeValue.Frame,
            command.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal(0, command.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.Throws<ArgumentException>(() => SetRobotMachineBaseTransformOperation.CreateCommand(new()));
    }

    [Fact]
    public void RobotModelLinkOperationsMapEveryTypedFieldAndExactChoice()
    {
        var getCommand = GetRobotMachineModelLinkParametersOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 }
        });
        Assert.Equal(2, getCommand.InputArguments.Count);
        Assert.Equal(26, getCommand.OutputArguments.Count);
        Assert.Equal("", getCommand.InputArguments[1].RequireValue<WorkerTextValue>().Value);

        var setRequest = new Api.SetRobotMachineModelLinkParametersRequest
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            LinkName = "Link 1",
            LinkType = Api.RobotModelLinkType.SixDof,
            ActiveJointComponent = Api.RobotActiveJointComponent.Rz,
            SegmentCgInSegment = new() { X = 1, Y = 2, Z = 3 }
        };
        var setCommand = SetRobotMachineModelLinkParametersOperation.CreateCommand(setRequest);
        Assert.Equal(27, setCommand.InputArguments.Count);
        Assert.Equal("6DOF", setCommand.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Rz", setCommand.InputArguments[16].RequireValue<WorkerTextValue>().Value);
        Assert.Equal(new WorkerVectorValue(1, 2, 3), setCommand.InputArguments[26].RequireValue<WorkerVectorValue>());

        var defaults = SetRobotMachineModelLinkParametersOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            SegmentCgInSegment = new()
        });
        Assert.Equal("DH", defaults.InputArguments[2].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("NONE", defaults.InputArguments[16].RequireValue<WorkerTextValue>().Value);
        Assert.Throws<ArgumentException>(() => SetRobotMachineModelLinkParametersOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            LinkType = Api.RobotModelLinkType.Unspecified,
            SegmentCgInSegment = new()
        }));

        var outputValues = GetRobotMachineModelLinkParametersOperation.OutputContracts
            .Select((contract, index) => new WorkerRetrievedOutput(contract.ArgumentName, contract.Kind,
                contract.Kind switch
                {
                    WorkerMpValueKind.Text => new WorkerTextValue(index == 0 ? "6DOF" : "RZ"),
                    WorkerMpValueKind.Logical => new WorkerBooleanValue(index == 19),
                    WorkerMpValueKind.WholeNumber => new WorkerIntegerValue(7),
                    WorkerMpValueKind.Vector => new WorkerVectorValue(4, 5, 6),
                    _ => new WorkerDoubleValue(index)
                }))
            .Cast<WorkerMpOutputValue>()
            .ToArray();
        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputValues, "completed");
        var result = GetRobotMachineModelLinkParametersOperation.CreateResult(
            new SuccessfulOperationExecution(execution, new Api.MpExecutionDetails()));
        Assert.True(result.HasLinkType);
        Assert.Equal(Api.RobotModelLinkType.SixDof, result.LinkType);
        Assert.Equal(Api.RobotActiveJointComponent.Rz, result.ActiveJointComponent);
        Assert.Equal(new Api.Vector { X = 4, Y = 5, Z = 6 }, result.SegmentCgInSegment);
        Assert.True(result.EncoderSenseNegative);
        Assert.False(result.IncludeAdditionalEncoder);
    }

    [Fact]
    public void RobotCalibrationLifecycleUsesMachineIdentityAndPreservesDestructivePolicy()
    {
        var create = CreateRobotCalibrationOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 }
        });
        var delete = DeleteRobotCalibrationOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            CalibrationName = "Old"
        });
        var activate = SetActiveRobotCalibrationOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            CalibrationName = "Current"
        });

        Assert.Equal(new WorkerCollectionMachineIdValue("Robot", 4),
            create.InputArguments[0].RequireValue<WorkerCollectionMachineIdValue>());
        Assert.Equal("", create.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Old", delete.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("Current", activate.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Contains("destructive", DeleteRobotCalibrationOperation.Descriptor.RiskFlags);
        Assert.Throws<ArgumentException>(() => CreateRobotCalibrationOperation.CreateCommand(new()));
    }

    [Fact]
    public void RobotCalibrationFramesUseTypedTransformsAndPreserveOptionalCalibrationName()
    {
        var transform = new Api.Transform { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } };
        var toolFrame = SetRobotCalibrationToolFrameOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            ToolFrame = transform
        });
        var offset = SetRobotCalibrationMeasurementOffsetInToolFrameOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            MeasurementFrame = transform
        });

        Assert.Equal("", toolFrame.InputArguments[1].RequireValue<WorkerTextValue>().Value);
        Assert.Equal("SetTransformArg", toolFrame.InputArguments[2].SdkBinding);
        Assert.Equal(transform.Values, toolFrame.InputArguments[2].RequireValue<WorkerTransformValue>().Values);
        Assert.Equal("Measurement Frame (relative to tool)", offset.InputArguments[2].Name);
        Assert.Throws<ArgumentException>(() => SetRobotCalibrationToolFrameOperation.CreateCommand(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 }, ToolFrame = new()
        }));
    }

    [Fact]
    public async Task GeneratedClientReachesTypedRobotAddRoute()
    {
        var worker = new RobotWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RobotOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RobotOperations.RobotOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var result = await client.AddRobotMachineSaMachineAsync(new()
        {
            SaMachineFile = new() { Path = "robot.SAMachine" }
        }, options);

        var parameter = await client.GetRobotMachineParameterAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", InstrumentId = 7 },
            ParameterName = "Reach"
        }, options);
        await client.SetRobotMachineParameterAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            ParameterName = "Reach",
            ParameterValue = 2.5
        }, options);
        await client.StartRobotMachineInterfaceAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", InstrumentId = 7 },
            InterfaceType = 2,
            RunInSimulation = true
        }, options);
        await client.StopRobotMachineInterfaceAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", InstrumentId = 7 }
        }, options);
        await client.SetRobotMachineBaseTransformAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            DestinationTransform = new() { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } },
            ReferenceFrame = new() { CollectionName = "Collection", ObjectName = "Reference" }
        }, options);
        await client.SetRobotMachineModelLinkParametersAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            SegmentCgInSegment = new()
        }, options);
        var modelLink = await client.GetRobotMachineModelLinkParametersAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 }
        }, options);
        await client.CreateRobotCalibrationAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 }
        }, options);
        await client.DeleteRobotCalibrationAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            CalibrationName = "Old"
        }, options);
        await client.SetActiveRobotCalibrationAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            CalibrationName = "Current"
        }, options);
        await client.SetRobotCalibrationToolFrameAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            ToolFrame = new() { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } }
        }, options);
        await client.SetRobotCalibrationMeasurementOffsetInToolFrameAsync(new()
        {
            MachineId = new() { CollectionName = "Robot", MachineId = 4 },
            MeasurementFrame = new() { Values = { Enumerable.Range(0, 16).Select(index => (double)index) } }
        }, options);

        Assert.Equal(Api.MpExecutionState.Succeeded, result.Execution.State);
        Assert.Equal(42.5, parameter.ParameterValue);
        Assert.Equal(Api.RobotModelLinkType.Dh, modelLink.LinkType);
        Assert.Equal(
            [
                "robot_operations.add_robot_machine_sa_machine",
                "robot_operations.get_robot_machine_parameter",
                "robot_operations.set_robot_machine_parameter",
                "robot_operations.start_robot_machine_interface",
                "robot_operations.stop_robot_machine_interface",
                "robot_operations.set_robot_machine_base_transform",
                "robot_operations.set_robot_machine_model_link_parameters",
                "robot_operations.get_robot_machine_model_link_parameters",
                "robot_operations.create_robot_calibration",
                "robot_operations.delete_robot_calibration",
                "robot_operations.set_active_robot_calibration",
                "robot_operations.set_robot_calibration_tool_frame",
                "robot_operations.set_robot_calibration_measurement_offset_in_tool_frame"
            ],
            worker.Commands.Select(command => command.OperationId));
    }

    private sealed class RobotWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "robot_operations.get_robot_machine_parameter" =>
                    [new WorkerRetrievedOutput("Parameter Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(42.5))],
                "robot_operations.get_robot_machine_model_link_parameters" =>
                    command.OutputArguments.Select((argument, index) => new WorkerRetrievedOutput(argument.Name,
                        argument.Kind, argument.Kind switch
                        {
                            WorkerMpValueKind.Text => new WorkerTextValue(index == 0 ? "DH" : "NONE"),
                            WorkerMpValueKind.Logical => new WorkerBooleanValue(false),
                            WorkerMpValueKind.WholeNumber => new WorkerIntegerValue(0),
                            WorkerMpValueKind.Vector => new WorkerVectorValue(0, 0, 0),
                            _ => new WorkerDoubleValue(0)
                        })).Cast<WorkerMpOutputValue>().ToArray(),
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
