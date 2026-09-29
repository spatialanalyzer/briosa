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

public sealed class TypedAnalysisQueryOperationTests
{
    private static readonly string[] MigratedIds =
    [
        "analysis_operations.query_clouds_to_objects",
        "analysis_operations.query_clouds_to_surface",
        "analysis_operations.query_frame_to_frame",
        "analysis_operations.query_groups_to_objects",
        "analysis_operations.query_point_to_objects",
        "analysis_operations.query_point_to_point_along_curve",
        "analysis_operations.query_points_to_circle",
        "analysis_operations.query_points_to_objects",
        "analysis_operations.query_points_to_single_point"
    ];

    [Fact]
    public void EachQueryHasOneTypedRegistrationAndPreservesValidationFlags()
    {
        foreach (var id in MigratedIds)
        {
            var registration = Assert.Single(SpatialAnalyzerApi.Operations, item => item.OperationId == id);
            string[] expectedRiskFlags = id is "analysis_operations.query_clouds_to_objects" or
                "analysis_operations.query_clouds_to_surface"
                    ? ["fixture_validation_pending"] : [];
            Assert.Equal(expectedRiskFlags, registration.RiskFlags);
        }
    }

    [Fact]
    public void RequiredInputsAndBindingsMatchTheReviewedCommandSequences()
    {
        var objectName = Object("object");
        var cloudToObjects = QueryCloudsToObjectsOperation.CreateCommand(new()
        {
            CloudNames = { Object("cloud") },
            ObjectNames = { objectName },
            ResultingObjectName = Object("result")
        });
        var cloudToSurface = QueryCloudsToSurfaceOperation.CreateCommand(new()
        {
            CloudNames = { Object("cloud") },
            FilterSurfaceName = Object("surface"),
            ResultingObjectName = Object("result")
        });
        var frameToFrame = QueryFrameToFrameOperation.CreateCommand(new()
        {
            ReferenceFrameName = Object("reference"),
            CorrespondingFrameName = Object("corresponding")
        });
        var groupsToObjects = QueryGroupsToObjectsOperation.CreateCommand(new()
        {
            GroupNameList = { Object("group") },
            ObjectNameList = { objectName },
            ResultingObjectName = Object("result")
        });
        var pointToObjects = QueryPointToObjectsOperation.CreateCommand(new()
        {
            PointName = Point("point"),
            Objects = { objectName }
        });
        var pointToPoint = QueryPointToPointAlongCurveOperation.CreateCommand(new()
        {
            Value1StPoint = Point("first"),
            Value2NdPoint = Point("second"),
            Curve = Object("curve")
        });
        var pointsToCircle = QueryPointsToCircleOperation.CreateCommand(new()
        {
            CircleName = Object("circle"),
            PointGroupName = Object("points"),
            VectorGroupNameForRadial = Object("radial"),
            VectorGroupNameForPlanar = Object("planar"),
            VectorGroupNameForCombined = Object("combined")
        });
        var pointsToObjects = QueryPointsToObjectsOperation.CreateCommand(new()
        {
            PointNames = { Point("point") },
            ObjectNameList = { objectName },
            ResultingObjectName = Object("result")
        });
        var pointsToSinglePoint = QueryPointsToSinglePointOperation.CreateCommand(new()
        {
            PointNames = { Point("point") },
            SinglePoint = Point("single")
        });

        WorkerMpCommand[] commands =
        [cloudToObjects, cloudToSurface, frameToFrame, groupsToObjects, pointToObjects,
            pointToPoint, pointsToCircle, pointsToObjects, pointsToSinglePoint];

        Assert.Equal(MigratedIds, commands.Select(command => command.OperationId));
        AssertBindings(cloudToObjects, "Query Clouds to Objects",
            ("Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg"),
            ("Object Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg"),
            ("Resulting Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg"),
            ("Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Skip Factor", WorkerMpValueKind.WholeNumber, "SetIntegerArg"),
            ("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));
        AssertBindings(cloudToSurface, "Query Clouds to Surface",
            ("Cloud Names", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg"),
            ("Filter Surface Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Resulting Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg"),
            ("Proximity", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Skip Factor", WorkerMpValueKind.WholeNumber, "SetIntegerArg"),
            ("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"));
        AssertBindings(frameToFrame, "Query Frame to Frame",
            ("Reference Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Corresponding Frame Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"));
        AssertBindings(groupsToObjects, "Query Groups to Objects",
            ("Group Name List (Groups to Project)", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg"),
            ("Object Name List (Objects to Project to)", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg"),
            ("Resulting Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg"),
            ("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Show Results Dialog?", WorkerMpValueKind.Logical, "SetBoolArg"));
        AssertBindings(pointToObjects, "Query Point to Objects",
            ("Point Name", WorkerMpValueKind.PointName, "SetPointNameArg"),
            ("Objects", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg"),
            ("Ignore Target Offset", WorkerMpValueKind.Logical, "SetBoolArg"));
        AssertBindings(pointToPoint, "Query Point to Point Along Curve",
            ("1st Point", WorkerMpValueKind.PointName, "SetPointNameArg"),
            ("2nd Point", WorkerMpValueKind.PointName, "SetPointNameArg"),
            ("Curve", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"));
        AssertBindings(pointsToCircle, "Query Points to Circle",
            ("Circle Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Point Group Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Is Inside Measurement", WorkerMpValueKind.Logical, "SetBoolArg"),
            ("Auto Scale Vectors to % of Radius", WorkerMpValueKind.WholeNumber, "SetIntegerArg"),
            ("Vector Group Name for Radial", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Vector Group Name for Planar", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Vector Group Name for Combined", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"));
        AssertBindings(pointsToObjects, "Query Points to Objects",
            ("Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg"),
            ("Object Name List (Objects to Project to)", WorkerMpValueKind.CollectionObjectNameList, "SetCollectionObjectNameRefListArg"),
            ("Resulting Object Name", WorkerMpValueKind.CollectionObjectName, "SetCollectionObjectNameArg2"),
            ("Projection Options", WorkerMpValueKind.ProjectionOptions, "SetProjectionOptionsArg"),
            ("RMS Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Maximum Absolute Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint, "SetDoubleArg"),
            ("Show Results Dialog?", WorkerMpValueKind.Logical, "SetBoolArg"));
        AssertBindings(pointsToSinglePoint, "Query Points to Single Point",
            ("Point Names", WorkerMpValueKind.PointNameList, "SetPointNameRefListArg"),
            ("Single Point", WorkerMpValueKind.PointName, "SetPointNameArg"),
            ("Show Vector Properties?", WorkerMpValueKind.Logical, "SetBoolArg"));

        Assert.Equal(["RMS Deviation", "Maximum Absolute Deviation"],
            cloudToObjects.OutputArguments.Select(argument => argument.Name));
        Assert.Equal(["X", "Y", "Z", "Rx (Roll)", "Ry (Pitch)", "Rz (Yaw)"],
            frameToFrame.OutputArguments.Select(argument => argument.Name));
        Assert.Equal(["dX", "dY", "dZ", "dMag", "Resultant Object"],
            pointToObjects.OutputArguments.Select(argument => argument.Name));
        Assert.Equal(["Distance Along Curve"], pointToPoint.OutputArguments.Select(argument => argument.Name));
        Assert.Empty(pointsToCircle.OutputArguments);
        Assert.Empty(pointsToSinglePoint.OutputArguments);

        var cloudDefaults = cloudToObjects.InputArguments;
        Assert.Equal(new WorkerProjectionOptionsValue("Object To Probe Vectors", false, false, 0d, false, 0d),
            cloudDefaults[3].RequireValue<WorkerProjectionOptionsValue>());
        Assert.Equal(0d, cloudDefaults[4].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0, cloudDefaults[5].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(0d, cloudDefaults[6].RequireValue<WorkerDoubleValue>().Value);
        Assert.Equal(0d, cloudDefaults[7].RequireValue<WorkerDoubleValue>().Value);
        Assert.True(pointsToCircle.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(40, pointsToCircle.InputArguments[3].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(groupsToObjects.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(pointToObjects.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(pointsToObjects.InputArguments[6].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(pointsToSinglePoint.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);

        var explicitProjection = QueryPointsToObjectsOperation.CreateCommand(new()
        {
            PointNames = { Point("point") },
            ObjectNameList = { objectName },
            ResultingObjectName = Object("result"),
            ProjectionOptions = new()
            {
                ProjectionType = "Custom",
                IgnoreEdgeProjections = true,
                OverrideTargetOffsets = true,
                OverrideTargetOffsetsValue = 1.25,
                AddExtraMaterialThickness = true,
                ExtraMaterialThicknessValue = 2.5
            }
        });
        Assert.Equal(new WorkerProjectionOptionsValue("Custom", true, true, 1.25, true, 2.5),
            explicitProjection.InputArguments[3].RequireValue<WorkerProjectionOptionsValue>());
    }

    [Fact]
    public void MissingRequiredNamesAndListsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => QueryCloudsToObjectsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => QueryCloudsToSurfaceOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => QueryFrameToFrameOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => QueryGroupsToObjectsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => QueryPointToObjectsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => QueryPointToPointAlongCurveOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => QueryPointsToCircleOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => QueryPointsToObjectsOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => QueryPointsToSinglePointOperation.CreateCommand(new()));
    }

    [Fact]
    public async Task GeneratedClientRoutesAllNineQueriesAndMapsResultsInOrder()
    {
        var worker = new QueryWorker();
        var grpcHost = await GrpcTestHost.StartAsync<AnalysisOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.AnalysisOperations.AnalysisOperationsClient(channel);
        var deadline = DateTime.UtcNow.AddSeconds(20);

        var clouds = await client.QueryCloudsToObjectsAsync(new()
        {
            CloudNames = { Object("cloud") }, ObjectNames = { Object("object") },
            ResultingObjectName = Object("result")
        }, new CallOptions(deadline: deadline));
        var surface = await client.QueryCloudsToSurfaceAsync(new()
        {
            CloudNames = { Object("cloud") }, FilterSurfaceName = Object("surface"),
            ResultingObjectName = Object("result")
        }, new CallOptions(deadline: deadline));
        var frame = await client.QueryFrameToFrameAsync(new()
        {
            ReferenceFrameName = Object("reference"), CorrespondingFrameName = Object("corresponding")
        }, new CallOptions(deadline: deadline));
        var groups = await client.QueryGroupsToObjectsAsync(new()
        {
            GroupNameList = { Object("group") }, ObjectNameList = { Object("object") },
            ResultingObjectName = Object("result")
        }, new CallOptions(deadline: deadline));
        var point = await client.QueryPointToObjectsAsync(new()
        {
            PointName = Point("point"), Objects = { Object("object") }
        }, new CallOptions(deadline: deadline));
        var alongCurve = await client.QueryPointToPointAlongCurveAsync(new()
        {
            Value1StPoint = Point("first"), Value2NdPoint = Point("second"), Curve = Object("curve")
        }, new CallOptions(deadline: deadline));
        var circle = await client.QueryPointsToCircleAsync(new()
        {
            CircleName = Object("circle"), PointGroupName = Object("points"),
            VectorGroupNameForRadial = Object("radial"), VectorGroupNameForPlanar = Object("planar"),
            VectorGroupNameForCombined = Object("combined")
        }, new CallOptions(deadline: deadline));
        var points = await client.QueryPointsToObjectsAsync(new()
        {
            PointNames = { Point("point") }, ObjectNameList = { Object("object") },
            ResultingObjectName = Object("result")
        }, new CallOptions(deadline: deadline));
        var singlePoint = await client.QueryPointsToSinglePointAsync(new()
        {
            PointNames = { Point("point") }, SinglePoint = Point("single")
        }, new CallOptions(deadline: deadline));

        Assert.Equal(MigratedIds, worker.Commands.Select(command => command.OperationId));
        Assert.All(new[] { clouds.Execution, surface.Execution, frame.Execution, groups.Execution,
            point.Execution, alongCurve.Execution, circle.Execution, points.Execution, singlePoint.Execution },
            execution => Assert.Equal(Api.MpExecutionState.Succeeded, execution.State));
        Assert.Equal(1d, clouds.RmsDeviation);
        Assert.Equal(2d, clouds.MaximumAbsoluteDeviation);
        Assert.Equal(1d, frame.X);
        Assert.Equal(6d, frame.Rz);
        Assert.Equal(1d, groups.RmsDeviation);
        Assert.Equal(4d, groups.StandardDeviation);
        Assert.Equal(1d, point.DX);
        Assert.Equal(4d, point.DMag);
        Assert.Equal("surface", point.ResultantObject.ObjectName);
        Assert.Equal(Api.ObjectType.Surface, point.ResultantObject.ObjectType);
        Assert.Equal(1d, alongCurve.DistanceAlongCurve);
    }

    private static Api.CollectionObjectName Object(string name) => new() { ObjectName = name };

    private static Api.PointName Point(string name) => new() { TargetName = name };

    private static void AssertBindings(WorkerMpCommand command, string stepName,
        params (string Name, WorkerMpValueKind Kind, string? Binding)[] expected)
    {
        Assert.Equal(stepName, command.StepName);
        Assert.Equal(expected,
            command.InputArguments.Select(argument => (argument.Name, argument.Kind, argument.SdkBinding)).ToArray());
    }

    private sealed class QueryWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            var outputs = command.OutputArguments.Select((argument, index) =>
            {
                WorkerMpValue value = argument.Kind switch
                {
                    WorkerMpValueKind.FloatingPoint => new WorkerDoubleValue(index + 1),
                    WorkerMpValueKind.WholeNumber => new WorkerIntegerValue(index + 1),
                    WorkerMpValueKind.CollectionObjectName => new WorkerCollectionObjectNameValue(
                        "collection", "surface", WorkerObjectTypeValue.Surface),
                    _ => throw new InvalidOperationException($"Unexpected fake output kind {argument.Kind}.")
                };
                return (WorkerMpOutputValue)new WorkerRetrievedOutput(argument.Name, argument.Kind, value);
            }).ToArray();
            var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, outputs, "completed");
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, execution, null, "completed", 1));
        }
    }
}
