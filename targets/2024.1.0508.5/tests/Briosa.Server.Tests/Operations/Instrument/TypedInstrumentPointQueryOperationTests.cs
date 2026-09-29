using Briosa.Server.Operations;
using Briosa.Server.Operations.InstrumentOperations;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedInstrumentPointQueryOperationTests
{
    private static readonly string[] OperationIds =
    [
        "instrument_operations.get_obscured_points_from_instrument",
        "instrument_operations.make_surface_face_list_from_point_proximity"
    ];

    [Fact]
    public void PointQueryCommandsKeepInputDefaultsAndTypedRegistration()
    {
        foreach (var id in OperationIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
        }

        var instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 };
        var candidate = new Api.PointName { CollectionName = "Work", GroupName = "Points", TargetName = "T1" };
        var obscured = GetObscuredPointsFromInstrumentOperation.CreateCommand(new()
        {
            Instrument = instrument,
            CandidatePoints = { candidate }
        });
        Assert.False(obscured.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("T1", obscured.InputArguments[1].RequireValue<WorkerPointNameListValue>().Values[0].TargetName);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedPointQueryRoutes()
    {
        var worker = new InstrumentWorker();
        var grpcHost = await GrpcTestHost.StartAsync<InstrumentOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.InstrumentOperations.InstrumentOperationsClient(channel);
        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));

        var obscured = await client.GetObscuredPointsFromInstrumentAsync(new()
        {
            Instrument = new Api.CollectionInstrumentId { CollectionName = "Trackers", InstrumentId = 4 },
            CandidatePoints = { new Api.PointName { CollectionName = "Work", GroupName = "Points", TargetName = "T1" } }
        }, options);
        var surface = await client.MakeSurfaceFaceListFromPointProximityAsync(new()
        {
            MeasuredPoints = { new Api.PointName { CollectionName = "Work", GroupName = "Measured", TargetName = "P1" } }
        }, options);

        Assert.Equal("Obscured", Assert.Single(obscured.ObscuredPoints).TargetName);
        Assert.Equal("1,2,5", surface.SurfaceFaces.Value);
        Assert.Equal(OperationIds, worker.Commands.Select(command => command.OperationId));
    }

    private sealed class InstrumentWorker : IWorkerCommandExecutor
    {
        private static readonly WorkerPointNameListValue Obscured = new(
            [new WorkerPointNameValue("Work", "Points", "Obscured")]);

        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                "instrument_operations.get_obscured_points_from_instrument" =>
                    [new WorkerRetrievedOutput("Obscured Points", WorkerMpValueKind.PointNameList, Obscured)],
                "instrument_operations.make_surface_face_list_from_point_proximity" =>
                    [new WorkerRetrievedOutput("Selected Surface Faces", WorkerMpValueKind.Text, new WorkerTextValue("1,2,5"))],
                _ => []
            };
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
