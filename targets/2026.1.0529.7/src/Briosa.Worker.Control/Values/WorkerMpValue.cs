using System.Text.Json.Serialization;

namespace Briosa.Worker.Control;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "valueType")]
[JsonDerivedType(typeof(WorkerBooleanValue), 1)]
[JsonDerivedType(typeof(WorkerIntegerValue), 2)]
[JsonDerivedType(typeof(WorkerDoubleValue), 3)]
[JsonDerivedType(typeof(WorkerTextValue), 4)]
[JsonDerivedType(typeof(WorkerPointNameValue), 5)]
[JsonDerivedType(typeof(WorkerVectorValue), 6)]
[JsonDerivedType(typeof(WorkerToleranceVectorOptionsValue), 7)]
[JsonDerivedType(typeof(WorkerCollectionInstrumentIdValue), 8)]
[JsonDerivedType(typeof(WorkerCollectionInstrumentIdListValue), 9)]
[JsonDerivedType(typeof(WorkerCollectionMachineIdValue), 10)]
[JsonDerivedType(typeof(WorkerCollectionItemNameValue), 11)]
[JsonDerivedType(typeof(WorkerCollectionItemNameListValue), 12)]
[JsonDerivedType(typeof(WorkerCollectionObjectNameValue), 13)]
[JsonDerivedType(typeof(WorkerCollectionObjectNameListValue), 14)]
[JsonDerivedType(typeof(WorkerCollectionGroupNameListValue), 15)]
[JsonDerivedType(typeof(WorkerCollectionVectorGroupNameValue), 16)]
[JsonDerivedType(typeof(WorkerCollectionVectorGroupNameListValue), 17)]
[JsonDerivedType(typeof(WorkerPointNameListValue), 18)]
[JsonDerivedType(typeof(WorkerStringListValue), 19)]
[JsonDerivedType(typeof(WorkerVectorNameListValue), 20)]
[JsonDerivedType(typeof(WorkerDoubleArrayValue), 21)]
[JsonDerivedType(typeof(WorkerTransformValue), 22)]
[JsonDerivedType(typeof(WorkerWorldTransformValue), 23)]
[JsonDerivedType(typeof(WorkerFileReferenceValue), 24)]
[JsonDerivedType(typeof(WorkerFitConstraintScalarOptionsValue), 25)]
[JsonDerivedType(typeof(WorkerToleranceScalarOptionsValue), 26)]
public abstract record WorkerMpValue
{
    private protected WorkerMpValue() { }

    internal bool MatchesOutputKind(WorkerMpValueKind kind) => this switch
    {
        WorkerBooleanValue => kind == WorkerMpValueKind.Logical,
        WorkerIntegerValue => kind == WorkerMpValueKind.WholeNumber,
        WorkerDoubleValue => kind == WorkerMpValueKind.FloatingPoint,
        WorkerTextValue => kind is WorkerMpValueKind.Text or WorkerMpValueKind.InstrumentTypeName or
            WorkerMpValueKind.ChartName or WorkerMpValueKind.CloudName or WorkerMpValueKind.CollectionName or
            WorkerMpValueKind.FrameName or WorkerMpValueKind.VectorGroupName or WorkerMpValueKind.ViewName,
        WorkerPointNameValue => kind == WorkerMpValueKind.PointName,
        WorkerVectorValue => kind == WorkerMpValueKind.Vector,
        WorkerToleranceVectorOptionsValue => kind == WorkerMpValueKind.ToleranceVectorOptions,
        WorkerCollectionInstrumentIdValue => kind == WorkerMpValueKind.CollectionInstrumentId,
        WorkerCollectionInstrumentIdListValue => kind == WorkerMpValueKind.CollectionInstrumentIdList,
        WorkerCollectionMachineIdValue => kind == WorkerMpValueKind.CollectionMachineId,
        WorkerCollectionItemNameValue => kind == WorkerMpValueKind.CollectionItemName,
        WorkerCollectionItemNameListValue => kind == WorkerMpValueKind.CollectionItemNameList,
        WorkerCollectionObjectNameValue => kind == WorkerMpValueKind.CollectionObjectName,
        WorkerCollectionObjectNameListValue => kind == WorkerMpValueKind.CollectionObjectNameList,
        WorkerCollectionGroupNameListValue => kind == WorkerMpValueKind.CollectionGroupNameList,
        WorkerCollectionVectorGroupNameValue => kind == WorkerMpValueKind.CollectionVectorGroupName,
        WorkerCollectionVectorGroupNameListValue => kind == WorkerMpValueKind.CollectionVectorGroupNameList,
        WorkerPointNameListValue => kind == WorkerMpValueKind.PointNameList,
        WorkerStringListValue => kind is WorkerMpValueKind.StringList or WorkerMpValueKind.EditText,
        WorkerVectorNameListValue => kind == WorkerMpValueKind.VectorNameList,
        WorkerDoubleArrayValue => kind == WorkerMpValueKind.DoubleArray,
        WorkerTransformValue => kind == WorkerMpValueKind.Transform,
        WorkerWorldTransformValue => kind == WorkerMpValueKind.WorldTransform,
        WorkerFileReferenceValue => kind == WorkerMpValueKind.FileReference,
        WorkerFitConstraintScalarOptionsValue => kind == WorkerMpValueKind.FitConstraintScalarOptions,
        WorkerToleranceScalarOptionsValue => kind == WorkerMpValueKind.ToleranceScalarOptions,
        _ => false
    };
}
