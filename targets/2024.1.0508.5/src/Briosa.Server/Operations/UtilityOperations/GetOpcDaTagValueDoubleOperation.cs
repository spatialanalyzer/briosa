using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetOpcDaTagValueDoubleOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_opc_da_tag_value_double", "Get OPC DA Tag Value Double", "briosa.UtilityOperations",
        "GetOpcDaTagValueDouble", "/briosa.UtilityOperations/GetOpcDaTagValueDouble", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("value", "Value", WorkerMpValueKind.FloatingPoint)];

    public static WorkerMpCommand CreateCommand(Api.GetOpcDaTagValueDoubleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("OPC Server DA Tag Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.OpcServerDaTagName), "SetStringArg")],
            [new("Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")]);
    }

    public static Api.GetOpcDaTagValueDoubleResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Value = completed.Execution.OutputValues[0].RequireValue<WorkerDoubleValue>().Value,
        Execution = completed.Details
    };
}
