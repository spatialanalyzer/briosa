using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetOpcDaTagValueIntegerOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_opc_da_tag_value_integer", "Set OPC DA Tag Value Integer", "briosa.UtilityOperations",
        "SetOpcDaTagValueInteger", "/briosa.UtilityOperations/SetOpcDaTagValueInteger", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetOpcDaTagValueIntegerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("OPC Server DA Tag Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.OpcServerDaTagName), "SetStringArg"),
            new("Value", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.Value), "SetIntegerArg")
        ], []);
    }

    public static Api.SetOpcDaTagValueIntegerResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
