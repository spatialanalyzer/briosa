using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.DimensionOperations;

internal sealed class DimensionOperationsService(OperationExecutor executor)
    : Api.DimensionOperations.DimensionOperationsBase
{
    [OperationImplementation("dimension_operations.delete_dimension")]
    public override Task<Api.DeleteDimensionResult> DeleteDimension(
        Api.DeleteDimensionRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteDimensionOperation.Descriptor,
            DeleteDimensionOperation.CreateCommand, DeleteDimensionOperation.OutputContracts,
            DeleteDimensionOperation.CreateResult);

    [OperationImplementation("dimension_operations.get_dimension_value")]
    public override Task<Api.GetDimensionValueResult> GetDimensionValue(
        Api.GetDimensionValueRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetDimensionValueOperation.Descriptor,
            GetDimensionValueOperation.CreateCommand, GetDimensionValueOperation.OutputContracts,
            GetDimensionValueOperation.CreateResult);

    [OperationImplementation("dimension_operations.set_dimension_tolerance")]
    public override Task<Api.SetDimensionToleranceResult> SetDimensionTolerance(
        Api.SetDimensionToleranceRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetDimensionToleranceOperation.Descriptor,
            SetDimensionToleranceOperation.CreateCommand, SetDimensionToleranceOperation.OutputContracts,
            SetDimensionToleranceOperation.CreateResult);

}
