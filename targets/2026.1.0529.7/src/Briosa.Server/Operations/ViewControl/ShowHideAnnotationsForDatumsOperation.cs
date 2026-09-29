using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class ShowHideAnnotationsForDatumsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.show_hide_annotations_for_datums", "Show/Hide Annotations for Datums", "briosa.ViewControl",
        "ShowHideAnnotationsForDatums", "/briosa.ViewControl/ShowHideAnnotationsForDatums", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ShowHideAnnotationsForDatumsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Datum Name List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.DatumNameList, "datum_name_list"), "SetCollectionObjectNameRefListArg"),
            new("Show?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Show), "SetBoolArg"),
            new("Highlight?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.Highlight), "SetBoolArg"),
            new("Set Inspection View?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.SetInspectionView), "SetBoolArg")
        ], []);
    }

    public static Api.ShowHideAnnotationsForDatumsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
