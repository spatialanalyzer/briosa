using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ScaleBarOperations;

internal static class SetInwardPositiveNormalOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "scale_bar_operations.set_inward_positive_normal", "Set Inward Positive Normal",
        "briosa.ScaleBarOperations", "SetInwardPositiveNormal", "/briosa.ScaleBarOperations/SetInwardPositiveNormal",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetInwardPositiveNormalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Object Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ObjectName, "object_name"), "SetCollectionObjectNameArg2"),
                new("Inward Positive?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasInwardPositive ? request.InwardPositive : true), "SetBoolArg")
            ], []);
    }

    public static Api.SetInwardPositiveNormalResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
