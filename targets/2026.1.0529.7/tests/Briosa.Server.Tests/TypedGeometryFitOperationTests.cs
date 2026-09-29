using Briosa.Server.Operations;
using Briosa.Server.Operations.AnalysisOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGeometryFitOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.best_fit_transformation_group_to_group",
        "analysis_operations.compute_group_to_group_orientation_rx_ry_rz",
        "analysis_operations.fit_geometry_to_point_group",
        "analysis_operations.fit_geometry_to_point_group_projected_to_plane",
        "analysis_operations.fit_geometry_to_points"
    ];

    [Fact]
    public void EachFitOperationHasOneTypedRegistrationAndNoCatalogEntry()
    {
        foreach (var id in MigratedIds)
        {
            Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
        }
    }

    [Fact]
    public void GeometryFitCommandsRequireReviewedNamesAndPreserveDefaults()
    {
        var group = new Api.CollectionObjectName { ObjectName = "group" };
        var geometry = new Api.CollectionObjectName { ObjectName = "fitted" };
        var starting = new Api.CollectionObjectName { ObjectName = "seed" };
        var command = FitGeometryToPointGroupOperation.CreateCommand(new()
        {
            GeometryType = Api.GeometryType.Circle,
            GroupToFit = group,
            ResultingObjectName = geometry,
            StartingConditionGeometry = starting
        });

        Assert.Equal("SetGeometryTypeArg", command.InputArguments[0].SdkBinding);
        Assert.Equal(WorkerGeometryTypeValue.Circle,
            command.InputArguments[0].RequireValue<WorkerChoiceValue<WorkerGeometryTypeValue>>().Value);
        Assert.Equal(string.Empty, command.InputArguments[3].RequireValue<WorkerTextValue>().Value);
        Assert.False(command.InputArguments[4].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(-1d, command.InputArguments[5].RequireValue<WorkerDoubleValue>().Value);
        Assert.False(command.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.Throws<ArgumentException>(() => FitGeometryToPointGroupOperation.CreateCommand(new()
        {
            GeometryType = Api.GeometryType.Circle
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => FitGeometryToPointGroupOperation.CreateCommand(new()
        {
            GeometryType = Api.GeometryType.Unspecified,
            GroupToFit = group,
            ResultingObjectName = geometry,
            StartingConditionGeometry = starting
        }));
    }

    [Fact]
    public void ProjectedAndPointFitsPreserveInputOrderAndRejectEmptyPointLists()
    {
        var names = new Api.CollectionObjectName { ObjectName = "group" };
        var projected = FitGeometryToPointGroupProjectedToPlaneOperation.CreateCommand(new()
        {
            GeometryType = Api.GeometryType.Plane,
            GroupToFit = names,
            PlaneName = new Api.CollectionObjectName { ObjectName = "plane" },
            ResultingObjectName = new Api.CollectionObjectName { ObjectName = "fit" },
            StartingConditionGeometry = new Api.CollectionObjectName { ObjectName = "seed" }
        });
        Assert.Equal("Geometry Type", projected.InputArguments[0].Name);
        Assert.Equal("Group to Fit", projected.InputArguments[1].Name);
        Assert.Equal("Plane Name", projected.InputArguments[2].Name);
        Assert.Equal("Resulting Object Name", projected.InputArguments[3].Name);

        var pointsRequest = new Api.FitGeometryToPointsRequest
        {
            GeometryType = Api.GeometryType.Sphere,
            ResultingObjectName = new Api.CollectionObjectName { ObjectName = "fit" },
            StartingConditionGeometry = new Api.CollectionObjectName { ObjectName = "seed" }
        };
        Assert.Throws<ArgumentException>(() => FitGeometryToPointsOperation.CreateCommand(pointsRequest));
        pointsRequest.PointsToFit.Add(new Api.PointName { TargetName = "P1" });
        var points = FitGeometryToPointsOperation.CreateCommand(pointsRequest);
        Assert.Equal("SetPointNameRefListArg", points.InputArguments[1].SdkBinding);
        Assert.Single(points.InputArguments[1].RequireValue<WorkerPointNameListValue>().Values);
    }

    [Fact]
    public void BestFitCommandAndResultKeepTheirHeterogeneousValuesInOrder()
    {
        var command = BestFitTransformationGroupToGroupOperation.CreateCommand(new()
        {
            ReferenceGroup = new Api.CollectionObjectName { ObjectName = "reference" },
            CorrespondingGroup = new Api.CollectionObjectName { ObjectName = "measured" },
            FilePathForCsvTextReport = new Api.FileReference { Path = "fit.csv" }
        });
        Assert.Equal(15, command.InputArguments.Count);
        Assert.False(command.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(0d, command.InputArguments[3].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(command.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetFilePathArg", command.InputArguments[14].SdkBinding);
        Assert.Throws<ArgumentException>(() => BestFitTransformationGroupToGroupOperation.CreateCommand(new()));

        var matrix = Enumerable.Range(0, 16).Select(i => (double)i).ToArray();
        var result = BestFitTransformationGroupToGroupOperation.CreateResult(Completed(
            new WorkerRetrievedOutput("Transform in Working", WorkerMpValueKind.Transform, new WorkerTransformValue(matrix)),
            new WorkerRetrievedOutput("Optimum Transform", WorkerMpValueKind.WorldTransform,
                new WorkerWorldTransformValue(new WorkerTransformValue(matrix), 2d)),
            new WorkerRetrievedOutput("RMS Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1d)),
            new WorkerRetrievedOutput("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2d)),
            new WorkerRetrievedOutput("Number of Unknowns", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(3)),
            new WorkerRetrievedOutput("Number of Equations", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(4)),
            new WorkerRetrievedOutput("Robustness", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5d))));

        Assert.Equal(matrix, result.TransformInWorking.Values);
        Assert.Equal(matrix, result.OptimumTransform.Transform.Values);
        Assert.Equal(2d, result.OptimumTransform.ScaleFactor);
        Assert.Equal(3, result.NumberOfUnknowns);
        Assert.Equal(5d, result.Robustness);
    }

    [Fact]
    public async Task GeneratedClientReachesTypedGroupOrientationRoute()
    {
        var worker = new OrientationWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var result = await new Api.AnalysisOperations.AnalysisOperationsClient(channel)
            .ComputeGroupToGroupOrientationRxRyRzAsync(new()
            {
                ReferenceGroup = new Api.CollectionObjectName { ObjectName = "reference" },
                CorrespondingGroup = new Api.CollectionObjectName { ObjectName = "measured" }
            }, new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20)));

        Assert.Equal(1d, result.Rx);
        Assert.Equal(2d, result.Ry);
        Assert.Equal(3d, result.Rz);
        Assert.Equal(MigratedIds[1], Assert.Single(worker.Commands).OperationId);
    }

    private static SuccessfulOperationExecution Completed(params WorkerMpOutputValue[] outputs) => new(
        WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed"),
        new Api.MpExecutionDetails());

    private sealed class OrientationWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            WorkerMpOutputValue[] outputs =
            [
                new WorkerRetrievedOutput("Rx", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1d)),
                new WorkerRetrievedOutput("Ry", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2d)),
                new WorkerRetrievedOutput("Rz", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3d))
            ];
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
