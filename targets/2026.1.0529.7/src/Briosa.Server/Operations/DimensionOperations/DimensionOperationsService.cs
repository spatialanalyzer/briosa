using Briosa.Server.Operations.WaveA;
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
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteDimensionRequest, Api.DeleteDimensionResult>(
            executor,
            request,
            context,
            "dimension_operations.delete_dimension");

    [OperationImplementation("dimension_operations.get_dimension_value")]
    public override Task<Api.GetDimensionValueResult> GetDimensionValue(
        Api.GetDimensionValueRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetDimensionValueRequest, Api.GetDimensionValueResult>(
            executor,
            request,
            context,
            "dimension_operations.get_dimension_value");

    [OperationImplementation("dimension_operations.set_dimension_tolerance")]
    public override Task<Api.SetDimensionToleranceResult> SetDimensionTolerance(
        Api.SetDimensionToleranceRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetDimensionToleranceRequest, Api.SetDimensionToleranceResult>(
            executor,
            request,
            context,
            "dimension_operations.set_dimension_tolerance");

}
