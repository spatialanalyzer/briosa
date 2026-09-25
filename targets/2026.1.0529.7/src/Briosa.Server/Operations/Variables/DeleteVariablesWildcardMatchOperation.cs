using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.Variables;

internal static class DeleteVariablesWildcardMatchOperation
{
    // The documentation title uses one hyphen, but this target's View SDK Code
    // uses two. A licensed 2026 probe rejected the documentation spelling (MP -1).
    public static OperationDescriptor Descriptor { get; } = new(
        "variables.delete_variables_wildcard_match", "Delete Variables -- Wildcard Match",
        "briosa.Variables", "DeleteVariablesWildcardMatch", "/briosa.Variables/DeleteVariablesWildcardMatch",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteVariablesWildcardMatchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Variable Wildcard Criteria", WorkerMpValueKind.Text, new WorkerTextValue(request.VariableWildcardCriteria), "SetStringArg")], []);
    }

    public static Api.DeleteVariablesWildcardMatchResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
