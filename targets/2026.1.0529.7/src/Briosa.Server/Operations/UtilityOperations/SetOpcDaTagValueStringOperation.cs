using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetOpcDaTagValueStringOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_opc_da_tag_value_string", "Set OPC DA Tag Value String", "briosa.UtilityOperations",
        "SetOpcDaTagValueString", "/briosa.UtilityOperations/SetOpcDaTagValueString", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetOpcDaTagValueStringRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("OPC Server DA Tag Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.OpcServerDaTagName), "SetStringArg"),
            new("Value", WorkerMpValueKind.Text, new WorkerTextValue(request.Value), "SetStringArg")
        ], []);
    }

    public static Api.SetOpcDaTagValueStringResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
