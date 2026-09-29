using Briosa.Server.Operations;
using Briosa.Server.Operations.GdtOperations;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Tests;

public sealed class TypedGdtFeatureCheckDatumReferencesTests
{
    private static readonly string[] OutputBindingsPerDatum =
    [
        "GetStringArg", "GetStringArg", "GetCollectionObjectNameRefListArg",
        "GetCollectionObjectNameRefListArg", "GetCollectionObjectNameRefListArg",
        "GetCollectionObjectNameRefListArg"
    ];

    [Fact]
    public void MapsFeatureCheckAndAllDatumOutputs()
    {
        var command = GetFeatureCheckDatumReferencesOperation.CreateCommand(new()
        {
            FeatureCheck = new() { CollectionName = "Checks", ItemName = "FC1" }
        });

        var featureCheck = Assert.Single(command.InputArguments);
        Assert.Equal("SetCollectionObjectNameArg2", featureCheck.SdkBinding);
        Assert.Equal(WorkerItemTypeValue.FeatureCheck,
            featureCheck.RequireValue<WorkerCollectionItemNameValue>().ItemType);
        Assert.Equal(18, command.OutputArguments.Count);
        Assert.Equal(Enumerable.Range(0, 3).SelectMany(_ => OutputBindingsPerDatum),
            command.OutputArguments.Select(output => output.SdkBinding));

        var execution = WorkerMpExecutionResult.FromEvidence(true, true, true, 2, 1, CreateOutputs(), "completed");
        var details = new Api.MpExecutionDetails { State = Api.MpExecutionState.Succeeded, MpResultCode = 2 };
        var result = GetFeatureCheckDatumReferencesOperation.CreateResult(
            new SuccessfulOperationExecution(execution, details));

        Assert.Equal("Reference 1", result.Datum1.ReferenceString);
        Assert.Equal("CAD 1", result.Datum1.CadFaces);
        Assert.Equal("SA object 1", Assert.Single(result.Datum1.SaObjects).ObjectName);
        Assert.Equal(Api.ObjectType.Cloud, result.Datum1.SaObjects[0].ObjectType);
        Assert.Equal("Aux object 1", Assert.Single(result.Datum1.AuxiliarySaObjects).ObjectName);
        Assert.Equal("Geometry relationship 1", Assert.Single(result.Datum1.GeometryRelationships).ItemName);
        Assert.Equal("Aux geometry relationship 1", Assert.Single(result.Datum1.AuxiliaryGeometryRelationships).ItemName);
        Assert.Equal("Reference 2", result.Datum2.ReferenceString);
        Assert.Equal("SA object 2", Assert.Single(result.Datum2.SaObjects).ObjectName);
        Assert.Equal("Geometry relationship 2", Assert.Single(result.Datum2.GeometryRelationships).ItemName);
        Assert.Equal("Reference 3", result.Datum3.ReferenceString);
        Assert.Equal("Aux object 3", Assert.Single(result.Datum3.AuxiliarySaObjects).ObjectName);
        Assert.Equal("Aux geometry relationship 3", Assert.Single(result.Datum3.AuxiliaryGeometryRelationships).ItemName);
        Assert.Same(details, result.Execution);

        Assert.Throws<ArgumentException>(() => GetFeatureCheckDatumReferencesOperation.CreateCommand(new()));
        var id = GetFeatureCheckDatumReferencesOperation.Descriptor.OperationId;
        Assert.Single(SpatialAnalyzerApi.Operations, operation => operation.OperationId == id);
    }

    private static WorkerRetrievedOutput[] CreateOutputs() => Enumerable.Range(1, 3)
        .SelectMany(index => new WorkerRetrievedOutput[]
        {
            new($"Datum {index} Reference String", WorkerMpValueKind.Text,
                new WorkerTextValue($"Reference {index}")),
            new($"Datum {index} CAD Faces", WorkerMpValueKind.Text,
                new WorkerTextValue($"CAD {index}")),
            new($"Datum {index} SA Objects", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue(
                [new WorkerCollectionObjectNameValue("Objects", $"SA object {index}", WorkerObjectTypeValue.Cloud)])),
            new($"Datum {index} Aux SA Objects", WorkerMpValueKind.CollectionObjectNameList,
                new WorkerCollectionObjectNameListValue(
                [new WorkerCollectionObjectNameValue("Objects", $"Aux object {index}", WorkerObjectTypeValue.Cylinder)])),
            new($"Datum {index} Geometry Relationships", WorkerMpValueKind.CollectionItemNameList,
                new WorkerCollectionItemNameListValue(
                [new WorkerCollectionItemNameValue("Relationships", $"Geometry relationship {index}", WorkerItemTypeValue.Relationship)])),
            new($"Datum {index} Aux Geometry Relationships", WorkerMpValueKind.CollectionItemNameList,
                new WorkerCollectionItemNameListValue(
                [new WorkerCollectionItemNameValue("Relationships", $"Aux geometry relationship {index}", WorkerItemTypeValue.Relationship)]))
        }).ToArray();
}
