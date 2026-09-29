using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal static class GetPointNotesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "utility_operations.get_point_notes", "Get Point Notes", "briosa.UtilityOperations",
        "GetPointNotes", "/briosa.UtilityOperations/GetPointNotes", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("notes", "Notes", WorkerMpValueKind.EditText)];

    public static WorkerMpCommand CreateCommand(Api.GetPointNotesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.Point, "point"), "SetPointNameArg")],
            [new("Notes", WorkerMpValueKind.EditText, "GetEditTextArg")]);
    }

    public static Api.GetPointNotesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetPointNotesResult { Execution = completed.Details };
        result.Notes.AddRange(completed.Execution.OutputValues[0].RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}
