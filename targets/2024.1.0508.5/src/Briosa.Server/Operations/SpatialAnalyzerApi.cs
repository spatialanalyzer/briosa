using Briosa.Server.Operations.MpTaskOverview;
using Briosa.Server.Operations.MpSubroutines;
using Briosa.Server.Operations.ProcessFlowOperations;
using Briosa.Server.Operations.VectorOperations;
using Briosa.Server.Operations.EventOperations;
using Briosa.Server.Operations.DimensionOperations;
using Briosa.Server.Operations.ScaleBarOperations;
using Briosa.Server.Operations.UtilityOperations;
using Briosa.Server.Operations.FileOperations;
using Briosa.Server.Operations.AnalysisOperations;
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
            AddTaskOverviewItemOperation.Descriptor,
            CreateClearTaskOverviewListOperation.Descriptor,
            SetCurrentTaskOperation.Descriptor,
            SetOverviewImageOperation.Descriptor,
            SetOverviewTitleOperation.Descriptor,
            SetTaskItemCommentOperation.Descriptor,
            SetTaskItemCompletionValuesOperation.Descriptor,
            SetTaskItemNameOperation.Descriptor,
            ShowProgressForTaskItemOperation.Descriptor,
            ShowTaskOverviewListOperation.Descriptor,
            RunSubroutineOperation.Descriptor,
            AskForDoubleOperation.Descriptor,
            AskForIntegerOperation.Descriptor,
            AskForPointNameOperation.Descriptor,
            AskForStringOperation.Descriptor,
            AskForStringPullDownVersionOperation.Descriptor,
            AskForUserDecisionFromImageOperation.Descriptor,
            AskForUserDecisionFromStringsOperation.Descriptor,
            ObjectExistenceTestCheckOnlyOperation.Descriptor,
            AddAVectorToVectorNameRefListOperation.Descriptor,
            AutoRangeAndSetVectorGroupColorizationAllOperation.Descriptor,
            AutoRangeAndSetVectorGroupColorizationSelectedOperation.Descriptor,
            DeleteIthVectorFromVectorGroupOperation.Descriptor,
            DeleteVectorByNameOperation.Descriptor,
            DeleteVectorsOperation.Descriptor,
            GetIthVectorFromVectorGroupOperation.Descriptor,
            GetIthVectorFromVectorNameRefListOperation.Descriptor,
            GetNumberOfVectorsInVectorGroupOperation.Descriptor,
            GetNumberOfVectorsInVectorNameRefListOperation.Descriptor,
            GetVectorFromVectorGroupByNameOperation.Descriptor,
            GetVectorGroupPropertiesOperation.Descriptor,
            SetVectorGroupColorizationOptionsAllOperation.Descriptor,
            SetVectorGroupColorizationOptionsSelectedOperation.Descriptor,
            SortVectorsOperation.Descriptor,
            DeleteDimensionOperation.Descriptor,
            GetDimensionValueOperation.Descriptor,
            SetDimensionToleranceOperation.Descriptor,
            DeleteScaleBarOperation.Descriptor,
            GetScaleBarStatsOperation.Descriptor,
            ScaleBarCheckOperation.Descriptor,
            SetInwardPositiveNormalOperation.Descriptor,
            DeleteEventOperation.Descriptor,
            ExportEventRefListOperation.Descriptor,
            GetIthEventFromEventRefListOperation.Descriptor,
            GetNumberOfEventsInEventRefListOperation.Descriptor,
            RenameEventOperation.Descriptor,
            GetActiveCollectionNameOperation.Descriptor,
            GetIthCollectionNameOperation.Descriptor,
            GetNumberOfCollectionsOperation.Descriptor,
            GetWorkingDirectoryOperation.Descriptor,
            GetActiveUnitsOperation.Descriptor,
            GetWorkingFramePropertiesOperation.Descriptor,
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
