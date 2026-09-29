using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetOpcDaTagValueStringOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_opc_da_tag_value_string", "Get OPC DA Tag Value String", "briosa.UtilityOperations",
        "GetOpcDaTagValueString", "/briosa.UtilityOperations/GetOpcDaTagValueString", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.Text)];

    public static WorkerMpCommand CreateCommand(Api.GetOpcDaTagValueStringRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("OPC Server DA Tag Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.OpcServerDaTagName), "SetStringArg")],
            [new("Value", WorkerMpValueKind.Text, "GetStringArg")]);
    }

    public static Api.GetOpcDaTagValueStringResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Value = completed.Execution.OutputValues[0].RequireValue<WorkerTextValue>().Value,
        Execution = completed.Details
    };
}
