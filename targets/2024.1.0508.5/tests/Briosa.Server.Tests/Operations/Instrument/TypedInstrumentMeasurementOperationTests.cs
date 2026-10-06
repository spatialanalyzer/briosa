using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentMeasurementOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.measure",
        "instrument_operations.measure_existing_single_point",
        "instrument_operations.measure_existing_single_point_and_compare",
        "instrument_operations.measure_existing_single_point_manual_guide",
        "instrument_operations.measure_nominal_feature",
        "instrument_operations.measure_single_point_here"
    ];

    [Fact]
    public void MeasurementCommandsPreserveBindingsDefaultsAndOptionalPromptOmission()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var target = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };
        var group = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" };

        var measure = MeasureOperation.CreateCommand(new() { Instrument = instrument });
        Assert.Equal("SetColInstIdArg", Assert.Single(measure.InputArguments).SdkBinding);

        var existing = MeasureExistingSinglePointOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ExistingTargetId = target,
            GroupNameForNewPoint = group
        });
        Assert.Equal(
            ["Instrument ID", "Existing Target ID", "Group name for new point", "Measure Immediately"],
            existing.InputArguments.Select(argument => argument.Name));
        Assert.Equal(WorkerObjectTypeValue.PointGroup,
            existing.InputArguments[2].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        // Measure Immediately defaults to true since the breaking release (#293).
        Assert.True(existing.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);

        var compare = MeasureExistingSinglePointAndCompareOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ExistingTargetId = target,
            GroupNameForNewPoint = group,
            HtmlPromptFile = new() { Path = @"C:\prompts\measure.html" }
        });
        Assert.Equal(
            ["Instrument ID", "Existing Target ID", "Group name for new point", "Measure Immediately",
                "HTML Prompt File (optional)", "Tolerance (0.0 for none)"],
            compare.InputArguments.Select(argument => argument.Name));
        Assert.Equal(@"C:\prompts\measure.html",
            compare.InputArguments[4].RequireValue<WorkerFileReferenceValue>().Path);
        Assert.Equal(0, compare.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(["GetVectorArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetDoubleArg", "GetPointNameArg"],
            compare.OutputArguments.Select(argument => argument.SdkBinding));

        var manual = MeasureExistingSinglePointManualGuideOperation.CreateCommand(new()
        {
            Instrument = instrument,
            ExistingTargetId = target,
            GroupNameForNewPoint = group
        });
        Assert.Equal(4, manual.InputArguments.Count);
        Assert.Equal("GetPointNameArg", Assert.Single(manual.OutputArguments).SdkBinding);

        var nominal = MeasureNominalFeatureOperation.CreateCommand(new()
        {
            Instrument = instrument,
            Feature = new Api.CollectionObjectName { CollectionName = "Models", ObjectName = "Part" },
            ResultingPoint = target
        });
        Assert.Equal(WorkerObjectTypeValue.Any,
            nominal.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>().ObjectType);
        Assert.Equal("SetPointNameArg", nominal.InputArguments[2].SdkBinding);

        var here = MeasureSinglePointHereOperation.CreateCommand(new()
        {
            Instrument = instrument,
            TargetId = target
        });
        Assert.Equal(3, here.InputArguments.Count);
        Assert.True(here.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedInstrumentMeasurementRoutes()
    {
        var worker = new MeasurementWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var target = new Api.PointName { CollectionName = "Points", GroupName = "Nominal", TargetName = "P1" };
        var group = new Api.CollectionObjectName { CollectionName = "Points", ObjectName = "Measured" };

        await client.MeasureAsync(new() { Instrument = instrument }, options);
        var existing = await client.MeasureExistingSinglePointAsync(new()
        {
            Instrument = instrument, ExistingTargetId = target, GroupNameForNewPoint = group
        }, options);
        var compare = await client.MeasureExistingSinglePointAndCompareAsync(new()
        {
            Instrument = instrument, ExistingTargetId = target, GroupNameForNewPoint = group,
            HtmlPromptFile = new() { Path = @"C:\prompts\measure.html" }, Tolerance = 0.5
        }, options);
        var manual = await client.MeasureExistingSinglePointManualGuideAsync(new()
        {
            Instrument = instrument, ExistingTargetId = target, GroupNameForNewPoint = group
        }, options);
        await client.MeasureNominalFeatureAsync(new()
        {
            Instrument = instrument,
            Feature = new Api.CollectionObjectName { CollectionName = "Models", ObjectName = "Part" },
            ResultingPoint = target
        }, options);
        await client.MeasureSinglePointHereAsync(new() { Instrument = instrument, TargetId = target }, options);

        Assert.Equal("Measured1", existing.ResultingPointName.TargetName);
        Assert.Equal(new Api.Vector { X = 1, Y = 2, Z = 3 }, compare.VectorRepresentation);
        Assert.Equal([4d, 5d, 6d, 7d], [compare.XValue, compare.YValue, compare.ZValue, compare.Magnitude]);
        Assert.Equal("Measured2", compare.ResultingPointName.TargetName);
        Assert.Equal("Measured3", manual.ResultingPointName.TargetName);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
        Assert.Equal("SetFilePathArg", worker.Commands[2].InputArguments[4].SdkBinding);
        Assert.Equal(0.5, worker.Commands[2].InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
    }

    private sealed class MeasurementWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.measure_existing_single_point" =>
                    [new WorkerRetrievedOutput("Resulting Point Name", WorkerMpValueKind.PointName,
                        new WorkerPointNameValue("Points", "Measured", "Measured1"))],
                "instrument_operations.measure_existing_single_point_and_compare" =>
                [
                    new WorkerRetrievedOutput("Vector Representation", WorkerMpValueKind.Vector,
                        new WorkerVectorValue(1, 2, 3)),
                    new WorkerRetrievedOutput("X Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4)),
                    new WorkerRetrievedOutput("Y Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5)),
                    new WorkerRetrievedOutput("Z Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(6)),
                    new WorkerRetrievedOutput("Magnitude", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(7)),
                    new WorkerRetrievedOutput("Resulting Point Name", WorkerMpValueKind.PointName,
                        new WorkerPointNameValue("Points", "Measured", "Measured2"))
                ],
                "instrument_operations.measure_existing_single_point_manual_guide" =>
                    [new WorkerRetrievedOutput("Resulting Point Name", WorkerMpValueKind.PointName,
                        new WorkerPointNameValue("Points", "Measured", "Measured3"))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
