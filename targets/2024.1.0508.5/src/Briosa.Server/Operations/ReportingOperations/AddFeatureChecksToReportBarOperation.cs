using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class AddFeatureChecksToReportBarOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.add_feature_checks_to_report_bar", "Add Feature Checks to Report Bar", "briosa.ReportingOperations",
        "AddFeatureChecksToReportBar", "/briosa.ReportingOperations/AddFeatureChecksToReportBar", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.AddFeatureChecksToReportBarRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Checks", WorkerMpValueKind.CollectionItemNameList,
                CollectionItemNameMapper.RequiredList(request.FeatureChecks, "feature_checks"), "SetCollectionObjectNameRefListArg"),
            new("Clear Existing?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ClearExisting), "SetBoolArg")
        ], []);
    }

    public static Api.AddFeatureChecksToReportBarResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
