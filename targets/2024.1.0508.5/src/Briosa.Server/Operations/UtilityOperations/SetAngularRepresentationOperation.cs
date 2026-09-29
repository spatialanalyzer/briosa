using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetAngularRepresentationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_angular_representation", "Set Angular Representation", "briosa.UtilityOperations",
        "SetAngularRepresentation", "/briosa.UtilityOperations/SetAngularRepresentation", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetAngularRepresentationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("0-360, (FALSE = +/-180)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.Value0360), "SetBoolArg")], []);
    }

    public static Api.SetAngularRepresentationResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
