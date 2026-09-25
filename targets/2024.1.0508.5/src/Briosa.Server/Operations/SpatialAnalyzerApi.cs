using Briosa.Server.Operations.ConstructionOperations;
using Briosa.Server.Operations.WaveA;
using Briosa.Server.Operations.Variables;
using Briosa.Server.Operations.RelationshipOperations;
using Briosa.Server.Security;

namespace Briosa.Server.Operations;

/// <summary>
/// Identifies the exact SpatialAnalyzer API implemented by this Briosa build.
/// </summary>
internal static class SpatialAnalyzerApi
{
    public const string TargetVersion = "2024.1.0508.5";
    public const string ProtocolPackage = "briosa";

    public static IReadOnlyList<OperationDescriptor> Operations { get; } =
        [
            .. MpOperationCatalog.Operations.Select(operation => operation.Descriptor),
            GetActiveCollectionNameOperation.Descriptor,
            AddDoubleToNamedDoubleListVariableOperation.Descriptor,
            ClearNamedDoubleListVariableOperation.Descriptor,
            DeleteVariableOperation.Descriptor,
            DeleteVariablesWildcardMatchOperation.Descriptor,
            GetBooleanVariableOperation.Descriptor,
            GetCollectionObjectNameVariableOperation.Descriptor,
            GetCollectionObjectRefListVariableOperation.Descriptor,
            GetIntegerVariableOperation.Descriptor,
            GetNamedDoubleListVariableMinMaxOperation.Descriptor,
            GetPointNameRefListVariableOperation.Descriptor,
            GetPointNameVariableOperation.Descriptor,
            GetRelationshipRefListVariableOperation.Descriptor,
            GetReportItemsReferenceListVariableOperation.Descriptor,
            GetStringRefListVariableOperation.Descriptor,
            GetStringVariableOperation.Descriptor,
            GetTransformVariableOperation.Descriptor,
            GetVectorNameRefListVariableOperation.Descriptor,
            GetVectorVariableOperation.Descriptor,
            SetBooleanVariableOperation.Descriptor,
            SetCollectionObjectNameVariableOperation.Descriptor,
            SetCollectionObjectRefListVariableOperation.Descriptor,
            SetFontVariableOperation.Descriptor,
            SetIntegerVariableOperation.Descriptor,
            SetPointNameRefListVariableOperation.Descriptor,
            SetPointNameVariableOperation.Descriptor,
            SetRelationshipRefListVariableOperation.Descriptor,
            SetReportItemsReferenceListVariableOperation.Descriptor,
            SetStringRefListVariableOperation.Descriptor,
            SetStringVariableOperation.Descriptor,
            SetTransformVariableOperation.Descriptor,
            SetVectorNameRefListVariableOperation.Descriptor,
            SetVectorVariableOperation.Descriptor,
            SetDoubleVariableOperation.Descriptor,
            GetDoubleVariableOperation.Descriptor,
            SetNamedDoubleListVariableOperation.Descriptor,
            GetNamedDoubleListVariableOperation.Descriptor,
            GetRelationshipFitConstraintsScalarTypeOperation.Descriptor,
            SetRelationshipFitConstraintsScalarTypeOperation.Descriptor,
        ];
}
