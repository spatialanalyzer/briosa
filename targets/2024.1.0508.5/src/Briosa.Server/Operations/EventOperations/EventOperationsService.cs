using Briosa.Server.Operations.WaveA;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.EventOperations;

internal sealed class EventOperationsService(OperationExecutor executor)
    : Api.EventOperations.EventOperationsBase
{
    [OperationImplementation("event_operations.delete_event")]
    public override Task<Api.DeleteEventResult> DeleteEvent(
        Api.DeleteEventRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.DeleteEventRequest, Api.DeleteEventResult>(
            executor,
            request,
            context,
            "event_operations.delete_event");

    [OperationImplementation("event_operations.export_event_ref_list")]
    public override Task<Api.ExportEventRefListResult> ExportEventRefList(
        Api.ExportEventRefListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.ExportEventRefListRequest, Api.ExportEventRefListResult>(
            executor,
            request,
            context,
            "event_operations.export_event_ref_list");

    [OperationImplementation("event_operations.get_ith_event_from_event_ref_list")]
    public override Task<Api.GetIthEventFromEventRefListResult> GetIthEventFromEventRefList(
        Api.GetIthEventFromEventRefListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetIthEventFromEventRefListRequest, Api.GetIthEventFromEventRefListResult>(
            executor,
            request,
            context,
            "event_operations.get_ith_event_from_event_ref_list");

    [OperationImplementation("event_operations.get_number_of_events_in_event_ref_list")]
    public override Task<Api.GetNumberOfEventsInEventRefListResult> GetNumberOfEventsInEventRefList(
        Api.GetNumberOfEventsInEventRefListRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.GetNumberOfEventsInEventRefListRequest, Api.GetNumberOfEventsInEventRefListResult>(
            executor,
            request,
            context,
            "event_operations.get_number_of_events_in_event_ref_list");

    [OperationImplementation("event_operations.rename_event")]
    public override Task<Api.RenameEventResult> RenameEvent(
        Api.RenameEventRequest request,
        ServerCallContext context) =>
        MpOperationServiceExecutor.ExecuteAsync<Api.RenameEventRequest, Api.RenameEventResult>(
            executor,
            request,
            context,
            "event_operations.rename_event");

}
