using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class GetIthCalloutPositionInCalloutViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.get_ith_callout_position_in_callout_view", "Get I-th Callout Position in Callout View",
        "briosa.ConstructionOperations", "GetIthCalloutPositionInCalloutView", "/briosa.ConstructionOperations/GetIthCalloutPositionInCalloutView",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("x_position", "X Position", WorkerMpValueKind.WholeNumber),
        new("y_position", "Y Position", WorkerMpValueKind.WholeNumber),
        new("x_anchor_position", "X Anchor Position", WorkerMpValueKind.WholeNumber),
        new("y_anchor_position", "Y Anchor Position", WorkerMpValueKind.WholeNumber),
        new("callout_width", "Callout Width", WorkerMpValueKind.WholeNumber),
        new("callout_height", "Callout Height", WorkerMpValueKind.WholeNumber)
    ];
    public static WorkerMpCommand CreateCommand(Api.GetIthCalloutPositionInCalloutViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.CalloutView, "callout_view", WorkerItemTypeValue.CalloutView), "SetCollectionObjectNameArg2"),
            new("Callout View Index", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.CalloutViewIndex), "SetIntegerArg")
        ],
        [
            new("X Position", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Y Position", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("X Anchor Position", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Y Anchor Position", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Callout Width", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Callout Height", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
        ]);
    }
    public static Api.GetIthCalloutPositionInCalloutViewResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        XPosition = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        YPosition = completed.Execution.OutputValues[1].RequireValue<WorkerIntegerValue>().Value,
        XAnchorPosition = completed.Execution.OutputValues[2].RequireValue<WorkerIntegerValue>().Value,
        YAnchorPosition = completed.Execution.OutputValues[3].RequireValue<WorkerIntegerValue>().Value,
        CalloutWidth = completed.Execution.OutputValues[4].RequireValue<WorkerIntegerValue>().Value,
        CalloutHeight = completed.Execution.OutputValues[5].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
