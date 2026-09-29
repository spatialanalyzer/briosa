using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class SetPointNotesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.set_point_notes", "Set Point Notes", "briosa.UtilityOperations",
        "SetPointNotes", "/briosa.UtilityOperations/SetPointNotes", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointNotesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Notes.Count == 0)
            throw new ArgumentException("Notes are required.", nameof(request));
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg"),
            new("Notes", WorkerMpValueKind.EditText, new WorkerStringListValue(request.Notes), "SetEditTextArg"),
            new("Append? (FALSE = Overwrite)", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasAppend || request.Append), "SetBoolArg")
        ], []);
    }

    public static Api.SetPointNotesResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
