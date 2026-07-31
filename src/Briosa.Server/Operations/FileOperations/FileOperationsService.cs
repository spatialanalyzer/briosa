using Briosa.Server.Services;
using Grpc.Core;
using TargetProtocol = Briosa.Sa.V2026_1_0529_7.V1Alpha1;

namespace Briosa.Server.Operations.FileOperations;

internal sealed class FileOperationsService(OperationExecutor operationExecutor) :
    TargetProtocol.FileOperations.FileOperationsBase
{
    private readonly OperationExecutor _operationExecutor =
        operationExecutor ?? throw new ArgumentNullException(nameof(operationExecutor));

    [OperationImplementation(GetWorkingDirectoryOperation.OperationId)]
    public override Task<TargetProtocol.GetWorkingDirectoryResult> GetWorkingDirectory(
        TargetProtocol.GetWorkingDirectoryRequest request,
        ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(
            request,
            context,
            GetWorkingDirectoryOperation.Descriptor,
            GetWorkingDirectoryOperation.CreateCommand,
            GetWorkingDirectoryOperation.OutputContracts,
            GetWorkingDirectoryOperation.CreateResult);

    internal Task<TargetProtocol.GetWorkingDirectoryResult> ExecuteGetWorkingDirectory(
        TargetProtocol.GetWorkingDirectoryRequest request,
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
