using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.MpTaskOverview;

internal sealed class MpTaskOverviewService(OperationExecutor executor)
    : Api.MpTaskOverview.MpTaskOverviewBase
{
    [OperationImplementation("mp_task_overview.add_task_overview_item")]
    public override Task<Api.AddTaskOverviewItemResult> AddTaskOverviewItem(
        Api.AddTaskOverviewItemRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.AddTaskOverviewItemRequest, Api.AddTaskOverviewItemResult>(
            executor,
            request,
            context,
            "mp_task_overview.add_task_overview_item");

    [OperationImplementation("mp_task_overview.create_clear_task_overview_list")]
    public override Task<Api.CreateClearTaskOverviewListResult> CreateClearTaskOverviewList(
        Api.CreateClearTaskOverviewListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.CreateClearTaskOverviewListRequest, Api.CreateClearTaskOverviewListResult>(
            executor,
            request,
            context,
            "mp_task_overview.create_clear_task_overview_list");

    [OperationImplementation("mp_task_overview.set_current_task")]
    public override Task<Api.SetCurrentTaskResult> SetCurrentTask(
        Api.SetCurrentTaskRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetCurrentTaskRequest, Api.SetCurrentTaskResult>(
            executor,
            request,
            context,
            "mp_task_overview.set_current_task");

    [OperationImplementation("mp_task_overview.set_overview_image")]
    public override Task<Api.SetOverviewImageResult> SetOverviewImage(
        Api.SetOverviewImageRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetOverviewImageRequest, Api.SetOverviewImageResult>(
            executor,
            request,
            context,
            "mp_task_overview.set_overview_image");

    [OperationImplementation("mp_task_overview.set_overview_title")]
    public override Task<Api.SetOverviewTitleResult> SetOverviewTitle(
        Api.SetOverviewTitleRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetOverviewTitleRequest, Api.SetOverviewTitleResult>(
            executor,
            request,
            context,
            "mp_task_overview.set_overview_title");

    [OperationImplementation("mp_task_overview.set_task_item_comment")]
    public override Task<Api.SetTaskItemCommentResult> SetTaskItemComment(
        Api.SetTaskItemCommentRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTaskItemCommentRequest, Api.SetTaskItemCommentResult>(
            executor,
            request,
            context,
            "mp_task_overview.set_task_item_comment");

    [OperationImplementation("mp_task_overview.set_task_item_completion_values")]
    public override Task<Api.SetTaskItemCompletionValuesResult> SetTaskItemCompletionValues(
        Api.SetTaskItemCompletionValuesRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTaskItemCompletionValuesRequest, Api.SetTaskItemCompletionValuesResult>(
            executor,
            request,
            context,
            "mp_task_overview.set_task_item_completion_values");

    [OperationImplementation("mp_task_overview.set_task_item_name")]
    public override Task<Api.SetTaskItemNameResult> SetTaskItemName(
        Api.SetTaskItemNameRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.SetTaskItemNameRequest, Api.SetTaskItemNameResult>(
            executor,
            request,
            context,
            "mp_task_overview.set_task_item_name");

    [OperationImplementation("mp_task_overview.show_progress_for_task_item")]
    public override Task<Api.ShowProgressForTaskItemResult> ShowProgressForTaskItem(
        Api.ShowProgressForTaskItemRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowProgressForTaskItemRequest, Api.ShowProgressForTaskItemResult>(
            executor,
            request,
            context,
            "mp_task_overview.show_progress_for_task_item");

    [OperationImplementation("mp_task_overview.show_task_overview_list")]
    public override Task<Api.ShowTaskOverviewListResult> ShowTaskOverviewList(
        Api.ShowTaskOverviewListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ShowTaskOverviewListRequest, Api.ShowTaskOverviewListResult>(
            executor,
            request,
            context,
            "mp_task_overview.show_task_overview_list");

}
