using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal sealed class AnalysisOperationsService(OperationExecutor operationExecutor) :
    Api.AnalysisOperations.AnalysisOperationsBase
{
    private readonly OperationExecutor _operationExecutor =
        operationExecutor ?? throw new ArgumentNullException(nameof(operationExecutor));

    [OperationImplementation(GetIThCollectionNameOperation.OperationId)]
    public override Task<Api.GetIThCollectionNameResult> GetIThCollectionName(
        Api.GetIThCollectionNameRequest request,
        ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(
            request,
            context,
            GetIThCollectionNameOperation.Descriptor,
            GetIThCollectionNameOperation.CreateCommand,
            GetIThCollectionNameOperation.OutputContracts,
            GetIThCollectionNameOperation.CreateResult);

    internal Task<Api.GetIThCollectionNameResult> ExecuteGetIThCollectionName(
        Api.GetIThCollectionNameRequest request,
        CancellationToken cancellationToken,
        DateTime? deadline = null,
        Guid? correlationId = null,
        string actorCategory = "internal-unattributed") =>
        _operationExecutor.ExecuteAsync(
            request,
            GetIThCollectionNameOperation.Descriptor,
            GetIThCollectionNameOperation.CreateCommand,
            GetIThCollectionNameOperation.OutputContracts,
            GetIThCollectionNameOperation.CreateResult,
            cancellationToken,
            deadline,
            correlationId,
            actorCategory);
}
