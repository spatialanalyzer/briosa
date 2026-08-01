using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal sealed class ConstructionOperationsService(OperationExecutor operationExecutor) :
    Api.ConstructionOperations.ConstructionOperationsBase
{
    private readonly OperationExecutor _operationExecutor =
        operationExecutor ?? throw new ArgumentNullException(nameof(operationExecutor));

    [OperationImplementation(GetActiveCollectionNameOperation.OperationId)]
    public override Task<Api.GetActiveCollectionNameResult> GetActiveCollectionName(
        Api.GetActiveCollectionNameRequest request,
        ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(
            request,
            context,
            GetActiveCollectionNameOperation.Descriptor,
            GetActiveCollectionNameOperation.CreateCommand,
            GetActiveCollectionNameOperation.OutputContracts,
            GetActiveCollectionNameOperation.CreateResult);
}
