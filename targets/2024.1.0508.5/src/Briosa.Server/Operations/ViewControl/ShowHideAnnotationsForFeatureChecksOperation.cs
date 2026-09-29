using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideAnnotationsForFeatureChecksOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_annotations_for_feature_checks", "Show/Hide Annotations for Feature Checks", "briosa.ViewControl",
        "ShowHideAnnotationsForFeatureChecks", "/briosa.ViewControl/ShowHideAnnotationsForFeatureChecks", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideAnnotationsForFeatureChecksRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Check Name List", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.FeatureCheckNameList, "feature_check_name_list"), "SetCollectionObjectNameRefListArg"),
            new("Show?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Show), "SetBoolArg"),
            new("Highlight?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Highlight), "SetBoolArg"),
            new("Set Inspection View?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.SetInspectionView), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHideAnnotationsForFeatureChecksResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
