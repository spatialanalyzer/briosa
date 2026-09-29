using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetMeasurementAuxiliaryDataOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_measurement_auxiliary_data", "Set Measurement Auxiliary Data",
        "briosa.AnalysisOperations", "SetMeasurementAuxiliaryData", "/briosa.AnalysisOperations/SetMeasurementAuxiliaryData",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetMeasurementAuxiliaryDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Point Name", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
                new("Auxiliary Name", WorkerMpValueKind.Text, new WorkerTextValue(request.AuxiliaryName), "SetStringArg"),
                new("Value", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Value), "SetDoubleArg"),
                new("Units", WorkerMpValueKind.Text, new WorkerTextValue(request.Units), "SetStringArg")
            ], []);
    }

    public static Api.SetMeasurementAuxiliaryDataResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}
