using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowItemsInTreeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_items_in_tree", "Show Items in Tree", "briosa.ViewControl",
        "ShowItemsInTree", "/briosa.ViewControl/ShowItemsInTree", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowItemsInTreeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Collapse all other Items?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasCollapseAllOtherItems || request.CollapseAllOtherItems), "SetBoolArg"),
            new("Points", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.Points, "points"), "SetPointNameRefListArg"),
            new("Objects", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Objects, "objects"), "SetCollectionObjectNameRefListArg"),
            new("Instruments", WorkerMpValueKind.CollectionInstrumentIdList,
                CollectionInstrumentIdMapper.RequiredList(request.Instruments, "instruments"), "SetColInstIdRefListArg"),
            new("Feature Checks", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.FeatureChecks, "feature_checks"), "SetCollectionObjectNameRefListArg"),
            new("Datums", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Datums, "datums"), "SetCollectionObjectNameRefListArg"),
            new("Collections", WorkerMpValueKind.StringList,
                StringListMapper.RequiredList(request.Collections, "collections"), "SetStringRefListArg")
        ], []);
    }

    public static Api.ShowItemsInTreeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
