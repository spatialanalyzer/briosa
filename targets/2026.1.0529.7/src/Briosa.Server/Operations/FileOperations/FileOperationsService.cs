using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal sealed class FileOperationsService(OperationExecutor operationExecutor) :
    Api.FileOperations.FileOperationsBase
{
    private readonly OperationExecutor _operationExecutor =
        operationExecutor ?? throw new ArgumentNullException(nameof(operationExecutor));

    [OperationImplementation(GetWorkingDirectoryOperation.OperationId)]
    public override Task<Api.GetWorkingDirectoryResult> GetWorkingDirectory(
        Api.GetWorkingDirectoryRequest request,
        ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(
            request,
            context,
            GetWorkingDirectoryOperation.Descriptor,
            GetWorkingDirectoryOperation.CreateCommand,
            GetWorkingDirectoryOperation.OutputContracts,
            GetWorkingDirectoryOperation.CreateResult);

    internal Task<Api.GetWorkingDirectoryResult> ExecuteGetWorkingDirectory(
        Api.GetWorkingDirectoryRequest request,
        CancellationToken cancellationToken,
        DateTime? deadline = null,
        Guid? correlationId = null,
        string actorCategory = "internal-unattributed") =>
        _operationExecutor.ExecuteAsync(
            request,
            GetWorkingDirectoryOperation.Descriptor,
            GetWorkingDirectoryOperation.CreateCommand,
            GetWorkingDirectoryOperation.OutputContracts,
            GetWorkingDirectoryOperation.CreateResult,
            cancellationToken,
            deadline,
            correlationId,
            actorCategory);
}
