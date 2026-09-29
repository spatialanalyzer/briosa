using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class ConstructMeasuredPointUncertaintyEllipsoidsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.construct_measured_point_uncertainty_ellipsoids",
        "Construct Measured Point Uncertainty Ellipsoids", "briosa.InstrumentOperations",
        "ConstructMeasuredPointUncertaintyEllipsoids",
        "/briosa.InstrumentOperations/ConstructMeasuredPointUncertaintyEllipsoids",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructMeasuredPointUncertaintyEllipsoidsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Measurements", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.Measurements, "measurements"), "SetPointNameRefListArg")], []);
    }

    public static Api.ConstructMeasuredPointUncertaintyEllipsoidsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
