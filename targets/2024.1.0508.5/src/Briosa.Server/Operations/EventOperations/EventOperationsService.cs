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
        executor.ExecuteAsync(request, context, DeleteEventOperation.Descriptor,
            DeleteEventOperation.CreateCommand, DeleteEventOperation.OutputContracts, DeleteEventOperation.CreateResult);

    [OperationImplementation("event_operations.export_event_ref_list")]
    public override Task<Api.ExportEventRefListResult> ExportEventRefList(
        Api.ExportEventRefListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ExportEventRefListOperation.Descriptor,
            ExportEventRefListOperation.CreateCommand, ExportEventRefListOperation.OutputContracts, ExportEventRefListOperation.CreateResult);

    [OperationImplementation("event_operations.get_ith_event_from_event_ref_list")]
    public override Task<Api.GetIthEventFromEventRefListResult> GetIthEventFromEventRefList(
        Api.GetIthEventFromEventRefListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetIthEventFromEventRefListOperation.Descriptor,
            GetIthEventFromEventRefListOperation.CreateCommand, GetIthEventFromEventRefListOperation.OutputContracts,
            GetIthEventFromEventRefListOperation.CreateResult);

    [OperationImplementation("event_operations.get_number_of_events_in_event_ref_list")]
    public override Task<Api.GetNumberOfEventsInEventRefListResult> GetNumberOfEventsInEventRefList(
        Api.GetNumberOfEventsInEventRefListRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetNumberOfEventsInEventRefListOperation.Descriptor,
            GetNumberOfEventsInEventRefListOperation.CreateCommand, GetNumberOfEventsInEventRefListOperation.OutputContracts,
            GetNumberOfEventsInEventRefListOperation.CreateResult);

    [OperationImplementation("event_operations.rename_event")]
    public override Task<Api.RenameEventResult> RenameEvent(
        Api.RenameEventRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, RenameEventOperation.Descriptor,
            RenameEventOperation.CreateCommand, RenameEventOperation.OutputContracts, RenameEventOperation.CreateResult);

}
