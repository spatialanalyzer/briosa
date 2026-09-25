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
        executor.ExecuteAsync(request, context, AddTaskOverviewItemOperation.Descriptor,
            AddTaskOverviewItemOperation.CreateCommand, AddTaskOverviewItemOperation.OutputContracts,
            AddTaskOverviewItemOperation.CreateResult);

    [OperationImplementation("mp_task_overview.create_clear_task_overview_list")]
    public override Task<Api.CreateClearTaskOverviewListResult> CreateClearTaskOverviewList(
        Api.CreateClearTaskOverviewListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, CreateClearTaskOverviewListOperation.Descriptor,
            CreateClearTaskOverviewListOperation.CreateCommand, CreateClearTaskOverviewListOperation.OutputContracts,
            CreateClearTaskOverviewListOperation.CreateResult);

    [OperationImplementation("mp_task_overview.set_current_task")]
    public override Task<Api.SetCurrentTaskResult> SetCurrentTask(
        Api.SetCurrentTaskRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetCurrentTaskOperation.Descriptor,
            SetCurrentTaskOperation.CreateCommand, SetCurrentTaskOperation.OutputContracts,
            SetCurrentTaskOperation.CreateResult);

    [OperationImplementation("mp_task_overview.set_overview_image")]
    public override Task<Api.SetOverviewImageResult> SetOverviewImage(
        Api.SetOverviewImageRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetOverviewImageOperation.Descriptor,
            SetOverviewImageOperation.CreateCommand, SetOverviewImageOperation.OutputContracts,
            SetOverviewImageOperation.CreateResult);

    [OperationImplementation("mp_task_overview.set_overview_title")]
    public override Task<Api.SetOverviewTitleResult> SetOverviewTitle(
        Api.SetOverviewTitleRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetOverviewTitleOperation.Descriptor,
            SetOverviewTitleOperation.CreateCommand, SetOverviewTitleOperation.OutputContracts,
            SetOverviewTitleOperation.CreateResult);

    [OperationImplementation("mp_task_overview.set_task_item_comment")]
    public override Task<Api.SetTaskItemCommentResult> SetTaskItemComment(
        Api.SetTaskItemCommentRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTaskItemCommentOperation.Descriptor,
            SetTaskItemCommentOperation.CreateCommand, SetTaskItemCommentOperation.OutputContracts,
            SetTaskItemCommentOperation.CreateResult);

    [OperationImplementation("mp_task_overview.set_task_item_completion_values")]
    public override Task<Api.SetTaskItemCompletionValuesResult> SetTaskItemCompletionValues(
        Api.SetTaskItemCompletionValuesRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTaskItemCompletionValuesOperation.Descriptor,
            SetTaskItemCompletionValuesOperation.CreateCommand, SetTaskItemCompletionValuesOperation.OutputContracts,
            SetTaskItemCompletionValuesOperation.CreateResult);

    [OperationImplementation("mp_task_overview.set_task_item_name")]
    public override Task<Api.SetTaskItemNameResult> SetTaskItemName(
        Api.SetTaskItemNameRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetTaskItemNameOperation.Descriptor,
            SetTaskItemNameOperation.CreateCommand, SetTaskItemNameOperation.OutputContracts,
            SetTaskItemNameOperation.CreateResult);

    [OperationImplementation("mp_task_overview.show_progress_for_task_item")]
    public override Task<Api.ShowProgressForTaskItemResult> ShowProgressForTaskItem(
        Api.ShowProgressForTaskItemRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowProgressForTaskItemOperation.Descriptor,
            ShowProgressForTaskItemOperation.CreateCommand, ShowProgressForTaskItemOperation.OutputContracts,
            ShowProgressForTaskItemOperation.CreateResult);

    [OperationImplementation("mp_task_overview.show_task_overview_list")]
    public override Task<Api.ShowTaskOverviewListResult> ShowTaskOverviewList(
        Api.ShowTaskOverviewListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ShowTaskOverviewListOperation.Descriptor,
            ShowTaskOverviewListOperation.CreateCommand, ShowTaskOverviewListOperation.OutputContracts,
            ShowTaskOverviewListOperation.CreateResult);

}
