using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class GetGdtExtendedOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.get_gdt_extended_options", "Get GD&T Extended Options",
        "briosa.GdtOperations", "GetGdtExtendedOptions", "/briosa.GdtOperations/GetGdtExtendedOptions",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [new("use_extended_options", "Use Extended Options", WorkerMpValueKind.Logical)];

    public static WorkerMpCommand CreateCommand(Api.GetGdtExtendedOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [],
            [new("Use Extended Options", WorkerMpValueKind.Logical, "GetBoolArg")]);
    }

    public static Api.GetGdtExtendedOptionsResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        UseExtendedOptions = completed.Execution.OutputValues[0].RequireValue<WorkerBooleanValue>().Value,
        Execution = completed.Details
    };
}
