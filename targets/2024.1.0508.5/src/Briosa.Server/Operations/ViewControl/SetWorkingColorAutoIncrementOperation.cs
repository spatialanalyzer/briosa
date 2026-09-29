using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ViewControl;

internal static class SetWorkingColorAutoIncrementOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "view_control.set_working_color_auto_increment", "Set Working Color Auto Increment", "briosa.ViewControl",
        "SetWorkingColorAutoIncrement", "/briosa.ViewControl/SetWorkingColorAutoIncrement", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetWorkingColorAutoIncrementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [new("Auto Increment", WorkerMpValueKind.Logical,
            new WorkerBooleanValue(request.AutoIncrement), "SetBoolArg")], []);
    }

    public static Api.SetWorkingColorAutoIncrementResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
