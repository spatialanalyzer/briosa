using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetAngularRepresentationOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_angular_representation", "Get Angular Representation", "briosa.UtilityOperations",
        "GetAngularRepresentation", "/briosa.UtilityOperations/GetAngularRepresentation", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value_0_360", "0-360, (FALSE = +/-180)", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.GetAngularRepresentationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
            [new("0-360, (FALSE = +/-180)", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.GetAngularRepresentationResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Value0360 = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        Execution = completed.Details
    };
}
