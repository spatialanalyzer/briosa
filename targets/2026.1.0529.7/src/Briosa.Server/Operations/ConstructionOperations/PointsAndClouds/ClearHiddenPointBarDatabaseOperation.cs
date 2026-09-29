using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ClearHiddenPointBarDatabaseOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.clear_hidden_point_bar_database", "Clear Hidden Point Bar Database",
        "briosa.ConstructionOperations", "ClearHiddenPointBarDatabase", "/briosa.ConstructionOperations/ClearHiddenPointBarDatabase",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ClearHiddenPointBarDatabaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep, [], []);
    }

    public static Api.ClearHiddenPointBarDatabaseResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
