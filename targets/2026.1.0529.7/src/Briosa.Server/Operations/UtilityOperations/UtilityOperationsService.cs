using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.UtilityOperations;

internal sealed class UtilityOperationsService(OperationExecutor operationExecutor) :
    Api.UtilityOperations.UtilityOperationsBase
{
    private readonly OperationExecutor _operationExecutor =
        operationExecutor ?? throw new ArgumentNullException(nameof(operationExecutor));

    [OperationImplementation(GetActiveUnitsOperation.OperationId)]
    public override Task<Api.GetActiveUnitsResult> GetActiveUnits(
        Api.GetActiveUnitsRequest request,
        ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(
            request,
            context,
            GetActiveUnitsOperation.Descriptor,
            GetActiveUnitsOperation.CreateCommand,
            GetActiveUnitsOperation.OutputContracts,
            GetActiveUnitsOperation.CreateResult);

    [OperationImplementation(GetWorkingFramePropertiesOperation.OperationId)]
    public override Task<Api.GetWorkingFramePropertiesResult> GetWorkingFrameProperties(
        Api.GetWorkingFramePropertiesRequest request,
        ServerCallContext context) =>
        _operationExecutor.ExecuteAsync(
            request,
            context,
            GetWorkingFramePropertiesOperation.Descriptor,
            GetWorkingFramePropertiesOperation.CreateCommand,
            GetWorkingFramePropertiesOperation.OutputContracts,
            GetWorkingFramePropertiesOperation.CreateResult);
}
