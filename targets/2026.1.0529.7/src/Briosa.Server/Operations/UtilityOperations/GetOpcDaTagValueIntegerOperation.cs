using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetOpcDaTagValueIntegerOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_opc_da_tag_value_integer", "Get OPC DA Tag Value Integer", "briosa.UtilityOperations",
        "GetOpcDaTagValueInteger", "/briosa.UtilityOperations/GetOpcDaTagValueInteger", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.WholeNumber)];

    public static WorkerMpCommand CreateCommand(Api.GetOpcDaTagValueIntegerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("OPC Server DA Tag Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.OpcServerDaTagName), "SetStringArg")],
            [new("Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetOpcDaTagValueIntegerResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Value = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        Execution = completed.Details
    };
}
