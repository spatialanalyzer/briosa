using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ScaleBarOperations;

internal sealed class ScaleBarOperationsService(OperationExecutor executor)
    : Api.ScaleBarOperations.ScaleBarOperationsBase
{
    [OperationImplementation("scale_bar_operations.delete_scale_bar")]
    public override Task<Api.DeleteScaleBarResult> DeleteScaleBar(
        Api.DeleteScaleBarRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteScaleBarRequest, Api.DeleteScaleBarResult>(
            executor,
            request,
            context,
            "scale_bar_operations.delete_scale_bar");

    [OperationImplementation("scale_bar_operations.get_scale_bar_stats")]
    public override Task<Api.GetScaleBarStatsResult> GetScaleBarStats(
        Api.GetScaleBarStatsRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetScaleBarStatsRequest, Api.GetScaleBarStatsResult>(
            executor,
            request,
            context,
            "scale_bar_operations.get_scale_bar_stats");

    [OperationImplementation("scale_bar_operations.scale_bar_check")]
    public override Task<Api.ScaleBarCheckResult> ScaleBarCheck(
        Api.ScaleBarCheckRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ScaleBarCheckRequest, Api.ScaleBarCheckResult>(
            executor,
            request,
            context,
            "scale_bar_operations.scale_bar_check");

    [OperationImplementation("scale_bar_operations.set_inward_positive_normal")]
    public override Task<Api.SetInwardPositiveNormalResult> SetInwardPositiveNormal(
        Api.SetInwardPositiveNormalRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetInwardPositiveNormalRequest, Api.SetInwardPositiveNormalResult>(
            executor,
            request,
            context,
            "scale_bar_operations.set_inward_positive_normal");

}
