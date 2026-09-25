using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class SetReportItemsReferenceListVariableOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.set_report_items_reference_list_variable", "Set Report Items Reference List Variable",
        "briosa.Variables", "SetReportItemsReferenceListVariable", "/briosa.Variables/SetReportItemsReferenceListVariable",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetReportItemsReferenceListVariableRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Name", WorkerMpValueKind.Text, new WorkerTextValue(request.Name), "SetStringArg"),
             new("Value", WorkerMpValueKind.CollectionItemNameList, CollectionItemNameMapper.RequiredList(request.Value, "value"), "SetCollectionObjectNameRefListArg")], []);
    }

    public static Api.SetReportItemsReferenceListVariableResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
