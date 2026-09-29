using Briosa.Server.Operations;
using Briosa.Server.Operations.ReportingOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedReportOptionsOperationTests
{
    private static readonly string[] Ids =
    [
        "reporting_operations.set_point_group_report_options", "reporting_operations.set_relationship_report_options",
        "reporting_operations.set_report_options_for_object", "reporting_operations.set_vector_group_report_options"
    ];

    [Fact]
    public void ReportOptionOperationsAreRegisteredAndRetainValidationStatus()
    {
        foreach (var id in Ids)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        Assert.Contains("fixture_validation_pending", SetReportOptionsForObjectOperation.Descriptor.RiskFlags);
    }

    [Fact]
    public void ReportOptionMappingsPreserveDefaultsAndNestedValues()
    {
        Assert.Throws<ArgumentException>(() => SetPointGroupReportOptionsOperation.CreateCommand(new()));
        var pointGroup = SetPointGroupReportOptionsOperation.CreateCommand(new()
        {
            PointGroup = Object("Targets"),
            CoordinateSystem = Api.CoordinateSystemType.Polar,
            ShowXComponent = false,
            ShowOffsets = true,
            MakeDefault = true
        });
        Assert.Equal(14, pointGroup.InputArguments.Count);
        Assert.Equal(WorkerCoordinateSystemTypeValue.Polar,
            pointGroup.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerCoordinateSystemTypeValue>>().Value);
        Assert.Equal([false, true, true, true, true, false, false, false, false, true, true, false],
            pointGroup.InputArguments.Skip(2).Select(argument => argument.RequireValue<WorkerBooleanValue>().Value));
        Assert.Throws<ArgumentException>(() => SetPointGroupReportOptionsOperation.CreateCommand(new()
        {
            PointGroup = Object("Targets"), CoordinateSystem = Api.CoordinateSystemType.Unspecified
        }));

        Assert.Throws<ArgumentException>(() => SetRelationshipReportOptionsOperation.CreateCommand(new()));
        var relationship = SetRelationshipReportOptionsOperation.CreateCommand(new()
        {
            RelationshipName = Object("Alignment")
        });
        var defaults = relationship.InputArguments[1].RequireValue<WorkerPointDeltaReportOptionsValue>();
        Assert.Equal(new WorkerPointDeltaReportOptionsValue(0, "Single", true, true, true, true,
            true, true, true, false, true, true), defaults);
        var customRelationship = SetRelationshipReportOptionsOperation.CreateCommand(new()
        {
            RelationshipName = Object("Alignment"),
            ReportOptions = new Api.PointDeltaReportOptions
            {
                CoordinateSystem = Api.CoordinateSystemType.Cylindric,
                DetailsFormat = "Full",
                ShowPointA = false,
                SortPointNames = true,
                ColorizeInToleranceFields = false
            }
        });
        Assert.Equal(new WorkerPointDeltaReportOptionsValue(1, "Full", false, true, true, true,
            true, true, true, true, true, false),
            customRelationship.InputArguments[1].RequireValue<WorkerPointDeltaReportOptionsValue>());

        Assert.Throws<ArgumentException>(() => SetReportOptionsForObjectOperation.CreateCommand(new()));
        Assert.Equal("Object", SetReportOptionsForObjectOperation.CreateCommand(new()
        {
            Object = Object("Plane")
        }).InputArguments[0].Name);
        Assert.Throws<ArgumentException>(() => SetVectorGroupReportOptionsOperation.CreateCommand(new()));
        var vectorGroup = SetVectorGroupReportOptionsOperation.CreateCommand(new()
        {
            VectorGroup = Object("Vectors"),
            ReportOptions = new Api.PointDeltaReportOptions { ShowDelta = false }
        });
        Assert.False(vectorGroup.InputArguments[1]
            .RequireValue<WorkerPointDeltaReportOptionsValue>().ShowDelta);
    }

    [Fact]
    public async Task GeneratedClientRoutesReportOptionOperationsThroughTypedMappings()
    {
        var worker = new ReportOptionsWorker();
        var grpcHost = await GrpcTestHost.StartAsync<ReportingOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.ReportingOperations.ReportingOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        await client.SetPointGroupReportOptionsAsync(new() { PointGroup = Object("Targets") }, options);
        await client.SetRelationshipReportOptionsAsync(new() { RelationshipName = Object("Alignment") }, options);
        await client.SetReportOptionsForObjectAsync(new() { Object = Object("Plane") }, options);
        await client.SetVectorGroupReportOptionsAsync(new() { VectorGroup = Object("Vectors") }, options);

        Assert.Equal(Ids, worker.Commands.Select(command => command.OperationId));
        Assert.All(worker.Commands, command => Assert.Empty(command.OutputArguments));
    }

    private static Api.CollectionObjectName Object(string name) => new()
    {
        CollectionName = "Parts",
        ObjectName = name,
        ObjectType = Api.ObjectType.Unspecified
    };

    private sealed class ReportOptionsWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, [], "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
