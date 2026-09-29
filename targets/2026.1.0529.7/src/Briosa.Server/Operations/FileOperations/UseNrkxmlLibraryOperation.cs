using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class UseNrkxmlLibraryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.use_nrkxml_library", "Use NRKXML Library", "briosa.FileOperations",
        "UseNrkxmlLibrary", "/briosa.FileOperations/UseNrkxmlLibrary", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.UseNrkxmlLibraryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Use library?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasUseLibrary || request.UseLibrary), "SetBoolArg")], []);
    }

    public static Api.UseNrkxmlLibraryResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
