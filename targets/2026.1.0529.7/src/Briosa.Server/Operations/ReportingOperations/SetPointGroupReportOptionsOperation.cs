using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class SetPointGroupReportOptionsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.set_point_group_report_options", "Set Point Group Report Options", "briosa.ReportingOperations",
        "SetPointGroupReportOptions", "/briosa.ReportingOperations/SetPointGroupReportOptions", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetPointGroupReportOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var coordinateSystem = CoordinateSystemTypeMapper.OrDefault(
            request.HasCoordinateSystem ? request.CoordinateSystem : null,
            Api.CoordinateSystemType.Cartesian);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Group", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PointGroup, "point_group"), "SetCollectionObjectNameArg2"),
            new("Coordinate System", WorkerMpValueKind.CoordinateSystemType,
                new WorkerChoiceValue<WorkerCoordinateSystemTypeValue>(coordinateSystem), "SetCoordinateSystemTypeArg"),
            new("Show X Component", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasShowXComponent || request.ShowXComponent), "SetBoolArg"),
            new("Show Y Component", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasShowYComponent || request.ShowYComponent), "SetBoolArg"),
            new("Show Z Component", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasShowZComponent || request.ShowZComponent), "SetBoolArg"),
            new("Show Offsets", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasShowOffsets && request.ShowOffsets), "SetBoolArg"),
            new("Show Uncertainty", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasShowUncertainty || request.ShowUncertainty), "SetBoolArg"),
            new("Show Notes", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasShowNotes && request.ShowNotes), "SetBoolArg"),
            new("Show Measurements", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasShowMeasurements && request.ShowMeasurements), "SetBoolArg"),
            new("Show Measurement Details", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasShowMeasurementDetails && request.ShowMeasurementDetails), "SetBoolArg"),
            new("Show PointingError/Worst Angle", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasShowPointingErrorWorstAngle && request.ShowPointingErrorWorstAngle), "SetBoolArg"),
            new("Sort by Point Names", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(!request.HasSortByPointNames || request.SortByPointNames), "SetBoolArg"),
            new("Make Default", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasMakeDefault && request.MakeDefault), "SetBoolArg"),
            new("Apply to All", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasApplyToAll && request.ApplyToAll), "SetBoolArg")
        ], []);
    }

    public static Api.SetPointGroupReportOptionsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
