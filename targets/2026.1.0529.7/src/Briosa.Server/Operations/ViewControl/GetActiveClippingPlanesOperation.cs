using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class GetActiveClippingPlanesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.get_active_clipping_planes", "Get Active Clipping Planes", "briosa.ViewControl",
        "GetActiveClippingPlanes", "/briosa.ViewControl/GetActiveClippingPlanes", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("objects", "Objects", WorkerMpValueKind.CollectionObjectNameList)];

    public static WorkerMpCommand CreateCommand(Api.GetActiveClippingPlanesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
            [new("Objects", WorkerMpValueKind.CollectionObjectNameList, "GetCollectionObjectNameRefListArg")]);
    }

    public static Api.GetActiveClippingPlanesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetActiveClippingPlanesResult { Execution = completed.Details };
        foreach (var value in completed.Execution.OutputValues[0]
                     .RequireValue<WorkerCollectionObjectNameListValue>().Values)
        {
            result.Objects.Add(CollectionObjectNameMapper.ToProtocol(value));
        }

        return result;
    }
}
