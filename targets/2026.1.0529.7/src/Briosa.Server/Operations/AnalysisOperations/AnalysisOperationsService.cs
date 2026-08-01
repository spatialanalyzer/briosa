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

    [OperationImplementation(GetNumberOfCollectionsOperation.OperationId)]
    public override Task<Api.GetNumberOfCollectionsResult> GetNumberOfCollections(
        Api.GetNumberOfCollectionsRequest request,
        ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(
            request,
            context,
            GetNumberOfCollectionsOperation.Descriptor,
            GetNumberOfCollectionsOperation.CreateCommand,
            GetNumberOfCollectionsOperation.OutputContracts,
            GetNumberOfCollectionsOperation.CreateResult);
}
