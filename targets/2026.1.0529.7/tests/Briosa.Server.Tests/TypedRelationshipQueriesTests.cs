using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Services;
using Briosa.Server.Workers;
using Briosa.Worker.Control;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedRelationshipQueriesTests
{
    [Fact]
    public async Task GeneratedClientReceivesRelationshipTypeAndWeight()
    {
        var relation = new Api.CollectionObjectName { ObjectName = "relation" };
        var typeCommand = GetRelationshipTypeOperation.CreateCommand(new() { RelationshipName = relation });
        var weightCommand = GetRelationshipWeightingOperation.CreateCommand(new() { RelationshipName = relation });
        var cardinalCommand = GetGeomRelationshipCardinalPointsOperation.CreateCommand(new() { RelationshipName = relation });
        var outlierCommand = GetRelationshipOutlierRejectionScalarTypeOperation.CreateCommand(new() { RelationshipName = relation });
        var measuredCommand = GetGeomRelationshipMeasuredAvgPointOperation.CreateCommand(new() { RelationshipName = relation });
        var nominalCommand = GetGeomRelationshipNominalAvgPointOperation.CreateCommand(new() { RelationshipName = relation });
        var measuredGeometryCommand = GetGeomRelationshipMeasuredGeometryOperation.CreateCommand(new() { RelationshipName = relation });
        var nominalGeometryCommand = GetGeomRelationshipNominalGeometryOperation.CreateCommand(new() { RelationshipName = relation });
        var autoVectorsCommand = GetGeomRelationshipAutoVectorsOperation.CreateCommand(new() { RelationshipName = relation });
        var pointListCommand = GetGeomRelationshipPointListOperation.CreateCommand(new() { RelationshipName = relation });
        var criteriaDefaultCommand = GetGeomRelationshipCriteriaOperation.CreateCommand(new() { RelationshipName = relation });
        var criteriaCommand = GetGeomRelationshipCriteriaOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            Criteria = "High Tolerance"
        });
        var setCriteriaDefaultCommand = SetGeomRelationshipCriteriaOperation.CreateCommand(new()
        {
            RelationshipName = relation
        });
        var setCriteriaRequest = new Api.SetGeomRelationshipCriteriaRequest
        {
            RelationshipName = relation,
            Criteria = "High Tolerance",
            ShowInReport = false,
            ToleranceOptions = new()
            {
                High = new() { Enabled = true, Value = 0.25 },
                Low = new() { Enabled = true, Value = -0.5 }
            },
            OptimizationDeltaWeight = 0.6,
            OptimizationOutOfToleranceWeight = 0.4
        };
        var setCriteriaCommand = SetGeomRelationshipCriteriaOperation.CreateCommand(setCriteriaRequest);
        var projectionPlaneCommand = GetGeomRelationshipProjectionPlaneOperation.CreateCommand(new() { RelationshipName = relation });
        var criteriaNameRequest = new Api.GetGeomRelationshipCriteriaNameListRequest
        {
            RelationshipName = new() { CollectionName = "collection", ItemName = "relation" },
            IncludeAllCriteria = true
        };
        var criteriaNamesDefaultCommand = GetGeomRelationshipCriteriaNameListOperation.CreateCommand(new()
        {
            RelationshipName = criteriaNameRequest.RelationshipName
        });
        var criteriaNamesCommand = GetGeomRelationshipCriteriaNameListOperation.CreateCommand(criteriaNameRequest);
        var projectionOptionsCommand = GetRelationshipProjectionOptionsOperation.CreateCommand(new() { RelationshipName = relation });
        var projectionOptionsSetDefaultCommand = SetRelationshipProjectionOptionsOperation.CreateCommand(new() { RelationshipName = relation });
        var projectionOptionsSetCommand = SetRelationshipProjectionOptionsOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            ProjectionOptions = new()
            {
                ProjectionType = "Object To Probe Vectors",
                IgnoreEdgeProjections = true,
                OverrideTargetOffsets = true,
                OverrideTargetOffsetsValue = 0.125,
                AddExtraMaterialThickness = true,
                ExtraMaterialThicknessValue = 0.02
            }
        });
        var subSamplingOptionsCommand = GetRelationshipSubSamplingOptionsOperation.CreateCommand(new() { RelationshipName = relation });
        var subSamplingSetDefaultCommand = SetRelationshipSubSamplingOptionsOperation.CreateCommand(new() { RelationshipName = relation });
        var subSamplingSetCommand = SetRelationshipSubSamplingOptionsOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            UseEveryIthPoint = true,
            IValue = 7,
            UseNoMoreThanNPoints = false,
            NValue = 99
        });
        var scalarToleranceSetDefaultCommand = SetRelationshipToleranceScalarTypeOperation.CreateCommand(new() { RelationshipName = relation });
        var scalarToleranceSetCommand = SetRelationshipToleranceScalarTypeOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            ToleranceOptions = new()
            {
                High = new() { Enabled = true, Value = 0.75 },
                Low = new() { Enabled = false, Value = -0.5 }
            }
        });
        var vectorToleranceSetRequest = new Api.SetRelationshipToleranceVectorTypeRequest
        {
            RelationshipName = relation,
            VectorTolerance = new()
            {
                HighX = new() { Enabled = true, Value = 1.25 },
                LowMagnitude = new() { Enabled = true, Value = 8.5 }
            }
        };
        var vectorToleranceSetCommand = SetRelationshipToleranceVectorTypeOperation.CreateCommand(vectorToleranceSetRequest);
        var orientationConstraintsRequest = new Api.SetRelationshipOrientationFitConstraintsVectorTypeRequest
        {
            RelationshipName = relation,
            OrientationVectorConstraint = new()
            {
                HighX = new() { Enabled = true, Value = 0.25 },
                LowMagnitude = new() { Enabled = true, Value = 0.5 }
            }
        };
        var orientationConstraintsCommand = SetRelationshipOrientationFitConstraintsVectorTypeOperation.CreateCommand(orientationConstraintsRequest);
        var positionConstraintsRequest = new Api.SetRelationshipPositionFitConstraintsVectorTypeRequest
        {
            RelationshipName = relation,
            PositionVectorConstraint = new()
            {
                HighY = new() { Enabled = true, Value = 1.25 },
                LowZ = new() { Enabled = true, Value = 2.5 }
            }
        };
        var positionConstraintsCommand = SetRelationshipPositionFitConstraintsVectorTypeOperation.CreateCommand(positionConstraintsRequest);
        var toleranceCommand = GetRelationshipToleranceScalarTypeOperation.CreateCommand(new() { RelationshipName = relation });
        var vectorToleranceCommand = GetRelationshipToleranceVectorTypeOperation.CreateCommand(new() { RelationshipName = relation });
        var weightingSetDefaultCommand = SetRelationshipWeightingOperation.CreateCommand(new() { RelationshipName = relation });
        var weightingSetCommand = SetRelationshipWeightingOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            Weight = 1.75
        });
        var normalizedWeightsDefaultRequest = new Api.SetRelationshipWeightsNormalizedRequest
        {
            CollectionName = new() { Name = "collection" }
        };
        var normalizedWeightsDefaultCommand = SetRelationshipWeightsNormalizedOperation.CreateCommand(normalizedWeightsDefaultRequest);
        var normalizedWeightsRequest = new Api.SetRelationshipWeightsNormalizedRequest
        {
            CollectionName = new() { Name = "collection" },
            PickWeightingMode = Api.RelWeightingMode.NormalizeSquareRootAndToleranceWidth
        };
        var normalizedWeightsCommand = SetRelationshipWeightsNormalizedOperation.CreateCommand(normalizedWeightsRequest);
        var reportingFrameRequest = new Api.SetRelationshipReportingFrameRequest
        {
            RelationshipName = relation,
            ReportingFrame = new() { CollectionName = "frames", ObjectName = "reporting frame", ObjectType = Api.ObjectType.Frame }
        };
        var reportingFrameCommand = SetRelationshipReportingFrameOperation.CreateCommand(reportingFrameRequest);
        var deleteRelationshipRequest = new Api.DeleteRelationshipRequest
        {
            RelationshipName = new() { CollectionName = "collection", ItemName = "relationship to delete" }
        };
        var deleteRelationshipCommand = DeleteRelationshipOperation.CreateCommand(deleteRelationshipRequest);
        var measuredGeometrySetCommand = SetGeomRelationshipMeasuredGeometryOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            MeasuredGeometry = new() { CollectionName = "collection", ObjectName = "measured", ObjectType = Api.ObjectType.Sphere }
        });
        var nominalGeometrySetCommand = SetGeomRelationshipNominalGeometryOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            NominalGeometry = new() { CollectionName = "collection", ObjectName = "nominal", ObjectType = Api.ObjectType.Cylinder }
        });
        var nominalAverageSetCommand = SetGeomRelationshipNominalAvgPointOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            NominalAveragePoint = new() { CollectionName = "collection", GroupName = "group", TargetName = "nominal average" }
        });
        var cardinalPointsSetCommand = SetGeomRelationshipCardinalPointsOperation.CreateCommand(new() { RelationshipName = relation });
        var projectionPlaneSetCommand = SetGeomRelationshipProjectionPlaneOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            ProjectionPlaneName = new() { CollectionName = "collection", ObjectName = "plane", ObjectType = Api.ObjectType.Plane }
        });
        var nominalAutoVectorsDefaultCommand = SetGeomRelationshipAutoVectorsNominalAvnOperation.CreateCommand(new()
        {
            RelationshipName = relation
        });
        var nominalAutoVectorsCommand = SetGeomRelationshipAutoVectorsNominalAvnOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            CreateAutoVectorsAvn = true,
            PointsType = Api.PointFilterInputType.NominalCardinalPoints,
            UseVectorGroupCustomPrefix = true,
            VectorGroupCustomPrefix = "Nominal"
        });
        var fitAutoVectorsDefaultCommand = SetRelationshipAutoVectorsFitAvfOperation.CreateCommand(new() { RelationshipName = relation });
        var fitAutoVectorsCommand = SetRelationshipAutoVectorsFitAvfOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            CreateAutoVectorsAvf = true,
            UseVectorGroupCustomPrefix = true,
            VectorGroupCustomPrefix = "Fit"
        });
        var desiredMeasurementCountDefaultCommand = SetRelationshipDesiredMeasCountOperation.CreateCommand(new() { RelationshipName = relation });
        var desiredMeasurementCountCommand = SetRelationshipDesiredMeasCountOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            DesiredMeasurementCount = 12
        });
        var autoVectorPrefixesDefaultCommand = SetRelationshipAutoVectorsGroupDefaultPrefixOperation.CreateCommand(new());
        var autoVectorPrefixesCommand = SetRelationshipAutoVectorsGroupDefaultPrefixOperation.CreateCommand(new()
        {
            GeomRelAvnVgDefaultPrefix = "Nominal:",
            GeomRelAvfVgDefaultPrefix = "Fit:",
            NonGeomRelVgDefaultPrefix = "Other:"
        });
        var dormantStatusDefaultRequest = new Api.SetRelationshipDormantStatusRequest();
        dormantStatusDefaultRequest.Relationships.Add(new Api.CollectionItemName { CollectionName = "collection", ItemName = "relation", ItemType = Api.ItemType.Relationship });
        var dormantStatusDefaultCommand = SetRelationshipDormantStatusOperation.CreateCommand(dormantStatusDefaultRequest);
        var dormantStatusRequest = new Api.SetRelationshipDormantStatusRequest { DormantStatus = true };
        dormantStatusRequest.Relationships.Add(new Api.CollectionItemName { CollectionName = "collection", ItemName = "relation", ItemType = Api.ItemType.Relationship });
        dormantStatusRequest.Relationships.Add(new Api.CollectionItemName { CollectionName = "collection", ItemName = "second" });
        var dormantStatusCommand = SetRelationshipDormantStatusOperation.CreateCommand(dormantStatusRequest);
        Assert.Equal("Get Relationship Type", typeCommand.StepName);
        Assert.Equal("SetCollectionObjectNameArg2", typeCommand.InputArguments[0].SdkBinding);
        Assert.Equal("GetStringArg", typeCommand.OutputArguments[0].SdkBinding);
        Assert.Equal("Get Relationship Weighting", weightCommand.StepName);
        Assert.Equal("GetDoubleArg", weightCommand.OutputArguments[0].SdkBinding);
        Assert.Equal("GetPointNameRefListArg", cardinalCommand.OutputArguments[0].SdkBinding);
        Assert.Equal(
            ["GetBoolArg", "GetDoubleArg", "GetBoolArg", "GetDoubleArg"],
            outlierCommand.OutputArguments.Select(output => output.SdkBinding));
        Assert.Contains("fixture_validation_pending", GetRelationshipOutlierRejectionScalarTypeOperation.Descriptor.RiskFlags);
        Assert.Equal("GetPointNameArg", measuredCommand.OutputArguments[0].SdkBinding);
        Assert.Equal("GetPointNameArg", nominalCommand.OutputArguments[0].SdkBinding);
        Assert.Equal("GetCollectionObjectNameArg", measuredGeometryCommand.OutputArguments[0].SdkBinding);
        Assert.Equal("GetCollectionObjectNameArg", nominalGeometryCommand.OutputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.VectorGroup, autoVectorsCommand.OutputArguments[1].ObjectTypeWhenOmitted);
        Assert.Equal(WorkerObjectTypeValue.VectorGroup, autoVectorsCommand.OutputArguments[3].ObjectTypeWhenOmitted);
        Assert.Equal(
            ["All Points", "Used Points", "Ignored Points"],
            pointListCommand.OutputArguments.Select(output => output.Name));
        Assert.All(pointListCommand.OutputArguments, output => Assert.Equal("GetPointNameRefListArg", output.SdkBinding));
        Assert.Equal(new WorkerTextValue("Empty"), criteriaDefaultCommand.InputArguments[1].Value);
        Assert.Equal(new WorkerTextValue("High Tolerance"), criteriaCommand.InputArguments[1].Value);
        Assert.Equal("SetStringArg", criteriaCommand.InputArguments[1].SdkBinding);
        Assert.Equal(10, criteriaCommand.OutputArguments.Count);
        Assert.Equal(new WorkerTextValue("Empty"), setCriteriaDefaultCommand.InputArguments[1].Value);
        Assert.True(setCriteriaDefaultCommand.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerToleranceScalarOptionsValue(new(false, 0), new(false, 0)),
            setCriteriaDefaultCommand.InputArguments[3].RequireValue<WorkerToleranceScalarOptionsValue>());
        Assert.Equal(new WorkerDoubleValue(0), setCriteriaDefaultCommand.InputArguments[4].Value);
        Assert.Equal(new WorkerDoubleValue(0), setCriteriaDefaultCommand.InputArguments[5].Value);
        Assert.Equal(
            ["SetCollectionObjectNameArg2", "SetStringArg", "SetBoolArg", "SetToleranceScalarOptionsArg", "SetDoubleArg", "SetDoubleArg"],
            setCriteriaCommand.InputArguments.Select(input => input.SdkBinding));
        Assert.Equal(new WorkerTextValue("High Tolerance"), setCriteriaCommand.InputArguments[1].Value);
        Assert.False(setCriteriaCommand.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerToleranceScalarOptionsValue(new(true, 0.25), new(true, -0.5)),
            setCriteriaCommand.InputArguments[3].RequireValue<WorkerToleranceScalarOptionsValue>());
        Assert.Equal(new WorkerDoubleValue(0.6), setCriteriaCommand.InputArguments[4].Value);
        Assert.Equal(new WorkerDoubleValue(0.4), setCriteriaCommand.InputArguments[5].Value);
        Assert.Throws<ArgumentException>(() => SetGeomRelationshipCriteriaOperation.CreateCommand(new()));
        Assert.Equal("SetCollectionObjectNameArg2", projectionPlaneCommand.InputArguments[0].SdkBinding);
        Assert.Equal("GetCollectionObjectNameArg", projectionPlaneCommand.OutputArguments[0].SdkBinding);
        Assert.Equal(WorkerObjectTypeValue.Plane, projectionPlaneCommand.OutputArguments[0].ObjectTypeWhenOmitted);
        Assert.Equal(WorkerItemTypeValue.Relationship,
            criteriaNamesDefaultCommand.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.False(criteriaNamesDefaultCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(criteriaNamesCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("GetStringRefListArg", criteriaNamesCommand.OutputArguments[0].SdkBinding);
        Assert.Equal(
            ["GetBoolArg", "GetBoolArg", "GetDoubleArg", "GetBoolArg", "GetDoubleArg"],
            projectionOptionsCommand.OutputArguments.Select(output => output.SdkBinding));
        Assert.Equal(new WorkerProjectionOptionsValue("Object To Probe Vectors", false, false, 0, false, 0),
            projectionOptionsSetDefaultCommand.InputArguments[1].RequireValue<WorkerProjectionOptionsValue>());
        Assert.Equal(new WorkerProjectionOptionsValue("Object To Probe Vectors", true, true, 0.125, true, 0.02),
            projectionOptionsSetCommand.InputArguments[1].RequireValue<WorkerProjectionOptionsValue>());
        Assert.Equal("SetProjectionOptionsArg", projectionOptionsSetCommand.InputArguments[1].SdkBinding);
        Assert.Throws<ArgumentException>(() => SetRelationshipProjectionOptionsOperation.CreateCommand(new()));
        Assert.Equal(
            ["GetBoolArg", "GetIntegerArg", "GetBoolArg", "GetIntegerArg"],
            subSamplingOptionsCommand.OutputArguments.Select(output => output.SdkBinding));
        Assert.False(subSamplingSetDefaultCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(20, subSamplingSetDefaultCommand.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.True(subSamplingSetDefaultCommand.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(10000, subSamplingSetDefaultCommand.InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(
            ["SetBoolArg", "SetIntegerArg", "SetBoolArg", "SetIntegerArg"],
            subSamplingSetCommand.InputArguments.Skip(1).Select(input => input.SdkBinding));
        Assert.True(subSamplingSetCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(7, subSamplingSetCommand.InputArguments[2].RequireValue<WorkerIntegerValue>().Value);
        Assert.False(subSamplingSetCommand.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(99, subSamplingSetCommand.InputArguments[4].RequireValue<WorkerIntegerValue>().Value);
        Assert.Equal(new WorkerToleranceScalarOptionsValue(new(false, 0), new(false, 0)),
            scalarToleranceSetDefaultCommand.InputArguments[1].RequireValue<WorkerToleranceScalarOptionsValue>());
        Assert.Equal(new WorkerToleranceScalarOptionsValue(new(true, 0.75), new(false, -0.5)),
            scalarToleranceSetCommand.InputArguments[1].RequireValue<WorkerToleranceScalarOptionsValue>());
        Assert.Equal("SetToleranceScalarOptionsArg", scalarToleranceSetCommand.InputArguments[1].SdkBinding);
        var vectorToleranceSetOptions = vectorToleranceSetCommand.InputArguments[1].RequireValue<WorkerToleranceVectorOptionsValue>();
        Assert.Equal(new WorkerToleranceLimit(true, 1.25), vectorToleranceSetOptions.HighX);
        Assert.Equal(new WorkerToleranceLimit(false, 0), vectorToleranceSetOptions.HighY);
        Assert.Equal(new WorkerToleranceLimit(true, 8.5), vectorToleranceSetOptions.LowMagnitude);
        Assert.Equal("SetToleranceVectorOptionsArg", vectorToleranceSetCommand.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerToleranceLimit(true, 0.25),
            orientationConstraintsCommand.InputArguments[1].RequireValue<WorkerToleranceVectorOptionsValue>().HighX);
        Assert.Equal(new WorkerToleranceLimit(true, 0.5),
            orientationConstraintsCommand.InputArguments[1].RequireValue<WorkerToleranceVectorOptionsValue>().LowMagnitude);
        Assert.Equal("SetToleranceVectorOptionsArg", orientationConstraintsCommand.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerToleranceLimit(true, 1.25),
            positionConstraintsCommand.InputArguments[1].RequireValue<WorkerToleranceVectorOptionsValue>().HighY);
        Assert.Equal(new WorkerToleranceLimit(true, 2.5),
            positionConstraintsCommand.InputArguments[1].RequireValue<WorkerToleranceVectorOptionsValue>().LowZ);
        Assert.Equal("SetToleranceVectorOptionsArg", positionConstraintsCommand.InputArguments[1].SdkBinding);
        Assert.Throws<ArgumentException>(() => SetRelationshipToleranceScalarTypeOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetRelationshipToleranceVectorTypeOperation.CreateCommand(new()
        {
            RelationshipName = relation
        }));
        Assert.Throws<ArgumentException>(() => SetRelationshipOrientationFitConstraintsVectorTypeOperation.CreateCommand(new()
        {
            RelationshipName = relation
        }));
        Assert.Throws<ArgumentException>(() => SetRelationshipPositionFitConstraintsVectorTypeOperation.CreateCommand(new()
        {
            RelationshipName = relation
        }));
        Assert.Equal(
            ["GetBoolArg", "GetDoubleArg", "GetBoolArg", "GetDoubleArg", "GetToleranceScalarOptionsArg"],
            toleranceCommand.OutputArguments.Select(output => output.SdkBinding));
        Assert.Equal(17, vectorToleranceCommand.OutputArguments.Count);
        Assert.Equal("GetToleranceVectorOptionsArg", vectorToleranceCommand.OutputArguments[^1].SdkBinding);
        Assert.Equal(new WorkerDoubleValue(0), weightingSetDefaultCommand.InputArguments[1].Value);
        Assert.Equal("SetDoubleArg", weightingSetCommand.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerDoubleValue(1.75), weightingSetCommand.InputArguments[1].Value);
        Assert.Throws<ArgumentException>(() => SetRelationshipWeightingOperation.CreateCommand(new()));
        Assert.Equal(new WorkerTextValue("collection"), normalizedWeightsDefaultCommand.InputArguments[0].Value);
        Assert.Equal("SetCollectionNameArg", normalizedWeightsDefaultCommand.InputArguments[0].SdkBinding);
        Assert.Equal(WorkerRelationshipWeightingModeValue.NormalizeEquationCount,
            normalizedWeightsDefaultCommand.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerRelationshipWeightingModeValue>>().Value);
        Assert.Equal(WorkerRelationshipWeightingModeValue.NormalizeSquareRootAndToleranceWidth,
            normalizedWeightsCommand.InputArguments[1].RequireValue<WorkerChoiceValue<WorkerRelationshipWeightingModeValue>>().Value);
        Assert.Equal("SetRelWeightingModeArg", normalizedWeightsCommand.InputArguments[1].SdkBinding);
        Assert.Throws<ArgumentException>(() => SetRelationshipWeightsNormalizedOperation.CreateCommand(new()));
        Assert.Throws<ArgumentOutOfRangeException>(() => SetRelationshipWeightsNormalizedOperation.CreateCommand(new()
        {
            CollectionName = new() { Name = "collection" },
            PickWeightingMode = Api.RelWeightingMode.Unspecified
        }));
        Assert.Equal(new WorkerCollectionObjectNameValue("frames", "reporting frame", WorkerObjectTypeValue.Frame),
            reportingFrameCommand.InputArguments[1].RequireValue<WorkerCollectionObjectNameValue>());
        Assert.Equal("SetCollectionObjectNameArg2", reportingFrameCommand.InputArguments[1].SdkBinding);
        Assert.Throws<ArgumentException>(() => SetRelationshipReportingFrameOperation.CreateCommand(new()
        {
            RelationshipName = relation
        }));
        Assert.Equal(new WorkerCollectionItemNameValue("collection", "relationship to delete", WorkerItemTypeValue.Relationship),
            deleteRelationshipCommand.InputArguments[0].RequireValue<WorkerCollectionItemNameValue>());
        Assert.Contains("destructive", DeleteRelationshipOperation.Descriptor.RiskFlags);
        Assert.Throws<ArgumentException>(() => DeleteRelationshipOperation.CreateCommand(new()));
        Assert.Equal("SetCollectionObjectNameArg2", measuredGeometrySetCommand.InputArguments[1].SdkBinding);
        Assert.Equal("SetCollectionObjectNameArg2", nominalGeometrySetCommand.InputArguments[2].SdkBinding);
        Assert.True(nominalGeometrySetCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetPointNameArg", nominalAverageSetCommand.InputArguments[2].SdkBinding);
        Assert.True(nominalAverageSetCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(cardinalPointsSetCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(cardinalPointsSetCommand.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerTextValue("GR-Cardinal Pts"), cardinalPointsSetCommand.InputArguments[3].Value);
        Assert.True(projectionPlaneSetCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(nominalAutoVectorsDefaultCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(WorkerPointFilterInputTypeValue.CardinalPoints,
            nominalAutoVectorsDefaultCommand.InputArguments[2].RequireValue<WorkerChoiceValue<WorkerPointFilterInputTypeValue>>().Value);
        Assert.False(nominalAutoVectorsDefaultCommand.InputArguments[3].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerTextValue(string.Empty), nominalAutoVectorsDefaultCommand.InputArguments[4].Value);
        Assert.Equal("SetPointFilterInputTypeArg", nominalAutoVectorsCommand.InputArguments[2].SdkBinding);
        Assert.Equal(WorkerPointFilterInputTypeValue.NominalCardinalPoints,
            nominalAutoVectorsCommand.InputArguments[2].RequireValue<WorkerChoiceValue<WorkerPointFilterInputTypeValue>>().Value);
        Assert.Equal(new WorkerTextValue("Nominal"), nominalAutoVectorsCommand.InputArguments[4].Value);
        Assert.False(fitAutoVectorsDefaultCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.False(fitAutoVectorsDefaultCommand.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerTextValue(string.Empty), fitAutoVectorsDefaultCommand.InputArguments[3].Value);
        Assert.Equal("SetBoolArg", fitAutoVectorsCommand.InputArguments[1].SdkBinding);
        Assert.True(fitAutoVectorsCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.True(fitAutoVectorsCommand.InputArguments[2].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal(new WorkerTextValue("Fit"), fitAutoVectorsCommand.InputArguments[3].Value);
        Assert.Equal(new WorkerIntegerValue(0), desiredMeasurementCountDefaultCommand.InputArguments[1].Value);
        Assert.Equal("SetIntegerArg", desiredMeasurementCountCommand.InputArguments[1].SdkBinding);
        Assert.Equal(new WorkerIntegerValue(12), desiredMeasurementCountCommand.InputArguments[1].Value);
        Assert.Equal(new WorkerTextValue("GR-AVN-"), autoVectorPrefixesDefaultCommand.InputArguments[0].Value);
        Assert.Equal(new WorkerTextValue("GR-AVF-"), autoVectorPrefixesDefaultCommand.InputArguments[1].Value);
        Assert.Equal(new WorkerTextValue("Auto Vectors:"), autoVectorPrefixesDefaultCommand.InputArguments[2].Value);
        Assert.All(autoVectorPrefixesCommand.InputArguments, input => Assert.Equal("SetStringArg", input.SdkBinding));
        Assert.Equal(["Nominal:", "Fit:", "Other:"], autoVectorPrefixesCommand.InputArguments
            .Select(input => input.RequireValue<WorkerTextValue>().Value));
        Assert.False(dormantStatusDefaultCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        var relationshipRefs = dormantStatusCommand.InputArguments[0].RequireValue<WorkerCollectionItemNameListValue>().Values;
        Assert.Equal(2, relationshipRefs.Count);
        Assert.Equal(WorkerItemTypeValue.Relationship, relationshipRefs[0].ItemType);
        Assert.Equal(WorkerItemTypeValue.Any, relationshipRefs[1].ItemType);
        Assert.True(dormantStatusCommand.InputArguments[1].RequireValue<WorkerBooleanValue>().Value);
        Assert.Equal("SetCollectionObjectNameRefListArg", dormantStatusCommand.InputArguments[0].SdkBinding);
        Assert.Throws<ArgumentException>(() => SetRelationshipDormantStatusOperation.CreateCommand(new()));
        Assert.Throws<ArgumentOutOfRangeException>(() => SetGeomRelationshipAutoVectorsNominalAvnOperation.CreateCommand(new()
        {
            RelationshipName = relation,
            PointsType = Api.PointFilterInputType.Unspecified
        }));
        Assert.Throws<ArgumentException>(() => SetRelationshipAutoVectorsFitAvfOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => SetRelationshipDesiredMeasCountOperation.CreateCommand(new()));
        Assert.Throws<ArgumentException>(() => GetRelationshipTypeOperation.CreateCommand(new()));

        var worker = new RelationshipQueryWorker();
        var grpcHost = await GrpcTestHost.StartAsync<RelationshipOperationsService>(worker).ConfigureAwait(true);
        await using var grpcLifetime = grpcHost.ConfigureAwait(true);
        var channel = grpcHost.Channel;
        var client = new Api.RelationshipOperations.RelationshipOperationsClient(channel);

        var type = await client.GetRelationshipTypeAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var weight = await client.GetRelationshipWeightingAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var weightSet = await client.SetRelationshipWeightingAsync(new()
        {
            RelationshipName = relation,
            Weight = 1.75
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var normalizedWeightsSet = await client.SetRelationshipWeightsNormalizedAsync(normalizedWeightsRequest,
            deadline: DateTime.UtcNow.AddSeconds(10));
        var reportingFrameSet = await client.SetRelationshipReportingFrameAsync(reportingFrameRequest,
            deadline: DateTime.UtcNow.AddSeconds(10));
        var deletedRelationship = await client.DeleteRelationshipAsync(deleteRelationshipRequest,
            deadline: DateTime.UtcNow.AddSeconds(10));
        var cardinalPoints = await client.GetGeomRelationshipCardinalPointsAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var outlier = await client.GetRelationshipOutlierRejectionScalarTypeAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var measuredAverage = await client.GetGeomRelationshipMeasuredAvgPointAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var nominalAverage = await client.GetGeomRelationshipNominalAvgPointAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var measuredGeometry = await client.GetGeomRelationshipMeasuredGeometryAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var nominalGeometry = await client.GetGeomRelationshipNominalGeometryAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var autoVectors = await client.GetGeomRelationshipAutoVectorsAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var pointList = await client.GetGeomRelationshipPointListAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var criteria = await client.GetGeomRelationshipCriteriaAsync(new()
        {
            RelationshipName = relation,
            Criteria = "High Tolerance"
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var criteriaSet = await client.SetGeomRelationshipCriteriaAsync(setCriteriaRequest,
            deadline: DateTime.UtcNow.AddSeconds(10));
        var projectionPlane = await client.GetGeomRelationshipProjectionPlaneAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var criteriaNames = await client.GetGeomRelationshipCriteriaNameListAsync(criteriaNameRequest,
            deadline: DateTime.UtcNow.AddSeconds(10));
        var projectionOptions = await client.GetRelationshipProjectionOptionsAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var projectionOptionsSet = await client.SetRelationshipProjectionOptionsAsync(new()
        {
            RelationshipName = relation,
            ProjectionOptions = new()
            {
                ProjectionType = "Object To Probe Vectors",
                IgnoreEdgeProjections = true,
                OverrideTargetOffsets = true,
                OverrideTargetOffsetsValue = 0.125,
                AddExtraMaterialThickness = true,
                ExtraMaterialThicknessValue = 0.02
            }
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var subSamplingOptions = await client.GetRelationshipSubSamplingOptionsAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var subSamplingOptionsSet = await client.SetRelationshipSubSamplingOptionsAsync(new()
        {
            RelationshipName = relation,
            UseEveryIthPoint = true,
            IValue = 7,
            UseNoMoreThanNPoints = false,
            NValue = 99
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var scalarToleranceSet = await client.SetRelationshipToleranceScalarTypeAsync(new()
        {
            RelationshipName = relation,
            ToleranceOptions = new()
            {
                High = new() { Enabled = true, Value = 0.75 },
                Low = new() { Enabled = false, Value = -0.5 }
            }
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var vectorToleranceSet = await client.SetRelationshipToleranceVectorTypeAsync(vectorToleranceSetRequest,
            deadline: DateTime.UtcNow.AddSeconds(10));
        var orientationConstraintsSet = await client.SetRelationshipOrientationFitConstraintsVectorTypeAsync(
            orientationConstraintsRequest, deadline: DateTime.UtcNow.AddSeconds(10));
        var positionConstraintsSet = await client.SetRelationshipPositionFitConstraintsVectorTypeAsync(
            positionConstraintsRequest, deadline: DateTime.UtcNow.AddSeconds(10));
        var tolerance = await client.GetRelationshipToleranceScalarTypeAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var vectorTolerance = await client.GetRelationshipToleranceVectorTypeAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var measuredGeometrySet = await client.SetGeomRelationshipMeasuredGeometryAsync(new()
        {
            RelationshipName = relation,
            MeasuredGeometry = new() { CollectionName = "collection", ObjectName = "measured", ObjectType = Api.ObjectType.Sphere }
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var nominalGeometrySet = await client.SetGeomRelationshipNominalGeometryAsync(new()
        {
            RelationshipName = relation,
            NominalGeometry = new() { CollectionName = "collection", ObjectName = "nominal", ObjectType = Api.ObjectType.Cylinder }
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var nominalAverageSet = await client.SetGeomRelationshipNominalAvgPointAsync(new()
        {
            RelationshipName = relation,
            NominalAveragePoint = new() { CollectionName = "collection", GroupName = "group", TargetName = "nominal average" }
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var cardinalPointsSet = await client.SetGeomRelationshipCardinalPointsAsync(new() { RelationshipName = relation },
            deadline: DateTime.UtcNow.AddSeconds(10));
        var projectionPlaneSet = await client.SetGeomRelationshipProjectionPlaneAsync(new()
        {
            RelationshipName = relation,
            ProjectionPlaneName = new() { CollectionName = "collection", ObjectName = "plane", ObjectType = Api.ObjectType.Plane }
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var nominalAutoVectorsSet = await client.SetGeomRelationshipAutoVectorsNominalAvnAsync(new()
        {
            RelationshipName = relation,
            CreateAutoVectorsAvn = true,
            PointsType = Api.PointFilterInputType.NominalCardinalPoints,
            UseVectorGroupCustomPrefix = true,
            VectorGroupCustomPrefix = "Nominal"
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var fitAutoVectorsSet = await client.SetRelationshipAutoVectorsFitAvfAsync(new()
        {
            RelationshipName = relation,
            CreateAutoVectorsAvf = true,
            UseVectorGroupCustomPrefix = true,
            VectorGroupCustomPrefix = "Fit"
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var desiredMeasurementCountSet = await client.SetRelationshipDesiredMeasCountAsync(new()
        {
            RelationshipName = relation,
            DesiredMeasurementCount = 12
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var autoVectorPrefixesSet = await client.SetRelationshipAutoVectorsGroupDefaultPrefixAsync(new()
        {
            GeomRelAvnVgDefaultPrefix = "Nominal:",
            GeomRelAvfVgDefaultPrefix = "Fit:",
            NonGeomRelVgDefaultPrefix = "Other:"
        }, deadline: DateTime.UtcNow.AddSeconds(10));
        var dormantStatusSet = await client.SetRelationshipDormantStatusAsync(dormantStatusRequest,
            deadline: DateTime.UtcNow.AddSeconds(10));

        Assert.Equal("Geom Relationship", type.RelationshipType);
        Assert.Equal(0.875, weight.Weight);
        Assert.Equal("point one", cardinalPoints.CardinalPointNameList[0].TargetName);
        Assert.Equal("group", cardinalPoints.CardinalPointNameList[1].GroupName);
        Assert.True(outlier.HasUseHighLimit);
        Assert.True(outlier.UseHighLimit);
        Assert.True(outlier.HasHighLimit);
        Assert.Equal(42.5, outlier.HighLimit);
        Assert.True(outlier.HasUseLowLimit);
        Assert.False(outlier.UseLowLimit);
        Assert.True(outlier.HasLowLimit);
        Assert.Equal(-3.5, outlier.LowLimit);
        Assert.Equal("measured average", measuredAverage.MeasuredAveragePoint.TargetName);
        Assert.Equal("nominal average", nominalAverage.NominalAveragePoint.TargetName);
        Assert.Equal("measured geometry", measuredGeometry.MeasuredGeometry.ObjectName);
        Assert.Equal(Api.ObjectType.Sphere, measuredGeometry.MeasuredGeometry.ObjectType);
        Assert.Equal("nominal geometry", nominalGeometry.NominalGeometry.ObjectName);
        Assert.Equal(Api.ObjectType.Cylinder, nominalGeometry.NominalGeometry.ObjectType);
        Assert.True(autoVectors.AutoVectorsNominalEnabled);
        Assert.Equal("nominal vectors", autoVectors.AutoVectorsNominalName.ObjectName);
        Assert.Equal(Api.ObjectType.VectorGroup, autoVectors.AutoVectorsNominalName.ObjectType);
        Assert.False(autoVectors.AutoVectorsFitEnabled);
        Assert.Equal("fit vectors", autoVectors.AutoVectorsFitName.ObjectName);
        Assert.Equal("Cardinal Points", autoVectors.PointsType);
        Assert.Equal(2, pointList.AllPoints.Count);
        Assert.Equal("all point 1", pointList.AllPoints[0].TargetName);
        Assert.Equal("used point", Assert.Single(pointList.UsedPoints).TargetName);
        Assert.Equal("ignored point", pointList.IgnoredPoints.Single().TargetName);
        Assert.True(criteria.HasNominal);
        Assert.Equal(12, criteria.Nominal);
        Assert.Equal(11.5, criteria.Measured);
        Assert.Equal(0.5, criteria.Delta);
        Assert.Equal("true", criteria.IsWithinTolerance);
        Assert.True(criteria.HasUncertainty);
        Assert.Equal(0.025, criteria.Uncertainty);
        Assert.Equal("projection plane", projectionPlane.ProjectionPlaneName.ObjectName);
        Assert.Equal(Api.ObjectType.Plane, projectionPlane.ProjectionPlaneName.ObjectType);
        Assert.Equal(["Nominal", "Measured"], criteriaNames.CriteriaNameList);
        Assert.True(projectionOptions.HasIgnoreEdgeProjections);
        Assert.False(projectionOptions.IgnoreEdgeProjections);
        Assert.True(projectionOptions.HasProbeOffsetsOverrideTargetValues);
        Assert.True(projectionOptions.ProbeOffsetsOverrideTargetValues);
        Assert.True(projectionOptions.HasProbeOffsetsOverrideValue);
        Assert.Equal(0.125, projectionOptions.ProbeOffsetsOverrideValue);
        Assert.True(projectionOptions.HasAddExtraMaterial);
        Assert.False(projectionOptions.AddExtraMaterial);
        Assert.True(projectionOptions.HasExtraMaterialThickness);
        Assert.Equal(0.02, projectionOptions.ExtraMaterialThickness);
        Assert.True(subSamplingOptions.HasUseEveryIthPoint);
        Assert.True(subSamplingOptions.UseEveryIthPoint);
        Assert.True(subSamplingOptions.HasIValue);
        Assert.Equal(3, subSamplingOptions.IValue);
        Assert.True(subSamplingOptions.HasUseNoMoreThanNPoints);
        Assert.False(subSamplingOptions.UseNoMoreThanNPoints);
        Assert.True(subSamplingOptions.HasNValue);
        Assert.Equal(15, subSamplingOptions.NValue);
        Assert.True(tolerance.HasUseHighTolerance);
        Assert.True(tolerance.UseHighTolerance);
        Assert.True(tolerance.HasHighTolerance);
        Assert.Equal(0.5, tolerance.HighTolerance);
        Assert.True(tolerance.HasUseLowTolerance);
        Assert.False(tolerance.UseLowTolerance);
        Assert.True(tolerance.HasLowTolerance);
        Assert.Equal(-0.25, tolerance.LowTolerance);
        Assert.NotNull(tolerance.ToleranceOptions);
        Assert.True(tolerance.ToleranceOptions.High.Enabled);
        Assert.Equal(0.75, tolerance.ToleranceOptions.High.Value);
        Assert.False(tolerance.ToleranceOptions.Low.Enabled);
        Assert.Equal(-0.5, tolerance.ToleranceOptions.Low.Value);
        Assert.Equal(
            [true, false, true, false, true, false, true, false],
            [vectorTolerance.UseHighXTolerance, vectorTolerance.UseHighYTolerance, vectorTolerance.UseHighZTolerance,
                vectorTolerance.UseHighMagTolerance, vectorTolerance.UseLowXTolerance, vectorTolerance.UseLowYTolerance,
                vectorTolerance.UseLowZTolerance, vectorTolerance.UseLowMagTolerance]);
        Assert.Equal([1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d],
            [vectorTolerance.HighXTolerance, vectorTolerance.HighYTolerance, vectorTolerance.HighZTolerance,
                vectorTolerance.HighMagTolerance, vectorTolerance.LowXTolerance, vectorTolerance.LowYTolerance,
                vectorTolerance.LowZTolerance, vectorTolerance.LowMagTolerance]);
        Assert.NotNull(vectorTolerance.VectorTolerance);
        Assert.Equal(new Api.ToleranceLimit { Enabled = true, Value = 0.11 }, vectorTolerance.VectorTolerance.HighX);
        Assert.Equal(new Api.ToleranceLimit { Enabled = false, Value = 0.22 }, vectorTolerance.VectorTolerance.HighY);
        Assert.Equal(new Api.ToleranceLimit { Enabled = true, Value = 0.33 }, vectorTolerance.VectorTolerance.HighZ);
        Assert.Equal(new Api.ToleranceLimit { Enabled = false, Value = 0.44 }, vectorTolerance.VectorTolerance.HighMagnitude);
        Assert.Equal(new Api.ToleranceLimit { Enabled = true, Value = 0.55 }, vectorTolerance.VectorTolerance.LowX);
        Assert.Equal(new Api.ToleranceLimit { Enabled = false, Value = 0.66 }, vectorTolerance.VectorTolerance.LowY);
        Assert.Equal(new Api.ToleranceLimit { Enabled = true, Value = 0.77 }, vectorTolerance.VectorTolerance.LowZ);
        Assert.Equal(new Api.ToleranceLimit { Enabled = false, Value = 0.88 }, vectorTolerance.VectorTolerance.LowMagnitude);
        Assert.Equal(Api.MpExecutionState.Succeeded, measuredGeometrySet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, nominalGeometrySet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, nominalAverageSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, cardinalPointsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, projectionPlaneSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, nominalAutoVectorsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, fitAutoVectorsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, desiredMeasurementCountSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, autoVectorPrefixesSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, dormantStatusSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, subSamplingOptionsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, scalarToleranceSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, vectorToleranceSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, orientationConstraintsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, positionConstraintsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, projectionOptionsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, weightSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, normalizedWeightsSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, reportingFrameSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, criteriaSet.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, deletedRelationship.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, type.Execution.State);
        Assert.Equal(Api.MpExecutionState.Succeeded, weight.Execution.State);
        Assert.Equal(38, worker.Commands.Count);

        var invalid = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.GetRelationshipTypeAsync(new(), deadline: DateTime.UtcNow.AddSeconds(10)));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(38, worker.Commands.Count);

    }

    private sealed class RelationshipQueryWorker : IWorkerCommandExecutor
    {
        public List<WorkerMpCommand> Commands { get; } = [];

        public Task<WorkerExecutionOutcome> ExecuteAsync(
            WorkerMpCommand command,
            CancellationToken cancellationToken = default)
        {
            command = RoundTrip(WorkerControlMessage.Execute(Guid.NewGuid(), command)).Command!;
            Commands.Add(command);
            WorkerMpOutputValue[] outputs = command.OperationId switch
            {
                var id when id == GetRelationshipTypeOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Relationship Type", WorkerMpValueKind.Text, new WorkerTextValue("Geom Relationship"))],
                var id when id == GetRelationshipWeightingOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.875))],
                var id when id == GetGeomRelationshipCardinalPointsOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Cardinal Point Name List", WorkerMpValueKind.PointNameList,
                    new WorkerPointNameListValue([
                        new WorkerPointNameValue("collection", "group", "point one"),
                        new WorkerPointNameValue("collection", "group", "point two")]))],
                var id when id == GetRelationshipOutlierRejectionScalarTypeOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Use High Limit?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("High Limit", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(42.5)),
                    new WorkerRetrievedOutput("Use Low Limit?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("Low Limit", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(-3.5))
                ],
                var id when id == GetGeomRelationshipMeasuredAvgPointOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Measured Average Point", WorkerMpValueKind.PointName,
                    new WorkerPointNameValue("collection", "group", "measured average"))],
                var id when id == GetGeomRelationshipNominalAvgPointOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Nominal Average Point", WorkerMpValueKind.PointName,
                    new WorkerPointNameValue("collection", "group", "nominal average"))],
                var id when id == GetGeomRelationshipMeasuredGeometryOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Measured Geometry", WorkerMpValueKind.CollectionObjectName,
                    new WorkerCollectionObjectNameValue("collection", "measured geometry", WorkerObjectTypeValue.Sphere))],
                var id when id == GetGeomRelationshipNominalGeometryOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Nominal Geometry", WorkerMpValueKind.CollectionObjectName,
                    new WorkerCollectionObjectNameValue("collection", "nominal geometry", WorkerObjectTypeValue.Cylinder))],
                var id when id == GetGeomRelationshipAutoVectorsOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Auto Vectors Nominal (AVN) - Enabled?", WorkerMpValueKind.Logical,
                        new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Auto Vectors Nominal (AVN) - Name", WorkerMpValueKind.CollectionObjectName,
                        new WorkerCollectionObjectNameValue("collection", "nominal vectors", WorkerObjectTypeValue.VectorGroup)),
                    new WorkerRetrievedOutput("Auto Vectors Fit (AVF) - Enabled?", WorkerMpValueKind.Logical,
                        new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("Auto Vectors Fit (AVF) - Name", WorkerMpValueKind.CollectionObjectName,
                        new WorkerCollectionObjectNameValue("collection", "fit vectors", WorkerObjectTypeValue.VectorGroup)),
                    new WorkerRetrievedOutput("Points Type", WorkerMpValueKind.Text, new WorkerTextValue("Cardinal Points"))
                ],
                var id when id == GetGeomRelationshipPointListOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("All Points", WorkerMpValueKind.PointNameList,
                        new WorkerPointNameListValue([
                            new WorkerPointNameValue("collection", "group", "all point 1"),
                            new WorkerPointNameValue("collection", "group", "all point 2")])),
                    new WorkerRetrievedOutput("Used Points", WorkerMpValueKind.PointNameList,
                        new WorkerPointNameListValue([
                            new WorkerPointNameValue("collection", "group", "used point")])),
                    new WorkerRetrievedOutput("Ignored Points", WorkerMpValueKind.PointNameList,
                        new WorkerPointNameListValue([
                            new WorkerPointNameValue("collection", "group", "ignored point")]))
                ],
                var id when id == GetGeomRelationshipCriteriaOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Nominal", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(12)),
                    new WorkerRetrievedOutput("Measured", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(11.5)),
                    new WorkerRetrievedOutput("Delta", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.5)),
                    new WorkerRetrievedOutput("Low Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(-1)),
                    new WorkerRetrievedOutput("High Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1)),
                    new WorkerRetrievedOutput("Optimization: Delta Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.25)),
                    new WorkerRetrievedOutput("Optimization: Out of Tolerance Weight", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.75)),
                    new WorkerRetrievedOutput("Is within Tolerance?", WorkerMpValueKind.Text, new WorkerTextValue("true")),
                    new WorkerRetrievedOutput("Has Uncertainty?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Uncertainty", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.025))
                ],
                var id when id == GetGeomRelationshipProjectionPlaneOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Projection Plane Name", WorkerMpValueKind.CollectionObjectName,
                    new WorkerCollectionObjectNameValue("collection", "projection plane", WorkerObjectTypeValue.Plane))],
                var id when id == GetGeomRelationshipCriteriaNameListOperation.Descriptor.OperationId =>
                [new WorkerRetrievedOutput("Criteria Name List", WorkerMpValueKind.StringList,
                    new WorkerStringListValue(["Nominal", "Measured"]))],
                var id when id == GetRelationshipProjectionOptionsOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Ignore Edge Projections?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("Probe Offsets - Override Target Values?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Probe Offsets - Override Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.125)),
                    new WorkerRetrievedOutput("Add Extra Material?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("Extra Material Thickness", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.02))
                ],
                var id when id == GetRelationshipSubSamplingOptionsOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Use every i-th point", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("i value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(3)),
                    new WorkerRetrievedOutput("Use no more than n points", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("n value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(15))
                ],
                var id when id == GetRelationshipToleranceScalarTypeOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Use High Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("High Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(0.5)),
                    new WorkerRetrievedOutput("Use Low Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("Low Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(-0.25)),
                    new WorkerRetrievedOutput("Tolerance Options", WorkerMpValueKind.ToleranceScalarOptions,
                        new WorkerToleranceScalarOptionsValue(new(true, 0.75), new(false, -0.5)))
                ],
                var id when id == GetRelationshipToleranceVectorTypeOperation.Descriptor.OperationId =>
                [
                    new WorkerRetrievedOutput("Use High X Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("High X Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(1)),
                    new WorkerRetrievedOutput("Use High Y Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("High Y Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(2)),
                    new WorkerRetrievedOutput("Use High Z Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("High Z Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(3)),
                    new WorkerRetrievedOutput("Use High Mag Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("High Mag Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(4)),
                    new WorkerRetrievedOutput("Use Low X Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Low X Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(5)),
                    new WorkerRetrievedOutput("Use Low Y Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("Low Y Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(6)),
                    new WorkerRetrievedOutput("Use Low Z Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(true)),
                    new WorkerRetrievedOutput("Low Z Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(7)),
                    new WorkerRetrievedOutput("Use Low Mag Tolerance?", WorkerMpValueKind.Logical, new WorkerBooleanValue(false)),
                    new WorkerRetrievedOutput("Low Mag Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(8)),
                    new WorkerRetrievedOutput("Vector Tolerance", WorkerMpValueKind.ToleranceVectorOptions,
                        new WorkerToleranceVectorOptionsValue(new(true, 0.11), new(false, 0.22), new(true, 0.33), new(false, 0.44),
                            new(true, 0.55), new(false, 0.66), new(true, 0.77), new(false, 0.88)))
                ],
                var id when id == SetGeomRelationshipMeasuredGeometryOperation.Descriptor.OperationId => [],
                var id when id == SetGeomRelationshipNominalGeometryOperation.Descriptor.OperationId => [],
                var id when id == SetGeomRelationshipNominalAvgPointOperation.Descriptor.OperationId => [],
                var id when id == SetGeomRelationshipCardinalPointsOperation.Descriptor.OperationId => [],
                var id when id == SetGeomRelationshipProjectionPlaneOperation.Descriptor.OperationId => [],
                var id when id == SetGeomRelationshipAutoVectorsNominalAvnOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipAutoVectorsFitAvfOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipDesiredMeasCountOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipAutoVectorsGroupDefaultPrefixOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipDormantStatusOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipSubSamplingOptionsOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipToleranceScalarTypeOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipToleranceVectorTypeOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipOrientationFitConstraintsVectorTypeOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipPositionFitConstraintsVectorTypeOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipProjectionOptionsOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipWeightingOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipWeightsNormalizedOperation.Descriptor.OperationId => [],
                var id when id == SetRelationshipReportingFrameOperation.Descriptor.OperationId => [],
                var id when id == SetGeomRelationshipCriteriaOperation.Descriptor.OperationId => [],
                var id when id == DeleteRelationshipOperation.Descriptor.OperationId => [],
                _ => throw new InvalidOperationException($"Unexpected operation '{command.OperationId}'.")
            };
            var response = RoundTrip(WorkerControlMessage.ExecutionResult(Guid.NewGuid(), new(
                WorkerExecutionResponseStatus.Completed,
                new WorkerMpResultAvailable(2, 1, outputs, null),
                new(WorkerConnectionState.Disconnected, WorkerExecutionReadinessState.Unverified,
                    null, 0, 1, "test", DateTimeOffset.UnixEpoch), null)));
            return Task.FromResult(new WorkerExecutionOutcome(WorkerExecutionStatus.Completed,
                WorkerExecutionDisposition.Completed, response.ExecutionResponse!.Execution, null, "completed", 1));
        }

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
