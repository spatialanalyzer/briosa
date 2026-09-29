using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpSubroutines;

internal static class RunSubroutineOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "mp_subroutines.run_subroutine", "Run Subroutine",
        "briosa.MpSubroutines", "RunSubroutine", "/briosa.MpSubroutines/RunSubroutine",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe,
        ["fixture_validation_pending"]);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RunSubroutineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("MP Subroutine File Path", WorkerMpValueKind.FileReference,
                FileReferenceMapper.Required(request.MpSubroutineFilePath, "mp_subroutine_file_path"), "SetFilePathArg"),
            new("Share Parent Variables?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShareParentVariables), "SetBoolArg")
        ], []);
    }

    public static Api.RunSubroutineResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
