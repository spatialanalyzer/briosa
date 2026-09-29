using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class GetNumberOfCalloutsInCalloutViewOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.get_number_of_callouts_in_callout_view", "Get Number of Callouts in Callout View",
        "briosa.ConstructionOperations", "GetNumberOfCalloutsInCalloutView", "/briosa.ConstructionOperations/GetNumberOfCalloutsInCalloutView",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("callouts_count", "Callouts Count", WorkerMpValueKind.WholeNumber)];
    public static WorkerMpCommand CreateCommand(Api.GetNumberOfCalloutsInCalloutViewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Callout View", WorkerMpValueKind.CollectionItemName,
                CollectionItemNameMapper.Required(request.CalloutView, "callout_view", WorkerItemTypeValue.CalloutView), "SetCollectionObjectNameArg2")],
            [new("Callouts Count", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }
    public static Api.GetNumberOfCalloutsInCalloutViewResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        CalloutsCount = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
