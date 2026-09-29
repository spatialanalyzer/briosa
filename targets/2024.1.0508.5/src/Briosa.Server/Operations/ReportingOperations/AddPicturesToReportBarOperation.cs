using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class AddPicturesToReportBarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.add_pictures_to_report_bar", "Add Pictures to Report Bar", "briosa.ReportingOperations",
        "AddPicturesToReportBar", "/briosa.ReportingOperations/AddPicturesToReportBar", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddPicturesToReportBarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Picture(s)", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.Pictures, "pictures"), "SetCollectionObjectNameRefListArg"),
            new("Clear Existing?", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.ClearExisting), "SetBoolArg")
        ], []);
    }

    public static Api.AddPicturesToReportBarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
