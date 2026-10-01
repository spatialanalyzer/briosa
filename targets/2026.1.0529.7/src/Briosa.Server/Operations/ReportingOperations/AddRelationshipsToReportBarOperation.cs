using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class AddRelationshipsToReportBarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.add_relationships_to_report_bar", "Add Relationships to Report Bar", "briosa.ReportingOperations",
        "AddRelationshipsToReportBar", "/briosa.ReportingOperations/AddRelationshipsToReportBar", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddRelationshipsToReportBarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Relationship(s)", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.Relationships, "relationships"), "SetCollectionObjectNameRefListArg"),
            new("Clear Existing?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ClearExisting), "SetBoolArg")
        ], []);
    }

    public static Api.AddRelationshipsToReportBarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
